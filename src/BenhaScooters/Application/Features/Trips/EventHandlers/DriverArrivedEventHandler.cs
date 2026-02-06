using BenhaScooters.Application.Abstractions;
using BenhaScooters.Infrastructure.Notifications;
using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Trips.EventHandlers;

/// <summary>
/// Handles the DriverArrivedEvent for rider notifications
/// </summary>
public class DriverArrivedEventHandler : INotificationHandler<DriverArrivedEvent>
{
    private readonly ILogger<DriverArrivedEventHandler> _logger;
    private readonly IHubContext<RiderHub, IRiderNotifications> _riderHub;

    public DriverArrivedEventHandler(
        ILogger<DriverArrivedEventHandler> logger,
        IHubContext<RiderHub, IRiderNotifications> riderHub)
    {
        _logger = logger;
        _riderHub = riderHub;
    }

    public async Task Handle(DriverArrivedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Driver {DriverId} arrived for trip {TripId} at {ArrivedAt}",
            notification.DriverId,
            notification.TripId,
            notification.OccurredAt);

        // Notify rider via SignalR
        var driverArrivedNotification = new DriverArrivedNotification(
            TripId: notification.TripId,
            ArrivedAt: notification.OccurredAt);

        await _riderHub.Clients.Group(notification.RiderId.ToString())
            .NotifyDriverArrived(notification.RiderId.ToString(), driverArrivedNotification);

        _logger.LogInformation("Notified rider {RiderId} that driver has arrived for trip {TripId}",
            notification.RiderId, notification.TripId);
    }
}
