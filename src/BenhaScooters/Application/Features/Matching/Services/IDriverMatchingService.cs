using BenhaScooters.Application.Abstractions;
using BenhaScooters.Application.Features.Matching.Settings;
using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Common.Geo;
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
    Task HandleRoundTimeoutAsync(MatchingSessionId matchingSessionId, CancellationToken cancellationToken = default);
}

public class DriverMatchingService : IDriverMatchingService
{
    private readonly AppDbContext _dbContext;
    private readonly IDriverRankingService _driverRanking;
    private readonly IGeoService _geoService;
    private readonly ILogger<DriverMatchingService> _logger;
    private readonly IHubContext<DriverHub, IDriverNotifications> _hub;
    private readonly IPushNotificationService _pushNotification;
    private readonly MatchingSessionOptions _settings;

    public DriverMatchingService(
        AppDbContext dbContext,
        IDriverRankingService driverRanking,
        IGeoService geoService,
        ILogger<DriverMatchingService> logger,
        IHubContext<DriverHub, IDriverNotifications> hub,
        IPushNotificationService pushNotification,
        IOptions<MatchingSessionOptions> options)
    {
        _dbContext = dbContext;
        _driverRanking = driverRanking;
        _geoService = geoService;
        _logger = logger;
        _hub = hub;
        _pushNotification = pushNotification;
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
                excludedDrivers: matchingSession.GetRejectedOrPendingDrivers(),
                cancellationToken: cancellationToken);

            if (rankedDrivers.Count == 0) 
            {
                _logger.LogInformation("No available drivers found for matching session {MatchingSessionId} in round {CurrentRound}",
                    matchingSessionId, matchingSession.CurrentRound);

                BackgroundJob.Schedule<IMatchingOrchestrator>(
                    orchestrator => orchestrator.HandlePostOutcomeAsync(
                        tripRequest.Id,
                        cancellationToken),
                    _settings.RoundTimeout); // delay then advance to next round or cancel 

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
                    tripRequest.FinalFare.Distance.ToKilometers(),
                    match.DistanceToPickup.ToKilometers(),
                    match.EstimatedArrivalTime.ToMinutes(),
                    match.CreatedAt); 

                await _hub.Clients.Groups(match.DriverUserId.ToString())
                    .NotifyRideRequestOffer(match.DriverUserId.ToString(), notification);

                // Also send FCM push notification for drivers not connected via SignalR
                await _pushNotification.SendToUserAsync(
                    match.DriverUserId,
                    "طلب رحلة جديد",
                    $"لديك طلب رحلة من {notification.RiderName} - {notification.EstimatedFare:F0} جنيه",
                    new Dictionary<string, string>
                    {
                        ["type"] = "ride_request_offer",
                        ["matchAttemptId"] = match.Id.ToString(),
                        ["riderName"] = notification.RiderName,
                        ["pickupLocation"] = $"{notification.PickupLatitude},{notification.PickupLongitude}",
                        ["dropoffLocation"] = $"{notification.DropoffLatitude},{notification.DropoffLongitude}",
                        ["pickupAddress"] = notification.PickupAddress ?? "Unknown pickup location",
                        ["dropoffAddress"] = notification.DropoffAddress ?? "Unknown dropoff location",
                        ["fare"] = notification.EstimatedFare.ToString(),
                        ["distanceToPickup"] = notification.DistanceToPickup.ToString(),
                        ["estimatedArrival"] = notification.EstimatedArrivalTime.ToString(),
                    });
            }));

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

            if (matchingSession == null || !matchingSession.IsActive)
            {
                _logger.LogInformation("Matching session {MatchingSessionId} is not active, skipping round timeout",
                    matchingSessionId);
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
                ? _settings.FinalRoundWait 
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
