using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Matching.Events;

/// <summary>
/// Event published when a trip assignment offer is created for a driver
/// </summary>
public record DriverMatchOfferCreatedEvent(
    TripRequestId TripRequestId,
    UserId DriverId,
    Distance DistanceToPickup,
    Duration EstimatedArrivalTime,
    decimal DriverScore
) : DomainEvent;
