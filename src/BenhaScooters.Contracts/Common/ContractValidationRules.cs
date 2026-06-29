using BenhaScooters.Shared.Validation;
using FluentValidation;

namespace BenhaScooters.Contracts.Common;

public static class ContractValidationRules
{
    public static IRuleBuilderOptions<T, string> EgyptianPhoneNumber<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty().WithMessage("رقم الهاتف مطلوب.")
            .Matches(ValidationRegex.PhoneNumber).WithMessage("رقم الهاتف غير صالح. يجب أن يكون رقمًا مصريًا.");

    public static IRuleBuilderOptions<T, string> EmailAddressContract<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty().WithMessage("Email cannot be empty.")
            .Matches(ValidationRegex.Email).WithMessage("Invalid email format.");

    public static IRuleBuilderOptions<T, string> EgyptianNationalId<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .Matches(ValidationRegex.NationalId)
            .WithMessage("الرقم القومي يجب أن يكون 14 رقم بالضبط.");

    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty()
            .MinimumLength(6)
            .WithMessage("كلمة المرور يجب أن تكون 6 أحرف على الأقل.");
}
