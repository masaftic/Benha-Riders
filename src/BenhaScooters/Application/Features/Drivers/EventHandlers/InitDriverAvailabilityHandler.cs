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
        var userId = notification.DriverId;
        var driverStatus = new DriverStatus(userId);
        _db.DriverStatuses.Add(driverStatus);

        await _db.SaveChangesAsync(cancellationToken);
    }
}