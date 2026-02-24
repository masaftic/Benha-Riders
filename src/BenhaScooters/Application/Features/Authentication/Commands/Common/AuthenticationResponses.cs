using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace BenhaScooters.Application.Features.Authentication.Commands.Common;


// type is onboarding_required or success

public static class AuthenticationResultType
{
    public const string Success = "success";
    public const string OnboardingRequired = "onboarding_required";

    public static bool IsValid(string type) => type == Success || type == OnboardingRequired;
}


public record AuthenticationResponse(string Type, object Result);

public record AuthenticationSuccess(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public record OnboardingRequired(string OnboardingToken, string NextStep);
