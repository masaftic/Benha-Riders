using BenhaScooters.Data;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Features.Trips.Events;
using BenhaScooters.Infrastructure.Trips.Services;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Features.Trips;

public record RequestTripRequest(
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude,
    string? PickupAddress = null,
    string? DropoffAddress = null
);

public class RequestTripRequestValidator : Validator<RequestTripRequest>
{
    public RequestTripRequestValidator()
    {
        RuleFor(x => x.PickupLatitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Pickup latitude must be between -90 and 90");

        RuleFor(x => x.PickupLongitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Pickup longitude must be between -180 and 180");

        RuleFor(x => x.DropoffLatitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Dropoff latitude must be between -90 and 90");

        RuleFor(x => x.DropoffLongitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Dropoff longitude must be between -180 and 180");

        RuleFor(x => x.PickupAddress)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.PickupAddress));

        RuleFor(x => x.DropoffAddress)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.DropoffAddress));
    }
}

public record RequestTripResponse(
    TripRequestId TripRequestId,
    decimal EstimatedFare,
    double EstimatedDistance,
    double EstimatedDuration,
    DateTime RequestedAt
);

public class RequestTripEndpoint(AppDbContext db, IFareEstimator fareEstimator) : Endpoint<RequestTripRequest, RequestTripResponse>
{
    public override void Configure()
    {
        Post("/trips/request");
        Claims(JwtClaims.Sub);
        Roles("Rider");
        Description(x => x
            .WithSummary("Request a new trip")
            .WithTags("Trips")
            .Produces<RequestTripResponse>()
            .Produces(400)
            .Produces(401)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Request a new trip";
            s.Description = "Creates a new trip request with pickup and dropoff locations. Calculates estimated fare and distance using the Haversine formula.";
            s.ExampleRequest = new RequestTripRequest(40.7128, -74.0060, 40.7589, -73.9851, "123 Main St, New York, NY", "456 Broadway, New York, NY");
        });
    }

    public override async Task HandleAsync(RequestTripRequest request, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        // Get rider profile
        var riderId = await db.Riders
            .Where(r => r.UserId == userId)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        if (!riderId.IsInitialized())
        {
            ThrowError("Rider profile not found", 404);
        }


        // Create location points
        var pickupLocation = new Point(request.PickupLongitude, request.PickupLatitude) { SRID = 4326 };
        var dropoffLocation = new Point(request.DropoffLongitude, request.DropoffLatitude) { SRID = 4326 };

        // Estimate fare
        var fareEstimate = await fareEstimator.EstimateFareAsync(pickupLocation, dropoffLocation, ct);

        // Create trip request
        var tripRequest = new TripRequest(
            riderId,
            pickupLocation,
            dropoffLocation,
            request.PickupAddress,
            request.DropoffAddress,
            fareEstimate);

        await db.TripRequests.AddAsync(tripRequest, ct);
        await db.SaveChangesAsync(ct);

        await SendAsync(new RequestTripResponse(
            tripRequest.Id,
            fareEstimate.Amount,
            fareEstimate.Distance,
            fareEstimate.Time,
            tripRequest.RequestedAt
        ), cancellation: ct);

        await PublishAsync(new TripRequested(tripRequest.Id, riderId), waitMode: Mode.WaitForNone, cancellation: ct);
    }
}