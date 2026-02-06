using BenhaScooters.Domain.Matching;

namespace BenhaScooters.Contracts.Matching;

public record AcceptMatchRequest(DriverMatchAttemptId DriverMatchAttemptId);

public record RejectMatchRequest(DriverMatchAttemptId DriverMatchAttemptId, string? Reason = null);
