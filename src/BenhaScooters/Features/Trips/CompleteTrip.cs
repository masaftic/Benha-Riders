using BenhaScooters.Data;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

public record CompleteTripRequest(TripId TripId, double FinalLatitude, double FinalLongitude);

public class CompleteTripRequestValidator : Validator<CompleteTripRequest>
{
    public CompleteTripRequestValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("Trip ID is required");
            
        RuleFor(x => x.FinalLatitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Final latitude must be between -90 and 90");
            
        RuleFor(x => x.FinalLongitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Final longitude must be between -180 and 180");
    }
}

public record CompleteTripResponse(
    TripId TripId,
    string Message,
    DateTime CompletedAt,
    TimeSpan? TotalDuration
);

public class CompleteTripEndpoint(AppDbContext db) : Endpoint<CompleteTripRequest, CompleteTripResponse>
{
    public override void Configure()
    {
        Post("/trips/{tripId}/complete");
        Claims(JwtClaims.Sub);
        Roles("Driver");
        Description(x => x
            .WithSummary("Complete the trip")
            .WithTags("Trips")
            .Produces<CompleteTripResponse>()
            .Produces(400)
            .Produces(401)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Complete the trip";
            s.Description = "Allows drivers to complete the trip when they reach the destination. Updates driver availability back to available.";
            s.ExampleRequest = new CompleteTripRequest(TripId.From(123), 30.0444, 31.2357);
        });
    }

    public override async Task HandleAsync(CompleteTripRequest req, CancellationToken ct)
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

        // Find the trip
        var trip = await db.Trips
            .FirstOrDefaultAsync(t => t.Id == req.TripId && t.DriverId == driverId, ct);

        if (trip == null)
        {
            ThrowError("Trip not found or you are not the assigned driver", 404);
        }

        // Create final location point
        var finalLocation = new Point(req.FinalLongitude, req.FinalLatitude) { SRID = 4326 };

        // Complete the trip
        trip.CompleteTrip(finalLocation);

        // Update driver availability back to available
        var driverAvailability = await db.DriverAvailabilities
            .FirstOrDefaultAsync(x => x.DriverId == driverId, ct);

        if (driverAvailability != null)
        {
            driverAvailability.CompleteTrip();
        }

        await db.SaveChangesAsync(ct);

        await SendAsync(new CompleteTripResponse(
            trip.Id,
            "Trip completed successfully",
            trip.CompletedAt!.Value,
            trip.TotalDuration
        ), cancellation: ct);
    }
}
