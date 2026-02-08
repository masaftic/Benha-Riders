using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using MediatR;

namespace BenhaScooters.Domain.Matching.Events;

public record MatchAttemptCancelledEvent(
    DriverMatchAttemptId MatchAttemptId,
    UserId DriverUserId,
    MatchingSessionId MatchingSessionId,
    DateTime CancelledAt) : DomainEvent, INotification;
