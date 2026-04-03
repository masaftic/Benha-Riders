using BenhaScooters.Application.Features.Matching.Services;
using BenhaScooters.Domain.Matching.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Matching.EventHandlers;

public class TripMatchRejectedEventHandler(
    IMatchingOrchestrator orchestrator,
    ILogger<TripMatchRejectedEventHandler> logger) : INotificationHandler<MatchAttemptRejectedEvent>
{
    public async Task Handle(MatchAttemptRejectedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling trip match rejection for trip request {TripRequestId} from driver {DriverId}",
            notification.TripRequestId, notification.DriverId);

        // var result = await orchestrator.HandlePostOutcomeAsync(notification.TripRequestId, cancellationToken);

        // if (result.IsError)
        // {
        //     logger.LogWarning("Post-outcome handling failed for trip request {TripRequestId}: {Errors}",
        //         notification.TripRequestId,
        //         string.Join(", ", result.Errors.Select(e => e.Description)));
        // }
    }
}
