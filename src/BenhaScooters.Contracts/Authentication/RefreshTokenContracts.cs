using FluentValidation;

namespace BenhaScooters.Contracts.Authentication;

public record RefreshTokenRequest(string RefreshToken, App App);

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
        RuleFor(x => x.App).IsInEnum();
    }
}
