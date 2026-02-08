namespace BenhaScooters.Application.Features.Authentication.Commands.Common;

public record AuthenticationResponse(string Type, object Result);

public record AuthenticationSuccess(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public record OnboardingRequired(string OnboardingToken, string NextStep);
