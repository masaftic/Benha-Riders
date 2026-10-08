using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Matching.Events;

public record MatchAttemptRejectedEvent(
    TripRequestId TripRequestId,
    UserId DriverId,
    string? RejectionReason,
    DateTime RejectedAt) : DomainEvent;
