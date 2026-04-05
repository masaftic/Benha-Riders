using BenhaScooters.Application.Features.Matching.Settings;
using BenhaScooters.Data;
using BenhaScooters.Domain.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Application.Workflows.Matching;

public class EvaluateMatchingProgressHandler(
    IMessageScheduler scheduler,
    AppDbContext dbContext,
    IOptions<MatchingSessionOptions> options,
    ILogger<EvaluateMatchingProgressHandler> logger) : IRequestHandler<EvaluateMatchingProgress>
{
    public async Task Handle(EvaluateMatchingProgress message, CancellationToken ct)
    {
        var matchingSession = await dbContext.MatchingSessions
            .Include(ms => ms.MatchAttempts)
            .FirstOrDefaultAsync(ms => ms.Id == message.SessionId, ct);

        if (matchingSession == null || !matchingSession.IsActive)
            return;

        if (matchingSession.IsExpired)
        {
            logger.LogInformation(
                "Matching session {MatchingSessionId} expired during progress evaluation. Cancelling session.",
                matchingSession.Id);

            var cancelResult = matchingSession.Cancel(false, "Matching session timed out");
            if (cancelResult.IsError)
            {
                logger.LogWarning(
                    "Failed to cancel expired matching session {MatchingSessionId}: {Errors}",
                    matchingSession.Id,
                    string.Join(", ", cancelResult.Errors.Select(e => e.Description)));
                return;
            }

            await dbContext.SaveChangesAsync(ct);
            return;
        }

        // Ensure a minimum duration has passed before concluding no match, to allow for late acceptances
        var settings = options.Value;
        if (matchingSession.IsLastRound() && matchingSession.MatchAttempts.Any(ma => ma.Status == MatchAttemptStatus.Pending))
        {
            var elapsed = DateTime.UtcNow - matchingSession.CreatedAt;
            var remaining = settings.MinimumSessionDuration - elapsed;

            if (remaining > TimeSpan.Zero)
            {
                logger.LogInformation(
                    "Session {MatchingSessionId} has match attempts but hasn't reached minimum duration. " +
                    "Waiting {RemainingSeconds:F0}s before final cancellation",
                    matchingSession.Id, remaining.TotalSeconds);

                await scheduler.ScheduleAsync(
                    new EvaluateMatchingProgress(message.SessionId),
                    remaining + TimeSpan.FromSeconds(1), // add a small buffer to ensure we don't check too early
                    ct);
                return;
            }
        }


        logger.LogInformation("Processing matching session {SessionId} for next round or cancellation", message.SessionId);
        var transitionResult = matchingSession.TryTransitionToNextRound();

        if (transitionResult.IsError)
        {
            logger.LogWarning(
                "Failed to transition matching session {MatchingSessionId} after outcome: {Errors}",
                matchingSession.Id,
                string.Join(", ", transitionResult.Errors.Select(e => e.Description)));

            return;
        }

        // Persist state change before triggering new matching round or concluding the session
        await dbContext.SaveChangesAsync(ct);

        switch (transitionResult.Value)
        {
            case RoundTransitioned:
                logger.LogInformation(
                    "Advancing to round {CurrentRound} for session {MatchingSessionId}",
                    matchingSession.CurrentRound,
                    matchingSession.Id);

                await scheduler.EnqueueAsync(
                    new StartMatchingRound(matchingSession.Id),
                    ct);
                break;

            case MatchingCanceled:
                logger.LogInformation(
                    "Matching session {MatchingSessionId} cancelled after all rounds completed with no match",
                    matchingSession.Id);
                break;

            case MatchingCompleted:
            default:
                break;
        }

    }
}
