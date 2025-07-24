using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;

namespace BenhaScooters.Domain.Trips.Events;

/// <summary>
/// Event published when a driver rejects a trip request
/// </summary>
public record TripRejectedEvent(
    TripRequestId TripRequestId,
    DriverId DriverId,
    string? RejectionReason,
    DateTime RejectedAt
) : DomainEvent;
