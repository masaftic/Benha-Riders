using BenhaScooters.Application.Abstractions;
using BenhaScooters.Infrastructure.Notifications;
using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Trips.EventHandlers;

/// <summary>
/// Handles the TripCompletedEvent for rider notifications
/// </summary>
public class TripCompletedEventHandler : INotificationHandler<TripCompletedEvent>
{
    private readonly ILogger<TripCompletedEventHandler> _logger;
    private readonly IHubContext<RiderHub, IRiderNotifications> _riderHub;

    public TripCompletedEventHandler(
        ILogger<TripCompletedEventHandler> logger,
        IHubContext<RiderHub, IRiderNotifications> riderHub)
    {
        _logger = logger;
        _riderHub = riderHub;
    }

    public async Task Handle(TripCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Trip {TripId} completed by driver {DriverId} at {CompletedAt}",
            notification.TripId,
            notification.DriverId,
            notification.OccurredAt);

        // Notify rider via SignalR
        var tripCompletedNotification = new TripCompletedNotification(
            TripId: notification.TripId,
            CompletedAt: notification.OccurredAt);

        await _riderHub.Clients.Group(notification.RiderId.ToString())
            .NotifyTripCompletedAsync(notification.RiderId.ToString(), tripCompletedNotification);

        _logger.LogInformation("Notified rider {RiderId} that trip {TripId} has completed",
            notification.RiderId, notification.TripId);
    }
}
