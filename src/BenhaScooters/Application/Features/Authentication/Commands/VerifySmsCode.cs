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

public record VerifySmsCodeCommand(UserId UserId, string Code, App App, string? IpAddress) : IRequest<ErrorOr<AuthenticationResponse>>;

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
    private readonly IOtpSecurityService _otpSecurity;

    public VerifySmsCodeCommandHandler(
        AppDbContext db,
        IAuthenticationService authenticationService,
        IOtpSecurityService otpSecurity)
    {
        _db = db;
        _authenticationService = authenticationService;
        _otpSecurity = otpSecurity;
    }

    public async Task<ErrorOr<AuthenticationResponse>> Handle(VerifySmsCodeCommand request, CancellationToken cancellationToken)
    {
        // Delegate verification to OtpSecurityService (handles brute-force, rate limiting, etc.)
        var verifyResult = await _otpSecurity.VerifyCodeAsync(request.UserId, request.Code, request.IpAddress, cancellationToken);
        if (verifyResult.IsError)
        {
            return verifyResult.Errors;
        }

        // Code is valid — proceed with user verification
        var user = await _db.Users
            .Include(u => u.DriverProfile)
            .Include(u => u.RiderProfile)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return UserErrors.UserNotFound;
        }

        user.VerifyPhoneNumber(user.PhoneNumber);

        // Create profile if it doesn't exist for the requested app
        if (request.App == App.DriverApp && user.DriverProfile is null)
        {
            user.CreateDriverProfile();
        }
        else if (request.App == App.RiderApp && user.RiderProfile is null)
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

