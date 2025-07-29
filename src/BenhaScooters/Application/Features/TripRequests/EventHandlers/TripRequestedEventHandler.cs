using BenhaScooters.Application.Features.Matching.Commands;
using BenhaScooters.Application.Features.Matching.Services;
using BenhaScooters.Domain.TripRequests.Events;
using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.TripRequests.EventHandlers;


public class TripRequestedEventHandler : INotificationHandler<TripRequestedEvent>
{
    private readonly ILogger<TripRequestedEventHandler> _logger;
    private readonly ISender _sender;
    private readonly IDriverMatchingService _driverMatchingService;

    public TripRequestedEventHandler(
        ILogger<TripRequestedEventHandler> logger, 
        ISender sender,
        IDriverMatchingService driverMatchingService)
    {
        _logger = logger;
        _sender = sender;
        _driverMatchingService = driverMatchingService;
    }

    public async Task Handle(TripRequestedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Trip requested: {TripRequestId} by rider {RiderId} from {PickupAddress} to {DropoffAddress}. Starting matching process...",
            notification.TripRequestId.Value,
            notification.RiderId.Value,
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
                    notification.TripRequestId.Value, string.Join(", ", sessionResult.Errors.Select(e => e.Description)));
                return;
            }

            _logger.LogInformation("Matching session {MatchingSessionId} created for trip request {TripRequestId}",
                sessionResult.Value.MatchingSessionId.Value,
                notification.TripRequestId.Value);

            // Start the actual driver matching process
            var matchingResult = await _driverMatchingService.ProcessMatchingAsync(
                sessionResult.Value.MatchingSessionId,
                cancellationToken);

            if (matchingResult.IsError)
            {
                _logger.LogError("Failed to process driver matching for session {MatchingSessionId}: {Errors}",
                    sessionResult.Value.MatchingSessionId.Value,
                    string.Join(", ", matchingResult.Errors.Select(e => e.Description)));
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing trip request {TripRequestId}", notification.TripRequestId.Value);
        }
    }
}
