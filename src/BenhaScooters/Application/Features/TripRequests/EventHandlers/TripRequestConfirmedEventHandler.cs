using BenhaScooters.Application.Workflows;
using BenhaScooters.Application.Workflows.Matching;
using BenhaScooters.Domain.TripRequests.Events;

namespace BenhaScooters.Application.Features.TripRequests.EventHandlers;


public class TripRequestConfirmedEventHandler(
    ILogger<TripRequestConfirmedEventHandler> logger,
    IMessageScheduler scheduler) : INotificationHandler<TripRequestConfirmedEvent>
{
    public async Task Handle(TripRequestConfirmedEvent notification, CancellationToken ct)
    {
        logger.LogInformation("Trip request {TripRequestId} confirmed. Scheduling matching session...", notification.TripRequestId);

        await scheduler.EnqueueAsync(
            new StartMatchingSession(notification.TripRequestId),
            ct);
    }
}
