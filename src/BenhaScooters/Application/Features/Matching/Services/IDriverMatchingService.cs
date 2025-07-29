using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Infrastructure.Matching.Services;
using ErrorOr;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Matching.Services;

public interface IDriverMatchingService
{

    Task<ErrorOr<Success>> ProcessMatchingAsync(MatchingSessionId matchingSessionId, CancellationToken cancellationToken = default);

    Task HandleMatchAttemptTimeoutAsync(MatchingSessionId matchingSessionId, DriverId driverId, CancellationToken cancellationToken = default);
}

public class DriverMatchingService : IDriverMatchingService
{
    private readonly AppDbContext _dbContext;
    private readonly IDriverRankingService _driverRanking;
    private readonly ILogger<DriverMatchingService> _logger;

    public DriverMatchingService(
        AppDbContext dbContext,
        IDriverRankingService driverRanking,
        ILogger<DriverMatchingService> logger)
    {
        _dbContext = dbContext;
        _driverRanking = driverRanking;
        _logger = logger;
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
                matchingSessionId.Value, matchingSession.CurrentRound);

            var rankedDrivers = await _driverRanking.FindTopNDriversAsync(
                tripRequest.PickupLocation,
                matchingSession.OffersPerRound[matchingSession.CurrentRound - 1],
                excludedDrivers: matchingSession.GetRejectedOrExpiredDrivers(),
                cancellationToken: cancellationToken);

            if (rankedDrivers.Count == 0)
            {
                _logger.LogInformation("No drivers found for matching session {MatchingSessionId}. Session cancelled.",
                    matchingSessionId.Value);

                matchingSession.Cancel("No drivers available for matching");

                await _dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Result.Success;
            }

            foreach (var driver in rankedDrivers)
            {
                // Create match attempt for each driver
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

                _logger.LogInformation("Created match attempt for driver {DriverId} in session {MatchingSessionId}",
                    driver.DriverId.Value, matchingSessionId.Value);

                BackgroundJob.Schedule<IDriverMatchingService>(
                    service => service.HandleMatchAttemptTimeoutAsync(
                        matchingSessionId,
                        driver.DriverId,
                        cancellationToken),
                    TimeSpan.FromSeconds(30));
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


    public async Task HandleMatchAttemptTimeoutAsync(MatchingSessionId matchingSessionId, DriverId driverId, CancellationToken cancellationToken = default)
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
                driverId.Value, matchingSessionId.Value);
            var result = matchingSession.ExpireMatch(driverId);
            if (result.IsError)
            {
                _logger.LogWarning("Failed to expire match attempt for driver {DriverId} in session {MatchingSessionId}: {Errors}",
                    driverId.Value, matchingSessionId.Value, string.Join(", ", result.Errors.Select(e => e.Description)));
                return;
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (matchingSession.ShouldAdvanceToNextRound())
            {
                _logger.LogInformation("Advancing to next round for session {MatchingSessionId}", matchingSessionId.Value);
                matchingSession.AdvanceToNextRound();
                var matchResult = await ProcessMatchingAsync(matchingSessionId, cancellationToken);
                if (matchResult.IsError)
                {
                    _logger.LogWarning("Failed to advance matching session {MatchingSessionId}: {Errors}",
                        matchingSessionId.Value, string.Join(", ", matchResult.Errors.Select(e => e.Description)));
                }
            }

            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling match attempt timeout for driver {DriverId} in session {MatchingSessionId}",
                driverId.Value, matchingSessionId.Value);
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
