using BenhaScooters.Application.Abstractions;
using BenhaScooters.Domain.Matching.Events;
using BenhaScooters.Infrastructure.Notifications;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Matching.EventHandlers;

public class MatchAttemptExpiredEventHandler : INotificationHandler<MatchAttemptExpiredEvent>
{
    private readonly IHubContext<DriverHub, IDriverNotifications> _hub;
    private readonly ILogger<MatchAttemptExpiredEventHandler> _logger;

    public MatchAttemptExpiredEventHandler(
        IHubContext<DriverHub, IDriverNotifications> hub,
        ILogger<MatchAttemptExpiredEventHandler> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Handle(MatchAttemptExpiredEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Match attempt {MatchAttemptId} expired for driver {DriverId} in session {MatchingSessionId}",
            notification.MatchAttemptId,
            notification.DriverUserId,
            notification.MatchingSessionId);

        var expiredNotification = new RideOfferExpiredNotification(
            notification.MatchAttemptId.ToString(),
            notification.ExpiredAt);

        await _hub.Clients.Groups(notification.DriverUserId.ToString())
            .NotifyRideRequestOfferExpired(notification.DriverUserId.ToString(), expiredNotification);

        _logger.LogInformation(
            "Notified driver {DriverId} about expired match attempt {MatchAttemptId}",
            notification.DriverUserId,
            notification.MatchAttemptId);
    }
}
