using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Shared.Validation;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record VerifySmsCodeCommand(string PhoneNumber, string Code) : IRequest<ErrorOr<VerifySmsCodeResponse>>;

public class VerifySmsCodeCommandValidator : AbstractValidator<VerifySmsCodeCommand>
{
    public VerifySmsCodeCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(ValidationRegex.PhoneNumber).WithMessage("Invalid phone number format.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("Verification code must contain only digits.");
    }
}

public record VerifySmsCodeResponse(string Message, bool IsVerified);

public class VerifySmsCodeCommandHandler : IRequestHandler<VerifySmsCodeCommand, ErrorOr<VerifySmsCodeResponse>>
{
    private readonly AppDbContext _db;

    public VerifySmsCodeCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<VerifySmsCodeResponse>> Handle(VerifySmsCodeCommand request, CancellationToken cancellationToken)
    {
        var normalizedPhone = User.NormalizePhone(PhoneNumber.From(request.PhoneNumber));
        
        // Find user by phone number
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.PhoneNumberNormalized == normalizedPhone, cancellationToken);

        if (user is null)
        {
            return UserErrors.UserNotFound;
        }

        if (user.PhoneNumberVerified)
        {
            return new VerifySmsCodeResponse("Phone number is already verified.", true);
        }

        // Find the verification code
        var verificationCode = await _db.SmsVerificationCodes
            .Where(x => x.UserId == user.Id && x.PhoneNumber == normalizedPhone && x.Code == request.Code)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (verificationCode is null)
        {
            return UserErrors.InvalidVerificationCode;
        }

        if (!verificationCode.IsValid)
        {
            return verificationCode.IsUsed ? UserErrors.VerificationCodeAlreadyUsed : UserErrors.VerificationCodeExpired;
        }

        // Mark code as used and verify user's phone number
        verificationCode.MarkAsUsed();
        user.VerifyPhoneNumber();

        await _db.SaveChangesAsync(cancellationToken);

        return new VerifySmsCodeResponse("Phone number verified successfully.", true);
    }
}
