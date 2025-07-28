using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips;

namespace BenhaScooters.Domain.Trips.Events;

/// <summary>
/// Event published when a trip is completed with fare calculated and payment processed
/// </summary>
public record TripCompletedEvent(
    TripId TripId,
    DriverId DriverId,
    RiderId RiderId
) : DomainEvent;
