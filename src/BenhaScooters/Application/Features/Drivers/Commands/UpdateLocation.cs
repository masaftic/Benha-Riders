using BenhaScooters.Application.Abstractions;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Notifications;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Application.Features.Drivers.Commands;

public record UpdateLocationCommand(
    UserId DriverId,
    double Latitude,
    double Longitude) : IRequest<ErrorOr<UpdateLocationResponse>>;


public record UpdateLocationResponse(
    double Latitude,
    double Longitude);


public class UpdateLocationCommandValidator : AbstractValidator<UpdateLocationCommand>
{
    public UpdateLocationCommandValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
    }
}


public class UpdateLocationCommandHandler : IRequestHandler<UpdateLocationCommand, ErrorOr<UpdateLocationResponse>>
{
    private readonly AppDbContext _db;
    private readonly IHubContext<RiderHub, IRiderNotifications> _riderHub;

    public UpdateLocationCommandHandler(
        AppDbContext db,
        IHubContext<RiderHub, IRiderNotifications> riderHub)
    {
        _db = db;
        _riderHub = riderHub;
    }

    public async Task<ErrorOr<UpdateLocationResponse>> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var location = geometryFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));

        var userId = request.DriverId;
        var driverLocation = new DriverLocation(userId, location);

        _db.DriverLocations.Update(driverLocation);

        var driverStatus = await _db.DriverStatuses
            .Where(ds => ds.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        TripId? tripId = null;
        UserId? riderId = null;
        Point? pickupLocation = null;

        if (driverStatus?.Status == DriverAvailabilityStatus.OnTrip && driverStatus.CurrentTripId.HasValue)
        {
            tripId = driverStatus.CurrentTripId;

            var gpsPoint = new TripGpsPoint(
                tripId.Value,
                location,
                DateTime.UtcNow);

            _db.TripGpsPoints.Add(gpsPoint);

            // Get trip details for rider notification
            var trip = await _db.Trips
                .Where(t => t.Id == tripId.Value)
                .Select(t => new { t.RiderId, t.PickupLocation })
                .FirstOrDefaultAsync(cancellationToken);

            if (trip != null)
            {
                riderId = trip.RiderId;
                pickupLocation = trip.PickupLocation;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Broadcast location to rider if driver is on trip
        if (tripId.HasValue && riderId.HasValue && pickupLocation != null)
        {
            var driverPoint = geometryFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));
            var distanceMeters = driverPoint.Distance(pickupLocation);
            var estimatedArrivalMinutes = EstimateArrivalTime(distanceMeters) / 60.0;

            var locationUpdate = new DriverLocationUpdate(
                Latitude: request.Latitude,
                Longitude: request.Longitude,
                Timestamp: DateTime.UtcNow,
                EstimatedArrivalMinutes: estimatedArrivalMinutes);

            var riderIdString = riderId.ToString();
            await _riderHub.Clients.Group(riderIdString)
                .NotifyDriverLocationUpdateAsync(riderIdString, locationUpdate);
        }

        return new UpdateLocationResponse(request.Latitude, request.Longitude);
    }

    private static double EstimateArrivalTime(double distanceMeters)
    {
        // Simple estimation: assume 30 km/h average speed in city
        const double averageSpeedKmh = 30.0;
        const double averageSpeedMs = averageSpeedKmh * 1000.0 / 3600.0; // m/s

        return distanceMeters / averageSpeedMs; // seconds
    }
}
