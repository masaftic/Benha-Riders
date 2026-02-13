using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.TripRequests.Enums;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.TripRequests.Queries;

public record GetAvailableTripsQuery(UserId DriverId) : IRequest<ErrorOr<GetAvailableTripsResult>>;

public record AvailableTripDto(
    TripRequestId TripRequestId,
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
        var userId = request.DriverId;
        
        // Check if driver is available to see trips
        var driverStatus = await db.DriverStatuses
            .FirstOrDefaultAsync(ds => ds.UserId == userId, cancellationToken);

        if (driverStatus == null || driverStatus.Status != DriverAvailabilityStatus.Online)
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
                tr.FinalFare,
                tr.RequestedAt,
                tr.ExpiresAt
            })
            .ToListAsync(cancellationToken);

        var result = availableTrips.Select(
            at => new AvailableTripDto(
                at.Id,
                at.PickupLocation.Y, // Latitude
                at.PickupLocation.X, // Longitude
                at.DropoffLocation.Y, // Latitude
                at.DropoffLocation.X, // Longitude
                at.PickupAddress,
                at.DropoffAddress,
                at.FinalFare.Amount,
                at.FinalFare.Distance.ToKilometers(), // TODO: Future - Update DTO to use Distance value object
                at.FinalFare.Time.ToMinutes(), // TODO: Future - Update DTO to use Duration value object
                at.RequestedAt,
                at.ExpiresAt)).ToList();

        return new GetAvailableTripsResult(result);
    }
}
