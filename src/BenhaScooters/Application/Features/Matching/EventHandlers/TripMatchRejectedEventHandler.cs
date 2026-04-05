using BenhaScooters.Application.Workflows;
using BenhaScooters.Application.Workflows.Matching;
using BenhaScooters.Data;
using BenhaScooters.Domain.Matching.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Matching.EventHandlers;

public class TripMatchRejectedEventHandler(
    AppDbContext db,
    IMessageScheduler scheduler,
    ILogger<TripMatchRejectedEventHandler> logger) : INotificationHandler<MatchAttemptRejectedEvent>
{
    public async Task Handle(MatchAttemptRejectedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling trip match rejection for trip request {TripRequestId} from driver {DriverId}",
            notification.TripRequestId, notification.DriverId);

        var session = await db.MatchingSessions
            .Include(ms => ms.MatchAttempts)
            .FirstOrDefaultAsync(ms => ms.TripRequestId == notification.TripRequestId, cancellationToken);

        if (session is null || !session.IsActive)
            return;

        if (session.IsExpired)
        {
            logger.LogInformation(
                "Matching session {MatchingSessionId} already expired while handling rejection from driver {DriverId}. " +
                "Waiting for timeout/evaluation flow to finalize cancellation.",
                session.Id,
                notification.DriverId);
            return;
        }

        var hasPendingAttemptsInCurrentRound = session.MatchAttempts.Any(ma =>
            ma.MatchingRound == session.CurrentRound &&
            ma.Status == Domain.Matching.MatchAttemptStatus.Pending);

        if (hasPendingAttemptsInCurrentRound)
        {
            logger.LogInformation(
                "Session {MatchingSessionId} still has pending offers in round {CurrentRound}; rejection from driver {DriverId} will not advance the workflow yet.",
                session.Id,
                session.CurrentRound,
                notification.DriverId);
            return;
        }

        logger.LogInformation(
            "All offers in round {CurrentRound} have been resolved for session {MatchingSessionId}. Enqueuing evaluation after rejection.",
            session.CurrentRound,
            session.Id);

        await scheduler.EnqueueAsync(new EvaluateMatchingProgress(session.Id), cancellationToken);
    }
}
