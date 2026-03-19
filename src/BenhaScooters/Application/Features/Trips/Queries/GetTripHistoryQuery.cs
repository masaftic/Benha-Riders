using BenhaScooters.Application.Features.Trips.Queries.Common;
using BenhaScooters.Application.Services;
using BenhaScooters.Data;
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

public record GetTripHistoryQuery(UserId UserId, int PageNumber = 1, int PageSize = 10) : IRequest<ErrorOr<GetTripHistoryResult>>;

public record GetTripHistoryResult(
    IReadOnlyList<TripHistoryItem> Trips,
    int TotalCount,
    int PageNumber,
    int PageSize,
    bool HasNextPage);

public record TripHistoryItem(
    TripId TripId,
    TripStatus Status,
    string PickupAddress,
    string DropoffAddress,
    Coordinate PickupLocation,
    Coordinate DropoffLocation,
    decimal EstimatedFare,
    decimal? FinalFare,
    decimal? BaseFare,
    decimal? DistanceFare,
    decimal? DurationFare,
    decimal? SurgeMultiplier,
    string? PaymentMethod,
    string? PaymentStatus,
    bool IsPaid,
    DateTime AssignedAt,
    DateTime? DriverArrivedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    double? DurationMinutes,
    double? DistanceKm,
    RiderInfo Rider,
    DriverInfo Driver,
    int? Rating,
    string? RatingComment);

public class GetTripHistoryQueryHandler : IRequestHandler<GetTripHistoryQuery, ErrorOr<GetTripHistoryResult>>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3Service;

    public GetTripHistoryQueryHandler(AppDbContext db, IS3Service s3Service)
    {
        _db = db;
        _s3Service = s3Service;
    }

    public async Task<ErrorOr<GetTripHistoryResult>> Handle(GetTripHistoryQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Trips
            .AsNoTracking()
            .Where(t => t.DriverId == request.UserId || t.RiderId == request.UserId)
            .OrderByDescending(t => t.AssignedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var trips = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new
            {
                t.Id,
                t.Status,
                t.PickupAddress,
                t.DropoffAddress,
                t.PickupLocation,
                t.DropoffLocation,
                t.FinalFare,
                t.TripFare,
                t.TripPayment,
                t.AssignedAt,
                t.DriverArrivedAt,
                t.StartedAt,
                t.CompletedAt,
                RiderName = t.RiderProfile.PreferredName ?? t.RiderProfile.User.Name,
                RiderPhoneNumber = t.RiderProfile.User.PhoneNumber,
                DriverName = t.DriverProfile.PersonalInfo!.FullName,
                DriverPhoneNumber = t.DriverProfile.User.PhoneNumber,
                DriverPhotoUrl = t.DriverProfile.Documents
                    .FirstOrDefault(d => d.Type == DocumentType.DriverPhoto)!.ImageUrl,
                DriverVehicleBrand = t.DriverProfile.Vehicle!.Brand,
                DriverVehicleColor = t.DriverProfile.Vehicle!.Color,
                DriverVehicleModel = t.DriverProfile.Vehicle!.Model,
                DriverVehicleLicensePlate = t.DriverProfile.Vehicle!.LicensePlate,
                Rating = _db.DriverRatings
                    .Where(r => r.TripId == t.Id)
                    .Select(r => new { r.Rating, r.Comment })
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var items = new List<TripHistoryItem>(trips.Count);

        foreach (var t in trips)
        {
            var driverPhotoUrl = t.DriverPhotoUrl != null
                ? await _s3Service.GetPreSignedUrlAsync(t.DriverPhotoUrl, TimeSpan.FromHours(1), cancellationToken)
                : null;

            double? durationMinutes = t.StartedAt.HasValue && t.CompletedAt.HasValue
                ? (t.CompletedAt.Value - t.StartedAt.Value).TotalMinutes
                : null;

            items.Add(new TripHistoryItem(
                t.Id,
                t.Status,
                t.PickupAddress ?? "Unknown pickup location",
                t.DropoffAddress ?? "Unknown dropoff location",
                Coordinate.FromPoint(t.PickupLocation),
                Coordinate.FromPoint(t.DropoffLocation),
                t.FinalFare.Amount,
                t.TripFare?.TotalFare,
                t.TripFare?.BaseFare,
                t.TripFare?.DistanceFare,
                t.TripFare?.DurationFare,
                t.TripFare?.SurgeMultiplier,
                t.TripPayment?.Method.ToString(),
                t.TripPayment?.Status.ToString(),
                t.TripPayment?.IsPaid ?? false,
                t.AssignedAt,
                t.DriverArrivedAt,
                t.StartedAt,
                t.CompletedAt,
                durationMinutes,
                t.FinalFare.Distance.ToKilometers(),
                new RiderInfo(
                    t.RiderName,
                    t.RiderPhoneNumber!),
                new DriverInfo(
                    t.DriverName,
                    t.DriverPhoneNumber!,
                    driverPhotoUrl,
                    t.DriverVehicleModel,
                    t.DriverVehicleBrand,
                    t.DriverVehicleColor,
                    t.DriverVehicleLicensePlate),
                t.Rating?.Rating,
                t.Rating?.Comment));
        }

        var hasNextPage = (request.PageNumber * request.PageSize) < totalCount;

        return new GetTripHistoryResult(
            items,
            totalCount,
            request.PageNumber,
            request.PageSize,
            hasNextPage);
    }
}
