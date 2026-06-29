using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Trips.Events;

/// <summary>
/// Event published when a trip is cancelled by driver or rider before it starts
/// </summary>
public record TripCancelledEvent(
    TripId TripId,
    UserId DriverId,
    UserId RiderId,
    UserId CancelledBy,
    string CancellationReason
) : DomainEvent;
