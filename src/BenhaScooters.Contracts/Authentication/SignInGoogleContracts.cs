using FluentValidation;

namespace BenhaScooters.Contracts.Authentication;

public record SignInGoogleRequest(string IdToken, App App);

public class SignInGoogleRequestValidator : AbstractValidator<SignInGoogleRequest>
{
    public SignInGoogleRequestValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty().WithMessage("Google ID token is required.");
        RuleFor(x => x.App).IsInEnum();
    }
}
