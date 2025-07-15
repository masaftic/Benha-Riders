using System.Security.Claims;
using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Features.Drivers.Common;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Features.Drivers;

public record UpdateLocationRequest(
    double Latitude,
    double Longitude,
    double Heading,
    double Speed
);

public class UpdateLocationRequestValidator : Validator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.Heading).InclusiveBetween(0, 360);
        RuleFor(x => x.Speed).GreaterThanOrEqualTo(0);
    }
}


public class UpdateLocationEndpoint(AppDbContext db) : Endpoint<UpdateLocationRequest>
{
    public override void Configure()
    {
        Put("/driver/location");
        PreProcessor<OnboardedProcessor<UpdateLocationRequest>>();
        Roles("Driver");
        Claims(JwtClaims.Sub);
        Description(x => x
            .WithSummary("Update current driver location")
            .Produces(200)
            .Produces(401));

        Summary(s =>
        {
            s.Summary = "Update current driver location";
            s.Description = "Updates the driver's current GPS location, heading, and speed. Used for real-time tracking and trip monitoring.";
            s.ExampleRequest = new UpdateLocationRequest(40.7128, -74.0060, 45.5, 25.0);
        });
    }

    public override async Task HandleAsync(UpdateLocationRequest request, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        var driverId = await db.Drivers
            .Where(dp => dp.UserId == userId)
            .Select(dp => dp.Id)
            .FirstOrDefaultAsync(ct);

        if (!driverId.IsInitialized())
        {
            ThrowError("Driver not found", 404);
        }

        var driverLocation = await db.DriverLocations
            .FirstOrDefaultAsync(dl => dl.DriverId == driverId);

        if (driverLocation is null)
        {
            driverLocation = new DriverLocation(
                driverId,
                new Point(request.Longitude, request.Latitude) { SRID = 4326 },
                request.Heading,
                request.Speed,
                DateTime.UtcNow
            );

            db.DriverLocations.Add(driverLocation);
        }
        else
        {
            driverLocation.UpdateLocation(
                new Point(request.Longitude, request.Latitude) { SRID = 4326 },
                request.Heading,
                request.Speed
            );
        }

        await db.SaveChangesAsync(ct);
        await SendOkAsync(new
        {
            Message = "Location updated successfully",
            Location = new
            {
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Heading = request.Heading,
                Speed = request.Speed
            }
        }, ct);
    }
}
