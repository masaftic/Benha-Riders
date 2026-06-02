using BenhaScooters.Application.Features.Trips.Queries.Common;
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

namespace BenhaScooters.Application.Features.Trips.Queries;

public record GetTripDetailsByIdQuery(TripId TripId, UserId UserId) : IRequest<ErrorOr<GetTripDetailsByIdResult>>;

public record GetTripDetailsByIdResult(
    TripId TripId,
    TripStatus Status,
    string PickupAddress,
    string DropoffAddress,
    Coordinate PickupLocation,
    Coordinate DropoffLocation,
    decimal Fare,
    double DistanceKm,
    double EstimatedDurationMinutes,
    string? PaymentMethod,
    // string? PaymentStatus,
    DateTime AssignedAt,
    DateTime? DriverArrivedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    double? ActualDurationMinutes,
    RiderInfo Rider,
    DriverInfo Driver,
    int? Rating,
    string? RatingComment);

public class GetTripDetailsByIdQueryHandler : IRequestHandler<GetTripDetailsByIdQuery, ErrorOr<GetTripDetailsByIdResult>>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3Service;

    public GetTripDetailsByIdQueryHandler(AppDbContext db, IS3Service s3Service)
    {
        _db = db;
        _s3Service = s3Service;
    }

    public async Task<ErrorOr<GetTripDetailsByIdResult>> Handle(GetTripDetailsByIdQuery request, CancellationToken cancellationToken)
    {
        var tripResult = await _db.Trips
            .AsNoTracking()
            .Where(t => t.Id == request.TripId && (t.DriverId == request.UserId || t.RiderId == request.UserId))
            .Select(t => new
            {
                t.Id,
                t.Status,
                t.PickupAddress,
                t.DropoffAddress,
                t.PickupLocation,
                t.DropoffLocation,
                t.FinalFare,
                t.TripPayment,
                t.AssignedAt,
                t.DriverArrivedAt,
                t.StartedAt,
                t.CompletedAt,
                t.DriverId,
                RiderName = t.RiderProfile.PreferredName ?? t.RiderProfile.User.Name,
                RiderPhoneNumber = t.RiderProfile.User.PhoneNumber,
                DriverName = t.DriverProfile.PersonalInfo!.FullName,
                DriverPhoneNumber = t.DriverProfile.User.PhoneNumber,
                DriverPhotoUrl = t.DriverProfile.Documents
                    .Where(d => d.Type == DocumentType.DriverPhoto)
                    .Select(d => d.ImageUrl)
                    .FirstOrDefault(),
                DriverVehicleBrand = t.DriverProfile.Vehicle!.Brand,
                DriverVehicleColor = t.DriverProfile.Vehicle!.Color,
                DriverVehicleLicensePlate = t.DriverProfile.Vehicle!.LicensePlate,
                Rating = _db.TripRatings
                    .Where(r => r.TripId == t.Id)
                    .Select(r => new { Rating = r.DriverRating, Comment = r.DriverComment })
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (tripResult is null)
        {
            return AppErrors.Trip.NotFound();
        }

        var driverPhotoUrl = tripResult.DriverPhotoUrl != null
            ? await _s3Service.GetPreSignedUrlAsync(tripResult.DriverPhotoUrl, TimeSpan.FromHours(1), cancellationToken)
            : null;

        var driverRating = await _db.DriverStats
            .Where(ds => ds.UserId == tripResult.DriverId)
            .Select(ds => ds.AverageRating)
            .FirstOrDefaultAsync(cancellationToken);

        double? actualDurationMinutes = tripResult.StartedAt.HasValue && tripResult.CompletedAt.HasValue
            ? (tripResult.CompletedAt.Value - tripResult.StartedAt.Value).TotalMinutes
            : null;

        return new GetTripDetailsByIdResult(
            tripResult.Id,
            tripResult.Status,
            tripResult.PickupAddress ?? "Unknown pickup location",
            tripResult.DropoffAddress ?? "Unknown dropoff location",
            Coordinate.FromPoint(tripResult.PickupLocation),
            Coordinate.FromPoint(tripResult.DropoffLocation),
            tripResult.FinalFare.Amount,
            tripResult.FinalFare.Distance.ToKilometers(),
            tripResult.FinalFare.Time.ToMinutes(),
            tripResult.TripPayment?.Method.ToString(),
            // tripResult.TripPayment?.Status.ToString(),
            tripResult.AssignedAt,
            tripResult.DriverArrivedAt,
            tripResult.StartedAt,
            tripResult.CompletedAt,
            actualDurationMinutes,
            new RiderInfo(
                tripResult.RiderName,
                tripResult.RiderPhoneNumber!),
            new DriverInfo(
                tripResult.DriverName,
                tripResult.DriverPhoneNumber!,
                driverPhotoUrl,
                tripResult.DriverVehicleBrand,
                tripResult.DriverVehicleColor,
                tripResult.DriverVehicleLicensePlate,
                driverRating),
            tripResult.Rating?.Rating,
            tripResult.Rating?.Comment);
    }
}
