using BenhaScooters.Application.Abstractions;
using BenhaScooters.Application.Features.Matching.Settings;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Matching.Services;
using BenhaScooters.Infrastructure.Notifications;
using ErrorOr;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Application.Features.Matching.Services;

public interface IDriverMatchingService
{
    Task<ErrorOr<Success>> ProcessMatchingAsync(MatchingSessionId matchingSessionId, CancellationToken cancellationToken = default);
    Task HandleMatchAttemptTimeoutAsync(MatchingSessionId matchingSessionId, UserId driverId, CancellationToken cancellationToken = default);
}

public class DriverMatchingService : IDriverMatchingService
{
    private readonly AppDbContext _dbContext;
    private readonly IDriverRankingService _driverRanking;
    private readonly ILogger<DriverMatchingService> _logger;
    private readonly IHubContext<DriverHub, IDriverNotifications> _hub;
    private readonly MatchingSessionOptions _settings;

    public DriverMatchingService(
        AppDbContext dbContext,
        IDriverRankingService driverRanking,
        ILogger<DriverMatchingService> logger,
        IHubContext<DriverHub, IDriverNotifications> hub,
        IOptions<MatchingSessionOptions> options)
    {
        _dbContext = dbContext;
        _driverRanking = driverRanking;
        _logger = logger;
        _hub = hub;
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
                return MatchingErrors.Session.NotFound;
            }

            if (!matchingSession.IsActive)
            {
                return MatchingErrors.Session.NotActive;
            }

            var tripRequest = matchingSession.TripRequest;

            _logger.LogInformation("Starting matching for session {MatchingSessionId} in round: {CurrentRound}",
                matchingSessionId, matchingSession.CurrentRound);

            var rankedDrivers = await _driverRanking.FindTopNDriversAsync(
                tripRequest.PickupLocation,
                matchingSession.OffersPerRound[matchingSession.CurrentRound - 1],
                roundNumber: matchingSession.CurrentRound - 1,
                excludedDrivers: matchingSession.GetRejectedOrExpiredDrivers(),
                cancellationToken: cancellationToken);

            if (rankedDrivers.Count == 0) 
            {
                _logger.LogInformation("No available drivers found for matching session {MatchingSessionId} in round {CurrentRound}",
                    matchingSessionId, matchingSession.CurrentRound);

                BackgroundJob.Schedule<IMatchingOrchestrator>(
                    orchestrator => orchestrator.HandlePostOutcomeAsync(
                        tripRequest.Id,
                        cancellationToken),
                    _settings.TimeAfterEmptyRound); // delay then advance to next round or cancel 

                return Result.Success;
            }

            List<DriverMatchAttempt> matchAttempts = new List<DriverMatchAttempt>();

            foreach (var driver in rankedDrivers)
            {
                // Create match attempt for each driver
                var attemptResult = matchingSession.CreateDriverMatchAttempt(
                    driver.DriverId,
                    driver.DistanceToPickup,
                    EstimateArrivalTime(driver.DistanceToPickup),
                    driver.Score,
                    _settings.DriverResponseTimeout); // pass the configured expiration duration

                if (attemptResult.IsError)
                {
                    _logger.LogWarning("Failed to create match attempt for driver {DriverId}: {Errors}",
                        driver.DriverId, string.Join(", ", attemptResult.Errors.Select(e => e.Description)));
                    continue;
                }

                matchAttempts.Add(attemptResult.Value);

                _logger.LogInformation("Created match attempt for driver {DriverId} in session {MatchingSessionId}",
                    driver.DriverId, matchingSessionId);

                BackgroundJob.Schedule<IDriverMatchingService>(
                    service => service.HandleMatchAttemptTimeoutAsync(
                        matchingSessionId,
                        driver.DriverId,
                        cancellationToken),
                    _settings.DriverResponseTimeout); // delay then timeout
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Send notifications to drivers with full trip details
            await Task.WhenAll(matchAttempts.Select(async match =>
            {
                var notification = new RideRequestOfferNotification(
                    match.Id.ToString(),
                    tripRequest.RiderProfile.PreferredName ?? tripRequest.RiderProfile.User.Name,
                    tripRequest.PickupLocation.Y,
                    tripRequest.PickupLocation.X,
                    tripRequest.DropoffLocation.Y,
                    tripRequest.DropoffLocation.X,
                    tripRequest.PickupAddress,
                    tripRequest.DropoffAddress,
                    tripRequest.FinalFare.Amount,
                    tripRequest.FinalFare.Distance,
                    match.DistanceToPickup,
                    match.EstimatedArrivalTime,
                    match.CreatedAt,
                    match.ExpiresAt);

                await _hub.Clients.Groups(match.DriverUserId.ToString())
                    .NotifyRideRequestOffer(match.DriverUserId.ToString(), notification);
            }));

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting matching for session {MatchingSessionId}", matchingSessionId);
            throw;
        }
    }


    public async Task HandleMatchAttemptTimeoutAsync(MatchingSessionId matchingSessionId, UserId driverId, CancellationToken cancellationToken = default)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var matchingSession = await _dbContext.MatchingSessions
                .Include(ms => ms.MatchAttempts)
                .FirstOrDefaultAsync(ms => ms.Id == matchingSessionId, cancellationToken);

            if (matchingSession == null || !matchingSession.IsActive)
            {
                return;
            }

            _logger.LogInformation("Handling match attempt timeout for driver {DriverId} in session {MatchingSessionId}",
                driverId, matchingSessionId);
            var result = matchingSession.ExpireMatch(driverId);
            if (result.IsError)
            {
                _logger.LogWarning("Failed to expire match attempt for driver {DriverId} in session {MatchingSessionId}: {Errors}",
                    driverId, matchingSessionId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }

            var expiredMatch = matchingSession.MatchAttempts.First(m => m.DriverUserId == driverId);
            
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Notify driver that their offer expired
            var expiredNotification = new RideOfferExpiredNotification(
                expiredMatch.Id.ToString(),
                DateTime.UtcNow);
            
            await _hub.Clients.Groups(driverId.ToString())
                .NotifyRideRequestOfferExpired(driverId.ToString(), expiredNotification);

            // After the timeout and state change are persisted, let the orchestrator
            // decide whether to advance the round, cancel, or do nothing.
            BackgroundJob.Enqueue<IMatchingOrchestrator>(
                orchestrator => orchestrator.HandlePostOutcomeAsync(
                    matchingSession.TripRequestId,
                    CancellationToken.None));

            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling match attempt timeout for driver {DriverId} in session {MatchingSessionId}",
                driverId, matchingSessionId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }



    private static double EstimateArrivalTime(double distanceMeters)
    {
        // Simple estimation: assume 30 km/h average speed in city
        const double averageSpeedKmh = 30.0;
        const double averageSpeedMs = averageSpeedKmh * 1000.0 / 3600.0; // m/s

        return distanceMeters / averageSpeedMs; // seconds
    }
}
