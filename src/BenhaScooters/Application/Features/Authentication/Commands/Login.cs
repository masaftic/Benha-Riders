using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Infrastructure.Authentication.Services;
using BenhaScooters.Shared.Validation;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Authentication.Commands;

public record LoginCommand(string Email, string Password) : IRequest<ErrorOr<LoginResponse>>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Matches(ValidationRegex.Email).WithMessage("A valid email is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

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
            .FirstOrDefaultAsync(u => u.EmailNormalized == User.NormalizeEmail(Email.From(request.Email)), cancellationToken);
        
        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return UserErrors.InvalidCredentials;
        }

        if (!user.PhoneNumberVerified)
        {
            return UserErrors.PhoneNotVerified;
        }

        var driver = await _db.Drivers
            .FirstOrDefaultAsync(d => d.UserId == user.Id, cancellationToken);
        
        var rider = await _db.Riders
            .FirstOrDefaultAsync(r => r.UserId == user.Id, cancellationToken);

        var expiresAt = _jwtService.GetAccessTokenExpiryTime();
        var accessToken = _jwtService.GenerateAccessToken(user, driver, rider);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Create and store refresh token
        var refreshTokenEntity = user.CreateRefreshToken(refreshToken, _jwtService.GetRefreshTokenExpiryTime());
        await _db.SaveChangesAsync(cancellationToken);

        return new LoginResponse(accessToken, refreshToken, expiresAt);
    }
}
