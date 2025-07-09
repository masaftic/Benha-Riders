using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Features.Authentication.Services;
using FastEndpoints;
using FastEndpoints.Security;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Authentication;

public record LoginRequest(string Email, string Password);

public class LoginRequestValidator : Validator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}

public record LoginResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);


public class UserLoginEndpoint(AppDbContext db, IPasswordHasher passwordHasher, IJwtService jwtService) : Endpoint<LoginRequest>
{
    public override void Configure()
    {
        Post("/auth/login");
        AllowAnonymous();
        Description(x => x
            .WithSummary("User login")
            .Produces<LoginResponse>()
            .Produces(401));
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var user = await db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.EmailNormalized == Domain.User.NormalizeEmail(Email.From(req.Email)), ct);
        
        if (user is null || !passwordHasher.Verify(user.PasswordHash, req.Password))
        {
            ThrowError("Invalid Email Or Password", errorCode: "InvalidCredentials", statusCode: 401);
            return;
        }

        var expiresAt = jwtService.GetAccessTokenExpiryTime();
        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();

        // Create and store refresh token
        var refreshTokenEntity = user.CreateRefreshToken(refreshToken, jwtService.GetRefreshTokenExpiryTime());
        await db.SaveChangesAsync(ct);

        await SendAsync(new LoginResponse(accessToken, refreshToken, expiresAt), cancellation: ct);
    }
}
