using BenhaScooters.Data;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;


namespace BenhaScooters.Features.Trips;

public record StartTripRequest(TripId TripId);

public class StartTripRequestValidator : Validator<StartTripRequest>
{
    public StartTripRequestValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("Trip ID is required");
    }
}

public record StartTripResponse(
    TripId TripId,
    string Message,
    DateTime StartedAt
);

public class StartTripEndpoint(AppDbContext db) : Endpoint<StartTripRequest, StartTripResponse>
{
    public override void Configure()
    {
        Post("/trips/{tripId}/start");
        Claims(JwtClaims.Sub);
        Roles("Driver");
        Description(x => x
            .WithSummary("Start the trip")
            .WithTags("Trips")
            .Accepts<StartTripRequest>()
            .Produces<StartTripResponse>()
            .Produces(400)
            .Produces(401)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Start the trip";
            s.Description = "Allows drivers to start the trip after arriving at pickup location and picking up the rider.";
            s.ExampleRequest = new StartTripRequest(TripId.From(123));
        });
    }

    public override async Task HandleAsync(StartTripRequest req, CancellationToken ct)
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

        // Start the trip
        trip.StartTrip();

        await db.SaveChangesAsync(ct);

        await SendAsync(new StartTripResponse(
            trip.Id,
            "Trip started successfully",
            trip.StartedAt!.Value
        ), cancellation: ct);
    }
}
