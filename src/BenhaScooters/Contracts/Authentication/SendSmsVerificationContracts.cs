using BenhaScooters.Domain.Users;

namespace BenhaScooters.Contracts.Authentication;

public record SendSmsVerificationRequest(PhoneNumber PhoneNumber);
