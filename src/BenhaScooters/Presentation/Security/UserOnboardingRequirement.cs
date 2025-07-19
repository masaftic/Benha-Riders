using BenhaScooters.Domain;
using BenhaScooters.Domain.Users;
using BenhaScooters.Shared.Security;
using Microsoft.AspNetCore.Authorization;

namespace BenhaScooters.Presentation.Security;

public class UserOnboardingRequirement : IAuthorizationRequirement
{
    public string Step { get; }

    public UserOnboardingRequirement(string step)
    {
        if (!OnboardingSteps.IsValidStep(step))
            throw new ArgumentException($"Invalid onboarding step: {step}", nameof(step));
        
        Step = step;
    }
}


public class UserOnboardingRequirementHandler : AuthorizationHandler<UserOnboardingRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, UserOnboardingRequirement requirement)
    {
        // Check if the user is hitting the required onboarding step
        if (context.User.HasClaim(c => c.Type == JwtClaims.NextStep && c.Value == requirement.Step))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }

        return Task.CompletedTask;
    }
}