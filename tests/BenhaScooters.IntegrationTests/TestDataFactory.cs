using BenhaScooters.Features.Authentication;

namespace BenhaScooters.IntegrationTests;

public static class TestDataFactory
{
    private static int _userCounter = 0;
    private static readonly object _lock = new();

    public static RegisterRequest CreateRegisterRequest(
        string? name = null,
        string? email = null,
        string? phoneNumber = null,
        string? password = null,
        string role = "rider")
    {
        lock (_lock)
        {
            _userCounter++;
            return new RegisterRequest(
                Name: name ?? $"Test User {_userCounter}",
                Email: email ?? $"testuser{_userCounter}@example.com",
                PhoneNumber: phoneNumber ?? $"+20123456{_userCounter:D4}",
                Password: password ?? "password123",
                Role: role
            );
        }
    }

    public static LoginRequest CreateLoginRequest(string email, string password = "password123")
    {
        return new LoginRequest(Email: email, Password: password);
    }

    public static SendSmsVerificationRequest CreateSmsVerificationRequest(string phoneNumber)
    {
        return new SendSmsVerificationRequest(phoneNumber);
    }

    public static VerifySmsCodeRequest CreateVerifySmsCodeRequest(string phoneNumber, string code)
    {
        return new VerifySmsCodeRequest(phoneNumber, code);
    }

    public static class Rider
    {
        public static RegisterRequest CreateRegisterRequest(
            string? name = null,
            string? email = null,
            string? phoneNumber = null,
            string? password = null)
        {
            return TestDataFactory.CreateRegisterRequest(name, email, phoneNumber, password, "rider");
        }
    }

    public static class Driver
    {
        public static RegisterRequest CreateRegisterRequest(
            string? name = null,
            string? email = null,
            string? phoneNumber = null,
            string? password = null)
        {
            return TestDataFactory.CreateRegisterRequest(name, email, phoneNumber, password, "driver");
        }
    }
}
