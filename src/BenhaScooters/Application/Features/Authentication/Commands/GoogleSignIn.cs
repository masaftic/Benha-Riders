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

public record GoogleSignInCommand(string IdToken) : IRequest<ErrorOr<GoogleSignInResponse>>;

public class GoogleSignInCommandValidator : AbstractValidator<GoogleSignInCommand>
{
    public GoogleSignInCommandValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty().WithMessage("رمز Google ID مطلوب.");
    }
}

public abstract record GoogleSignInResponse;
public record GoogleSignInSuccess(string AccessToken, string RefreshToken, DateTime ExpiresAt) : GoogleSignInResponse;
public record GoogleSignInOnboardingRequired(string OnboardingToken, string NextStep) : GoogleSignInResponse;

public class GoogleSignInCommandHandler : IRequestHandler<GoogleSignInCommand, ErrorOr<GoogleSignInResponse>>
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

    public async Task<ErrorOr<GoogleSignInResponse>> Handle(GoogleSignInCommand request, CancellationToken cancellationToken)
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

    private async Task<ErrorOr<GoogleSignInResponse>> HandleNewUser(
        GoogleJsonWebSignature.Payload payload, 
        string googleId, 
        CancellationToken cancellationToken)
    {
        var newUser = new User(payload.Name, Email.From(payload.Email), null, null);
        
        if (payload.EmailVerified)
        {
            newUser.VerifyEmail();
        }
        
        newUser.AddExternalAuth(new ExternalAuth("Google", googleId));

        _db.Users.Add(newUser);
        await _db.SaveChangesAsync(cancellationToken);

        var nextStep = UserOnboardingStateMachine.GetNextStep(newUser.Status);
        var token = _jwtService.GenerateOnboardingToken(newUser.Id, newUser.Status, nextStep);
        
        return new GoogleSignInOnboardingRequired(token, nextStep);
    }

    private async Task<ErrorOr<GoogleSignInResponse>> HandleExistingUser(User user, CancellationToken cancellationToken)
    {
        if (user.Status != UserStatus.Active)
        {
            var nextStep = UserOnboardingStateMachine.GetNextStep(user.Status);
            var token = _jwtService.GenerateOnboardingToken(user.Id, user.Status, nextStep);
            return new GoogleSignInOnboardingRequired(token, nextStep);
        }

        var driver = await _db.Drivers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == user.Id, cancellationToken);

        var rider = await _db.Riders
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == user.Id, cancellationToken);

        var authenticatedResponse = await _authenticationService.GenerateAuthenticatedResponseAsync(user, driver, rider, cancellationToken);
        return new GoogleSignInSuccess(authenticatedResponse.AccessToken, authenticatedResponse.RefreshToken, authenticatedResponse.ExpiresAt);
    }
}
