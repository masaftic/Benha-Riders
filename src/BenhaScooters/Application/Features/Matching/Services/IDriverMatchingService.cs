using BenhaScooters.Application.Abstractions;
using BenhaScooters.Application.Features.Matching.Settings;
using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.Matching.Events;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Matching.Services;
using BenhaScooters.Infrastructure.Notifications;
using ErrorOr;
using Hangfire;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Application.Features.Matching.Services;

public interface IDriverMatchingService
{
    Task<ErrorOr<Success>> ProcessMatchingAsync(MatchingSessionId matchingSessionId, CancellationToken cancellationToken = default);
    Task HandleRoundTimeoutAsync(MatchingSessionId matchingSessionId, CancellationToken cancellationToken = default);
}

public class DriverMatchingService : IDriverMatchingService
{
    private readonly AppDbContext _dbContext;
    private readonly IDriverRankingService _driverRanking;
    private readonly IGeoService _geoService;
    private readonly ILogger<DriverMatchingService> _logger;
    private readonly IPublisher _publisher;
    private readonly MatchingSessionOptions _settings;

    public DriverMatchingService(
        AppDbContext dbContext,
        IDriverRankingService driverRanking,
        IGeoService geoService,
        ILogger<DriverMatchingService> logger,
        IPublisher publisher,
        IOptions<MatchingSessionOptions> options)
    {
        _dbContext = dbContext;
        _driverRanking = driverRanking;
        _geoService = geoService;
        _logger = logger;
        _publisher = publisher;
        _settings = options.Value;
    }

    public async Task<ErrorOr<Success>> ProcessMatchingAsync(MatchingSessionId matchingSessionId, CancellationToken cancellationToken = default)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Get the matching session with trip request
            var matchingSession = await _dbContext.MatchingSessions
                .Include(ms => ms.MatchAttempts)
                .Include(ms => ms.TripRequest)
                    .ThenInclude(tr => tr.RiderProfile)
                        .ThenInclude(rp => rp.User)
                .FirstOrDefaultAsync(ms => ms.Id == matchingSessionId, cancellationToken);

            if (matchingSession == null)
            {
                return AppErrors.Matching.Session.NotFound();
            }

            if (!matchingSession.IsActive)
            {
                return AppErrors.Matching.Session.NotActive();
            }

            var tripRequest = matchingSession.TripRequest;

            _logger.LogInformation("Starting matching for session {MatchingSessionId} in round: {CurrentRound}",
                matchingSessionId, matchingSession.CurrentRound);

            var rankedDrivers = await _driverRanking.FindTopNDriversAsync(
                tripRequest.PickupLocation,
                matchingSession.OffersPerRound[matchingSession.CurrentRound - 1],
                roundNumber: matchingSession.CurrentRound - 1,
                excludedDrivers: matchingSession.GetRejectedOrPendingDrivers(),
                cancellationToken: cancellationToken);

            if (rankedDrivers.Count == 0)
            {
                _logger.LogInformation("No available drivers found for matching session {MatchingSessionId} in round {CurrentRound}",
                    matchingSessionId, matchingSession.CurrentRound);

                BackgroundJob.Enqueue<IMatchingOrchestrator>(
                    orchestrator => orchestrator.HandlePostOutcomeAsync(
                        tripRequest.Id,
                        cancellationToken));

                return Result.Success;
            }

            List<DriverMatchAttempt> matchAttempts = new List<DriverMatchAttempt>();

            foreach (var driver in rankedDrivers)
            {
                // Create match attempt for each driver
                var estimatedArrival = _geoService.EstimateArrivalTime(driver.DistanceToPickup);
                var attemptResult = matchingSession.CreateDriverMatchAttempt(
                    driver.DriverId,
                    driver.DistanceToPickup,
                    estimatedArrival,
                    driver.Score);

                if (attemptResult.IsError)
                {
                    _logger.LogWarning("Failed to create match attempt for driver {DriverId}: {Errors}",
                        driver.DriverId, string.Join(", ", attemptResult.Errors.Select(e => e.Description)));
                    continue;
                }

                matchAttempts.Add(attemptResult.Value);

                _logger.LogInformation("Created match attempt for driver {DriverId} in session {MatchingSessionId}",
                    driver.DriverId, matchingSessionId);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Schedule round timeout to check if we need to advance to the next round
            BackgroundJob.Schedule<IDriverMatchingService>(
                service => service.HandleRoundTimeoutAsync(
                    matchingSessionId,
                    cancellationToken),
                _settings.RoundTimeout);

            // Send notifications to drivers with full trip details
            var riderName = tripRequest.RiderProfile.PreferredName ?? tripRequest.RiderProfile.User.Name;

            foreach (var match in matchAttempts)
            {
                await _publisher.Publish(new DriverMatchOfferCreatedEvent(
                    match.Id,
                    tripRequest.Id,
                    match.DriverUserId,
                    riderName,
                    tripRequest.PickupLocation.ToCoordinate(),
                    tripRequest.DropoffLocation.ToCoordinate(),
                    tripRequest.PickupAddress,
                    tripRequest.DropoffAddress,
                    tripRequest.FinalFare.Amount,
                    tripRequest.FinalFare.Distance,
                    match.DistanceToPickup,
                    match.EstimatedArrivalTime,
                    match.CreatedAt), cancellationToken);
            }

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting matching for session {MatchingSessionId}", matchingSessionId);
            throw;
        }
    }


    public async Task HandleRoundTimeoutAsync(MatchingSessionId matchingSessionId, CancellationToken cancellationToken = default)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var matchingSession = await _dbContext.MatchingSessions
                .Include(ms => ms.MatchAttempts)
                .FirstOrDefaultAsync(ms => ms.Id == matchingSessionId, cancellationToken);

            if (matchingSession == null)
            {
                _logger.LogInformation("Matching session {MatchingSessionId} not found, skipping round timeout",
                    matchingSessionId);
                return;
            }

            if (!matchingSession.IsActive)
            {
                // If the session expired by time but was never formally cancelled, cancel it now
                if (matchingSession.IsExpired && matchingSession.Status == MatchingSessionStatus.Active)
                {
                    _logger.LogInformation("Matching session {MatchingSessionId} has expired, cancelling",
                        matchingSessionId);
                    matchingSession.Cancel(false, "انتهت مهلة جلسة المطابقة");
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                else
                {
                    _logger.LogInformation("Matching session {MatchingSessionId} is not active, skipping round timeout",
                        matchingSessionId);
                }
                return;
            }

            _logger.LogInformation("Handling round timeout for session {MatchingSessionId} in round {CurrentRound}",
                matchingSessionId, matchingSession.CurrentRound);

            // Check if anyone has accepted
            var hasAcceptedMatch = matchingSession.MatchAttempts.Any(ma => ma.Status == MatchAttemptStatus.Accepted);
            if (hasAcceptedMatch)
            {
                _logger.LogInformation("Match already accepted for session {MatchingSessionId}, no action needed",
                    matchingSessionId);
                return;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Let the orchestrator decide whether to advance to next round or cancel
            var timeoutDelay = matchingSession.IsLastRound()
                ? _settings.RoundTimeout
                : TimeSpan.Zero; // Immediately advance if not last round

            BackgroundJob.Schedule<IMatchingOrchestrator>(
                orchestrator => orchestrator.HandlePostOutcomeAsync(
                    matchingSession.TripRequestId,
                    cancellationToken),
                timeoutDelay);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling round timeout for session {MatchingSessionId}",
                matchingSessionId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
