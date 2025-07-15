using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Infrastructure.Authentication.Services;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Authentication;

public record RefreshTokenRequest(string RefreshToken);

public class RefreshTokenRequestValidator : Validator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Refresh token is required.");
    }
}

public record RefreshTokenResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);

public class RefreshTokenEndpoint(AppDbContext db, IJwtService jwtService) : Endpoint<RefreshTokenRequest, RefreshTokenResponse>
{
    public override void Configure()
    {
        Post("/auth/refresh");
        AllowAnonymous();
        Description(x => x
            .WithSummary("Refresh access token")
            .Produces<RefreshTokenResponse>()
            .Produces(400)
            .Produces(401));

        Summary(s =>
        {
            s.Summary = "Refresh access token";
            s.Description = "Exchanges a valid refresh token for a new access token and refresh token pair. The old refresh token is invalidated.";
            s.ExampleRequest = new RefreshTokenRequest("your-refresh-token-here");
        });
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var refreshToken = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == req.RefreshToken, ct);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            ThrowError(req => req.RefreshToken, "Invalid or expired refresh token.", errorCode: "InvalidRefreshToken", statusCode: 401);
            return;
        }

        var user = await db.Users
            .Include(u => u.Roles)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == refreshToken.UserId, ct);

        if (user is null)
        {
            ThrowError("User not found.", errorCode: "UserNotFound", statusCode: 404);
            return;
        }

        // Revoke the old refresh token
        refreshToken.Revoke();

        // Generate new tokens
        var accessToken = jwtService.GenerateAccessToken(user);
        var newRefreshToken = jwtService.GenerateRefreshToken();
        var expiresAt = jwtService.GetAccessTokenExpiryTime();

        // Create and store new refresh token
        var newRefreshTokenEntity = user.CreateRefreshToken(newRefreshToken, jwtService.GetRefreshTokenExpiryTime());
        await db.SaveChangesAsync(ct);

        await SendAsync(new RefreshTokenResponse(accessToken, newRefreshToken, expiresAt), cancellation: ct);
    }
}
