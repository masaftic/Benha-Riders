using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Trips.EventHandlers;

/// <summary>
/// Handles the TripCreatedEvent for notifications and tracking
/// </summary>
public class TripCreatedEventHandler : INotificationHandler<TripCreatedEvent>
{
    private readonly ILogger<TripCreatedEventHandler> _logger;

    public TripCreatedEventHandler(ILogger<TripCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TripCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Driver {DriverId} accepted trip {TripId} for rider {RiderId} at {CreatedAt}",
            notification.DriverId.Value,
            notification.TripId.Value,
            notification.RiderId.Value,
            notification.OccurredAt);

        // TODO: In later phases, this will:
        // - Notify the rider
        // - Start driver tracking
        
        return Task.CompletedTask;
    }
}
