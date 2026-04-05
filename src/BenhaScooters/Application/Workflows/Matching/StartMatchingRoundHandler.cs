using BenhaScooters.Application.Features.Matching.Settings;
using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.Matching.Events;
using BenhaScooters.Infrastructure.Matching.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Application.Workflows.Matching;

public class StartMatchingRoundHandler(
    AppDbContext db,
    ILogger<StartMatchingRoundHandler> logger,
    IDriverRankingService driverRanking,
    IGeoService geoService,
    IPublisher publisher,
    IMessageScheduler scheduler,
    IOptions<MatchingSessionOptions> options) : IRequestHandler<StartMatchingRound>
{
    private static readonly TimeSpan NoDriversGracePeriod = TimeSpan.FromSeconds(3);

    public async Task Handle(StartMatchingRound message, CancellationToken ct)
    {
        var session = await db.MatchingSessions
            .Include(x => x.MatchAttempts)
            .Include(x => x.TripRequest)
                .ThenInclude(x => x.RiderProfile)
                    .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == message.SessionId, ct);

        if (session is null || !session.IsActive)
            return;

        if (session.IsExpired)
        {
            logger.LogInformation(
                "Matching session {MatchingSessionId} expired before round {RoundNumber} could start. Cancelling session.",
                session.Id,
                session.CurrentRound);

            var cancelResult = session.Cancel(false, "Matching session timed out");
            if (cancelResult.IsError)
            {
                logger.LogWarning(
                    "Failed to cancel expired matching session {MatchingSessionId}: {Errors}",
                    session.Id, string.Join(", ", cancelResult.Errors.Select(e => e.Description)));
                return;
            }

            await db.SaveChangesAsync(ct);
            return;
        }

        var tripRequest = session.TripRequest;

        var drivers = await driverRanking.FindTopNDriversAsync(
            tripRequest.PickupLocation,
            session.CurrentOffer,
            session.CurrentRound,
            session.GetRejectedOrPendingDrivers(),
            ct);

        if (drivers.Count == 0)
        {
            logger.LogInformation("No drivers found for matching session {MatchingSessionId} in round {RoundNumber}",
                session.Id, session.CurrentRound);

            await scheduler.ScheduleAsync(
                new EvaluateMatchingProgress(session.Id),
                NoDriversGracePeriod,
                ct);
            return;
        }

        foreach (var driver in drivers)
        {
            var eta = geoService.EstimateArrivalTime(driver.DistanceToPickup);

            var attemptResult = session.CreateDriverMatchAttempt(
                driver.DriverId,
                driver.DistanceToPickup,
                eta,
                driver.Score);

            if (attemptResult.IsError)
                continue;

            logger.LogInformation("Created match attempt for driver {DriverId} in session {MatchingSessionId}",
                driver.DriverId, session.Id);
        }

        await db.SaveChangesAsync(ct);

        foreach (var attempt in session.MatchAttempts.Where(x =>
                     x.MatchingRound == session.CurrentRound &&
                     x.Status == MatchAttemptStatus.Pending))
        {
            await publisher.Publish(new DriverMatchOfferCreatedEvent(
                attempt.Id,
                tripRequest.Id,
                attempt.DriverUserId,
                tripRequest.RiderProfile.PreferredName ?? tripRequest.RiderProfile.User.Name,
                tripRequest.PickupCoordinate,
                tripRequest.DropoffCoordinate,
                tripRequest.PickupAddress,
                tripRequest.DropoffAddress,
                tripRequest.FinalFare.Amount,
                tripRequest.FinalFare.Distance,
                attempt.DistanceToPickup,
                attempt.EstimatedArrivalTime,
                attempt.CreatedAt), ct);
        }

        await scheduler.ScheduleAsync(
            new MatchingRoundTimedOut(session.Id, session.CurrentRound),
            options.Value.RoundTimeout,
            ct);
    }
}
