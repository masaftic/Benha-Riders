using System.Security.Claims;
using BenhaScooters.Application.Abstractions;
using BenhaScooters.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BenhaScooters.Infrastructure.Notifications;

[Authorize(Policy = "DriverPolicy")]
public class DriverHub : Hub<IDriverNotifications>
{
    private readonly ILogger<DriverHub> _logger;

    public DriverHub(ILogger<DriverHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        var driverId = Context.User?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
        if (driverId != null)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, driverId);
            _logger.LogInformation("Driver connected: {DriverId}", driverId);
        }


        return base.OnConnectedAsync();
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
}
