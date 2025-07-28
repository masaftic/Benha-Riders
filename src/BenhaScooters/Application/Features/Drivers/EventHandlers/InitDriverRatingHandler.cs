using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Events;
using MediatR;

namespace BenhaScooters.Application.Features.Drivers.EventHandlers;

public class InitDriverRatingHandler : INotificationHandler<DriverOnboardingCompletedEvent>
{
    private readonly AppDbContext _db;

    public InitDriverRatingHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task Handle(DriverOnboardingCompletedEvent notification, CancellationToken cancellationToken)
    {
        var driverRating = new DriverRating(notification.DriverId);
        _db.DriverRatings.Add(driverRating);

        await _db.SaveChangesAsync(cancellationToken);
    }
}
