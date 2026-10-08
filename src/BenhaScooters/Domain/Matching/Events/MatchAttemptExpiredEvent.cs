using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using MediatR;

namespace BenhaScooters.Domain.Matching.Events;

public record MatchAttemptExpiredEvent(
    DriverMatchAttemptId MatchAttemptId,
    UserId DriverUserId,
    MatchingSessionId MatchingSessionId,
    DateTime ExpiredAt) : DomainEvent, INotification;
