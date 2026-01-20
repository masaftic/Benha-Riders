using BenhaScooters.Application.Features.Trips.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Queries;

public record GetDriverRecentTripsQuery(DriverId DriverId, int PageNumber = 1, int PageSize = 10) : IRequest<ErrorOr<GetDriverRecentTripsResult>>;

public record GetDriverRecentTripsResult(
    IReadOnlyList<DriverTripSummary> Trips,
    int TotalCount,
    int PageNumber,
    int PageSize,
    bool HasNextPage);

public record DriverTripSummary(
    int TripId,
    TripStatus Status,
    string PickupAddress,
    string DropoffAddress,
    decimal EstimatedFare,
    decimal? FinalFare,
    bool IsPaid,
    DateTime AssignedAt,
    DateTime? DriverArrivedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    RiderInfo Rider);


public class GetDriverRecentTripsQueryHandler : IRequestHandler<GetDriverRecentTripsQuery, ErrorOr<GetDriverRecentTripsResult>>
{
    private readonly AppDbContext _db;

    public GetDriverRecentTripsQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<GetDriverRecentTripsResult>> Handle(GetDriverRecentTripsQuery request, CancellationToken cancellationToken)
    {
        var query = _db.Trips
            .AsNoTracking()
            .Where(t => t.DriverId == request.DriverId)
            .OrderByDescending(t => t.AssignedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var trips = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new DriverTripSummary(
                t.Id.Value,
                t.Status,
                t.PickupAddress ?? "Unknown pickup location",
                t.DropoffAddress ?? "Unknown dropoff location",
                t.FinalFare.Amount,
                t.TripFare != null ? t.TripFare.TotalFare : null,
                t.TripPayment != null && t.TripPayment.IsPaid,
                t.AssignedAt,
                t.DriverArrivedAt,
                t.StartedAt,
                t.CompletedAt,
                new RiderInfo(
                    t.RiderProfile.PreferredName ?? "Unknown",
                    t.RiderProfile.User.PhoneNumber!.Value)))
            .ToListAsync(cancellationToken);

        var hasNextPage = (request.PageNumber * request.PageSize) < totalCount;

        return new GetDriverRecentTripsResult(
            trips,
            totalCount,
            request.PageNumber,
            request.PageSize,
            hasNextPage);
    }
}
