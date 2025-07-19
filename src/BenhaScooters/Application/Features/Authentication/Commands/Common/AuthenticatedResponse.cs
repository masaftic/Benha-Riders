namespace BenhaScooters.Application.Features.Authentication.Commands.Common;

public record AuthenticatedResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
