using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FastEndpoints.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Authentication;

public record LogoutRequest(string? RefreshToken = null);

public class LogoutRequestValidator : Validator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        // RefreshToken is optional - if not provided, we'll revoke all tokens for the user
    }
}

public record LogoutResponse(string Message);

public class LogoutEndpoint(AppDbContext db) : Endpoint<LogoutRequest, LogoutResponse>
{
    public override void Configure()
    {
        Post("/auth/logout");
        Claims("UserId");
        Description(x => x
            .WithSummary("Logout user")
            .Produces<LogoutResponse>()
            .Produces(401));

        Summary(s =>
        {
            s.Summary = "Logout user";
            s.Description = "Logs out the current user by invalidating all their refresh tokens. Requires authentication.";
            s.ExampleRequest = new LogoutRequest("your-refresh-token-here");
        });
    }

    public override async Task HandleAsync(LogoutRequest req, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        var user = await db.Users
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            ThrowError("User not found.", errorCode: "UserNotFound", statusCode: 404);
            return;
        }

        if (!string.IsNullOrEmpty(req.RefreshToken))
        {
            // Revoke specific refresh token
            user.RevokeRefreshToken(req.RefreshToken);
        }
        else
        {
            // Revoke all refresh tokens for the user
            user.RevokeAllRefreshTokens();
        }

        await db.SaveChangesAsync(ct);

        await SendAsync(new LogoutResponse("Logged out successfully"), cancellation: ct);
    }
}
