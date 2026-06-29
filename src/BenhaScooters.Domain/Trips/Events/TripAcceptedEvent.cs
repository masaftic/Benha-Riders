using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Trips.Events;

/// <summary>
/// Event published when a driver accepts a trip request
/// </summary>
public record TripCreatedEvent(
    TripId TripId,
    UserId DriverId,
    UserId RiderId
) : DomainEvent;
