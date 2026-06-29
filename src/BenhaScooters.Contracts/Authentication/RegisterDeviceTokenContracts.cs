using FluentValidation;

namespace BenhaScooters.Contracts.Authentication;

public record RegisterDeviceTokenRequest(string Token, string Platform);

public class RegisterDeviceTokenRequestValidator : AbstractValidator<RegisterDeviceTokenRequest>
{
    public RegisterDeviceTokenRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(512);
        RuleFor(x => x.Platform).NotEmpty().Must(p => p is "android" or "ios")
            .WithMessage("Platform must be 'android' or 'ios'.");
    }
}
