using BenhaScooters.Application.Abstractions;
using BenhaScooters.Data;
using BenhaScooters.Domain.Matching.Events;
using BenhaScooters.Infrastructure.Notifications;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Matching.EventHandlers;

public class MatchingSessionCancelledEventHandler : INotificationHandler<MatchingSessionCancelledEvent>
{
    private readonly ILogger<MatchingSessionCancelledEventHandler> _logger;
    private readonly IHubContext<RiderHub, IRiderNotifications> _hub;
    private readonly AppDbContext _db;

    public MatchingSessionCancelledEventHandler(
        ILogger<MatchingSessionCancelledEventHandler> logger, AppDbContext db, IHubContext<RiderHub, IRiderNotifications> hub)
    {
        _logger = logger;
        _db = db;
        _hub = hub;
    }

    public async Task Handle(MatchingSessionCancelledEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling cancellation for matching session {SessionId} due to: {Reason}",
            notification.SessionId, notification.Reason);

        var tripRequest = await _db.TripRequests
            .FirstOrDefaultAsync(tr => tr.Id == notification.TripRequestId, cancellationToken);

        if (tripRequest is null)
        {
            _logger.LogWarning("Trip request {TripRequestId} not found for cancellation", notification.TripRequestId);
            return;
        }

        var result = tripRequest.Cancel(notification.Reason);
        if (result.IsError)
        {
            _logger.LogError("Failed to cancel trip request {TripRequestId}: {Errors}",
                notification.TripRequestId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await _hub.Clients.Group(tripRequest.RiderId.ToString())
            .NotifyTripRequestCanceled(new TripRequestCanceledNotification(
                tripRequest.Id,
                notification.Reason,
                DateTime.UtcNow));
        
        _logger.LogInformation("Notified rider {RiderId} about cancellation of trip request {TripRequestId}",
            tripRequest.RiderId, tripRequest.Id);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
