namespace BenhaScooters.Shared.Security;

public static class JwtClaims
{
    public const string Sub = "sub";
    public const string Email = "email";
    public const string Name = "name";
    public const string PhoneNumber = "phone_number";
    public const string EmailVerified = "email_verified";
    public const string PhoneVerified = "phone_verified";
    public const string Status = "status";
    public const string NextStep = "next_step";
    public const string DriverId = "driver_id";
    public const string DriverOnboardingStatus = "driver_onboarding_status";
    public const string RiderId = "rider_id";
    public const string Roles = "roles";
}
