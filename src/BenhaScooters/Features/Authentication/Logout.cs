using BenhaScooters.Data;
using BenhaScooters.Domain;
using FastEndpoints;
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
    }

    public override async Task HandleAsync(LogoutRequest req, CancellationToken ct)
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userIdGuid))
        {
            ThrowError("Unauthorized access.", errorCode: "Unauthorized", statusCode: 401);
            return;
        }

        var userId = UserId.From(userIdGuid);
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
