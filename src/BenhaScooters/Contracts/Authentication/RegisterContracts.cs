using BenhaScooters.Domain.Users;

namespace BenhaScooters.Contracts.Authentication;

public record RegisterRequest(
    string Name,
    Email Email,
    PhoneNumber PhoneNumber,
    string Password);
