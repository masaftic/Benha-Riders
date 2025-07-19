namespace BenhaScooters.Domain.Users;

/// <summary>
/// Defines all possible onboarding steps in the registration flow
/// </summary>
public static class OnboardingSteps
{
    public const string VerifyPhone = "verify_phone";
    public const string SelectRole = "select_role";

    public static readonly string[] AllSteps = [VerifyPhone, SelectRole];

    public static bool IsValidStep(string step) => AllSteps.Contains(step);
}
