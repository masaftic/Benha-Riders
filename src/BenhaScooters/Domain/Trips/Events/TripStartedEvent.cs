using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Trips.Events;

/// <summary>
/// Event published when a trip is started by the driver
/// </summary>
public record TripStartedEvent(
    TripId TripId,
    UserId DriverId,
    UserId RiderId
) : DomainEvent;
