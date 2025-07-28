using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips;

namespace BenhaScooters.Domain.Trips.Events;

/// <summary>
/// Event published when a driver arrives at the pickup location
/// </summary>
public record DriverArrivedEvent(
    TripId TripId,
    DriverId DriverId,
    RiderId RiderId
) : DomainEvent;
