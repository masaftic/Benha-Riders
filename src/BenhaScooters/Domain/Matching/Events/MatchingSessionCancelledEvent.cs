using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.TripRequests;

namespace BenhaScooters.Domain.Matching.Events;

public record MatchingSessionCancelledEvent(
    MatchingSessionId SessionId,
    TripRequestId TripRequestId,
    bool IsCanceledByUser,
    string Reason) : DomainEvent;
