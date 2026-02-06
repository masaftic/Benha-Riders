using BenhaScooters.Domain.Users;

namespace BenhaScooters.Contracts.Authentication;

public record LoginRequest(PhoneNumber PhoneNumber, string Password);
