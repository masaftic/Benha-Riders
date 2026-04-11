using BenhaScooters.Application.Abstractions;
using BenhaScooters.Data;
using BenhaScooters.Domain.Users;
using FirebaseAdmin.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Infrastructure.Notifications;

public class FirebasePushNotificationService : IPushNotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<FirebasePushNotificationService> _logger;

    public FirebasePushNotificationService(AppDbContext dbContext, ILogger<FirebasePushNotificationService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SendToUserAsync(UserId userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken cancellationToken = default)
    {
        var tokens = await _dbContext.UserDeviceTokens
            .Where(t => t.UserId == userId)
            .Select(t => t.Token)
            .ToListAsync(cancellationToken);

        if (tokens.Count == 0)
        {
            _logger.LogDebug("No device tokens found for user {UserId}, skipping push notification", userId);
            return;
        }

        var message = new MulticastMessage
        {
            Tokens = tokens,
            Notification = new Notification
            {
                Title = title,
                Body = body,
            },
            Data = data,
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    ChannelId = "trip_channel",
                    Sound = "tripnotification",
                    DefaultSound = false,
                }
            },
            Apns = new ApnsConfig
            {
                Headers = new Dictionary<string, string>
                {
                    { "apns-priority", "10" }
                },
                Aps = new Aps
                {
                    Sound = "tripnotification.mp3",
                    Badge = 1,
                }
            }
        };

        try
        {
            var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message, cancellationToken);

            if (response.FailureCount > 0)
            {
                var invalidTokens = new List<string>();
                for (int i = 0; i < response.Responses.Count; i++)
                {
                    if (!response.Responses[i].IsSuccess)
                    {
                        var error = response.Responses[i].Exception;
                        if (error?.MessagingErrorCode == MessagingErrorCode.Unregistered ||
                            error?.MessagingErrorCode == MessagingErrorCode.InvalidArgument)
                        {
                            invalidTokens.Add(tokens[i]);
                        }

                        _logger.LogWarning("FCM send failed for user {UserId}: {Error}",
                            userId, error?.Message);
                    }
                }

                // Remove invalid tokens
                if (invalidTokens.Count > 0)
                {
                    var tokensToRemove = await _dbContext.UserDeviceTokens
                        .Where(t => invalidTokens.Contains(t.Token))
                        .ToListAsync(cancellationToken);

                    _dbContext.UserDeviceTokens.RemoveRange(tokensToRemove);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    _logger.LogInformation("Removed {Count} invalid FCM tokens for user {UserId}",
                        invalidTokens.Count, userId);
                }
            }

            _logger.LogInformation("FCM notification sent to user {UserId}: {SuccessCount}/{TotalCount} succeeded",
                userId, response.SuccessCount, tokens.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send FCM notification to user {UserId}", userId);
        }
    }
}
