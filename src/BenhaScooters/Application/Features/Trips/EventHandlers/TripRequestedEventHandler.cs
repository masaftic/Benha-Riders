using BenhaScooters.Application.Features.Matching.Commands;
using BenhaScooters.Application.Features.Matching.Services;
using BenhaScooters.Domain.TripRequests.Events;
using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BenhaScooters.Application.Features.Trips.EventHandlers;

/// <summary>
/// Handles the TripRequestedEvent to initiate the matching process asynchronously
/// This handler runs after the command completes, so it won't block the user response
/// </summary>
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
            "Trip requested: {TripRequestId} by rider {RiderId} from {PickupAddress} to {DropoffAddress}. Starting async matching process...",
            notification.TripRequestId.Value,
            notification.RiderId.Value,
            notification.PickupAddress ?? "Unknown location",
            notification.DropoffAddress ?? "Unknown location");

        await Task.Delay(5000);

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

            _logger.LogInformation("Matching session {MatchingSessionId} created for trip request {TripRequestId} in {Mode} mode",
                sessionResult.Value.MatchingSessionId.Value,
                notification.TripRequestId.Value,
                sessionResult.Value.Mode);

            // Start the actual driver matching process
            var matchingResult = await _driverMatchingService.StartMatchingAsync(
                sessionResult.Value.MatchingSessionId,
                cancellationToken);

            if (matchingResult.IsError)
            {
                _logger.LogError("Failed to start driver matching for session {MatchingSessionId}: {Errors}",
                    sessionResult.Value.MatchingSessionId.Value,
                    string.Join(", ", matchingResult.Errors.Select(e => e.Description)));
                return;
            }

            _logger.LogInformation("Driver matching started successfully for session {MatchingSessionId}",
                sessionResult.Value.MatchingSessionId.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing trip request {TripRequestId}", notification.TripRequestId.Value);
        }
    }
}
