namespace BenhaScooters.Contracts.Matching;

public record AcceptMatchRequest(int DriverMatchAttemptId);

public record RejectMatchRequest(int DriverMatchAttemptId, string? Reason = null);
