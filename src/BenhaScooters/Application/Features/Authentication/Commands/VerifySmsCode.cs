using BenhaScooters.Application.Features.Authentication.Commands.Common;
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

public record VerifySmsCodeCommand(UserId UserId, string Code) : IRequest<ErrorOr<OnboardingStatusToken>>;

public class VerifySmsCodeCommandValidator : AbstractValidator<VerifySmsCodeCommand>
{
    public VerifySmsCodeCommandValidator()
    {
        RuleFor(x => x.UserId.Value)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Verification code is required.")
            .Length(6).WithMessage("Verification code must be 6 digits.")
            .Matches(@"^\d{6}$").WithMessage("Verification code must contain only digits.");
    }
}

public class VerifySmsCodeCommandHandler : IRequestHandler<VerifySmsCodeCommand, ErrorOr<OnboardingStatusToken>>
{
    private readonly AppDbContext _db;
    private readonly IJwtService _jwtService;

    public VerifySmsCodeCommandHandler(AppDbContext db, IJwtService jwtService)
    {
        _db = db;
        _jwtService = jwtService;
    }

    public async Task<ErrorOr<OnboardingStatusToken>> Handle(VerifySmsCodeCommand request, CancellationToken cancellationToken)
    {
        // Find the verification code
        var verificationCode = await _db.SmsVerificationCodes
            .Where(x => x.UserId == request.UserId && x.Code == request.Code)
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

        var user = await _db.Users.FindAsync(new object[] { request.UserId }, cancellationToken);
        if (user is null)
        {
            return UserErrors.UserNotFound;
        }

        user.VerifyPhoneNumber();

        await _db.SaveChangesAsync(cancellationToken);

        var nextStep = UserOnboardingStateMachine.GetNextStep(user.Status);
        var token = _jwtService.GenerateOnboardingToken(user.Id, user.Status, nextStep);

        return new OnboardingStatusToken(token, nextStep);
    }
}
