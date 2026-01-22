using BenhaScooters.Domain.Matching.Events;
using MediatR;

namespace BenhaScooters.Application.Features.Matching.EventHandlers;

/// <summary>
/// Currently a no-op handler; trip creation is handled synchronously
/// in AcceptMatchCommandHandler. This handler is kept for future
/// analytics or notifications related to accepted matches.
/// </summary>
public class TripMatchAcceptedEventHandler(ILogger<TripMatchAcceptedEventHandler> logger)
    : INotificationHandler<TripMatchAcceptedEvent>
{
    public Task Handle(TripMatchAcceptedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Trip match accepted for trip request {TripRequestId} by driver {DriverId} at {AcceptedAt}",
            notification.TripRequestId.Value,
            notification.DriverId.Value,
            notification.AcceptedAt);

        return Task.CompletedTask;
    }
}
