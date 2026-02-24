using BenhaScooters.Data;
using BenhaScooters.Domain.Trips.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BenhaScooters.Application.Services;

// TODO: still doesn't solve the problem of trips that are hung because driver forgot to mark them as completed, 
// but at least it will clean up trips that were never completed at all 
// (e.g. driver accepted but never started the trip, or started but never completed, etc.)


/// <summary>
/// Background service that periodically cleans up hung trips (trips that were never completed)
/// </summary>
public class HungTripCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<HungTripCleanupService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(30);
    private readonly TimeSpan _hungTripThreshold = TimeSpan.FromHours(6);

    public HungTripCleanupService(IServiceProvider serviceProvider, ILogger<HungTripCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Hung Trip Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupHungTripsAsync(stoppingToken);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hung trip cleanup");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Hung Trip Cleanup Service stopped");
    }

    private async Task CleanupHungTripsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffTime = DateTime.UtcNow - _hungTripThreshold;

        // Find trips that are active for too long
        var hungTrips = await dbContext.Trips
            .Where(t => (t.Status == TripStatus.InProgress || t.Status == TripStatus.Assigned || t.Status == TripStatus.DriverArrived)
                         && t.AssignedAt < cutoffTime)
            .ToListAsync(cancellationToken);

        if (hungTrips.Any())
        {
            _logger.LogInformation("Found {Count} hung trips to clean up", hungTrips.Count);

            foreach (var trip in hungTrips)
            {
                var result = trip.ForceCancel("System cleanup: Trip exceeded maximum duration (forgot to complete)");
                if (result.IsError)
                {
                    _logger.LogWarning("Failed to force cancel trip {TripId}: {Error}", trip.Id, result.FirstError.Description);
                }
            }

            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Successfully processed hung trips cleanup");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving hung trips cleanup changes");
            }
        }
        else
        {
            _logger.LogDebug("No hung trips found");
        }
    }
}
