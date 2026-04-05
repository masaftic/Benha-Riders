using BenhaScooters.Data;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Workflows.Matching;

public class MatchingRoundTimedOutHandler(
    IMessageScheduler scheduler,
    ILogger<MatchingRoundTimedOutHandler> logger,
    AppDbContext db) : IRequestHandler<MatchingRoundTimedOut>
{
    public async Task Handle(MatchingRoundTimedOut message, CancellationToken ct)
    {
        var session = await db.MatchingSessions.FirstOrDefaultAsync(x => x.Id == message.SessionId, ct);

        if (session is null || !session.IsActive)
            return;

        if (session.IsExpired)
        {
            logger.LogInformation(
                "Matching session {MatchingSessionId} expired while handling timeout for round {RoundNumber}. Cancelling session.",
                session.Id,
                message.RoundNumber);

            var cancelResult = session.Cancel(false, "Matching session timed out");
            if (cancelResult.IsError)
            {
                logger.LogWarning(
                    "Failed to cancel expired matching session {MatchingSessionId}: {Errors}",
                    session.Id,
                    string.Join(", ", cancelResult.Errors.Select(e => e.Description)));
                return;
            }

            await db.SaveChangesAsync(ct);
            return;
        }

        // maybe the session was completed while this message was in the queue, 
        // so we check if the round number matches before proceeding
        if (session.CurrentRound != message.RoundNumber)
            return;

        await scheduler.EnqueueAsync(
            new EvaluateMatchingProgress(message.SessionId),
            ct);
    }
}
