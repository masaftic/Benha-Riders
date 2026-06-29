using BenhaScooters.Contracts.Common;
using FluentValidation;

namespace BenhaScooters.Contracts.Authentication;

public record LoginRequest(string PhoneNumber, string Password, App App);

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.PhoneNumber).EgyptianPhoneNumber();
        RuleFor(x => x.Password).NotEmpty();
        RuleFor(x => x.App).IsInEnum();
    }
}
