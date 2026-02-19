using BenhaScooters.Application.Features.Trips.Queries.Common;
using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.S3;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Application.Features.Trips.Queries;

public record GetCurrentTripQuery(UserId UserId) : IRequest<ErrorOr<GetCurrentTripResult>>;

public record GetCurrentTripResult(
    TripId TripId,
    TripStatus Status,
    string PickupAddress,
    string DropoffAddress,
    Coordinate PickupLocation,
    Coordinate DropoffLocation,
    decimal EstimatedFare,
    DateTime CreatedAt,
    DateTime? DriverArrivedAt,
    DateTime? StartedAt,
    RiderInfo? Rider,
    DriverInfo? Driver,
    double EstimatedDuration,
    double EstimatedDistance,
    double? DistanceToPickup,
    double? EstimatedArrivalTime);



public class GetCurrentTripQueryHandler : IRequestHandler<GetCurrentTripQuery, ErrorOr<GetCurrentTripResult>>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3Service;
    private readonly IGeoService _geoService;

    public GetCurrentTripQueryHandler(AppDbContext db, IS3Service s3Service, IGeoService geoService)
    {
        _db = db;
        _s3Service = s3Service;
        _geoService = geoService;
    }

    public async Task<ErrorOr<GetCurrentTripResult>> Handle(GetCurrentTripQuery request, CancellationToken cancellationToken)
    {
        // Active trip statuses: Assigned, DriverArrived, InProgress
        var activeStatuses = new[] { TripStatus.Assigned, TripStatus.DriverArrived, TripStatus.InProgress };

        var tripResult = await _db.Trips
            .AsNoTracking()
            .Where(t => (t.DriverId == request.UserId || t.RiderId == request.UserId)
                     && activeStatuses.Contains(t.Status))
            .Select(t => new
            {
                t.Id,
                t.Status,
                t.PickupAddress,
                t.DropoffAddress,
                t.FinalFare,
                t.AssignedAt,
                t.DriverArrivedAt,
                t.StartedAt,
                t.PickupLocation,
                t.DropoffLocation,
                t.DriverId,
                RiderName = t.RiderProfile.PreferredName ?? t.RiderProfile.User.Name,
                RiderPhoneNumber = t.RiderProfile.User.PhoneNumber,
                DriverName = t.DriverProfile.PersonalInfo!.FullName,
                DriverPhoneNumber = t.DriverProfile.User.PhoneNumber,
                DriverPhotoUrl = t.DriverProfile.Documents.FirstOrDefault(d => d.Type == Domain.Drivers.DocumentType.DriverPhoto)!.ImageUrl,
                DriverVehicleBrand = t.DriverProfile.Vehicle!.Brand,
                DriverVehicleColor = t.DriverProfile.Vehicle!.Color,
                DriverVehicleModel = t.DriverProfile.Vehicle!.Model,
                DriverVehicleLicensePlate = t.DriverProfile.Vehicle!.LicensePlate
            })
            .OrderByDescending(t => t.AssignedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (tripResult is null)
        {
            return TripErrors.Trip.NotFound;
        }

        var driverPhotoUrl = tripResult.DriverPhotoUrl != null
            ? await _s3Service.GetPreSignedUrlAsync(tripResult.DriverPhotoUrl, TimeSpan.FromHours(1), cancellationToken)
            : null;

        // Calculate distance and duration
        var distance = _geoService.CalculateDistance(tripResult.PickupLocation, tripResult.DropoffLocation);
        var duration = _geoService.EstimateArrivalTime(distance);

        // Calculate distance to pickup and estimated arrival time (only if trip not started yet)
        double? distanceToPickup = null;
        double? estimatedArrivalTime = null;
        
        if (tripResult.Status == TripStatus.Assigned || tripResult.Status == TripStatus.DriverArrived)
        {
            var driverLocation = await _db.DriverLocations
                .AsNoTracking()
                .Where(dl => dl.UserId == tripResult.DriverId)
                .Select(dl => dl.Location)
                .FirstOrDefaultAsync(cancellationToken);

            if (driverLocation != null)
            {
                var distanceToPickupObj = _geoService.CalculateDistance(driverLocation, tripResult.PickupLocation);
                distanceToPickup = distanceToPickupObj.ToKilometers();
                
                var arrivalDuration = _geoService.EstimateArrivalTime(distanceToPickupObj);
                estimatedArrivalTime = arrivalDuration.ToMinutes();
            }
        }

        return new GetCurrentTripResult(
            tripResult.Id,
            tripResult.Status,
            tripResult.PickupAddress ?? "Unknown pickup location",
            tripResult.DropoffAddress ?? "Unknown dropoff location",
            Coordinate.FromPoint(tripResult.PickupLocation),
            Coordinate.FromPoint(tripResult.DropoffLocation),
            tripResult.FinalFare.Amount,
            tripResult.AssignedAt,
            tripResult.DriverArrivedAt,
            tripResult.StartedAt,
            new RiderInfo(
                tripResult.RiderName,
                tripResult.RiderPhoneNumber!),
            new DriverInfo(
                tripResult.DriverName,
                tripResult.DriverPhoneNumber!,
                driverPhotoUrl,
                tripResult.DriverVehicleModel,
                tripResult.DriverVehicleBrand,
                tripResult.DriverVehicleColor,
                tripResult.DriverVehicleLicensePlate),
            duration.ToMinutes(),
            distance.ToKilometers(),
            distanceToPickup,
            estimatedArrivalTime);
    }
}
