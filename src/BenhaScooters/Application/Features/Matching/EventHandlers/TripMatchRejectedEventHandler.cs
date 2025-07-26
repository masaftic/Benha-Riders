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

        // Handle phase progression based on current phase
        await HandlePhaseProgression(matchingSession, cancellationToken);
    }

    private async Task HandlePhaseProgression(MatchingSession matchingSession, CancellationToken cancellationToken)
    {
        logger.LogInformation("Evaluating phase progression for session {SessionId} in phase {Phase}", 
            matchingSession.Id.Value, matchingSession.CurrentPhase);

        switch (matchingSession.CurrentPhase)
        {
            case MatchingPhase.Phase1_Push:
                // Always advance to Phase 2 after rejection in Phase 1
                logger.LogInformation("Phase 1 rejection - advancing to Phase 2 for session {SessionId}", 
                    matchingSession.Id.Value);
                
                var advanceResult = matchingSession.AdvanceToNextPhase();
                if (advanceResult.IsError)
                {
                    logger.LogError("Failed to advance to Phase 2 for session {SessionId}: {Error}", 
                        matchingSession.Id.Value, string.Join(", ", advanceResult.Errors.Select(e => e.Description)));
                    
                    matchingSession.Cancel("Failed to advance phase");
                    await db.SaveChangesAsync(cancellationToken);
                    return;
                }
                
                await db.SaveChangesAsync(cancellationToken);
                await matchingService.StartMatchingAsync(matchingSession.Id, cancellationToken);
                break;

            case MatchingPhase.Phase2_Broadcast:
            case MatchingPhase.Phase3_Broadcast:
                // Check if all drivers in current phase have responded
                if (matchingSession.ShouldAdvanceToNextPhase())
                {
                    logger.LogInformation("All drivers in {Phase} have responded - evaluating next step for session {SessionId}", 
                        matchingSession.CurrentPhase, matchingSession.Id.Value);
                    
                    var nextPhaseResult = matchingSession.AdvanceToNextPhase();
                    if (nextPhaseResult.IsError)
                    {
                        // No more phases, cancel the session
                        logger.LogWarning("No more phases available - cancelling session {SessionId}", 
                            matchingSession.Id.Value);
                        
                        matchingSession.Cancel("No available drivers after all phases");
                        
                        // Cancel the trip request as well
                        var tripRequest = await db.TripRequests
                            .FirstOrDefaultAsync(tr => tr.Id == matchingSession.TripRequestId, cancellationToken);
                        if (tripRequest != null)
                        {
                            tripRequest.Cancel("No drivers available");
                        }
                    }
                    else
                    {
                        // Start the next phase
                        logger.LogInformation("Advancing to next phase and continuing matching for session {SessionId}", 
                            matchingSession.Id.Value);
                        
                        await db.SaveChangesAsync(cancellationToken);
                        await matchingService.StartMatchingAsync(matchingSession.Id, cancellationToken);
                        return;
                    }
                    
                    await db.SaveChangesAsync(cancellationToken);
                }
                else
                {
                    logger.LogInformation("Waiting for other drivers in {Phase} to respond for session {SessionId}", 
                        matchingSession.CurrentPhase, matchingSession.Id.Value);
                    // Just wait for other drivers in the current phase - no action needed
                }
                break;

            default:
                logger.LogError("Unknown phase {Phase} for session {SessionId}", 
                    matchingSession.CurrentPhase, matchingSession.Id.Value);
                matchingSession.Cancel("Invalid phase state");
                await db.SaveChangesAsync(cancellationToken);
                break;
        }
    }
}
