using FluentValidation;

namespace BenhaScooters.Contracts.Authentication;

public record VerifySmsCodeRequest(string Code, App App);

public class VerifySmsCodeRequestValidator : AbstractValidator<VerifySmsCodeRequest>
{
    public VerifySmsCodeRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().Length(6).WithMessage("كود التحقق يجب أن يكون 6 أرقام.");
        RuleFor(x => x.App).IsInEnum();
    }
}
