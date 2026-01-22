using BenhaScooters.Application.Features.Matching.Services;
using BenhaScooters.Domain.Matching.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Matching.EventHandlers;

public class TripMatchRejectedEventHandler(
    IMatchingOrchestrator orchestrator,
    ILogger<TripMatchRejectedEventHandler> logger) : INotificationHandler<TripMatchRejectedEvent>
{
    public async Task Handle(TripMatchRejectedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling trip match rejection for trip request {TripRequestId} from driver {DriverId}",
            notification.TripRequestId.Value, notification.DriverId.Value);

        var result = await orchestrator.HandlePostOutcomeAsync(notification.TripRequestId, cancellationToken);

        if (result.IsError)
        {
            logger.LogWarning("Post-outcome handling failed for trip request {TripRequestId}: {Errors}",
                notification.TripRequestId.Value,
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }
}
