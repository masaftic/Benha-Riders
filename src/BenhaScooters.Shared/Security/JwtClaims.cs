namespace BenhaScooters.Shared.Security;

public static class JwtClaims
{
    public const string Sub = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string PhoneNumber = "phone_number";
    public const string EmailVerified = "email_verified";
    public const string PhoneVerified = "phone_verified";
    public const string App = "app";
    public const string DriverOnboardingStatus = "driver_onboarding_status";
    public const string Roles = "roles";
}
