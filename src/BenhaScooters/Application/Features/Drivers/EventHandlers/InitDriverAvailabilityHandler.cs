using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Events;
using MediatR;

namespace BenhaScooters.Application.Features.Drivers.EventHandlers;

public class InitDriverAvailabilityHandler : INotificationHandler<DriverOnboardingCompletedEvent>
{
    private readonly AppDbContext _db;

    public InitDriverAvailabilityHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DriverOnboardingCompletedEvent notification, CancellationToken cancellationToken)
    {
        var driverAvailability = new DriverAvailability(notification.DriverId);
        _db.DriverAvailabilities.Add(driverAvailability);

        await _db.SaveChangesAsync(cancellationToken);
    }
}