using BenhaScooters.Data;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.TripRequests.Enums;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Application.Features.Trips.Queries.Common;
using BenhaScooters.Infrastructure.S3;
using BenhaScooters.Infrastructure.Trips.Services;

namespace BenhaScooters.Application.Features.Riders.Queries;

public record GetRiderStatusQuery(UserId RiderId) : IRequest<ErrorOr<GetRiderStatusResult>>;

public record GetRiderStatusResult(
    string Status, // "idle" | "in_trip" | "requesting"
    int? TripId,
    int? TripRequestId,
    object? Details);

// Details objects for each status
public record IdleDetails(
    string Message);

public record InTripDetails(
    TripId TripId,
    TripStatus Status,
    string PickupAddress,
    string DropoffAddress,
    decimal EstimatedFare,
    double EstimatedArrivalMinutes,
    DateTime AssignedAt,
    DateTime? DriverArrivedAt,
    DateTime? StartedAt,
    DriverInfo? Driver);

public record RequestingDetails(
    int TripRequestId,
    TripRequestStatus Status,
    string PickupAddress,
    string DropoffAddress,
    decimal EstimatedFare,
    double EstimatedDistance,
    double EstimatedDuration,
    DateTime RequestedAt,
    DateTime ExpiresAt,
    DateTime? ConfirmedAt,
    DateTime? MatchedAt,
    string? MatchedDriverName);

public class GetRiderStatusQueryHandler : IRequestHandler<GetRiderStatusQuery, ErrorOr<GetRiderStatusResult>>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3Service;

    public GetRiderStatusQueryHandler(AppDbContext db, IS3Service s3Service)
    {
        _db = db;
        _s3Service = s3Service;
    }

    public async Task<ErrorOr<GetRiderStatusResult>> Handle(GetRiderStatusQuery request, CancellationToken cancellationToken)
    {
        // Active trip statuses: Assigned, DriverArrived, InProgress
        var activeStatuses = new[] { TripStatus.Assigned, TripStatus.DriverArrived, TripStatus.InProgress };

        var tripResult = await _db.Trips
            .AsNoTracking()
            .Where(t => (t.RiderId == request.RiderId)
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
                t.DropoffLocation,
                RiderName = t.RiderProfile.PreferredName ?? t.RiderProfile.User.Name,
                RiderPhoneNumber = t.RiderProfile.User.PhoneNumber,
                DriverId = t.DriverId,
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




        if (tripResult is not null)
        {
            var driverPhotoUrl = tripResult.DriverPhotoUrl != null
                ? await _s3Service.GetPreSignedUrlAsync(tripResult.DriverPhotoUrl, TimeSpan.FromHours(1), cancellationToken)
                : null;

            var driverLocation = await _db.DriverLocations
                .AsNoTracking()
                .Where(dl => dl.UserId == tripResult.DriverId)
                .Select(dl => new
                {
                    dl.Location
                })
                .FirstOrDefaultAsync(cancellationToken);
            
            var distance = GeoUtils.CalculateDistance(driverLocation!.Location, tripResult.DropoffLocation); // Rider location is not stored here

            var details = new InTripDetails(
                tripResult.Id,
                tripResult.Status,
                tripResult.PickupAddress ?? "Unknown",
                tripResult.DropoffAddress ?? "Unknown",
                tripResult.FinalFare.Amount,
                distance / 1000 / 0.5, // Assuming average speed of 30 km/h
                tripResult.AssignedAt,
                tripResult.DriverArrivedAt,
                tripResult.StartedAt,
                new DriverInfo(
                    tripResult.DriverName,
                    tripResult.DriverPhoneNumber!,
                    driverPhotoUrl,
                    tripResult.DriverVehicleModel,
                    tripResult.DriverVehicleBrand,
                    tripResult.DriverVehicleColor,
                    tripResult.DriverVehicleLicensePlate)
            );

            return new GetRiderStatusResult(
                "in_trip",
                tripResult.Id,
                null,
                details);
        }

        // Active trip request statuses: NotConfirmed, Pending
        var activeRequestStatuses = new[] { TripRequestStatus.Pending };

        var tripRequestResult = await _db.TripRequests
            .AsNoTracking()
            .Where(tr => tr.RiderId == request.RiderId
                      && activeRequestStatuses.Contains(tr.Status)
                      && tr.ExpiresAt > DateTime.UtcNow)
            .Select(tr => new
            {
                tr.Id,
                tr.Status,
                tr.PickupAddress,
                tr.DropoffAddress,
                tr.FinalFare,
                tr.RequestedAt,
                tr.ExpiresAt,
                tr.ConfirmedAt,
                tr.MatchedAt,
                MatchedDriverName = tr.MatchedDriverProfile != null
                    ? tr.MatchedDriverProfile.PersonalInfo!.FullName
                    : null
            })
            .OrderByDescending(tr => tr.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (tripRequestResult is not null)
        {
            var details = new RequestingDetails(
                tripRequestResult.Id,
                tripRequestResult.Status,
                tripRequestResult.PickupAddress ?? "Unknown pickup location",
                tripRequestResult.DropoffAddress ?? "Unknown dropoff location",
                tripRequestResult.FinalFare.Amount,
                tripRequestResult.FinalFare.Distance, // TODO: Future - Update DTO to use Distance value object
                tripRequestResult.FinalFare.Time, // TODO: Future - Update DTO to use Duration value object
                tripRequestResult.RequestedAt,
                tripRequestResult.ExpiresAt,
                tripRequestResult.ConfirmedAt,
                tripRequestResult.MatchedAt,
                tripRequestResult.MatchedDriverName);

            return new GetRiderStatusResult(
                "requesting",
                null,
                tripRequestResult.Id,
                details);
        }


        return new GetRiderStatusResult(
            "idle",
            null,
            null,
            null);
    }
}
