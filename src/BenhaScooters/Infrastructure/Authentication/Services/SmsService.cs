using BenhaScooters.Domain.Users;

namespace BenhaScooters.Infrastructure.Authentication.Services;

public interface ISmsService
{
    Task SendSmsAsync(UserId userId, PhoneNumber phoneNumber, string message);
    Task SendVerificationCodeAsync(UserId userId, PhoneNumber phoneNumber, string code);
}
