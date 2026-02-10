using BenhaScooters.Application.Abstractions;
using BenhaScooters.Infrastructure.Notifications;
using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Trips.EventHandlers;

/// <summary>
/// Handles the TripCancelledEvent for rider and driver notifications
/// </summary>
public class TripCancelledEventHandler : INotificationHandler<TripCancelledEvent>
{
    private readonly ILogger<TripCancelledEventHandler> _logger;
    private readonly IHubContext<RiderHub, IRiderNotifications> _riderHub;
    private readonly IHubContext<DriverHub, IDriverNotifications> _driverHub;

    public TripCancelledEventHandler(
        ILogger<TripCancelledEventHandler> logger,
        IHubContext<RiderHub, IRiderNotifications> riderHub,
        IHubContext<DriverHub, IDriverNotifications> driverHub)
    {
        _logger = logger;
        _riderHub = riderHub;
        _driverHub = driverHub;
    }

    public async Task Handle(TripCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Trip {TripId} cancelled by {CancelledBy} at {CancelledAt}. Reason: {Reason}",
            notification.TripId,
            notification.CancelledBy,
            notification.OccurredAt,
            notification.CancellationReason);

        var tripCancelledNotification = new TripCancelledNotification(
            TripId: notification.TripId,
            CancellationReason: notification.CancellationReason,
            CancelledAt: notification.OccurredAt);

        // Notify rider via SignalR
        await _riderHub.Clients.Group(notification.RiderId.ToString())
            .NotifyTripCancelled(notification.RiderId.ToString(), tripCancelledNotification);

        _logger.LogInformation("Notified rider {RiderId} that trip {TripId} was cancelled",
            notification.RiderId, notification.TripId);

        // Notify driver via SignalR
        await _driverHub.Clients.Group(notification.DriverId.ToString())
            .NotifyTripCancelled(notification.DriverId.ToString(), tripCancelledNotification);

        _logger.LogInformation("Notified driver {DriverId} that trip {TripId} was cancelled",
            notification.DriverId, notification.TripId);
    }
}
