using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Features.Trips.Events;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Trips.EventHandlers;

public class TripRequestedEventHandler(IServiceScopeFactory scopeFactory, ILogger<TripRequestedEventHandler> logger) : IEventHandler<TripRequested>
{
    public async Task HandleAsync(TripRequested @event, CancellationToken cancellationToken)
    {
        try
        {
            // Create a new scope to get a fresh DbContext
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var tripRequestId = @event.TripRequestId;
            var riderId = @event.RiderId;

            var tripRequest = await db.TripRequests
                .FindAsync(tripRequestId);

            if (tripRequest is null)
            {
                logger.LogWarning("Trip request {TripRequestId} not found", tripRequestId);
                return;
            }

            logger.LogInformation("Trip request {TripRequestId} created and available for drivers", tripRequestId);
            
            // For now, just log that the trip is available
            // In the future, this could:
            // 1. Send push notifications to nearby drivers
            // 2. Add to a real-time dispatch queue
            // 3. Calculate optimal driver assignments
            
            // Keep it simple for MVP - drivers will see available trips via the GET /trips/available endpoint
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling TripRequested event for TripRequestId: {TripRequestId}", @event.TripRequestId);
        }
    }
}

