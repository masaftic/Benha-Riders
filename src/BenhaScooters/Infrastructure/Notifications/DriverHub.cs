using BenhaScooters.Application.Abstractions;
using BenhaScooters.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BenhaScooters.Infrastructure.Notifications;

[Authorize(Policy = "DriverPolicy")]
public class DriverHub : Hub<IDriverNotifications>
{
    public override Task OnConnectedAsync()
    {
        var driverId = Context.User?.Claims.FirstOrDefault(c => c.Type == JwtClaims.DriverId)?.Value;
        if (driverId != null)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, driverId);
        }

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var driverId = Context.User?.Claims.FirstOrDefault(c => c.Type == JwtClaims.DriverId)?.Value;
        if (driverId != null)
        {
            Groups.RemoveFromGroupAsync(Context.ConnectionId, driverId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}
