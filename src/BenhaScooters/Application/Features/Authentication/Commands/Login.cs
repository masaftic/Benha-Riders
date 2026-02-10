using BenhaScooters.Application.Features.Authentication.Commands.Common;
using BenhaScooters.Application.Services;
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

public record LoginCommand(PhoneNumber PhoneNumber, string Password) : IRequest<ErrorOr<AuthenticationResponse>>;

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
    private readonly IJwtService _jwtService;
    private readonly IAuthenticationService _authenticationService;

    public LoginCommandHandler(AppDbContext db, IPasswordHasher passwordHasher, IJwtService jwtService, IAuthenticationService authenticationService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _authenticationService = authenticationService;
    }

    public async Task<ErrorOr<AuthenticationResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.PhoneNumber == request.PhoneNumber, cancellationToken);

        if (user is null || user.PasswordHash is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return UserErrors.InvalidCredentials;
        }

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
        var result = new AuthenticationSuccess(authenticatedResponse.AccessToken, authenticatedResponse.RefreshToken, authenticatedResponse.ExpiresAt);
        return new AuthenticationResponse("success", result);
    }
}
