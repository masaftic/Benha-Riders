using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Application.Workflows.Matching;


// Workflow messages
// Intent to DO SOMETHING, not something has happened (e.g. events)

public record StartMatchingSession(
    TripRequestId TripRequestId) : IRequest;

public record StartMatchingRound(
    MatchingSessionId SessionId) : IRequest;

public record MatchingRoundTimedOut(
    MatchingSessionId SessionId,
    int RoundNumber) : IRequest;

public record EvaluateMatchingProgress(
    MatchingSessionId SessionId) : IRequest;
