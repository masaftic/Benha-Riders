using BenhaScooters.Domain.Users;

namespace BenhaScooters.Infrastructure.Authentication.Services;

/// <summary>
/// Dummy SMS service for testing/development.
/// Logs SMS to console instead of sending.
/// </summary>
public class DummySmsService : ISmsService
{
    private readonly ILogger<DummySmsService> _logger;

    public DummySmsService(ILogger<DummySmsService> logger)
    {
        _logger = logger;
    }

    public Task SendSmsAsync(UserId userId, PhoneNumber phoneNumber, string message)
    {
        _logger.LogWarning("📱 [DUMMY SMS] To: {PhoneNumber} | Message: {Message}", phoneNumber, message);
        return Task.CompletedTask;
    }

    public Task SendVerificationCodeAsync(UserId userId, PhoneNumber phoneNumber, string code)
    {
        _logger.LogWarning("🔐 [DUMMY SMS VERIFICATION] To: {PhoneNumber} | Code: {Code}", phoneNumber, code);
        return Task.CompletedTask;
    }
}
