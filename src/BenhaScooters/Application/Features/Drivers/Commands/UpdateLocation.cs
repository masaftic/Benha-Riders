using BenhaScooters.Application.Abstractions;
using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
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
    private readonly IGeoService _geoService;
    private readonly ILogger<UpdateLocationCommandHandler> _logger;

    public UpdateLocationCommandHandler(
        AppDbContext db,
        IHubContext<RiderHub, IRiderNotifications> riderHub,
        IGeoService geoService,
        ILogger<UpdateLocationCommandHandler> logger)
    {
        _db = db;
        _riderHub = riderHub;
        _geoService = geoService;
        _logger = logger;
    }

    public async Task<ErrorOr<UpdateLocationResponse>> Handle(UpdateLocationCommand request, CancellationToken cancellationToken)
    {
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var location = geometryFactory.CreatePoint(new Coordinate(request.Longitude, request.Latitude));

        var userId = request.DriverId;
        var now = DateTime.UtcNow;

        // Atomic upsert for driver location using ExecuteUpdateAsync (bypasses change tracker, no concurrency issues)
        var rowsUpdated = await _db.DriverLocations
            .Where(dl => dl.UserId == userId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(dl => dl.Location, location)
                .SetProperty(dl => dl.Timestamp, now), cancellationToken);

        if (rowsUpdated == 0)
        {
            // First location update for this driver — insert with conflict handling
            try
            {
                _db.DriverLocations.Add(new DriverLocation(userId, location));
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Another concurrent request inserted first — just update
                _db.ChangeTracker.Clear();
                await _db.DriverLocations
                    .Where(dl => dl.UserId == userId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(dl => dl.Location, location)
                        .SetProperty(dl => dl.Timestamp, now), cancellationToken);
            }
        }

        var driverStatus = await _db.DriverStatuses
            .AsNoTracking()
            .Where(ds => ds.UserId == userId)
            .FirstOrDefaultAsync(cancellationToken);

        TripId? tripId = null;
        UserId? riderId = null;
        Point? pickupLocation = null;
        Point? dropoffLocation = null;
        TripStatus? tripStatus = null;

        if (driverStatus?.Status == DriverAvailabilityStatus.OnTrip && driverStatus.CurrentTripId.HasValue)
        {
            tripId = driverStatus.CurrentTripId;

            var gpsPoint = new TripGpsPoint(
                tripId.Value,
                location,
                now);

            _db.TripGpsPoints.Add(gpsPoint);

            // Get trip details for rider notification
            var trip = await _db.Trips
                .AsNoTracking()
                .Where(t => t.Id == tripId.Value)
                .Select(t => new { t.RiderId, t.PickupLocation, t.DropoffLocation, t.Status })
                .FirstOrDefaultAsync(cancellationToken);

            if (trip != null)
            {
                riderId = trip.RiderId;
                pickupLocation = trip.PickupLocation;
                dropoffLocation = trip.DropoffLocation;
                tripStatus = trip.Status;
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        // Broadcast location to rider if driver is on trip
        if (tripId.HasValue && riderId.HasValue && pickupLocation != null && dropoffLocation != null && tripStatus.HasValue && tripStatus == TripStatus.Assigned)
        {
            // Calculate ETA to pickup (if not started) or dropoff (if in progress)
            var targetLocation = tripStatus.Value == TripStatus.InProgress 
                ? dropoffLocation 
                : pickupLocation;
            
            var distance = _geoService.CalculateDistance(location, targetLocation);
            var arrivalDuration = _geoService.EstimateArrivalTime(distance);
            var estimatedArrivalMinutes = arrivalDuration.ToMinutes();

            var locationUpdate = new DriverLocationUpdate(
                Latitude: request.Latitude,
                Longitude: request.Longitude,
                Timestamp: DateTime.UtcNow,
                EstimatedArrivalMinutes: estimatedArrivalMinutes);

            var riderIdString = riderId.ToString();
            await _riderHub.Clients.Group(riderIdString)
                .NotifyDriverLocationUpdate(riderIdString, locationUpdate);
            
            _logger.LogInformation("Sent location update to rider {RiderId} for driver {DriverId}: ({Latitude}, {Longitude}), ETA: {ETA} minutes",
                riderId, userId, request.Latitude, request.Longitude, estimatedArrivalMinutes);
        }

        return new UpdateLocationResponse(request.Latitude, request.Longitude);
    }
}
