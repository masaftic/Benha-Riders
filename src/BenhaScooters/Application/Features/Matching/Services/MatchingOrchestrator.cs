using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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
            return MatchingErrors.Session.NotFound;
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
                    "Advancing to next round for session {MatchingSessionId}",
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
