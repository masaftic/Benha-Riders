using BenhaScooters.Data;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;


namespace BenhaScooters.Features.Trips;

// Driver Arrived Endpoint
public record DriverArrivedRequest(TripId TripId);

public class DriverArrivedRequestValidator : Validator<DriverArrivedRequest>
{
    public DriverArrivedRequestValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("Trip ID is required");
    }
}

public record DriverArrivedResponse(
    TripId TripId,
    string Message,
    DateTime ArrivedAt
);

public class DriverArrivedEndpoint(AppDbContext db) : Endpoint<DriverArrivedRequest, DriverArrivedResponse>
{
    public override void Configure()
    {
        Post("/trips/{TripId}/driver-arrived");
        Claims(JwtClaims.Sub);
        Roles("Driver");
        Description(x => x
            .WithSummary("Mark driver as arrived at pickup location")
            .WithTags("Trips")
            .Accepts<DriverArrivedRequest>()
            .Produces<DriverArrivedResponse>()
            .Produces(400)
            .Produces(401)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Mark driver as arrived";
            s.Description = "Allows drivers to mark themselves as arrived at the pickup location for an assigned trip.";
            s.ExampleRequest = new DriverArrivedRequest(TripId.From(123));
        });
    }

    public override async Task HandleAsync(DriverArrivedRequest req, CancellationToken ct)
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

        // Mark driver as arrived
        trip.DriverArrived();

        await db.SaveChangesAsync(ct);

        await SendAsync(new DriverArrivedResponse(
            trip.Id,
            "Driver marked as arrived successfully",
            trip.DriverArrivedAt!.Value
        ), cancellation: ct);
    }
}
