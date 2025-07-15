using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Features.Authentication.Services;
using BenhaScooters.Shared.Validation;
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
            .Matches(ValidationRegex.Email).WithMessage("A valid email is required.");

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
            .Produces(401)
            .Produces(403));

        Summary(s =>
        {
            s.Summary = "User login";
            s.Description = "Authenticates a user with email and password. Returns JWT access token and refresh token on successful authentication.";
            s.ExampleRequest = new LoginRequest("john.doe@example.com", "securePassword123");
        });
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

        if (!user.PhoneNumberVerified)
        {
            ThrowError("Phone number must be verified before login.", errorCode: "PhoneNotVerified", statusCode: 403);
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
