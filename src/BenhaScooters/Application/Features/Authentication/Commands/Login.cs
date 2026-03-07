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

public record LoginCommand(PhoneNumber PhoneNumber, string Password, App App) : IRequest<ErrorOr<AuthenticationResponse>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة.");
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, ErrorOr<AuthenticationResponse>>
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuthenticationService _authenticationService;

    public LoginCommandHandler(AppDbContext db, IPasswordHasher passwordHasher, IAuthenticationService authenticationService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _authenticationService = authenticationService;
    }

    public async Task<ErrorOr<AuthenticationResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .Include(u => u.DriverProfile)
            .Include(u => u.RiderProfile)
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);

        if (user is null || user.PasswordHash is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return UserErrors.InvalidCredentials;
        }

        // Load or create profiles based on app
        var driverProfile = user.DriverProfile;
        var riderProfile = user.RiderProfile;

        // Create profile if it doesn't exist for the requested app
        if (request.App == App.DriverApp && driverProfile is null)
        {
            user.CreateDriverProfile();
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (request.App == App.RiderApp && riderProfile is null)
        {
            user.CreateRiderProfile();
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Check if phone is verified
        if (!user.PhoneNumberVerified)
        {
            var authenticatedResponse = await _authenticationService.GenerateAuthenticatedResponseAsync(user, request.App, user.DriverProfile, user.RiderProfile, cancellationToken);
            return new AuthenticationResponse("onboarding_required", new OnboardingRequired(authenticatedResponse.AccessToken, "verify_phone"));
        }

        var successResponse = await _authenticationService.GenerateAuthenticatedResponseAsync(user, request.App, user.DriverProfile, user.RiderProfile, cancellationToken);
        var result = new AuthenticationSuccess(successResponse.AccessToken, successResponse.RefreshToken, successResponse.ExpiresAt);
        return new AuthenticationResponse("success", result);
    }
}
