using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips.Enums;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Trips.Queries;

public record GetAvailableTripsQuery(DriverId DriverId) : IRequest<ErrorOr<GetAvailableTripsResult>>;

public record AvailableTripDto(
    int TripRequestId,
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude,
    string? PickupAddress,
    string? DropoffAddress,
    decimal EstimatedFare,
    double EstimatedDistance,
    double EstimatedDuration,
    DateTime RequestedAt,
    DateTime ExpiresAt);

public record GetAvailableTripsResult(List<AvailableTripDto> AvailableTrips);

public class GetAvailableTripsQueryHandler(AppDbContext db) : IRequestHandler<GetAvailableTripsQuery, ErrorOr<GetAvailableTripsResult>>
{
    public async Task<ErrorOr<GetAvailableTripsResult>> Handle(GetAvailableTripsQuery request, CancellationToken cancellationToken)
    {
        // Check if driver is available to see trips
        var driverAvailability = await db.DriverAvailabilities
            .FirstOrDefaultAsync(da => da.DriverId == request.DriverId, cancellationToken);

        if (driverAvailability == null || driverAvailability.Status != DriverStatus.Online)
        {
            return TripErrors.Driver.NotOnline;
        }

        // Get available trip requests (pending status, not expired)
        var availableTrips = await db.TripRequests
            .AsNoTracking()
            .Where(tr => tr.Status == TripRequestStatus.Pending && tr.ExpiresAt > DateTime.UtcNow)
            .OrderBy(tr => tr.RequestedAt)
            .Select(tr => new
            {
                tr.Id,
                tr.PickupLocation,
                tr.DropoffLocation,
                tr.PickupAddress,
                tr.DropoffAddress,
                tr.EstimatedFare,
                tr.RequestedAt,
                tr.ExpiresAt
            })
            .ToListAsync(cancellationToken);

        var result = availableTrips.Select(
            at => new AvailableTripDto(
                at.Id.Value,
                at.PickupLocation.Y, // Latitude
                at.PickupLocation.X, // Longitude
                at.DropoffLocation.Y, // Latitude
                at.DropoffLocation.X, // Longitude
                at.PickupAddress,
                at.DropoffAddress,
                at.EstimatedFare.Amount,
                at.EstimatedFare.Distance,
                at.EstimatedFare.Time,
                at.RequestedAt,
                at.ExpiresAt)).ToList();

        return new GetAvailableTripsResult(result);
    }
}
