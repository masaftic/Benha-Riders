using BenhaScooters.Data;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Features.Drivers.Common;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Trips;

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
    DateTime ExpiresAt
);

public record GetAvailableTripsResponse(List<AvailableTripDto> AvailableTrips);

public class GetAvailableTripsEndpoint(AppDbContext db) : EndpointWithoutRequest<GetAvailableTripsResponse>
{
    public override void Configure()
    {
        Get("/trips/available");
        Claims(JwtClaims.Sub);
        Roles("Driver");
        PreProcessor<OnboardedProcessor<EmptyRequest>>();
        Description(x => x
            .WithSummary("Get available trip requests for drivers")
            .WithTags("Trips")
            .Produces<GetAvailableTripsResponse>()
            .Produces(401)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Get available trip requests for drivers";
            s.Description = "Returns a list of pending trip requests that drivers can accept. Only shows trips if the driver is available for requests.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();
        
        // Get driver profile and check availability
        var driverId = await db.Drivers
            .Where(d => d.UserId == userId)
            .Select(d => d.Id)
            .FirstOrDefaultAsync(ct);

        if (!driverId.IsInitialized())
        {
            ThrowError("Driver profile not found", 404);
        }

        // Check if driver is available to see trips
        var driverAvailability = await db.DriverAvailabilities
            .FirstOrDefaultAsync(da => da.DriverId == driverId, ct);

        if (driverAvailability == null || !driverAvailability.IsAvailableForRequests)
        {
            // Return empty list if driver is not available
            await SendAsync(new GetAvailableTripsResponse([]), cancellation: ct);
            return;
        }

        // Get available trip requests (pending status, not expired)
        var availableTrips = await db.TripRequests
            .Where(tr => tr.Status == TripRequestStatus.Pending && 
                        (tr.ExpiresAt == null || tr.ExpiresAt > DateTime.UtcNow))
            .OrderBy(tr => tr.RequestedAt)
            .Select(tr => new
            {
                TripRequestId = tr.Id.Value,
                PickupLocation = tr.PickupLocation,
                DropOffLocation = tr.DropoffLocation,
                PickupAddress = tr.PickupAddress,
                DropoffAddress = tr.DropoffAddress,
                EstimatedFare = tr.EstimatedFare.Amount,
                EstimatedDistance = tr.EstimatedFare.Distance, // Assuming this is available
                EstimatedDuration = tr.EstimatedFare.Time, // Assuming this is available
                RequestedAt = tr.RequestedAt,
                ExpiresAt = tr.ExpiresAt ?? DateTime.UtcNow.AddMinutes(10) // Default to 10 minutes if null
            })
            .ToListAsync(ct);

        // Map to DTO
        var availableTripsDto = availableTrips.Select(tr => new AvailableTripDto(
            tr.TripRequestId,
            tr.PickupLocation.Y, // Latitude
            tr.PickupLocation.X, // Longitude
            tr.DropOffLocation.Y, // Latitude
            tr.DropOffLocation.X, // Longitude
            tr.PickupAddress,
            tr.DropoffAddress,
            tr.EstimatedFare,
            tr.EstimatedDistance,
            tr.EstimatedDuration,
            tr.RequestedAt,
            tr.ExpiresAt
        )).ToList();

        await SendAsync(new GetAvailableTripsResponse(availableTripsDto), cancellation: ct);
    }
}
