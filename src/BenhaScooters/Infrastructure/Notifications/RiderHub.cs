using System.Security.Claims;
using BenhaScooters.Application.Abstractions;
using BenhaScooters.Application.Features.Riders.Queries;
using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Users;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Infrastructure.Notifications;

[Authorize(Policy = "RiderPolicy")]
public class RiderHub : Hub<IRiderNotifications>
{
    private readonly ILogger<RiderHub> _logger;
    private readonly IMediator _mediator;
    private readonly AppDbContext _dbContext;
    private readonly IGeoService _geoService;

    public RiderHub(ILogger<RiderHub> logger, IMediator mediator, AppDbContext dbContext, IGeoService geoService)
    {
        _logger = logger;
        _mediator = mediator;
        _dbContext = dbContext;
        _geoService = geoService;
        _dbContext = dbContext;
    }

    public override async Task OnConnectedAsync()
    {
        var riderIdString = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (riderIdString != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, riderIdString);
            _logger.LogInformation("Rider connected: {RiderId}", riderIdString);

            // Query and send rider status
            var riderId = UserId.Parse(riderIdString, null);

            var trip = await _dbContext.Trips
                .Where(t => t.RiderId == riderId && (t.Status == Domain.Trips.Enums.TripStatus.Assigned || t.Status == Domain.Trips.Enums.TripStatus.DriverArrived))
                .Select(t => new { t.Id, t.DriverId, t.PickupLocation })
                .FirstOrDefaultAsync();
            
            if (trip != null)
            {
                var driverId = trip.DriverId;
                var location = await _dbContext.DriverLocations
                    .Where(dl => dl.UserId == driverId)
                    .Select(dl => dl.Location)
                    .FirstOrDefaultAsync();
                
                var distance = _geoService.CalculateDistance(location, trip.PickupLocation);
                var arrivalDuration = _geoService.EstimateArrivalTime(distance);
                var estimatedArrivalMinutes = arrivalDuration.ToMinutes();

                var geoCoord = location.ToCoordinate();

                var locationUpdate = new DriverLocationUpdate(
                    Latitude: geoCoord.Latitude,
                    Longitude: geoCoord.Longitude,
                    Timestamp: DateTime.UtcNow,
                    EstimatedArrivalMinutes: estimatedArrivalMinutes);

                await Clients.Caller.NotifyDriverLocationUpdate(riderIdString, locationUpdate);
                
                _logger.LogInformation("Sent location update to rider {RiderId} for driver {DriverId}: ({Latitude}, {Longitude}), ETA: {ETA} minutes",
                    riderId, driverId, geoCoord.Latitude, geoCoord.Longitude, estimatedArrivalMinutes);
            }
        }

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var riderId = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (riderId != null)
        {
            Groups.RemoveFromGroupAsync(Context.ConnectionId, riderId);
            _logger.LogInformation("Rider disconnected: {RiderId}", riderId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}
