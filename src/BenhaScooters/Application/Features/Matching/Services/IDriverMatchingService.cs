using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Infrastructure.Matching.Services;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Matching.Services;

public interface IDriverMatchingService
{
    /// <summary>
    /// Start the matching process for a matching session
    /// </summary>
    Task<ErrorOr<Success>> StartMatchingAsync(MatchingSessionId matchingSessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Handle timeout for driver match attempts
    /// </summary>
    Task HandleMatchTimeoutAsync(MatchingSessionId matchingSessionId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Continue matching process after a driver rejection
    /// </summary>
    Task<ErrorOr<Success>> ContinueMatchingAfterRejectionAsync(TripRequestId tripRequestId, CancellationToken cancellationToken = default);
}

public class DriverMatchingService : IDriverMatchingService
{
    private readonly AppDbContext _dbContext;
    private readonly IDriverRankingService _driverRanking;
    private readonly ILogger<DriverMatchingService> _logger;
    private const int BroadcastDriverCount = 5;

    public DriverMatchingService(
        AppDbContext dbContext,
        IDriverRankingService driverRanking,
        ILogger<DriverMatchingService> logger)
    {
        _dbContext = dbContext;
        _driverRanking = driverRanking;
        _logger = logger;
    }

    public async Task<ErrorOr<Success>> StartMatchingAsync(MatchingSessionId matchingSessionId, CancellationToken cancellationToken = default)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            // Get the matching session with trip request
            var matchingSession = await _dbContext.MatchingSessions
                .Include(ms => ms.TripRequest)
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
            
            _logger.LogInformation("Starting matching for session {MatchingSessionId} in {Phase} phase",
                matchingSessionId.Value, matchingSession.CurrentPhase);

            ErrorOr<Success> result = matchingSession.CurrentPhase switch
            {
                MatchingPhase.Phase1_Push => await StartPhase1PushMatchingAsync(matchingSession, tripRequest, cancellationToken),
                MatchingPhase.Phase2_Broadcast => await StartPhase2BroadcastMatchingAsync(matchingSession, tripRequest, cancellationToken),
                MatchingPhase.Phase3_Broadcast => await StartPhase3BroadcastMatchingAsync(matchingSession, tripRequest, cancellationToken),
                _ => Error.Validation("INVALID_MATCHING_PHASE", "Invalid matching phase")
            };

            if (result.IsError)
            {
                return result.Errors;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting matching for session {MatchingSessionId}", matchingSessionId.Value);
            throw;
        }
    }

    private async Task<ErrorOr<Success>> StartPhase1PushMatchingAsync(
        MatchingSession matchingSession, 
        TripRequest tripRequest, 
        CancellationToken cancellationToken)
    {
        // Find the best single driver
        var bestDriver = await _driverRanking.FindBestDriverAsync(tripRequest.PickupLocation, cancellationToken);
        
        if (bestDriver == null)
        {
            _logger.LogWarning("No available drivers found for Phase 1 push for trip request {TripRequestId}", tripRequest.Id.Value);
            
            // Advance to Phase 2 if no drivers found
            var advanceResult = matchingSession.AdvanceToNextPhase();
            if (advanceResult.IsError)
            {
                return advanceResult.Errors;
            }
            
            return await StartPhase2BroadcastMatchingAsync(matchingSession, tripRequest, cancellationToken);
        }

        // Create driver match attempt
        var attemptResult = matchingSession.CreateDriverMatchAttempt(
            bestDriver.DriverId,
            bestDriver.DistanceToPickup,
            EstimateArrivalTime(bestDriver.DistanceToPickup),
            bestDriver.Score);

        if (attemptResult.IsError)
        {
            return attemptResult.Errors;
        }

        _logger.LogInformation("Created Phase 1 push match attempt for driver {DriverId} (distance: {Distance}m, score: {Score})",
            bestDriver.DriverId.Value, Math.Round(bestDriver.DistanceToPickup), bestDriver.Score);

        // TODO: Send push notification to driver
        // await _notificationService.SendTripOfferAsync(bestDriver.DriverId, tripRequest, attemptResult.Value);

        return Result.Success;
    }

    private async Task<ErrorOr<Success>> StartPhase2BroadcastMatchingAsync(
        MatchingSession matchingSession, 
        TripRequest tripRequest, 
        CancellationToken cancellationToken)
    {
        return await StartBroadcastMatchingAsync(matchingSession, tripRequest, cancellationToken, "Phase 2");
    }

    private async Task<ErrorOr<Success>> StartPhase3BroadcastMatchingAsync(
        MatchingSession matchingSession, 
        TripRequest tripRequest, 
        CancellationToken cancellationToken)
    {
        return await StartBroadcastMatchingAsync(matchingSession, tripRequest, cancellationToken, "Phase 3");
    }

    private async Task<ErrorOr<Success>> StartBroadcastMatchingAsync(
        MatchingSession matchingSession, 
        TripRequest tripRequest, 
        CancellationToken cancellationToken,
        string phaseDescription)
    {
        // Find top N drivers excluding those already contacted in this session
        var excludedDriverIds = matchingSession.MatchAttempts
            .Select(ma => ma.DriverId)
            .ToHashSet();

        var allTopDrivers = await _driverRanking.FindTopDriversAsync(
            tripRequest.PickupLocation, 
            BroadcastDriverCount * 2, // Get more candidates to account for exclusions
            cancellationToken);

        var topDrivers = allTopDrivers
            .Where(driver => !excludedDriverIds.Contains(driver.DriverId))
            .Take(BroadcastDriverCount)
            .ToList();
        
        if (!topDrivers.Any())
        {
            _logger.LogWarning("No available drivers found for {Phase} broadcast for trip request {TripRequestId}", 
                phaseDescription, tripRequest.Id.Value);
            
            // Check if we can advance to next phase or should cancel
            if (matchingSession.ShouldAdvanceToNextPhase())
            {
                var advanceResult = matchingSession.AdvanceToNextPhase();
                if (advanceResult.IsError)
                {
                    // No more phases, cancel the matching session
                    matchingSession.Cancel("No available drivers found after all phases");
                    return Result.Success;
                }
                
                // Start the next phase
                return await StartMatchingAsync(matchingSession.Id, cancellationToken);
            }
            else
            {
                // Cancel the matching session if no drivers available
                matchingSession.Cancel("No available drivers found");
                return Result.Success;
            }
        }

        // Create match attempts for all selected drivers
        foreach (var driver in topDrivers)
        {
            var attemptResult = matchingSession.CreateDriverMatchAttempt(
                driver.DriverId,
                driver.DistanceToPickup,
                EstimateArrivalTime(driver.DistanceToPickup),
                driver.Score);

            if (attemptResult.IsError)
            {
                _logger.LogWarning("Failed to create match attempt for driver {DriverId}: {Errors}",
                    driver.DriverId.Value, string.Join(", ", attemptResult.Errors.Select(e => e.Description)));
                continue;
            }

            // TODO: Send push notification to driver
            // await _notificationService.SendTripOfferAsync(driver.DriverId, tripRequest, attemptResult.Value);
        }

        _logger.LogInformation("Created {Phase} broadcast match attempts for {Count} drivers for trip request {TripRequestId}",
            phaseDescription, topDrivers.Count, tripRequest.Id.Value);

        return Result.Success;
    }

    public async Task HandleMatchTimeoutAsync(MatchingSessionId matchingSessionId, CancellationToken cancellationToken = default)
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        
        try
        {
            var matchingSession = await _dbContext.MatchingSessions
                .Include(ms => ms.TripRequest)
                .Include(ms => ms.MatchAttempts)
                .FirstOrDefaultAsync(ms => ms.Id == matchingSessionId, cancellationToken);

            if (matchingSession == null || !matchingSession.IsActive)
            {
                return;
            }

            _logger.LogInformation("Handling timeout for matching session {MatchingSessionId} in {Phase} phase", 
                matchingSessionId.Value, matchingSession.CurrentPhase);

            // Record timeout
            matchingSession.RecordTimeout();

            // Handle timeout based on current phase
            switch (matchingSession.CurrentPhase)
            {
                case MatchingPhase.Phase1_Push:
                    _logger.LogInformation("Phase 1 timeout - advancing to Phase 2 for session {MatchingSessionId}", 
                        matchingSessionId.Value);
                    
                    var advanceResult = matchingSession.AdvanceToNextPhase();
                    if (advanceResult.IsError)
                    {
                        matchingSession.Cancel("Failed to advance phase");
                        break;
                    }
                    
                    await StartMatchingAsync(matchingSessionId, cancellationToken);
                    break;

                case MatchingPhase.Phase2_Broadcast:
                    if (matchingSession.ShouldAdvanceToNextPhase())
                    {
                        _logger.LogInformation("Phase 2 timeout - advancing to Phase 3 for session {MatchingSessionId}", 
                            matchingSessionId.Value);
                        
                        var phase3AdvanceResult = matchingSession.AdvanceToNextPhase();
                        if (phase3AdvanceResult.IsError)
                        {
                            matchingSession.Cancel("No more phases available");
                            break;
                        }
                        
                        await StartMatchingAsync(matchingSessionId, cancellationToken);
                    }
                    else
                    {
                        _logger.LogInformation("Phase 2 timeout - waiting for remaining drivers for session {MatchingSessionId}", 
                            matchingSessionId.Value);
                    }
                    break;

                case MatchingPhase.Phase3_Broadcast:
                    if (matchingSession.ShouldAdvanceToNextPhase())
                    {
                        _logger.LogWarning("Phase 3 timeout - cancelling session {MatchingSessionId} (no more phases)", 
                            matchingSessionId.Value);
                        
                        matchingSession.Cancel("No drivers available after all phases");
                        
                        // Also cancel the trip request
                        var tripRequest = matchingSession.TripRequest;
                        tripRequest.Cancel("No drivers available");
                    }
                    else
                    {
                        _logger.LogInformation("Phase 3 timeout - waiting for remaining drivers for session {MatchingSessionId}", 
                            matchingSessionId.Value);
                    }
                    break;

                default:
                    _logger.LogError("Unknown phase {Phase} for session {MatchingSessionId}", 
                        matchingSession.CurrentPhase, matchingSessionId.Value);
                    matchingSession.Cancel("Unknown phase error");
                    break;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling timeout for matching session {MatchingSessionId}", matchingSessionId.Value);
            throw;
        }
    }

    public async Task<ErrorOr<Success>> ContinueMatchingAfterRejectionAsync(TripRequestId tripRequestId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Find the matching session for this trip request
            var matchingSession = await _dbContext.MatchingSessions
                .Include(ms => ms.MatchAttempts)
                .FirstOrDefaultAsync(ms => ms.TripRequestId == tripRequestId, cancellationToken);

            if (matchingSession == null)
            {
                return MatchingErrors.Session.NotFound;
            }

            if (!matchingSession.IsActive)
            {
                return MatchingErrors.Session.NotActive;
            }

            // Continue with the matching process
            // The session already recorded the rejection and may have switched modes
            return await StartMatchingAsync(matchingSession.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error continuing matching after rejection for trip request {TripRequestId}", tripRequestId.Value);
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
