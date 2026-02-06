using BenhaScooters.Application.Abstractions;
using BenhaScooters.Infrastructure.Notifications;
using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Trips.EventHandlers;

/// <summary>
/// Handles the TripStartedEvent for route tracking initialization
/// </summary>
public class TripStartedEventHandler : INotificationHandler<TripStartedEvent>
{
    private readonly ILogger<TripStartedEventHandler> _logger;
    private readonly IHubContext<RiderHub, IRiderNotifications> _riderHub;

    public TripStartedEventHandler(
        ILogger<TripStartedEventHandler> logger,
        IHubContext<RiderHub, IRiderNotifications> riderHub)
    {
        _logger = logger;
        _riderHub = riderHub;
    }

    public async Task Handle(TripStartedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Trip {TripId} started by driver {DriverId} at {StartedAt}",
            notification.TripId,
            notification.DriverId,
            notification.OccurredAt);

        // Notify rider via SignalR
        var tripStartedNotification = new TripStartedNotification(
            TripId: notification.TripId,
            StartedAt: notification.OccurredAt);

        await _riderHub.Clients.Group(notification.RiderId.ToString())
            .NotifyTripStarted(notification.RiderId.ToString(), tripStartedNotification);

        _logger.LogInformation("Notified rider {RiderId} that trip {TripId} has started",
            notification.RiderId, notification.TripId);
    }
}
