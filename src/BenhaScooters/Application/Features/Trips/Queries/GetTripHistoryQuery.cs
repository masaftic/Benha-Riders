using BenhaScooters.Data;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Queries;

public record GetTripHistoryQuery(UserId UserId, int PageNumber = 1, int PageSize = 10) : IRequest<ErrorOr<GetTripHistoryResult>>;

public record GetTripHistoryResult(
    IReadOnlyList<TripHistorySummary> Trips,
    int TotalCount,
    int PageNumber,
    int PageSize,
    bool HasNextPage);

public record TripHistorySummary(
    TripId TripId,
    TripStatus Status,
    string PickupAddress,
    string DropoffAddress,
    Coordinate PickupLocation,
    Coordinate DropoffLocation,
    decimal Fare,
    DateTime AssignedAt,
    DateTime? CompletedAt);

public class GetTripHistoryQueryHandler : IRequestHandler<GetTripHistoryQuery, ErrorOr<GetTripHistoryResult>>
{
    private readonly AppDbContext _db;

    public GetTripHistoryQueryHandler(AppDbContext db)
    {
        _db = db;
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
            .Select(t => new TripHistorySummary(
                t.Id,
                t.Status,
                t.PickupAddress ?? "Unknown pickup location",
                t.DropoffAddress ?? "Unknown dropoff location",
                Coordinate.FromPoint(t.PickupLocation),
                Coordinate.FromPoint(t.DropoffLocation),
                t.FinalFare.Amount,
                t.AssignedAt,
                t.CompletedAt))
            .ToListAsync(cancellationToken);

        var hasNextPage = (request.PageNumber * request.PageSize) < totalCount;

        return new GetTripHistoryResult(
            trips,
            totalCount,
            request.PageNumber,
            request.PageSize,
            hasNextPage);
    }
}
