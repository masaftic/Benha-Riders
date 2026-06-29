using BenhaScooters.Data;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Infrastructure.BackgroundServices;

public class TokenCleanupService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<TokenCleanupService> _logger;

    public TokenCleanupService(AppDbContext dbContext, ILogger<TokenCleanupService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task CleanupExpiredTokensAsync()
    {
        var cutoffDate = DateTime.UtcNow;

        var deletedCount = await _dbContext.RefreshTokens
           .Where(token => token.ExpiresAt < cutoffDate || token.IsRevoked)
           .ExecuteDeleteAsync();

        if (deletedCount > 0)
            _logger.LogInformation("Deleted {Count} expired/revoked refresh tokens", deletedCount);
        else
            _logger.LogDebug("No expired or revoked tokens found for cleanup");
    }
}
