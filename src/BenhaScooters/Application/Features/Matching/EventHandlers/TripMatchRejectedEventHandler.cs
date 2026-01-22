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

        var transitionResult = matchingSession.TryTransitionToNextRound();

        if (transitionResult.IsError)
        {
            logger.LogWarning("Failed to transition matching session {MatchingSessionId} after rejection: {Errors}",
                matchingSession.Id.Value, string.Join(", ", transitionResult.Errors.Select(e => e.Description)));
            return;
        }

        switch (transitionResult.Value)
        {
            case RoundTransitioned:
                logger.LogInformation("Advancing to next round for session {MatchingSessionId}", matchingSession.Id.Value);
                await db.SaveChangesAsync(cancellationToken);

                var result = await matchingService.ProcessMatchingAsync(matchingSession.Id, cancellationToken);
                if (result.IsError)
                {
                    logger.LogWarning("Failed to process matching for session {MatchingSessionId}: {Errors}",
                        matchingSession.Id.Value, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
                break;

            case MatchingCanceled:
                logger.LogInformation("Matching session {MatchingSessionId} cancelled after rejection", matchingSession.Id.Value);
                await db.SaveChangesAsync(cancellationToken);
                break;

            case NoTransition:
            case MatchingCompleted:
            default:
                // Nothing to do – either still waiting on other attempts or already completed
                break;
        }
    }
}
