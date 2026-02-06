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

public record SendSmsVerificationCommand(UserId UserId, PhoneNumber PhoneNumber) : IRequest<ErrorOr<SendSmsVerificationResponse>>;


public record SendSmsVerificationResponse(string Message);

public class SendSmsVerificationCommandHandler : IRequestHandler<SendSmsVerificationCommand, ErrorOr<SendSmsVerificationResponse>>
{
    private readonly AppDbContext _db;
    private readonly ISmsService _smsService;

    public SendSmsVerificationCommandHandler(AppDbContext db, ISmsService smsService)
    {
        _db = db;
        _smsService = smsService;
    }

    public async Task<ErrorOr<SendSmsVerificationResponse>> Handle(SendSmsVerificationCommand request, CancellationToken cancellationToken)
    {
        var normalizedPhone = request.PhoneNumber;
        
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

        // Check if there's a recent verification code (prevent spam)
        var recentCode = await _db.SmsVerificationCodes
            .Where(x => x.UserId == user.Id && x.CreatedAt > DateTime.UtcNow.AddMinutes(-1))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (recentCode != null)
        {
            return UserErrors.TooManyVerificationRequests;
        }

        // Generate and save verification code
        var code = SmsVerificationCode.GenerateCode();
        var verificationCode = new SmsVerificationCode(user.Id, normalizedPhone, code, TimeSpan.FromMinutes(10));
        
        _db.SmsVerificationCodes.Add(verificationCode);
        await _db.SaveChangesAsync(cancellationToken);

        // Send SMS
        await _smsService.SendVerificationCodeAsync(normalizedPhone, code);

        return new SendSmsVerificationResponse(code); // In production, this should be "Verification code sent successfully"
    }
}
