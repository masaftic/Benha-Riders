using System.Security.Claims;
using BenhaScooters.Application.Abstractions;
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
    private readonly AppDbContext _dbContext;

    public DriverHub(ILogger<DriverHub> logger, AppDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public override async Task OnConnectedAsync()
    {
        var driverId = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (driverId != null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, driverId);
            _logger.LogInformation("Driver connected: {DriverId}", driverId);

            // Send any pending offers the driver missed while offline
            await SendPendingOffersAsync(driverId);
        }

        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var driverId = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (driverId != null)
        {
            Groups.RemoveFromGroupAsync(Context.ConnectionId, driverId);
            _logger.LogInformation("Driver disconnected: {DriverId}", driverId);
        }

        return base.OnDisconnectedAsync(exception);
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
            var pendingOffers = await _dbContext.DriverMatchAttempts
                .Include(dma => dma.MatchingSession)
                    .ThenInclude(ms => ms.TripRequest)
                        .ThenInclude(tr => tr.RiderProfile)
                            .ThenInclude(rp => rp.User)
                .Where(dma => dma.DriverUserId == driverId
                    && dma.Status == MatchAttemptStatus.Pending
                    && dma.MatchingSession.IsActive
                    && dma.CreatedAt > DateTime.UtcNow.AddMinutes(-10)) // Only send offers from last 10 minutes
                .ToListAsync();

            if (pendingOffers.Any())
            {
                _logger.LogInformation(
                    "Sending {Count} pending offers to reconnected driver {DriverId}",
                    pendingOffers.Count,
                    driverIdString);

                foreach (var offer in pendingOffers)
                {
                    var tripRequest = offer.MatchingSession.TripRequest;
                    var notification = new RideRequestOfferNotification(
                        offer.Id.ToString(),
                        tripRequest.RiderProfile.PreferredName ?? tripRequest.RiderProfile.User.Name,
                        tripRequest.PickupLocation.Y,
                        tripRequest.PickupLocation.X,
                        tripRequest.DropoffLocation.Y,
                        tripRequest.DropoffLocation.X,
                        tripRequest.PickupAddress,
                        tripRequest.DropoffAddress,
                        tripRequest.FinalFare.Amount,
                        tripRequest.FinalFare.Distance.ToKilometers(),
                        offer.DistanceToPickup.ToKilometers(),
                        offer.EstimatedArrivalTime.ToMinutes(),
                        offer.CreatedAt);

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
