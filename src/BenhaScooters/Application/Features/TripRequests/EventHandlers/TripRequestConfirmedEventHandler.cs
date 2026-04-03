using BenhaScooters.Application.Features.Matching.Commands;
using BenhaScooters.Application.Features.Matching.Services;
using BenhaScooters.Domain.TripRequests.Events;
using BenhaScooters.Domain.Trips.Events;
using Hangfire;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.TripRequests.EventHandlers;


public class TripRequestConfirmedEventHandler : INotificationHandler<TripRequestConfirmedEvent>
{
    private readonly ILogger<TripRequestConfirmedEventHandler> _logger;
    private readonly ISender _sender;
    private readonly IDriverMatchingService _driverMatchingService;

    public TripRequestConfirmedEventHandler(
        ILogger<TripRequestConfirmedEventHandler> logger,
        ISender sender,
        IDriverMatchingService driverMatchingService)
    {
        _logger = logger;
        _sender = sender;
        _driverMatchingService = driverMatchingService;
    }

    public async Task Handle(TripRequestConfirmedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Trip requested: {TripRequestId} by rider {RiderId} from {PickupAddress} to {DropoffAddress}. Starting matching process...",
            notification.TripRequestId,
            notification.RiderId,
            notification.PickupAddress ?? "Unknown location",
            notification.DropoffAddress ?? "Unknown location");

        try
        {
            // 1. Create a matching session with PUSH mode
            var createSessionCommand = new CreateMatchingSessionCommand(notification.TripRequestId);
            var sessionResult = await _sender.Send(createSessionCommand, cancellationToken);

            if (sessionResult.IsError)
            {
                _logger.LogError("Failed to create matching session for trip request {TripRequestId}: {Errors}",
                    notification.TripRequestId, string.Join(", ", sessionResult.Errors.Select(e => e.Description)));
                return;
            }

            _logger.LogInformation("Matching session {MatchingSessionId} created for trip request {TripRequestId}",
                sessionResult.Value.MatchingSessionId,
                notification.TripRequestId);


            BackgroundJob.Enqueue<IDriverMatchingService>(
                ms => ms.ProcessMatchingAsync(
                    sessionResult.Value.MatchingSessionId, 
                    cancellationToken));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing trip request {TripRequestId}", notification.TripRequestId);
        }
    }
}
