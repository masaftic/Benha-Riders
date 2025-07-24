using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;

namespace BenhaScooters.Domain.Trips.Events;

/// <summary>
/// Event published when a trip is started by the driver
/// </summary>
public record TripStartedEvent(
    TripId TripId,
    DriverId DriverId,
    DateTime StartedAt
) : DomainEvent;
