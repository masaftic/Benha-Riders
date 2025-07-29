using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.TripRequests;

namespace BenhaScooters.Domain.Matching.Events;

public record TripMatchExpiredEvent(TripRequestId TripRequestId, DriverId DriverId) : DomainEvent;
