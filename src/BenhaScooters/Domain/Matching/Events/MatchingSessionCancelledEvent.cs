using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.TripRequests;

namespace BenhaScooters.Domain.Matching.Events;

public record MatchingSessionCancelledEvent(
    MatchingSessionId SessionId,
    TripRequestId TripRequestId,
    string Reason) : DomainEvent;
