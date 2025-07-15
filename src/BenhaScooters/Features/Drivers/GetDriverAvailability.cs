using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Features.Drivers.Common;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Drivers;

public record GetDriverAvailabilityResponse(
    DriverStatus Status,
    bool IsAvailableForRequests,
    DateTime LastStatusChange,
    DateTime LastLocationUpdate,
    bool IsLocationStale,
    TimeSpan? OnlineSessionDuration,
    TimeSpan TotalOnlineTime,
    TripId? CurrentTripId
);

public class GetDriverAvailabilityEndpoint(AppDbContext db) : EndpointWithoutRequest<GetDriverAvailabilityResponse>
{
    public override void Configure()
    {
        Get("/driver/availability");
        Claims(JwtClaims.Sub);
        Roles("Driver");
        PreProcessor<OnboardedProcessor<EmptyRequest>>();
        Description(x => x
            .WithSummary("Get current driver availability status")
            .Produces<GetDriverAvailabilityResponse>()
            .Produces(401)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Get current driver availability status";
            s.Description = "Retrieves the driver's current availability status, location information, session duration, and current trip details if applicable.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();
        
        // Get driver profile
        var driverId = await db.Drivers
            .Where(d => d.UserId == userId)
            .Select(d => d.Id)
            .FirstOrDefaultAsync(ct);

        if (!driverId.IsInitialized())
        {
            ThrowError("Driver profile not found", 404);
        }

        // Get driver availability
        var availability = await db.DriverAvailabilities
            .FirstOrDefaultAsync(da => da.DriverId == driverId, ct);

        if (availability == null)
        {
            // Create default availability if none exists
            availability = new DriverAvailability(driverId);
            await db.DriverAvailabilities.AddAsync(availability, ct);
            await db.SaveChangesAsync(ct);
        }

        // Calculate current session duration
        TimeSpan? sessionDuration = null;
        if (availability.OnlineSessionStart.HasValue && availability.Status == DriverStatus.Online)
        {
            sessionDuration = DateTime.UtcNow - availability.OnlineSessionStart.Value;
        }

        await SendAsync(new GetDriverAvailabilityResponse(
            availability.Status,
            availability.IsAvailableForRequests,
            availability.LastStatusChange,
            availability.LastLocationUpdate,
            availability.IsLocationStale,
            sessionDuration,
            availability.TotalOnlineTime,
            availability.CurrentTripId
        ), cancellation: ct);
    }
}
