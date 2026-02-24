using BenhaScooters.Domain.Users;

namespace BenhaScooters.Contracts.Authentication;

public record SignInGoogleRequest(string IdToken, App App);
