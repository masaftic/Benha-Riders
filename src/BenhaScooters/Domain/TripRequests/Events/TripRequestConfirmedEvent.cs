using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips.ValueObjects;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Domain.TripRequests.Events;


/// <summary>
/// Event published when a trip request is confirmed by a rider after he saw the price
/// </summary>
public record TripRequestConfirmedEvent(
    TripRequestId TripRequestId,
    RiderId RiderId,
    Point PickupLocation,
    Point DropoffLocation,
    string? PickupAddress,
    string? DropoffAddress,
    FareEstimate EstimatedFare,
    DateTime ConfirmedAt
) : DomainEvent;
