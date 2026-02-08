using BenhaScooters.Application.Features.Authentication.Commands.Common;
using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Authentication.Services;
using ErrorOr;
using FluentValidation;
using Google.Apis.Auth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record GoogleSignInCommand(string IdToken) : IRequest<ErrorOr<AuthenticationResponse>>;

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
    private readonly IJwtService _jwtService;
    private readonly IAuthenticationService _authenticationService;

    public GoogleSignInCommandHandler(
        AppDbContext db,
        IGoogleAuthService googleAuthService,
        IJwtService jwtService,
        IAuthenticationService authenticationService)
    {
        _db = db;
        _googleAuthService = googleAuthService;
        _jwtService = jwtService;
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
                .ThenInclude(u => u.Roles)
                .FirstOrDefaultAsync(x => x.ProviderUserId == googleId && x.Provider == "Google", cancellationToken);

            if (existingAuth is null)
            {
                return await HandleNewUser(payload, googleId, cancellationToken);
            }

            return await HandleExistingUser(existingAuth.User, cancellationToken);
        }
        catch (InvalidJwtException)
        {
            return UserErrors.InvalidGoogleIdToken;
        }
    }

    private async Task<ErrorOr<AuthenticationResponse>> HandleNewUser(
        GoogleJsonWebSignature.Payload payload,
        string googleId,
        CancellationToken cancellationToken)
    {
        var newUser = new User(payload.Name, Email.Create(payload.Email), null, null);

        if (payload.EmailVerified)
        {
            newUser.VerifyEmail();
        }

        newUser.AddExternalAuth(new ExternalAuth("Google", googleId));

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync(cancellationToken);

        var nextStep = UserOnboardingStateMachine.GetNextStep(newUser.Status);
        var token = _jwtService.GenerateOnboardingToken(newUser.Id, newUser.Status, nextStep);

        return new AuthenticationResponse("onboarding_required", new OnboardingRequired(token, nextStep));
    }

    private async Task<ErrorOr<AuthenticationResponse>> HandleExistingUser(User user, CancellationToken cancellationToken)
    {
        if (user.Status != UserStatus.Active)
        {
            var nextStep = UserOnboardingStateMachine.GetNextStep(user.Status);
            var token = _jwtService.GenerateOnboardingToken(user.Id, user.Status, nextStep);
            return new AuthenticationResponse("onboarding_required", new OnboardingRequired(token, nextStep));
        }

        var driverProfile = await _db.DriverProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == user.Id, cancellationToken);

        var riderProfile = await _db.RiderProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == user.Id, cancellationToken);

        var authenticatedResponse = await _authenticationService.GenerateAuthenticatedResponseAsync(user, driverProfile, riderProfile, cancellationToken);
        return new AuthenticationResponse("success", new AuthenticationSuccess(authenticatedResponse.AccessToken, authenticatedResponse.RefreshToken, authenticatedResponse.ExpiresAt));
    }
}
