using BenhaScooters.Application.Abstractions;
using BenhaScooters.Domain.Users;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Infrastructure.Notifications;

public class NoOpPushNotificationService : IPushNotificationService
{
    private readonly ILogger<NoOpPushNotificationService> _logger;

    public NoOpPushNotificationService(ILogger<NoOpPushNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendToUserAsync(UserId userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("FCM not configured. Skipping push notification to user {UserId}: {Title}", userId, title);
        return Task.CompletedTask;
    }
}
