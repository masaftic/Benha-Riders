using System.Security.Claims;
using BenhaScooters.Application.Abstractions;
using BenhaScooters.Application.Features.Matching.Queries;
using BenhaScooters.Data;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.Users;
using BenhaScooters.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Thinktecture;

namespace BenhaScooters.Infrastructure.Notifications;

[Authorize(Policy = "DriverPolicy")]
public class DriverHub : Hub<IDriverNotifications>
{
    private readonly ILogger<DriverHub> _logger;
    private readonly ISignalRConnectionTracker _connectionTracker;
    private readonly IMediator _mediator;

    public DriverHub(ILogger<DriverHub> logger, ISignalRConnectionTracker connectionTracker, IMediator mediator)
    {
        _logger = logger;
        _connectionTracker = connectionTracker;
        _mediator = mediator;
    }

    public override async Task OnConnectedAsync()
    {
        var driverId = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (driverId != null && int.TryParse(driverId, out var driverIdValue))
        {
            var userId = UserId.Create(driverIdValue);

            await Groups.AddToGroupAsync(Context.ConnectionId, driverId);
            await _connectionTracker.TrackConnectedAsync(userId, Context.ConnectionId);
            await _connectionTracker.RecordHeartbeat(userId);
            _logger.LogInformation("Driver connected: {DriverId}", driverId);

            // Send any pending offers the driver missed while offline
            await SendPendingOffersAsync(driverId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var driverId = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (driverId != null && int.TryParse(driverId, out var driverIdValue))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, driverId);
            await _connectionTracker.TrackDisconnectedAsync(UserId.Create(driverIdValue), Context.ConnectionId);
            _logger.LogInformation("Driver disconnected: {DriverId}", driverId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task Heartbeat()
    {
        var driverIdString = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (driverIdString != null && int.TryParse(driverIdString, out var driverIdValue))
        {
            await _connectionTracker.RecordHeartbeat(UserId.Create(driverIdValue));
        }
    }

    private async Task SendPendingOffersAsync(string driverIdString)
    {
        try
        {
            if (!int.TryParse(driverIdString, out var driverIdValue))
            {
                _logger.LogWarning("Invalid driver ID format: {DriverId}", driverIdString);
                return;
            }

            var driverId = UserId.Create(driverIdValue);

            // Query for pending match attempts for this driver
            var pendingOffersResult = await _mediator.Send(new GetDriverMatchOffersQuery(driverId));
            var pendingOffers = pendingOffersResult.Value.MatchOffers;

            if (pendingOffers.Any())
            {
                _logger.LogInformation(
                    "Sending {Count} pending offers to reconnected driver {DriverId}",
                    pendingOffers.Count,
                    driverIdString);

                foreach (var offer in pendingOffers)
                {
                    var notification = new RideRequestOfferNotification(
                        offer.DriverMatchAttemptId.ToString(),
                        offer.RiderName,
                        offer.PickupLatitude,
                        offer.PickupLongitude,
                        offer.DropoffLatitude,
                        offer.DropoffLongitude,
                        offer.PickupAddress,
                        offer.DropoffAddress,
                        offer.EstimatedFare,
                        offer.EstimatedDistance,
                        offer.DistanceToPickup,
                        offer.EstimatedArrivalTime,
                        offer.OfferedAt);

                    await Clients.Caller.NotifyRideRequestOffer(driverIdString, notification);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending pending offers to driver {DriverId}", driverIdString);
        }
    }
}
