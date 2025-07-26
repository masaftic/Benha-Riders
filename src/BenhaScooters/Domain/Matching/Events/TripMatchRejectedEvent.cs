using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.TripRequests;

namespace BenhaScooters.Domain.Matching.Events;

public record TripMatchRejectedEvent(
    TripRequestId TripRequestId,
    DriverId DriverId,
    string? RejectionReason,
    DateTime RejectedAt) : DomainEvent;
