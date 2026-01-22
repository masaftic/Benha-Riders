using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips.ValueObjects;
using BenhaScooters.Domain.Users;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Domain.TripRequests.Events;

/// <summary>
/// Event published when a rider requests a trip
/// </summary>
public record TripRequestedEvent(
    TripRequestId TripRequestId,
    UserId RiderId,
    Point PickupLocation,
    Point DropoffLocation,
    string? PickupAddress,
    string? DropoffAddress,
    FareEstimate EstimatedFare,
    DateTime RequestedAt
) : DomainEvent;
