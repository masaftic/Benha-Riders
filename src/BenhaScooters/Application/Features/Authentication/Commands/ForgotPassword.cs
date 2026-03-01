using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record ForgotPasswordCommand(PhoneNumber PhoneNumber, string? IpAddress) : IRequest<ErrorOr<ForgotPasswordResponse>>;

public record ForgotPasswordResponse(string Message, int NextCooldownSeconds);

public class ForgotPasswordCommandValidator : AbstractValidator<ForgotPasswordCommand>
{
    public ForgotPasswordCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("رقم الهاتف مطلوب.");
    }
}

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ErrorOr<ForgotPasswordResponse>>
{
    private readonly AppDbContext _db;
    private readonly ISmsService _smsService;
    private readonly IOtpSecurityService _otpSecurity;

    public ForgotPasswordCommandHandler(
        AppDbContext db,
        ISmsService smsService,
        IOtpSecurityService otpSecurity)
    {
        _db = db;
        _smsService = smsService;
        _otpSecurity = otpSecurity;
    }

    public async Task<ErrorOr<ForgotPasswordResponse>> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        // Find user by phone number
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);

        if (user is null)
        {
            // Security: Don't reveal if phone number exists or not
            // Return success but don't send SMS
            return new ForgotPasswordResponse(
                "إذا كان رقم الهاتف مسجلاً، سيتم إرسال رمز التحقق.",
                60);
        }

        // Delegate all security checks to OtpSecurityService
        var otpResult = await _otpSecurity.RequestCodeAsync(
            user.Id,
            request.PhoneNumber,
            request.IpAddress,
            cancellationToken);

        if (otpResult.IsError)
        {
            return otpResult.Errors;
        }

        // Send SMS with the OTP code
        await _smsService.SendPasswordResetCodeAsync(user.Id, request.PhoneNumber, otpResult.Value.Code);

        return new ForgotPasswordResponse(
            "تم إرسال رمز إعادة تعيين كلمة المرور بنجاح.",
            otpResult.Value.NextCooldownSeconds);
    }
}
