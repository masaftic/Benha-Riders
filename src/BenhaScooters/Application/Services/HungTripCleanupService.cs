using BenhaScooters.Data;
using BenhaScooters.Domain.Trips.Enums;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Services;

// TODO: still doesn't solve the problem of trips that are hung because driver forgot to mark them as completed,
// but at least it will clean up trips that were never completed at all
// (e.g. driver accepted but never started the trip, or started but never completed, etc.)

/// <summary>
/// Hangfire job that cleans up hung trips (trips that were never completed)
/// </summary>
public class HungTripCleanupService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<HungTripCleanupService> _logger;
    private static readonly TimeSpan HungTripThreshold = TimeSpan.FromHours(6);

    public HungTripCleanupService(AppDbContext dbContext, ILogger<HungTripCleanupService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task CleanupHungTripsAsync()
    {
        var cutoffTime = DateTime.UtcNow - HungTripThreshold;

        var hungTrips = await _dbContext.Trips
            .Where(t => (t.Status == TripStatus.InProgress || t.Status == TripStatus.Assigned || t.Status == TripStatus.DriverArrived)
                         && t.AssignedAt < cutoffTime)
            .ToListAsync();

        if (hungTrips.Count == 0)
        {
            _logger.LogDebug("No hung trips found");
            return;
        }

        _logger.LogInformation("Found {Count} hung trips to clean up", hungTrips.Count);

        foreach (var trip in hungTrips)
        {
            var result = trip.ForceCancel("System cleanup: Trip exceeded maximum duration (forgot to complete)");
            if (result.IsError)
                _logger.LogWarning("Failed to force cancel trip {TripId}: {Error}", trip.Id, result.FirstError.Description);
        }

        await _dbContext.SaveChangesAsync();
        _logger.LogInformation("Successfully processed hung trips cleanup");
    }
}
