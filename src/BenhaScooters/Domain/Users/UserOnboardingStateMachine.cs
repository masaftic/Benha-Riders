using ErrorOr;

namespace BenhaScooters.Domain.Users;

/// <summary>
/// Centralized state machine for user onboarding flow
/// Handles state transitions and determines next steps based on current user status
/// </summary>
public static class UserOnboardingStateMachine
{
    public static string GetNextStep(UserStatus status) => status switch
    {
        UserStatus.Registered => OnboardingSteps.VerifyPhone,
        UserStatus.PhoneVerified => OnboardingSteps.SelectRole,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown user status")
    };

    public static bool CanPerformStep(UserStatus userStatus, string requestedStep)
    {
        var currentStep = GetNextStep(userStatus);
        return currentStep == requestedStep;
    }

    public static ErrorOr<UserStatus> GetNewStatusAfterStep(UserStatus currentStatus, string completedStep)
    {
        if (!CanPerformStep(currentStatus, completedStep))
        {
            return Error.Validation(
                "INVALID_STEP_FOR_STATUS", 
                $"Cannot complete step '{completedStep}' from status '{currentStatus}'");
        }

        return completedStep switch
        {
            OnboardingSteps.VerifyPhone => UserStatus.PhoneVerified,
            OnboardingSteps.SelectRole => UserStatus.Active,
            _ => Error.Validation("INVALID_STEP", $"Unknown onboarding step: {completedStep}")
        };
    }
}
