using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;
using BenhaScooters.Shared.Validation;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record SendSmsVerificationCommand(UserId UserId, PhoneNumber PhoneNumber, string? IpAddress) : IRequest<ErrorOr<SendSmsVerificationResponse>>;


public record SendSmsVerificationResponse(string Message, int NextCooldownSeconds);

public class SendSmsVerificationCommandHandler : IRequestHandler<SendSmsVerificationCommand, ErrorOr<SendSmsVerificationResponse>>
{
    private readonly AppDbContext _db;
    private readonly ISmsService _smsService;
    private readonly IOtpSecurityService _otpSecurity;

    public SendSmsVerificationCommandHandler(AppDbContext db, ISmsService smsService, IOtpSecurityService otpSecurity)
    {
        _db = db;
        _smsService = smsService;
        _otpSecurity = otpSecurity;
    }

    public async Task<ErrorOr<SendSmsVerificationResponse>> Handle(SendSmsVerificationCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return UserErrors.UserNotFound;
        }

        if (user.PhoneNumberVerified)
        {
            return UserErrors.PhoneAlreadyVerified;
        }

        // Delegate all security checks to OtpSecurityService
        var otpResult = await _otpSecurity.RequestCodeAsync(request.UserId, request.PhoneNumber, request.IpAddress, cancellationToken);
        if (otpResult.IsError)
        {
            return otpResult.Errors;
        }

        // Send SMS (the plaintext code is only used here, never stored)
        await _smsService.SendVerificationCodeAsync(user.Id, request.PhoneNumber, otpResult.Value.Code);

        return new SendSmsVerificationResponse(
            "تم إرسال رمز التحقق بنجاح.",
            otpResult.Value.NextCooldownSeconds);
    }
}
