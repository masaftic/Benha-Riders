using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Matching.Commands;

public record AcceptMatchCommand(
    UserId DriverId,
    DriverMatchAttemptId DriverMatchAttemptId) : IRequest<ErrorOr<AcceptMatchResult>>;

public record AcceptMatchResult(TripId TripId, DateTime AcceptedAt);

public class AcceptMatchCommandValidator : AbstractValidator<AcceptMatchCommand>
{
    public AcceptMatchCommandValidator()
    {
        RuleFor(x => x.DriverId.Value)
            .NotEmpty()
            .WithMessage("Driver ID is required");

        RuleFor(x => x.DriverMatchAttemptId.Value)
            .NotEmpty()
            .WithMessage("Driver Match Attempt ID is required");
    }
}

public class AcceptMatchCommandHandler(AppDbContext db, IPublisher publisher, ILogger<AcceptMatchCommandHandler> logger) : IRequestHandler<AcceptMatchCommand, ErrorOr<AcceptMatchResult>>
{
    public async Task<ErrorOr<AcceptMatchResult>> Handle(AcceptMatchCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;

        // Locate the driver match attempt being accepted
        var matchAttempt = await db.DriverMatchAttempts
            .Include(ma => ma.MatchingSession)
            .ThenInclude(ms => ms.TripRequest)
            .FirstOrDefaultAsync(ma => ma.Id == request.DriverMatchAttemptId, cancellationToken);

        if (matchAttempt is null || matchAttempt.DriverUserId != request.DriverId)
        {
            logger.LogWarning("Match attempt {MatchAttemptId} not found or does not belong to driver {DriverId}",
                request.DriverMatchAttemptId.Value, request.DriverId.Value);
            return MatchingErrors.MatchAttempt.NotFound;
        }

        if (matchAttempt.ExpiresAt <= now)
        {
            logger.LogWarning("Match attempt {MatchAttemptId} for driver {DriverId} is expired",
                request.DriverMatchAttemptId.Value, request.DriverId.Value);
            return MatchingErrors.MatchAttempt.Expired;
        }

        if (matchAttempt.Status != MatchAttemptStatus.Pending)
        {
            logger.LogWarning("Match attempt {MatchAttemptId} for driver {DriverId} is not pending (status: {Status})",
                request.DriverMatchAttemptId.Value, request.DriverId.Value, matchAttempt.Status);
            return MatchingErrors.MatchAttempt.NotFound;
        }

        var matchingSession = matchAttempt.MatchingSession;

        if (matchingSession is null)
        {
            logger.LogWarning("No matching session found for match attempt {MatchAttemptId}",
                request.DriverMatchAttemptId.Value);
            return MatchingErrors.Session.NotFound;
        }

        var acceptResult = matchingSession.AcceptMatch(request.DriverId);
        if (acceptResult.IsError)
        {
            logger.LogWarning("Failed to accept match for driver {DriverId}: {Errors}",
                request.DriverId.Value, string.Join(", ", acceptResult.Errors.Select(e => e.Description)));
            return acceptResult.Errors;
        }

        var userId = request.DriverId;
        var driverStatus = await db.DriverStatuses
            .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

        if (driverStatus is null)
        {
            logger.LogWarning("Driver status not found for driver {DriverId} when accepting match", request.DriverId.Value);
            return DriverErrors.DriverNotFound;
        }

        var tripRequest = matchingSession.TripRequest;

        var trip = new Trip(
            driverStatus.UserId,
            tripRequest.RiderId,
            tripRequest.PickupLocation,
            tripRequest.DropoffLocation,
            tripRequest.PickupAddress,
            tripRequest.DropoffAddress,
            tripRequest.FinalFare);

        db.Trips.Add(trip);

        // Save to generate TripId
        await db.SaveChangesAsync(cancellationToken);

        var startTripResult = driverStatus.StartTrip(trip.Id);
        if (startTripResult.IsError)
        {
            logger.LogWarning("Failed to start trip {TripId} for driver {DriverId}: {Errors}",
                trip.Id.Value,
                request.DriverId.Value,
                string.Join(", ", startTripResult.Errors.Select(e => e.Description)));
            await transaction.RollbackAsync(cancellationToken);
            return startTripResult.Errors;
        }

        var tripRoute = new TripRoute(trip.Id);
        db.TripRoutes.Add(tripRoute);

        var markMatchedResult = tripRequest.MarkAsMatched(driverStatus.UserId);
        if (markMatchedResult.IsError)
        {
            logger.LogWarning("Failed to mark trip request {TripRequestId} as matched: {Errors}",
                tripRequest.Id.Value,
                string.Join(", ", markMatchedResult.Errors.Select(e => e.Description)));
            await transaction.RollbackAsync(cancellationToken);
            return markMatchedResult.Errors;
        }

        await db.SaveChangesAsync(cancellationToken);

        // Publish trip created event for downstream listeners
        await publisher.Publish(trip.CreateTripCreatedEvent(), cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return new AcceptMatchResult(trip.Id, trip.AssignedAt);
    }
}
