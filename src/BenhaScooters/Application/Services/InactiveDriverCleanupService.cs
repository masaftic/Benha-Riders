using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Services;

/// <summary>
/// Hangfire job that sets drivers with no recent heartbeat to offline
/// </summary>
public class InactiveDriverCleanupService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<InactiveDriverCleanupService> _logger;
    private static readonly TimeSpan InactivityThreshold = TimeSpan.FromHours(5);

    public InactiveDriverCleanupService(AppDbContext dbContext, ILogger<InactiveDriverCleanupService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SetInactiveDriversOfflineAsync()
    {
        var cutoff = DateTime.UtcNow - InactivityThreshold;

        var now = DateTime.UtcNow;

        var affected = await _dbContext.DriverStatuses
            .Where(ds => ds.Status == DriverAvailabilityStatus.Online
                && (ds.LastHeartbeat == null || ds.LastHeartbeat < cutoff))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(ds => ds.Status, DriverAvailabilityStatus.Offline)
                .SetProperty(ds => ds.LastStatusChange, now)
                .SetProperty(ds => ds.OnlineSessionStart, (DateTime?)null));

        if (affected > 0)
            _logger.LogInformation("Set {Count} inactive drivers (no heartbeat for {Hours}h) to Offline",
                affected, InactivityThreshold.TotalHours);
    }
}
