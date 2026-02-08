using BenhaScooters.Application.Abstractions;
using BenhaScooters.Domain.Matching.Events;
using BenhaScooters.Infrastructure.Notifications;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Matching.EventHandlers;

public class MatchAttemptCancelledEventHandler : INotificationHandler<MatchAttemptCancelledEvent>
{
    private readonly IHubContext<DriverHub, IDriverNotifications> _hub;
    private readonly ILogger<MatchAttemptCancelledEventHandler> _logger;

    public MatchAttemptCancelledEventHandler(
        IHubContext<DriverHub, IDriverNotifications> hub,
        ILogger<MatchAttemptCancelledEventHandler> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task Handle(MatchAttemptCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Match attempt {MatchAttemptId} cancelled for driver {DriverId} in session {MatchingSessionId}",
            notification.MatchAttemptId,
            notification.DriverUserId,
            notification.MatchingSessionId);

        var expiredNotification = new RideOfferExpiredNotification(
            notification.MatchAttemptId.ToString(),
            notification.CancelledAt);

        await _hub.Clients.Groups(notification.DriverUserId.ToString())
            .NotifyRideRequestOfferExpired(notification.DriverUserId.ToString(), expiredNotification);

        _logger.LogInformation(
            "Notified driver {DriverId} about cancelled match attempt {MatchAttemptId}",
            notification.DriverUserId,
            notification.MatchAttemptId);
    }
}
