using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching.Events;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Matching.EventHandlers;

public class TripMatchAcceptedEventHandler(AppDbContext db) : INotificationHandler<TripMatchAcceptedEvent>
{
    public async Task Handle(TripMatchAcceptedEvent notification, CancellationToken cancellationToken)
    {
        // Maybe track driver acceptance rate
    }
}
