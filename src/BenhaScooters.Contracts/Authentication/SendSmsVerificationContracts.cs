using BenhaScooters.Contracts.Common;
using FluentValidation;

namespace BenhaScooters.Contracts.Authentication;

public record SendSmsVerificationRequest(string PhoneNumber);

public class SendSmsVerificationRequestValidator : AbstractValidator<SendSmsVerificationRequest>
{
    public SendSmsVerificationRequestValidator()
    {
        RuleFor(x => x.PhoneNumber).EgyptianPhoneNumber();
    }
}
