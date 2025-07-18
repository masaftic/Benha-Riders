using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Commands;

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
        RuleFor(x => x.DriverId)
            .NotEmpty()
            .WithMessage("Driver ID is required");

        RuleFor(x => x.TripRequestId)
            .NotEmpty()
            .WithMessage("Trip request ID is required");
    }
}

public class AcceptTripCommandHandler(AppDbContext db) : IRequestHandler<AcceptTripCommand, ErrorOr<AcceptTripResult>>
{
    public async Task<ErrorOr<AcceptTripResult>> Handle(AcceptTripCommand request, CancellationToken cancellationToken)
    {
        // Check driver availability first
        var driverAvailability = await db.DriverAvailabilities
            .FirstOrDefaultAsync(x => x.DriverId == request.DriverId, cancellationToken);

        if (driverAvailability == null || driverAvailability.Status != DriverStatus.Online)
        {
            return TripErrors.Driver.NotAvailable;
        }

        // Find the trip request
        var tripRequest = await db.TripRequests
            .FirstOrDefaultAsync(tr => tr.Id == request.TripRequestId, cancellationToken);

        if (tripRequest == null)
        {
            return TripErrors.TripRequest.NotFound;
        }

        // Check if trip is still available
        if (tripRequest.Status != TripRequestStatus.Pending)
        {
            return TripErrors.TripRequest.NotPending;
        }

        // Check if trip has expired
        if (tripRequest.ExpiresAt <= DateTime.UtcNow)
        {
            return TripErrors.TripRequest.Expired;
        }

        // Accept the trip and create Trip entity in one transaction
        var acceptResult = tripRequest.AcceptByDriver(request.DriverId);
        if (acceptResult.IsError)
        {
            return acceptResult.Errors;
        }
        
        var trip = new Trip(
            tripRequest.Id,
            request.DriverId,
            tripRequest.RiderId,
            tripRequest.PickupLocation,
            tripRequest.DropoffLocation,
            tripRequest.PickupAddress,
            tripRequest.DropoffAddress,
            tripRequest.EstimatedFare);

        db.Trips.Add(trip);
        await db.SaveChangesAsync(cancellationToken);

        // Update driver availability to OnTrip
        var startTripResult = driverAvailability.StartTrip(trip.Id);
        if (startTripResult.IsError)
        {
            // TODO: Rollback
            return startTripResult.Errors;
        }

        await db.SaveChangesAsync(cancellationToken);

        return new AcceptTripResult(
            trip.Id,
            "Trip accepted and created successfully",
            DateTime.UtcNow);
    }
}
