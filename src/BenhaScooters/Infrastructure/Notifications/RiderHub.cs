using System.Security.Claims;
using BenhaScooters.Application.Abstractions;
using BenhaScooters.Application.Features.Riders.Queries;
using BenhaScooters.Domain.Users;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BenhaScooters.Infrastructure.Notifications;

[Authorize(Policy = "RiderPolicy")]
public class RiderHub : Hub<IRiderNotifications>
{
    private readonly ILogger<RiderHub> _logger;
    private readonly IMediator _mediator;

    public RiderHub(ILogger<RiderHub> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
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
            var statusResult = await _mediator.Send(new GetRiderStatusQuery(riderId));

            if (statusResult.IsError)
            {
                _logger.LogWarning("Failed to get rider status for {RiderId}: {Errors}", riderIdString, statusResult.Errors);
            }
            else
            {
                await Clients.Caller.ReceiveRiderStatus(statusResult.Value);
                _logger.LogInformation("Sent rider status to {RiderId}: {Status}", riderIdString, statusResult.Value.Status);
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
