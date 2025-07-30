using BenhaScooters.Application.Features.Trips.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Queries;

public record GetDriverTripByIdQuery(DriverId DriverId, TripId TripId) : IRequest<ErrorOr<GetDriverTripByIdResult>>;

public record GetDriverTripByIdResult(
    int TripId,
    TripStatus Status,
    string PickupAddress,
    string DropoffAddress,
    decimal EstimatedFare,
    decimal? FinalFare,
    bool IsPaid,
    DateTime CreatedAt,
    DateTime? DriverArrivedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    TimeSpan? TotalDuration,
    RiderInfo Rider);

public class GetDriverTripByIdQueryHandler : IRequestHandler<GetDriverTripByIdQuery, ErrorOr<GetDriverTripByIdResult>>
{
    private readonly AppDbContext _db;

    public GetDriverTripByIdQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<GetDriverTripByIdResult>> Handle(GetDriverTripByIdQuery request, CancellationToken cancellationToken)
    {
        var tripResult = await _db.Trips
            .AsNoTracking()
            .Where(t => t.Id == request.TripId && t.DriverId == request.DriverId)
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
            .FirstOrDefaultAsync(cancellationToken);

        if (tripResult is null)
        {
            return TripErrors.Trip.NotFound;
        }

        return new GetDriverTripByIdResult(
            tripResult.Id.Value,
            tripResult.Status,
            tripResult.PickupAddress ?? "Unknown pickup location",
            tripResult.DropoffAddress ?? "Unknown dropoff location",
            tripResult.EstimatedFare.Amount,
            tripResult.FinalFare?.TotalFare,
            tripResult.IsPaid?.IsPaid ?? false,
            tripResult.AssignedAt,
            tripResult.DriverArrivedAt,
            tripResult.StartedAt,
            tripResult.CompletedAt,
            tripResult.TotalDuration,
            new RiderInfo(
                tripResult.RiderName,
                tripResult.RiderPhoneNumber!.Value));
    }
}
