using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Application.Features.Trips.Commands;

public record UpdateGpsLocationCommand(
    DriverId DriverId,
    TripId TripId,
    double Latitude,
    double Longitude,
    double Heading,
    double Speed) : IRequest<ErrorOr<UpdateGpsLocationResult>>;

public record UpdateGpsLocationResult(
    TripId TripId,
    string Message,
    DateTime Timestamp,
    int TotalGpsPoints);

public class UpdateGpsLocationCommandValidator : AbstractValidator<UpdateGpsLocationCommand>
{
    public UpdateGpsLocationCommandValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty()
            .WithMessage("Driver ID is required");

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

public class UpdateGpsLocationCommandHandler(AppDbContext db) : IRequestHandler<UpdateGpsLocationCommand, ErrorOr<UpdateGpsLocationResult>>
{
    public async Task<ErrorOr<UpdateGpsLocationResult>> Handle(UpdateGpsLocationCommand request, CancellationToken cancellationToken)
    {
        // Validate coordinates
        if (!IsValidCoordinate(request.Latitude, request.Longitude))
        {
            return TripErrors.Trip.InvalidGpsCoordinates;
        }

        // Find the trip and verify it's in progress
        var trip = await db.Trips
            .Include(t => t.TripRoute)
            .FirstOrDefaultAsync(t => t.Id == request.TripId && t.DriverId == request.DriverId, cancellationToken);

        if (trip == null)
        {
            return TripErrors.Trip.NotFound;
        }

        if (trip.Status != TripStatus.InProgress)
        {
            return TripErrors.Trip.NotInProgress;
        }

        // Create GPS point
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var location = geometryFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));
        
        var gpsPoint = new TripGpsPoint(
            trip.Id,
            request.DriverId,
            location,
            request.Heading,
            request.Speed,
            DateTime.UtcNow
        );

        db.TripGpsPoints.Add(gpsPoint);

        // Update or create trip route
        UpdateTripRoute(trip, location);

        await db.SaveChangesAsync(cancellationToken);

        // Get total GPS points count for this trip
        var totalGpsPoints = await db.TripGpsPoints
            .CountAsync(gp => gp.TripId == trip.Id, cancellationToken);

        return new UpdateGpsLocationResult(
            trip.Id,
            "GPS location updated successfully",
            gpsPoint.Timestamp,
            totalGpsPoints);
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

    private static bool IsValidCoordinate(double latitude, double longitude)
    {
        return latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;
    }
}
