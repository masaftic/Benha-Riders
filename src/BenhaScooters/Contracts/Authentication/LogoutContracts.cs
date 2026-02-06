namespace BenhaScooters.Contracts.Authentication;

public record LogoutRequest(string? RefreshToken = null);
