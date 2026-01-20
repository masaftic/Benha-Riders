using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Matching.Commands;

public record AcceptMatchCommand(
    DriverId DriverId,
    DriverMatchAttemptId DriverMatchAttemptId) : IRequest<ErrorOr<AcceptMatchResult>>;

public record AcceptMatchResult(TripId TripId, string Message, DateTime AcceptedAt);

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

public class AcceptMatchCommandHandler(AppDbContext db, IPublisher publisher) : IRequestHandler<AcceptMatchCommand, ErrorOr<AcceptMatchResult>>
{
    public async Task<ErrorOr<AcceptMatchResult>> Handle(AcceptMatchCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        List<Error> errors = [];
        try
        {
            var matchAttempt = await db.DriverMatchAttempts
                .Include(ma => ma.MatchingSession)
                .ThenInclude(ms => ms.TripRequest)
                .Where(ma => ma.Id == request.DriverMatchAttemptId
                            && ma.ExpiresAt > DateTime.UtcNow
                            && ma.DriverId == request.DriverId
                            && ma.Status == MatchAttemptStatus.Pending)
                .FirstOrDefaultAsync(cancellationToken);

            // Find the matching session for this trip request
            var matchingSession = matchAttempt?.MatchingSession;

            if (matchingSession == null)
            {
                return MatchingErrors.Session.NotFound;
            }

            var acceptResult = matchingSession.AcceptMatch(request.DriverId);
            if (acceptResult.IsError)
            {
                return acceptResult.Errors;
            }

            var userId = request.DriverId.ToUserId();
            var driverStatus = await db.DriverStatuses
                .FirstOrDefaultAsync(d => d.UserId == userId, cancellationToken);

            if (driverStatus == null)
            {
                return DriverErrors.DriverNotFound;
            }

            var tripRequest = matchingSession.TripRequest;

            var trip = new Trip(
                driverStatus.GetDriverId(),
                tripRequest.RiderId,
                tripRequest.PickupLocation, tripRequest.DropoffLocation,
                tripRequest.PickupAddress, tripRequest.DropoffAddress,
                tripRequest.FinalFare
            );

            db.Trips.Add(trip);

            // Save to get the id
            await db.SaveChangesAsync(cancellationToken);

            var result = driverStatus.StartTrip(trip.Id);
            if (result.IsError)
            {
                errors = result.Errors;
                throw new Exception();
            }

            var tripRoute = new TripRoute(trip.Id);
            db.TripRoutes.Add(tripRoute);

            result = tripRequest.MarkAsMatched(driverStatus.GetDriverId());
            if (result.IsError)
            {
                errors = result.Errors;
                throw new Exception();
            }

            await db.SaveChangesAsync(cancellationToken);

            await publisher.Publish(trip.CreateTripCreatedEvent(), cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new AcceptMatchResult(trip.Id, "Trip created successfully", trip.AssignedAt);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return errors;
        }
    }
}
