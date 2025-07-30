using BenhaScooters.Application.Features.Trips.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Queries;

public record GetDriverCurrentTripQuery(DriverId DriverId) : IRequest<ErrorOr<GetDriverCurrentTripResult>>;

public record GetDriverCurrentTripResult(
    int TripId,
    TripStatus Status,
    string PickupAddress,
    string DropoffAddress,
    decimal EstimatedFare,
    DateTime CreatedAt,
    DateTime? DriverArrivedAt,
    DateTime? StartedAt,
    RiderInfo Rider);



public class GetDriverCurrentTripQueryHandler : IRequestHandler<GetDriverCurrentTripQuery, ErrorOr<GetDriverCurrentTripResult>>
{
    private readonly AppDbContext _db;

    public GetDriverCurrentTripQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<GetDriverCurrentTripResult>> Handle(GetDriverCurrentTripQuery request, CancellationToken cancellationToken)
    {
        var tripResult = await _db.Trips
            .AsNoTracking()
            .Where(t => t.DriverId == request.DriverId)
            .Select(t => new
            {
                t.Id,
                t.Status,
                t.PickupAddress,
                t.DropoffAddress,
                t.EstimatedFare,
                FinalFare = t.TripFare,
                IsPaid = t.TripPayment,
                t.AssignedAt,
                t.DriverArrivedAt,
                t.StartedAt,
                t.CompletedAt,
                TotalDuration = t.CompletedAt.HasValue ?
                    (t.CompletedAt - t.AssignedAt) :
                    (t.StartedAt.HasValue ?
                        (t.StartedAt - t.AssignedAt) : null),
                RiderName = t.Rider.PreferredName ?? "Unknown",
                RiderPhoneNumber = t.Rider.User.PhoneNumber
            })
            .OrderBy(t => t.AssignedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (tripResult is null)
        {
            return TripErrors.Trip.NotFound;
        }

        return new GetDriverCurrentTripResult(
            tripResult.Id.Value,
            tripResult.Status,
            tripResult.PickupAddress ?? "Unknown pickup location",
            tripResult.DropoffAddress ?? "Unknown dropoff location",
            tripResult.EstimatedFare.Amount,
            tripResult.AssignedAt,
            tripResult.DriverArrivedAt,
            tripResult.StartedAt,
            new RiderInfo(
                tripResult.RiderName,
                tripResult.RiderPhoneNumber!.Value));
    }
}
