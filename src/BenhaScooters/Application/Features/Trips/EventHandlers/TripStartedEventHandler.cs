using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Trips.EventHandlers;

/// <summary>
/// Handles the TripStartedEvent for route tracking initialization
/// </summary>
public class TripStartedEventHandler : INotificationHandler<TripStartedEvent>
{
    private readonly ILogger<TripStartedEventHandler> _logger;

    public TripStartedEventHandler(ILogger<TripStartedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TripStartedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Trip {TripId} started by driver {DriverId} at {StartedAt}",
            notification.TripId.Value,
            notification.DriverId.Value,
            notification.StartedAt);

        // TODO: In later phases, this will:
        // - Begin GPS tracking for route building
        // - Notify rider that trip has started
        // - Initialize real-time tracking
        
        return Task.CompletedTask;
    }
}
