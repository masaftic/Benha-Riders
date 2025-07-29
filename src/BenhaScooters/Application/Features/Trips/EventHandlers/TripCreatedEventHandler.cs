using BenhaScooters.Data;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Events;
using MediatR;

namespace BenhaScooters.Application.Features.Trips.EventHandlers;

public class TripCreatedEventHandler : INotificationHandler<TripCreatedEvent>
{
    private readonly ILogger<TripCreatedEventHandler> _logger;

    public TripCreatedEventHandler(ILogger<TripCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TripCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Trip created: {TripId} for Driver {DriverId} and Rider {RiderId}",
            notification.TripId.Value,
            notification.DriverId.Value,
            notification.RiderId.Value);

        // Additional logic can be added here, e.g., notifying other services or updating caches

        return Task.CompletedTask;
    }
}
