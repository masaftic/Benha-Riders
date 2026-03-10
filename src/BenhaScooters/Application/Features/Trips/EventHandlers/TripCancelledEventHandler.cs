using BenhaScooters.Application.Abstractions;
using BenhaScooters.Infrastructure.Notifications;
using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using BenhaScooters.Data;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Domain.Drivers;

namespace BenhaScooters.Application.Features.Trips.EventHandlers;

/// <summary>
/// Handles the TripCancelledEvent for rider and driver notifications
/// </summary>
public class TripCancelledEventHandler : INotificationHandler<TripCancelledEvent>
{
    private readonly ILogger<TripCancelledEventHandler> _logger;
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<RiderHub, IRiderNotifications> _riderHub;
    private readonly IHubContext<DriverHub, IDriverNotifications> _driverHub;

    public TripCancelledEventHandler(
        ILogger<TripCancelledEventHandler> logger,
        IHubContext<RiderHub, IRiderNotifications> riderHub,
        IHubContext<DriverHub, IDriverNotifications> driverHub,
        AppDbContext dbContext)
    {
        _logger = logger;
        _riderHub = riderHub;
        _driverHub = driverHub;
        _dbContext = dbContext;
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

        var driverStatus = await _dbContext.DriverStatuses.FirstOrDefaultAsync(ds => ds.UserId == notification.DriverId, cancellationToken);
        if (driverStatus != null)
        {
            driverStatus.UpdateStatus(DriverAvailabilityStatus.Online);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Notify rider via SignalR
        if (notification.RiderId != notification.CancelledBy)
        {
            await _riderHub.Clients.Group(notification.RiderId.ToString())
                .NotifyTripCancelled(notification.RiderId.ToString(), tripCancelledNotification);

            _logger.LogInformation("Notified rider {RiderId} that trip {TripId} was cancelled",
                notification.RiderId, notification.TripId);
        }

        if (notification.DriverId != notification.CancelledBy)
        {
            await _driverHub.Clients.Group(notification.DriverId.ToString())
                .NotifyTripCancelled(notification.DriverId.ToString(), tripCancelledNotification);

            _logger.LogInformation("Notified driver {DriverId} that trip {TripId} was cancelled",
                notification.DriverId, notification.TripId);
        }
    }
}
