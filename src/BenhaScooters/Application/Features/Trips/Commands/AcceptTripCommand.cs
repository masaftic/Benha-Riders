using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Trips.Events;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Commands;

// Deprecated
public record AcceptTripCommand(
    DriverId DriverId,
    TripRequestId TripRequestId) : IRequest<ErrorOr<AcceptTripResult>>;

public record AcceptTripResult(
    TripId TripId,
    string Message,
    DateTime AcceptedAt);

public class AcceptTripCommandValidator : AbstractValidator<AcceptTripCommand>
{
    public AcceptTripCommandValidator()
    {
        RuleFor(x => x.DriverId.Value)
            .NotEmpty()
            .WithMessage("Driver ID is required");

        RuleFor(x => x.TripRequestId.Value)
            .NotEmpty()
            .WithMessage("Trip request ID is required");
    }
}

public class AcceptTripCommandHandler(AppDbContext db) : IRequestHandler<AcceptTripCommand, ErrorOr<AcceptTripResult>>
{
    public async Task<ErrorOr<AcceptTripResult>> Handle(AcceptTripCommand request, CancellationToken cancellationToken)
    {
        // Use a transaction to ensure consistency
        using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Check driver availability first
            var driverAvailability = await db.DriverAvailabilities
                .FirstOrDefaultAsync(x => x.DriverId == request.DriverId, cancellationToken);

            if (driverAvailability == null || driverAvailability.Status != DriverStatus.Online)
            {
                return TripErrors.Driver.NotAvailable;
            }

            // Find the matching session for this trip request
            var matchingSession = await db.MatchingSessions
                .Include(ms => ms.TripRequest)
                .FirstOrDefaultAsync(ms => ms.TripRequestId == request.TripRequestId, cancellationToken);

            if (matchingSession == null)
            {
                return MatchingErrors.Session.NotFound;
            }

            if (!matchingSession.IsActive)
            {
                return MatchingErrors.Session.NotActive;
            }

            var tripRequest = matchingSession.TripRequest;

            var acceptResult = tripRequest.MarkAsMatched(request.DriverId);
            if (acceptResult.IsError)
            {
                return acceptResult.Errors;
            }

            // Complete the matching session
            var completeResult = matchingSession.Complete();
            if (completeResult.IsError)
            {
                return completeResult.Errors;
            }

            // Create trip entity
            var trip = new Trip(
                request.DriverId,
                tripRequest.RiderId,
                tripRequest.PickupLocation,
                tripRequest.DropoffLocation,
                tripRequest.PickupAddress,
                tripRequest.DropoffAddress,
                tripRequest.EstimatedFare);

            // Add trip to context
            db.Trips.Add(trip);

            // Save changes to get the trip id
            await db.SaveChangesAsync(cancellationToken);

            // Update driver availability to OnTrip
            var startTripResult = driverAvailability.StartTrip(trip.Id);
            if (startTripResult.IsError)
            {
                return startTripResult.Errors;
            }

            await db.SaveChangesAsync(cancellationToken);

            // Publish domain events after successful save
            trip.CreateTripCreatedEvent();

            // Commit the transaction
            await transaction.CommitAsync(cancellationToken);

            return new AcceptTripResult(
                trip.Id,
                "Trip accepted and created successfully",
                DateTime.UtcNow);
        }
        catch (Exception)
        {
            // Transaction will be automatically rolled back
            throw;
        }
    }
}
