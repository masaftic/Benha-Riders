using BenhaScooters.Application.Features.Matching.Settings;
using BenhaScooters.Data;
using BenhaScooters.Domain.Matching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Application.Workflows.Matching;

public class StartMatchingSessionHandler(
    AppDbContext db,
    IOptions<MatchingSessionOptions> options,
    IMessageScheduler scheduler,
    ILogger<StartMatchingSessionHandler> logger) : IRequestHandler<StartMatchingSession>
{
    public async Task Handle(StartMatchingSession command, CancellationToken ct)
    {
        var tripRequest = await db.TripRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == command.TripRequestId, ct);

        if (tripRequest is null || !tripRequest.CanBeAssigned)
            return;

        var session = await db.MatchingSessions
            .FirstOrDefaultAsync(x => x.TripRequestId == command.TripRequestId, ct);

        if (session is not null && session.IsActive && session.IsExpired)
        {
            logger.LogInformation(
                "Existing matching session {MatchingSessionId} for trip request {TripRequestId} has expired. Cancelling it before starting a new one.",
                session.Id,
                command.TripRequestId);

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
        }

        if (session is null || !session.IsActive || session.IsExpired)
        {
            var createResult = MatchingSession.Create(
                command.TripRequestId,
                options.Value.NumberOfRounds,
                options.Value.OffersPerRound.ToList());

            if (createResult.IsError)
            {
                logger.LogError("Failed to create matching session for trip request {TripRequestId}: {Errors}",
                    command.TripRequestId, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            session = createResult.Value;
            db.MatchingSessions.Add(session);
            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation("Started matching session {MatchingSessionId} for trip request {TripRequestId}",
            session.Id, command.TripRequestId);

        await scheduler.EnqueueAsync(new StartMatchingRound(session.Id), ct);
    }
}
