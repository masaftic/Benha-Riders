
using BenhaScooters.Presentation.Endpoints.Authentication;

namespace BenhaScooters.IntegrationTests;

public static class TestDataFactory
{
    private static int _userCounter = 0;
    private static readonly object _lock = new();

    public static RegisterEndpoint.RegisterRequestDto CreateRegisterRequest(
        string? name = null,
        string? email = null,
        string? phoneNumber = null,
        string? password = null,
        string role = "rider")
    {
        lock (_lock)
        {
            _userCounter++;
            return new RegisterEndpoint.RegisterRequestDto(
                Name: name ?? $"Test User {_userCounter}",
                Email: email ?? $"testuser{_userCounter}@example.com",
                PhoneNumber: phoneNumber ?? $"+20123456{_userCounter:D4}",
                Password: password ?? "password123",
                Role: role
            );
        }
    }

    public static LoginEndpoint.LoginRequestDto CreateLoginRequest(string email, string password = "password123")
    {
        return new LoginEndpoint.LoginRequestDto(Email: email, Password: password);
    }

    public static SendSmsVerificationEndpoint.SendSmsVerificationRequestDto CreateSmsVerificationRequest(string phoneNumber)
    {
        return new SendSmsVerificationEndpoint.SendSmsVerificationRequestDto(phoneNumber);
    }

    public static VerifySmsCodeEndpoint.VerifySmsCodeRequestDto CreateVerifySmsCodeRequest(string phoneNumber, string code)
    {
        return new VerifySmsCodeEndpoint.VerifySmsCodeRequestDto(phoneNumber, code);
    }

    public static class Rider
    {
        public static RegisterEndpoint.RegisterRequestDto CreateRegisterRequest(
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
        public static RegisterEndpoint.RegisterRequestDto CreateRegisterRequest(
            string? name = null,
            string? email = null,
            string? phoneNumber = null,
            string? password = null)
        {
            return TestDataFactory.CreateRegisterRequest(name, email, phoneNumber, password, "driver");
        }
    }
}
