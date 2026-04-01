using BenhaScooters.Data;
using BenhaScooters.Domain.Matching;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Services;

/// <summary>
/// Hangfire job that cleans up old match attempts
/// </summary>
public class MatchingCleanupService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<MatchingCleanupService> _logger;
    private static readonly TimeSpan OfferRetentionPeriod = TimeSpan.FromHours(24);

    public MatchingCleanupService(AppDbContext dbContext, ILogger<MatchingCleanupService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task CleanupOldOffersAsync()
    {
        var cutoffTime = DateTime.UtcNow - OfferRetentionPeriod;

        var oldOffers = await _dbContext.DriverMatchAttempts
            .Where(dma => dma.CreatedAt < cutoffTime)
            .ToListAsync();

        if (oldOffers.Count == 0)
        {
            _logger.LogDebug("No old match attempts to clean up");
            return;
        }

        _dbContext.DriverMatchAttempts.RemoveRange(oldOffers);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} old match attempts older than {CutoffTime}",
            oldOffers.Count, cutoffTime);
    }
}
