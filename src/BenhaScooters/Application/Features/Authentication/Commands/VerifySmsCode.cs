using BenhaScooters.Application.Features.Authentication.Commands.Common;
using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;
using BenhaScooters.Shared.Validation;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record VerifySmsCodeCommand(UserId UserId, string Code, App App) : IRequest<ErrorOr<AuthenticationResponse>>;

public class VerifySmsCodeCommandValidator : AbstractValidator<VerifySmsCodeCommand>
{
    public VerifySmsCodeCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("رمز التحقق مطلوب.")
            .Length(6).WithMessage("رمز التحقق يجب أن يكون 6 أرقام.")
            .Matches(@"^\d{6}$").WithMessage("رمز التحقق يجب أن يحتوي على أرقام فقط.");
    }
}

public class VerifySmsCodeCommandHandler : IRequestHandler<VerifySmsCodeCommand, ErrorOr<AuthenticationResponse>>
{
    private readonly AppDbContext _db;
    private readonly IAuthenticationService _authenticationService;

    public VerifySmsCodeCommandHandler(AppDbContext db, IAuthenticationService authenticationService)
    {
        _db = db;
        _authenticationService = authenticationService;
    }

    public async Task<ErrorOr<AuthenticationResponse>> Handle(VerifySmsCodeCommand request, CancellationToken cancellationToken)
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

        var user = await _db.Users
            .Include(u => u.DriverProfile)
            .Include(u => u.RiderProfile)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return UserErrors.UserNotFound;
        }

        user.VerifyPhoneNumber(verificationCode.PhoneNumber);

        // Load or create profiles based on app
        var driverProfile = user.DriverProfile;

        var riderProfile = user.RiderProfile;

        // Create profile if it doesn't exist for the requested app
        if (request.App == App.DriverApp && driverProfile is null)
        {
            user.CreateDriverProfile();
        }
        else if (request.App == App.RiderApp && riderProfile is null)
        {
            user.CreateRiderProfile();
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Phone is now verified, user is fully registered, return success
        var authenticatedResponse = await _authenticationService.GenerateAuthenticatedResponseAsync(user, request.App, user.DriverProfile, user.RiderProfile, cancellationToken);
        var result = new AuthenticationSuccess(authenticatedResponse.AccessToken, authenticatedResponse.RefreshToken, authenticatedResponse.ExpiresAt);
        return new AuthenticationResponse("success", result);
    }
}

