using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Features.Drivers.Common;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Drivers;

public record SetDriverAvailabilityRequest(
    DriverStatus Status
);

public class SetDriverAvailabilityRequestValidator : Validator<SetDriverAvailabilityRequest>
{
    public SetDriverAvailabilityRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum()
            .WithMessage("Invalid driver status");
    }
}

public record SetDriverAvailabilityResponse(
    DriverStatus Status,
    DateTime LastStatusChange,
    string Message
);

public class SetDriverAvailabilityEndpoint(AppDbContext db) : Endpoint<SetDriverAvailabilityRequest, SetDriverAvailabilityResponse>
{
    public override void Configure()
    {
        Post("/driver/availability");
        Claims(JwtClaims.Sub);
        Roles("Driver");
        PreProcessor<OnboardedProcessor<SetDriverAvailabilityRequest>>();
        Description(x => x
            .WithSummary("Set driver availability status")
            .Produces<SetDriverAvailabilityResponse>()
            .Produces(400)
            .Produces(401)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Set driver availability status";
            s.Description = "Updates the driver's availability status (Online, Offline, Busy). Drivers must be online to receive trip requests.";
            s.ExampleRequest = new SetDriverAvailabilityRequest(DriverStatus.Online);
        });
    }

    public override async Task HandleAsync(SetDriverAvailabilityRequest req, CancellationToken ct)
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

        // Get or create driver availability
        var availability = await db.DriverAvailabilities
            .FirstOrDefaultAsync(da => da.DriverId == driverId, ct);

        if (availability == null)
        {
            availability = new DriverAvailability(driverId);
            await db.DriverAvailabilities.AddAsync(availability, ct);
        }

        // Update driver availability based on requested status
        string message;
        try
        {
            switch (req.Status)
            {
                case DriverStatus.Online:
                    availability.GoOnline();
                    message = "Driver is now online and available";
                    break;

                case DriverStatus.Offline:
                    availability.GoOffline();
                    message = "Driver is now offline";
                    break;

                case DriverStatus.Busy:
                    availability.SetBusy();
                    message = "Driver is busy and not accepting requests";
                    break;

                case DriverStatus.OnTrip:
                    ThrowError("Cannot manually set status to OnTrip. This status is set automatically when a trip starts.", 400);
                    return;

                default:
                    ThrowError("Invalid status transition", 400);
                    return;
            }

            await db.SaveChangesAsync(ct);

            await SendAsync(new SetDriverAvailabilityResponse(
                availability.Status,
                availability.LastStatusChange,
                message
            ), cancellation: ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, 400);
        }
    }
}
