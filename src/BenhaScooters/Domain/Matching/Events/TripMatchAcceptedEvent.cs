using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.TripRequests;

namespace BenhaScooters.Domain.Matching.Events;

public record TripMatchAcceptedEvent(
    TripRequestId TripRequestId,
    DriverId DriverId,
    double DistanceToPickup,
    double EstimatedArrivalTime,
    DateTime AcceptedAt) : DomainEvent;
