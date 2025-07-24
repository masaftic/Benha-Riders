using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Trips.EventHandlers;

/// <summary>
/// Handles the TripAcceptedEvent for notifications and tracking
/// </summary>
public class TripAcceptedEventHandler : INotificationHandler<TripAcceptedEvent>
{
    private readonly ILogger<TripAcceptedEventHandler> _logger;

    public TripAcceptedEventHandler(ILogger<TripAcceptedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(TripAcceptedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Trip {TripId} accepted by driver {DriverId} for trip request {TripRequestId}",
            notification.TripId.Value,
            notification.DriverId.Value,
            notification.TripRequestId.Value);

        // TODO: In later phases, this will:
        // - Notify the rider
        // - Cancel other matching attempts
        // - Start driver tracking
        
        return Task.CompletedTask;
    }
}
