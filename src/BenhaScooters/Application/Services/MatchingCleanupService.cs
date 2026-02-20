using BenhaScooters.Data;
using BenhaScooters.Domain.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Services;

/// <summary>
/// Background service that periodically cleans up old match attempts
/// </summary>
public class MatchingCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MatchingCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1); // Run every hour
    private readonly TimeSpan _offerRetentionPeriod = TimeSpan.FromHours(24); // Keep offers for 24 hours

    public MatchingCleanupService(IServiceProvider serviceProvider, ILogger<MatchingCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Matching Cleanup Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldOffersAsync(stoppingToken);
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during matching cleanup");
                // Wait a bit before retrying
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Matching Cleanup Service stopped");
    }

    private async Task CleanupOldOffersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffTime = DateTime.UtcNow - _offerRetentionPeriod;

        // Delete old match attempts that are no longer needed
        var oldOffers = await dbContext.DriverMatchAttempts
            .Where(dma => dma.CreatedAt < cutoffTime)
            .ToListAsync(cancellationToken);

        if (oldOffers.Any())
        {
            dbContext.DriverMatchAttempts.RemoveRange(oldOffers);
            await dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Cleaned up {Count} old match attempts older than {CutoffTime}",
                oldOffers.Count,
                cutoffTime);
        }
        else
        {
            _logger.LogDebug("No old match attempts to clean up");
        }
    }
}
