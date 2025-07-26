using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.TripRequests;

namespace BenhaScooters.Domain.Matching.Events;

/// <summary>
/// Event published when a trip assignment offer is created for a driver
/// </summary>
public record DriverMatchOfferCreatedEvent(
    TripRequestId TripRequestId,
    DriverId DriverId,
    double DistanceToPickup,
    double EstimatedArrivalTime,
    decimal DriverScore,
    DateTime OfferExpiresAt
) : DomainEvent;
