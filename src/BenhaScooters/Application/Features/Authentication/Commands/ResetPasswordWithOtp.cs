using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record ResetPasswordWithOtpCommand(
    PhoneNumber PhoneNumber,
    string OtpCode,
    string NewPassword,
    string ConfirmPassword,
    string? IpAddress) : IRequest<ErrorOr<ResetPasswordResponse>>;

public record ResetPasswordResponse(string Message);

public class ResetPasswordWithOtpCommandValidator : AbstractValidator<ResetPasswordWithOtpCommand>
{
    public ResetPasswordWithOtpCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("رقم الهاتف مطلوب.");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("رمز التحقق مطلوب.")
            .Length(6).WithMessage("رمز التحقق يجب أن يكون 6 أرقام.")
            .Matches(@"^\d{6}$").WithMessage("رمز التحقق يجب أن يحتوي على أرقام فقط.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("كلمة المرور الجديدة مطلوبة.")
            .MinimumLength(6).WithMessage("كلمة المرور يجب أن تكون 6 أحرف على الأقل.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("تأكيد كلمة المرور مطلوب.")
            .Equal(x => x.NewPassword).WithMessage("كلمة المرور وتأكيد كلمة المرور غير متطابقين.");
    }
}

public class ResetPasswordWithOtpCommandHandler : IRequestHandler<ResetPasswordWithOtpCommand, ErrorOr<ResetPasswordResponse>>
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOtpSecurityService _otpSecurity;

    public ResetPasswordWithOtpCommandHandler(
        AppDbContext db,
        IPasswordHasher passwordHasher,
        IOtpSecurityService otpSecurity)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _otpSecurity = otpSecurity;
    }

    public async Task<ErrorOr<ResetPasswordResponse>> Handle(
        ResetPasswordWithOtpCommand request,
        CancellationToken cancellationToken)
    {
        // Find user by phone number
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);

        if (user is null)
        {
            return AppErrors.User.NotFound();
        }

        // Verify OTP code
        var verifyResult = await _otpSecurity.VerifyCodeAsync(
            user.Id,
            request.OtpCode,
            request.IpAddress,
            cancellationToken);

        if (verifyResult.IsError)
        {
            return verifyResult.Errors;
        }

        // Hash the new password
        var passwordHash = _passwordHasher.Hash(request.NewPassword);

        // Change password (this also revokes all refresh tokens)
        user.ChangePassword(passwordHash);

        await _db.SaveChangesAsync(cancellationToken);

        return new ResetPasswordResponse("تم إعادة تعيين كلمة المرور بنجاح. يمكنك الآن تسجيل الدخول بكلمة المرور الجديدة.");
    }
}
