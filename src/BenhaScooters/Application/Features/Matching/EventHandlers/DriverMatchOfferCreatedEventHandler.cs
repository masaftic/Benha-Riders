using BenhaScooters.Application.Abstractions;
using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching.Events;
using BenhaScooters.Infrastructure.Notifications;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Matching.EventHandlers;

public class DriverMatchOfferCreatedEventHandler : INotificationHandler<DriverMatchOfferCreatedEvent>
{
    private readonly IHubContext<DriverHub, IDriverNotifications> _hub;
    private readonly ILocalizedPushNotificationService _pushNotification;
    private readonly ISignalRConnectionTracker _connectionTracker;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<DriverMatchOfferCreatedEventHandler> _logger;

    public DriverMatchOfferCreatedEventHandler(
        IHubContext<DriverHub, IDriverNotifications> hub,
        ILocalizedPushNotificationService pushNotification,
        ISignalRConnectionTracker connectionTracker,
        AppDbContext dbContext,
        ILogger<DriverMatchOfferCreatedEventHandler> logger)
    {
        _hub = hub;
        _pushNotification = pushNotification;
        _connectionTracker = connectionTracker;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(DriverMatchOfferCreatedEvent e, CancellationToken cancellationToken)
    {
        var driverInfo = await _dbContext.DriverStatuses
            .AsNoTracking()
            .Where(ds => ds.UserId == e.DriverId)
            .Select(ds => new { ds.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (driverInfo is null)
        {
            _logger.LogWarning("Driver {DriverId} has no DriverStatus record, skipping notification", e.DriverId);
            return;
        }

        var hasActiveSignalRConnection = await _connectionTracker.HasActiveConnectionAsync(e.DriverId);

        if (driverInfo.Status != DriverAvailabilityStatus.Online && !hasActiveSignalRConnection)
        {
            _logger.LogInformation(
                "Driver {DriverId} is offline and has no active SignalR connection, skipping notification for match attempt {MatchAttemptId}",
                e.DriverId, e.MatchAttemptId);
            return;
        }

        var driverIdString = e.DriverId.ToString();

        var notification = new RideRequestOfferNotification(
            e.MatchAttemptId.ToString(),
            e.RiderName,
            e.Pickup.Latitude,
            e.Pickup.Longitude,
            e.Dropoff.Latitude,
            e.Dropoff.Longitude,
            e.PickupAddress,
            e.DropoffAddress,
            e.EstimatedFare,
            e.EstimatedDistance.ToKilometers(),
            e.DistanceToPickup.ToKilometers(),
            e.EstimatedArrival.ToMinutes(),
            e.OfferedAt);

        if (hasActiveSignalRConnection)
        {
            await _hub.Clients.Groups(driverIdString)
                .NotifyRideRequestOffer(driverIdString, notification);

            _logger.LogInformation(
                "Sent ride offer to driver {DriverId} for match attempt {MatchAttemptId} via SignalR",
                e.DriverId, e.MatchAttemptId);
        }
        else if (driverInfo.Status == DriverAvailabilityStatus.Online)
        {
            await _pushNotification.NotifyRideRequestOfferToDriverAsync(
                e.DriverId,
                e.MatchAttemptId.ToString(),
                e.RiderName,
                e.EstimatedFare,
                $"{e.Pickup.Latitude},{e.Pickup.Longitude}",
                $"{e.Dropoff.Latitude},{e.Dropoff.Longitude}",
                e.PickupAddress,
                e.DropoffAddress,
                e.DistanceToPickup.ToKilometers(),
                e.EstimatedArrival.ToMinutes(),
                cancellationToken);

            _logger.LogInformation(
                "Sent ride offer to driver {DriverId} for match attempt {MatchAttemptId} via FCM because no active SignalR connection was found",
                e.DriverId, e.MatchAttemptId);
        }
    }
}
