using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Matching.Events;

/// <summary>
/// Event published when a trip assignment offer is created for a driver.
/// Published after persistence so that MatchAttemptId is available.
/// </summary>
public record DriverMatchOfferCreatedEvent(
    DriverMatchAttemptId MatchAttemptId,
    TripRequestId TripRequestId,
    UserId DriverId,
    string RiderName,
    Coordinate Pickup,
    Coordinate Dropoff,
    string? PickupAddress,
    string? DropoffAddress,

    decimal EstimatedFare,
    Distance EstimatedDistance,
    Distance DistanceToPickup,
    Duration EstimatedArrival,

    DateTime OfferedAt
) : DomainEvent;
