using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Matching.Events;

public record TripMatchExpiredEvent(TripRequestId TripRequestId, UserId DriverId) : DomainEvent;
