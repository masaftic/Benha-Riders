using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Events;
using MediatR;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Application.Features.Drivers.EventHandlers;

public class InitDriverLocationHandler : INotificationHandler<DriverOnboardingCompletedEvent>
{
    private readonly AppDbContext _db;

    public InitDriverLocationHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DriverOnboardingCompletedEvent notification, CancellationToken cancellationToken)
    {
        var driverLocation = new DriverLocation(
            notification.DriverId,
            new Point(0, 0), // Initial location can be set to a default value
            0, // Initial heading
            0, // Initial speed
            DateTime.UtcNow // Current timestamp
        );

        _db.DriverLocations.Add(driverLocation);

        await _db.SaveChangesAsync(cancellationToken);
    }
}

