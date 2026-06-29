using BenhaScooters.Contracts.Common;
using FluentValidation;

namespace BenhaScooters.Contracts.Authentication;

// ──── Forgot Password ────────────────────────────────────────────────

public record ForgotPasswordRequest(string PhoneNumber);

public record ForgotPasswordResponseDto(string Message, int NextCooldownSeconds);

// ──── Reset Password with OTP ────────────────────────────────────────

public record ResetPasswordWithOtpRequest(
    string PhoneNumber,
    string OtpCode,
    string NewPassword,
    string ConfirmPassword);

public record ResetPasswordResponseDto(string Message);

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.PhoneNumber).EgyptianPhoneNumber();
    }
}

public class ResetPasswordWithOtpRequestValidator : AbstractValidator<ResetPasswordWithOtpRequest>
{
    public ResetPasswordWithOtpRequestValidator()
    {
        RuleFor(x => x.PhoneNumber).EgyptianPhoneNumber();
        RuleFor(x => x.OtpCode).NotEmpty().Length(6).WithMessage("كود التحقق يجب أن يكون 6 أرقام.");
        RuleFor(x => x.NewPassword).Password();
        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.NewPassword)
            .WithMessage("كلمة المرور وتأكيدها غير متطابقين.");
    }
}
