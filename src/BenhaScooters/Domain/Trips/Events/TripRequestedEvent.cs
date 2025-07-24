using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips.ValueObjects;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Domain.Trips.Events;

/// <summary>
/// Event published when a rider requests a trip
/// </summary>
public record TripRequestedEvent(
    TripRequestId TripRequestId,
    RiderId RiderId,
    Point PickupLocation,
    Point DropoffLocation,
    string? PickupAddress,
    string? DropoffAddress,
    FareEstimate EstimatedFare,
    DateTime RequestedAt
) : DomainEvent;
