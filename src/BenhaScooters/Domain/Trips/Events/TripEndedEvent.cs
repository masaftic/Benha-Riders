using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;

namespace BenhaScooters.Domain.Trips.Events;

/// <summary>
/// Event published when a trip ends and needs fare calculation
/// </summary>
public record TripEndedEvent(
    TripId TripId,
    DriverId DriverId,
    DateTime EndedAt,
    TimeSpan TripDuration
) : DomainEvent;
