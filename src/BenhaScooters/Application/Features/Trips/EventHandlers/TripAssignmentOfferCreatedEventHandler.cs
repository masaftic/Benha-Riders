using BenhaScooters.Domain.Matching.Events;
using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Trips.EventHandlers;

/// <summary>
/// Handles the TripAssignmentOfferCreatedEvent to notify drivers
/// </summary>
public class TripAssignmentOfferCreatedEventHandler : INotificationHandler<DriverMatchOfferCreatedEvent>
{
    private readonly ILogger<TripAssignmentOfferCreatedEventHandler> _logger;

    public TripAssignmentOfferCreatedEventHandler(ILogger<TripAssignmentOfferCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(DriverMatchOfferCreatedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Trip offer created for driver {DriverId} for trip request {TripRequestId}. Distance: {Distance}km, ETA: {ETA}min, Score: {Score}, Expires at: {ExpiresAt}",
            notification.DriverId.Value,
            notification.TripRequestId.Value,
            notification.DistanceToPickup,
            notification.EstimatedArrivalTime,
            notification.DriverScore,
            notification.OfferExpiresAt);

        // TODO: In the next phase, this will:
        // 1. Send push notification to the driver
        // 2. Send real-time notification via WebSocket/SignalR
        // 3. Create notification record in database
        // 4. Set up timeout handling for the offer

        await Task.CompletedTask;
    }
}
