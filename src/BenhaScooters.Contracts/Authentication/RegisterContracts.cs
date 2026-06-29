using BenhaScooters.Contracts.Common;
using FluentValidation;

namespace BenhaScooters.Contracts.Authentication;

public record RegisterRequest(
    string Name,
    string Email,
    string PhoneNumber,
    string Password,
    App App);

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("اسم المستخدم مطلوب.");
        RuleFor(x => x.Email).EmailAddressContract();
        RuleFor(x => x.PhoneNumber).EgyptianPhoneNumber();
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.App).IsInEnum();
    }
}
