using BenhaScooters.Data;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Services;

public class TokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TokenCleanupService> _logger;
    private readonly IConfiguration _configuration;
    private readonly TimeSpan _cleanupInterval;

    public TokenCleanupService(IServiceProvider serviceProvider, ILogger<TokenCleanupService> logger, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;

        // Get cleanup interval from configuration, default to 1 hour
        var intervalHours = _configuration.GetValue<double>("TokenCleanup:IntervalHours", 1.0);
        _cleanupInterval = TimeSpan.FromHours(intervalHours);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token cleanup service started with {Interval} cleanup interval", _cleanupInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync();
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during token cleanup");
                // Wait a shorter time before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Token cleanup service stopped");
    }

    private async Task CleanupExpiredTokensAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffDate = DateTime.UtcNow;

        var deletedCount = await dbContext.RefreshTokens
           .AsNoTracking()
           .Where(token => token.ExpiresAt < cutoffDate || token.IsRevoked)
           .ExecuteDeleteAsync();

        if (deletedCount > 0)
        {
            _logger.LogInformation("Deleted {Count} expired/revoked refresh tokens", deletedCount);
        }
        else
        {
            _logger.LogDebug("No expired or revoked tokens found for cleanup");
        }
    }
}
