using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Trips.Events;

/// <summary>
/// Event published when a driver arrives at the pickup location
/// </summary>
public record DriverArrivedEvent(
    TripId TripId,
    UserId DriverId,
    UserId RiderId
) : DomainEvent;
