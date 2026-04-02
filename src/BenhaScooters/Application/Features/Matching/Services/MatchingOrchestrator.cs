using BenhaScooters.Application.Features.Matching.Settings;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Infrastructure.Notifications;
using ErrorOr;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Application.Features.Matching.Services;

public interface IMatchingOrchestrator
{
    Task<ErrorOr<RoundTransitionResult>> HandlePostOutcomeAsync(
        TripRequestId tripRequestId,
        CancellationToken cancellationToken = default);
}

public class MatchingOrchestrator(
    AppDbContext dbContext,
    IDriverMatchingService driverMatchingService,
    IOptions<MatchingSessionOptions> options,
    ILogger<MatchingOrchestrator> logger) : IMatchingOrchestrator
{
    public async Task<ErrorOr<RoundTransitionResult>> HandlePostOutcomeAsync(
        TripRequestId tripRequestId,
        CancellationToken cancellationToken = default)
    {
        // Load the active matching session for this trip request
        var matchingSession = await dbContext.MatchingSessions
            .Include(ms => ms.MatchAttempts)
            .FirstOrDefaultAsync(ms => ms.TripRequestId == tripRequestId, cancellationToken);

        if (matchingSession is null || !matchingSession.IsActive)
        {
            logger.LogInformation(
                "No active matching session found for trip request {TripRequestId} when handling post-outcome",
                tripRequestId);
            return AppErrors.Matching.Session.NotFound();
        }

        // Check if anyone has accepted the match
        var hasAcceptedMatch = matchingSession.MatchAttempts.Any(ma => ma.Status == MatchAttemptStatus.Accepted);
        if (hasAcceptedMatch)
        {
            logger.LogInformation(
                "Match already accepted for session {MatchingSessionId}, no further action needed",
                matchingSession.Id);
            return new MatchingCompleted();
        }

        // If this is the last round and the session had real offers,
        // wait until the minimum session duration before cancelling.
        var settings = options.Value;
        if (matchingSession.IsLastRound() && matchingSession.MatchAttempts.Count > 0)
        {
            var elapsed = DateTime.UtcNow - matchingSession.CreatedAt;
            var remaining = settings.MinimumSessionDuration - elapsed;

            if (remaining > TimeSpan.Zero)
            {
                logger.LogInformation(
                    "Session {MatchingSessionId} has match attempts but hasn't reached minimum duration. " +
                    "Waiting {RemainingSeconds:F0}s before final cancellation",
                    matchingSession.Id, remaining.TotalSeconds);

                BackgroundJob.Schedule<IMatchingOrchestrator>(
                    o => o.HandlePostOutcomeAsync(tripRequestId, cancellationToken),
                    remaining);

                return new NoTransition();
            }
        }

        var transitionResult = matchingSession.TryTransitionToNextRound();

        if (transitionResult.IsError)
        {
            logger.LogWarning(
                "Failed to transition matching session {MatchingSessionId} after outcome: {Errors}",
                matchingSession.Id,
                string.Join(", ", transitionResult.Errors.Select(e => e.Description)));

            return transitionResult.Errors;
        }

        // Persist state change before triggering new matching round or concluding the session
        await dbContext.SaveChangesAsync(cancellationToken);

        switch (transitionResult.Value)
        {
            case RoundTransitioned:
                logger.LogInformation(
                    "Advancing to round {CurrentRound} for session {MatchingSessionId}",
                    matchingSession.CurrentRound,
                    matchingSession.Id);

                var matchResult = await driverMatchingService.ProcessMatchingAsync(matchingSession.Id, cancellationToken);
                if (matchResult.IsError)
                {
                    logger.LogWarning(
                        "Failed to process matching for session {MatchingSessionId}: {Errors}",
                        matchingSession.Id,
                        string.Join(", ", matchResult.Errors.Select(e => e.Description)));
                }
                break;

            case MatchingCanceled:
                logger.LogInformation(
                    "Matching session {MatchingSessionId} cancelled after all rounds completed with no match",
                    matchingSession.Id);
                break;

            case NoTransition:
            case MatchingCompleted:
            default:
                // Still waiting on other attempts in the current round or already completed.
                break;
        }

        return transitionResult.Value;
    }
}
