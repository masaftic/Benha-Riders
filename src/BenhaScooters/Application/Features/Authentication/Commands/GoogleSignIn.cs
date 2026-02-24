using BenhaScooters.Application.Features.Authentication.Commands.Common;
using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;
using ErrorOr;
using FluentValidation;
using Google.Apis.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record GoogleSignInCommand(string IdToken, App App) : IRequest<ErrorOr<AuthenticationResponse>>;

public class GoogleSignInCommandValidator : AbstractValidator<GoogleSignInCommand>
{
    public GoogleSignInCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("رمز Google ID مطلوب.");
    }
}

public class GoogleSignInCommandHandler : IRequestHandler<GoogleSignInCommand, ErrorOr<AuthenticationResponse>>
{
    private readonly AppDbContext _db;
    private readonly IGoogleAuthService _googleAuthService;
    private readonly IAuthenticationService _authenticationService;

    public GoogleSignInCommandHandler(
        AppDbContext db,
        IGoogleAuthService googleAuthService,
        IAuthenticationService authenticationService)
    {
        _db = db;
        _googleAuthService = googleAuthService;
        _authenticationService = authenticationService;
    }

    public async Task<ErrorOr<AuthenticationResponse>> Handle(GoogleSignInCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var payload = await _googleAuthService.ValidateTokenAsync(request.IdToken);
            var googleId = payload.Subject;

            var existingAuth = await _db.ExternalAuths
                .Include(ea => ea.User)
                .FirstOrDefaultAsync(x => x.ProviderUserId == googleId && x.Provider == "Google", cancellationToken);

            if (existingAuth is null)
            {
                return await HandleNewUser(payload, googleId, request.App, cancellationToken);
            }

            return await HandleExistingUser(existingAuth.User, request.App, cancellationToken);
        }
        catch (InvalidJwtException)
        {
            return UserErrors.InvalidGoogleIdToken;
        }
    }

    private async Task<ErrorOr<AuthenticationResponse>> HandleNewUser(
        GoogleJsonWebSignature.Payload payload,
        string googleId,
        App app,
        CancellationToken cancellationToken)
    {
        var newUser = new User(payload.Name, Email.Create(payload.Email), null, null);

        if (payload.EmailVerified)
        {
            newUser.VerifyEmail();
        }

        newUser.AddExternalAuth(new ExternalAuth("Google", googleId));

        _db.Users.Add(newUser);

        if (app == App.DriverApp)
        {
            newUser.CreateDriverProfile();
        }
        else if (app == App.RiderApp)
        {
            newUser.CreateRiderProfile();
        }

        await _db.SaveChangesAsync(cancellationToken);

        // Google users still need to verify phone
        var authenticatedResponse = await _authenticationService.GenerateAuthenticatedResponseAsync(newUser, app, newUser.DriverProfile, newUser.RiderProfile, cancellationToken);
        return new AuthenticationResponse("onboarding_required", new OnboardingRequired(authenticatedResponse.AccessToken, "verify_phone"));
    }

    private async Task<ErrorOr<AuthenticationResponse>> HandleExistingUser(User user, App app, CancellationToken cancellationToken)
    {
        // Load or create profiles based on app
        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(d => d.UserId == user.Id, cancellationToken);

        var riderProfile = await _db.RiderProfiles
            .FirstOrDefaultAsync(r => r.UserId == user.Id, cancellationToken);

        // Create profile if it doesn't exist for the requested app
        if (app == App.DriverApp && driverProfile is null)
        {
            driverProfile = new DriverProfile(user.Id);
            _db.DriverProfiles.Add(driverProfile);
            await _db.SaveChangesAsync(cancellationToken);
        }
        else if (app == App.RiderApp && riderProfile is null)
        {
            riderProfile = new RiderProfile(user.Id, user.Name);
            _db.RiderProfiles.Add(riderProfile);
            await _db.SaveChangesAsync(cancellationToken);
        }

        // Check if phone is verified
        if (!user.PhoneNumberVerified)
        {
            var onboardingResponse = await _authenticationService.GenerateAuthenticatedResponseAsync(user, app, driverProfile, riderProfile, cancellationToken);
            return new AuthenticationResponse("onboarding_required", new OnboardingRequired(onboardingResponse.AccessToken, "verify_phone"));
        }

        var authenticatedResponse = await _authenticationService.GenerateAuthenticatedResponseAsync(user, app, driverProfile, riderProfile, cancellationToken);
        return new AuthenticationResponse("success", new AuthenticationSuccess(authenticatedResponse.AccessToken, authenticatedResponse.RefreshToken, authenticatedResponse.ExpiresAt));
    }
}

