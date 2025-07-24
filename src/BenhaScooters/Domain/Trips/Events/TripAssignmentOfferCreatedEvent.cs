using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;

namespace BenhaScooters.Domain.Trips.Events;

/// <summary>
/// Event published when a trip assignment offer is created for a driver
/// </summary>
public record TripAssignmentOfferCreatedEvent(
    TripRequestId TripRequestId,
    DriverId DriverId,
    double DistanceToPickup,
    double EstimatedArrivalTime,
    decimal DriverScore,
    DateTime OfferExpiresAt
) : DomainEvent;
