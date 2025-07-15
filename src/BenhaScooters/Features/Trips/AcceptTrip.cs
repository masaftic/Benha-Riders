using BenhaScooters.Data;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Trips;

public record AcceptTripRequest(TripRequestId TripRequestId);

public class AcceptTripRequestValidator : Validator<AcceptTripRequest>
{
    public AcceptTripRequestValidator()
    {
        RuleFor(x => x.TripRequestId)
            .NotEmpty()
            .WithMessage("Trip request ID is required");
    }
}

public record AcceptTripResponse(
    TripId TripId,
    string Message,
    DateTime AcceptedAt
);

public class AcceptTripEndpoint(AppDbContext db) : Endpoint<AcceptTripRequest, AcceptTripResponse>
{
    public override void Configure()
    {
        Post("/trips/accept");
        Claims(JwtClaims.Sub);
        Roles("Driver");
        Description(x => x
            .WithSummary("Accept a trip request")
            .WithTags("Trips")
            .Produces<AcceptTripResponse>()
            .Produces(400)
            .Produces(401)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Accept a trip request";
            s.Description = "Allows drivers to accept a pending trip request. Creates a Trip entity and updates driver availability to OnTrip status.";
            s.ExampleRequest = new AcceptTripRequest(TripRequestId.From(123));
        });
    }

    public override async Task HandleAsync(AcceptTripRequest req, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();
        
        // Get driver profile
        var driverId = await db.Drivers
            .Where(d => d.UserId == userId)
            .Select(d => d.Id)
            .FirstOrDefaultAsync(ct);

        if (!driverId.IsInitialized())
        {
            ThrowError("Driver profile not found", 404);
        }

        // Check driver availability first
        var driverAvailability = await db.DriverAvailabilities
            .FirstOrDefaultAsync(x => x.DriverId == driverId, ct);

        if (driverAvailability == null || !driverAvailability.IsAvailableForRequests)
        {
            ThrowError("Driver is not available for trips", 400);
        }

        // Find the trip request
        var tripRequest = await db.TripRequests
            .FirstOrDefaultAsync(tr => tr.Id == req.TripRequestId, ct);

        if (tripRequest == null)
        {
            ThrowError("Trip request not found", 404);
        }

        // Check if trip is still available
        if (tripRequest.Status != TripRequestStatus.Pending)
        {
            ThrowError("Trip request is no longer available", 400);
        }

        // Accept the trip and create Trip entity in one transaction
        tripRequest.AcceptByDriver(driverId);
        
        var trip = new Trip(
            tripRequest.Id,
            driverId,
            tripRequest.RiderId,
            tripRequest.PickupLocation,
            tripRequest.DropoffLocation,
            tripRequest.PickupAddress,
            tripRequest.DropoffAddress,
            tripRequest.EstimatedFare);

        db.Trips.Add(trip);

        await db.SaveChangesAsync(ct); // To get the trip.Id. TODO: maybe do a transaction here

        // Update driver availability to OnTrip
        driverAvailability.StartTrip(trip.Id);

        await db.SaveChangesAsync(ct);

        await SendAsync(new AcceptTripResponse(
            trip.Id,
            "Trip accepted and created successfully",
            DateTime.UtcNow
        ), cancellation: ct);
    }
}
