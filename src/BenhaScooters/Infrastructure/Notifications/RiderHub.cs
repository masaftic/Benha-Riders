using System.Security.Claims;
using BenhaScooters.Application.Abstractions;
using BenhaScooters.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BenhaScooters.Infrastructure.Notifications;

[Authorize(Policy = "RiderPolicy")]
public class RiderHub : Hub<IRiderNotifications>
{
    private readonly ILogger<RiderHub> _logger;

    public RiderHub(ILogger<RiderHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        var riderId = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (riderId != null)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, riderId);
            _logger.LogInformation("Rider connected: {RiderId}", riderId);
        }

        return base.OnConnectedAsync();
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
