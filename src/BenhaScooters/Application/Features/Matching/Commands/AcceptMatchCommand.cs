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
    TripRequestId TripRequestId) : IRequest<ErrorOr<AcceptMatchResult>>;

public record AcceptMatchResult(TripId TripId, string Message, DateTime AcceptedAt);

public class AcceptMatchCommandValidator : AbstractValidator<AcceptMatchCommand>
{
    public AcceptMatchCommandValidator()
    {
        RuleFor(x => x.DriverId.Value)
            .NotEmpty()
            .WithMessage("Driver ID is required");

        RuleFor(x => x.TripRequestId.Value)
            .NotEmpty()
            .WithMessage("Trip request ID is required");
    }
}

public class AcceptMatchCommandHandler(AppDbContext db) : IRequestHandler<AcceptMatchCommand, ErrorOr<AcceptMatchResult>>
{
    public async Task<ErrorOr<AcceptMatchResult>> Handle(AcceptMatchCommand request, CancellationToken cancellationToken)
    {
        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        List<Error> errors = [];
        try
        {
            // Find the matching session for this trip request
            var matchingSession = await db.MatchingSessions
                .Include(ms => ms.MatchAttempts)
                .Include(ms => ms.TripRequest)
                .FirstOrDefaultAsync(ms => ms.TripRequestId == request.TripRequestId, cancellationToken);

            if (matchingSession == null)
            {
                return MatchingErrors.Session.NotFound;
            }

            var acceptResult = matchingSession.AcceptMatch(request.DriverId);
            if (acceptResult.IsError)
            {
                return acceptResult.Errors;
            }

            var driverAvailability = await db.DriverAvailabilities
                .FirstOrDefaultAsync(d => d.DriverId == request.DriverId, cancellationToken);

            if (driverAvailability == null)
            {
                return DriverErrors.DriverNotFound;
            }

            var tripRequest = matchingSession.TripRequest;

            var trip = new Trip(
                driverAvailability.DriverId,
                tripRequest.RiderId,
                tripRequest.PickupLocation, tripRequest.DropoffLocation,
                tripRequest.PickupAddress, tripRequest.DropoffAddress,
                tripRequest.EstimatedFare
            );

            await db.SaveChangesAsync(cancellationToken);

            var result = driverAvailability.StartTrip(trip.Id);
            if (result.IsError)
            {
                errors = result.Errors;
                throw new Exception();
            }

            result = tripRequest.MarkAsMatched(driverAvailability.DriverId);
            if (result.IsError)
            {
                errors = result.Errors;
                throw new Exception();
            }

            await db.SaveChangesAsync(cancellationToken);

            trip.PublishTripCreatedEvent();

            await transaction.CommitAsync(cancellationToken);

            return new AcceptMatchResult(trip.Id, "Trip created successfully", trip.CreatedAt);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            return errors;
        }
    }
}
