using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Trips.EventHandlers;

/// <summary>
/// Handles the DriverArrivedEvent for rider notifications
/// </summary>
public class DriverArrivedEventHandler : INotificationHandler<DriverArrivedEvent>
{
    private readonly ILogger<DriverArrivedEventHandler> _logger;

    public DriverArrivedEventHandler(ILogger<DriverArrivedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(DriverArrivedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Driver {DriverId} arrived for trip {TripId} at {ArrivedAt}",
            notification.DriverId.Value,
            notification.TripId.Value,
            notification.OccurredAt);

        // TODO: In later phases, this will:
        // - Notify the rider that driver has arrived
        // - Send push notification
        
        return Task.CompletedTask;
    }
}
