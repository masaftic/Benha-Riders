using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Events;
using MediatR;
using NetTopologySuite;
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
        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        var location = geometryFactory.CreatePoint(new Coordinate(0, 0));

        var userId = notification.DriverId.ToUserId();
        var driverLocation = new DriverLocation(userId, location);

        _db.DriverLocations.Add(driverLocation);

        await _db.SaveChangesAsync(cancellationToken);
    }
}

