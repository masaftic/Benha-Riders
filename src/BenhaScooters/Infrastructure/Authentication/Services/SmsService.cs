using BenhaScooters.Domain.Users;

namespace BenhaScooters.Infrastructure.Authentication.Services;

public interface ISmsService
{
    Task SendSmsAsync(PhoneNumber phoneNumber, string message);
    Task SendVerificationCodeAsync(PhoneNumber phoneNumber, string code);
}

public class DevSmsService : ISmsService
{
    private readonly ILogger<DevSmsService> _logger;

    public DevSmsService(ILogger<DevSmsService> logger)
    {
        _logger = logger;
    }

    public Task SendSmsAsync(PhoneNumber phoneNumber, string message)
    {
        _logger.LogInformation("SMS sent to {PhoneNumber}: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }

    public Task SendVerificationCodeAsync(PhoneNumber phoneNumber, string code)
    {
        _logger.LogInformation("SMS Verification Code sent to {PhoneNumber}: {Code}", phoneNumber, code);
        return Task.CompletedTask;
    }
}
