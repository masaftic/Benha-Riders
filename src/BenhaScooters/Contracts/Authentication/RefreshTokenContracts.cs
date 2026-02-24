using BenhaScooters.Domain.Users;

namespace BenhaScooters.Contracts.Authentication;

public record RefreshTokenRequest(string RefreshToken, App App);
