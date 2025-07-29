using BenhaScooters.Application.Features.Matching.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.Matching.Events;
using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Matching.EventHandlers;

public class TripMatchRejectedEventHandler(
    IDriverMatchingService matchingService,
    AppDbContext db,
    ILogger<TripMatchRejectedEventHandler> logger) : INotificationHandler<TripMatchRejectedEvent>
{
    public async Task Handle(TripMatchRejectedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling trip match rejection for trip request {TripRequestId} from driver {DriverId}",
            notification.TripRequestId.Value, notification.DriverId.Value);

        // Find the matching session for this trip request
        var matchingSession = await db.MatchingSessions
            .Include(ms => ms.MatchAttempts)
            .FirstOrDefaultAsync(ms => ms.TripRequestId == notification.TripRequestId, cancellationToken);

        if (matchingSession == null || !matchingSession.IsActive)
        {
            logger.LogWarning("No active matching session found for trip request {TripRequestId}",
                notification.TripRequestId.Value);
            return;
        }

        if (matchingSession.ShouldAdvanceToNextRound())
        {
            logger.LogInformation("Advancing to next round for session {MatchingSessionId}", matchingSession.Id.Value);
            matchingSession.AdvanceToNextRound();
            var result = await matchingService.ProcessMatchingAsync(matchingSession.Id, cancellationToken);
            if (result.IsError)
            {
                logger.LogWarning("Failed to advance matching session {MatchingSessionId}: {Errors}",
                    matchingSession.Id.Value, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }
}
