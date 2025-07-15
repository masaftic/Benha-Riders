using BenhaScooters.Data;
using BenhaScooters.Features.Trips.Events;
using FastEndpoints;

namespace BenhaScooters.Features.Trips.EventHandlers;

public class NotifyDriverHandler : IEventHandler<DriverMatchAttemptCreated>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotifyDriverHandler> _logger;

    public NotifyDriverHandler(IServiceScopeFactory scopeFactory, ILogger<NotifyDriverHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;

    }

    public async Task HandleAsync(DriverMatchAttemptCreated @event, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var driver = await db.Drivers.FindAsync(@event.DriverId);

        if (driver is null)
        {
            _logger.LogWarning("Driver {DriverId} not found", @event.DriverId);
            return;
        }

        // Notify the driver about the match attempt
        // This could be a push notification, SMS, etc.
        _logger.LogInformation("Notifying driver {DriverId} about match attempt for TripRequestId: {TripRequestId}",
            @event.DriverId, @event.TripRequestId);

        // TODO: Implement actual notification logic here
    }
}
