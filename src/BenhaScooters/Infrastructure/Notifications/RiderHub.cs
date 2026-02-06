using BenhaScooters.Application.Abstractions;
using BenhaScooters.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BenhaScooters.Infrastructure.Notifications;

[Authorize(Policy = "RiderPolicy")]
public class RiderHub : Hub<IRiderNotifications>
{
    public override Task OnConnectedAsync()
    {
        var riderId = Context.User?.Claims.FirstOrDefault(c => c.Type == JwtClaims.Sub)?.Value;
        if (riderId != null)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, riderId);
        }

        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var riderId = Context.User?.Claims.FirstOrDefault(c => c.Type == JwtClaims.Sub)?.Value;
        if (riderId != null)
        {
            Groups.RemoveFromGroupAsync(Context.ConnectionId, riderId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}
