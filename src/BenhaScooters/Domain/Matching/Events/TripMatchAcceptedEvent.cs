using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Matching.Events;

public record MatchAttemptAcceptedEvent(
    TripRequestId TripRequestId,
    UserId DriverId,
    double DistanceToPickup,
    double EstimatedArrivalTime,
    DateTime AcceptedAt) : DomainEvent;
