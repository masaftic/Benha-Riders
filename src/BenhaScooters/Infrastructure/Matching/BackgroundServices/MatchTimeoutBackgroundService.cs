// using BenhaScooters.Application.Features.Matching.Services;
// using BenhaScooters.Data;
// using BenhaScooters.Domain.Matching;
// using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;

// namespace BenhaScooters.Infrastructure.Matching.BackgroundServices;

// /// <summary>
// /// Background service that periodically checks for expired driver match attempts
// /// and handles timeouts appropriately
// /// </summary>
// public class MatchTimeoutBackgroundService : BackgroundService
// {
//     private readonly IServiceProvider _serviceProvider;
//     private readonly ILogger<MatchTimeoutBackgroundService> _logger;
//     private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(30); // Check every 30 seconds

//     public MatchTimeoutBackgroundService(
//         IServiceProvider serviceProvider,
//         ILogger<MatchTimeoutBackgroundService> logger)
//     {
//         _serviceProvider = serviceProvider;
//         _logger = logger;
//     }

//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         _logger.LogInformation("Match timeout background service started");

//         while (!stoppingToken.IsCancellationRequested)
//         {
//             try
//             {
//                 await CheckForExpiredMatchAttemptsAsync(stoppingToken);
//                 await Task.Delay(_checkInterval, stoppingToken);
//             }
//             catch (OperationCanceledException)
//             {
//                 // Expected when cancellation is requested
//                 break;
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error in match timeout background service");
                
//                 // Wait before retrying to avoid rapid failure loops
//                 await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
//             }
//         }

//         _logger.LogInformation("Match timeout background service stopped");
//     }

//     private async Task CheckForExpiredMatchAttemptsAsync(CancellationToken cancellationToken)
//     {
//         using var scope = _serviceProvider.CreateScope();
//         var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//         var matchingService = scope.ServiceProvider.GetRequiredService<IDriverMatchingService>();

//         // Find matching sessions with expired attempts
//         var now = DateTime.UtcNow;
//         var expiredSessionIds = await dbContext.MatchingSessions
//             .Where(ms => ms.Status == MatchingSessionStatus.Active)
//             .Where(ms => ms.MatchAttempts.Any(ma => 
//                 ma.Status == MatchAttemptStatus.Pending && 
//                 ma.ExpiresAt <= now))
//             .Select(ms => ms.Id)
//             .ToListAsync(cancellationToken);

//         if (!expiredSessionIds.Any())
//         {
//             return;
//         }

//         _logger.LogInformation("Found {Count} matching sessions with expired attempts", expiredSessionIds.Count);

//         // Process each expired session
//         foreach (var sessionId in expiredSessionIds)
//         {
//             try
//             {
//                 await matchingService.HandleMatchTimeoutAsync(sessionId, cancellationToken);
//                 _logger.LogDebug("Processed timeout for matching session {SessionId}", sessionId);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error processing timeout for matching session {SessionId}", sessionId);
//             }
//         }
//     }
// }
