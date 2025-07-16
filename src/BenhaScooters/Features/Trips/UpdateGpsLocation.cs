using BenhaScooters.Data;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite;

namespace BenhaScooters.Features.Trips;

public record UpdateGpsLocationRequest(
    TripId TripId,
    double Latitude,
    double Longitude,
    double Heading,
    double Speed
);

public class UpdateGpsLocationRequestValidator : Validator<UpdateGpsLocationRequest>
{
    public UpdateGpsLocationRequestValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage("Trip ID is required");
            
        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .WithMessage("Latitude must be between -90 and 90");
            
        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .WithMessage("Longitude must be between -180 and 180");
            
        RuleFor(x => x.Heading)
            .InclusiveBetween(0, 360)
            .WithMessage("Heading must be between 0 and 360 degrees");
            
        RuleFor(x => x.Speed)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Speed cannot be negative");
    }
}

public record UpdateGpsLocationResponse(
    TripId TripId,
    string Message,
    DateTime Timestamp,
    int TotalGpsPoints
);

public class UpdateGpsLocationEndpoint(AppDbContext db) : Endpoint<UpdateGpsLocationRequest, UpdateGpsLocationResponse>
{
    public override void Configure()
    {
        Post("/trips/{tripId}/gps-location");
        Claims(JwtClaims.Sub);
        Roles("Driver");
        Description(x => x
            .WithSummary("Update GPS location during trip")
            .WithTags("Trips")
            .Produces<UpdateGpsLocationResponse>()
            .Produces(400)
            .Produces(401)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Update GPS location";
            s.Description = "Allows drivers to send GPS location updates during an active trip. This builds the trip route in real-time.";
            s.ExampleRequest = new UpdateGpsLocationRequest(
                TripId.From(123), 
                30.0444, 
                31.2357, 
                45.5, 
                25.0
            );
        });
    }

    public override async Task HandleAsync(UpdateGpsLocationRequest req, CancellationToken ct)
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

        // Find the trip and verify it's in progress
        var trip = await db.Trips
            .Include(t => t.TripRoute)
            .FirstOrDefaultAsync(t => t.Id == req.TripId && t.DriverId == driverId, ct);

        if (trip == null)
        {
            ThrowError("Trip not found or you are not the assigned driver", 404);
        }

        if (trip.Status != TripStatus.InProgress)
        {
            ThrowError("Can only update GPS location for trips in progress", 400);
        }

        // Create GPS point
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var location = geometryFactory.CreatePoint(new Coordinate(req.Longitude, req.Latitude));
        
        var gpsPoint = new TripGpsPoint(
            trip.Id,
            driverId,
            location,
            req.Heading,
            req.Speed,
            DateTime.UtcNow
        );

        db.TripGpsPoints.Add(gpsPoint);

        // Update or create trip route
        UpdateTripRoute(trip, location);

        await db.SaveChangesAsync(ct);

        // Get total GPS points count for this trip
        var totalGpsPoints = await db.TripGpsPoints
            .CountAsync(gp => gp.TripId == trip.Id, ct);

        await SendAsync(new UpdateGpsLocationResponse(
            trip.Id,
            "GPS location updated successfully",
            gpsPoint.Timestamp,
            totalGpsPoints
        ), cancellation: ct);
    }

    private void UpdateTripRoute(Trip trip, Point newLocation)
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

        if (trip.TripRoute == null)
        {
            // Create new trip route with the first point
            var initialRoute = new TripRoute(trip.Id);
            
            // Create a line with just the pickup location and current location
            var coordinates = new[]
            {
                trip.PickupLocation.Coordinate,
                newLocation.Coordinate
            };
            
            var lineString = geometryFactory.CreateLineString(coordinates);
            var duration = trip.StartedAt.HasValue 
                ? DateTime.UtcNow - trip.StartedAt.Value 
                : TimeSpan.Zero;
                
            initialRoute.SetPath(lineString, duration);
            db.TripRoutes.Add(initialRoute);
        }
        else
        {
            var existingCoordinates = trip.TripRoute.Path.Coordinates.ToList();
            existingCoordinates.Add(newLocation.Coordinate);

            var updatedLineString = geometryFactory.CreateLineString(existingCoordinates.ToArray());
            var duration = trip.StartedAt.HasValue 
                ? DateTime.UtcNow - trip.StartedAt.Value 
                : TimeSpan.Zero;
                
            trip.TripRoute.SetPath(updatedLineString, duration);
        }
    }
}
