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

public record LoginCommand(string PhoneNumber, string Password) : IRequest<ErrorOr<LoginResponse>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("رقم الهاتف مطلوب.")
            .Matches(ValidationRegex.PhoneNumber).WithMessage("رقم هاتف صالح مطلوب.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("كلمة المرور مطلوبة.");
    }
}

public record LoginResponse;
public record LoginSuccess(string AccessToken, string RefreshToken, DateTime ExpiresAt) : LoginResponse;
public record OnboardingRequired(string OnboardingToken, string NextStep) : LoginResponse;


public class LoginCommandHandler : IRequestHandler<LoginCommand, ErrorOr<LoginResponse>>
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public LoginCommandHandler(AppDbContext db, IPasswordHasher passwordHasher, IJwtService jwtService)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<ErrorOr<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.PhoneNumberNormalized == User.NormalizePhone(PhoneNumber.From(request.PhoneNumber)), cancellationToken);

        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return UserErrors.InvalidCredentials;
        }

        if (user.Status != UserStatus.Active)
        {
            var nextStep = UserOnboardingStateMachine.GetNextStep(user.Status);
            var token = _jwtService.GenerateOnboardingToken(user.Id, user.Status, nextStep);
            return new OnboardingRequired(token, nextStep);
        }

        var driver = await _db.Drivers
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.UserId == user.Id, cancellationToken);

        var rider = await _db.Riders
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.UserId == user.Id, cancellationToken);

        var expiresAt = _jwtService.GetAccessTokenExpiryTime();
        var accessToken = _jwtService.GenerateAccessToken(user, driver, rider);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Create and store refresh token
        var refreshTokenEntity = user.CreateRefreshToken(refreshToken, _jwtService.GetRefreshTokenExpiryTime());
        await _db.SaveChangesAsync(cancellationToken);

        return new LoginSuccess(accessToken, refreshToken, expiresAt);
    }
}
