using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Features.Authentication;

public record MeResponse(
    UserId Id,
    string Name,
    Email Email,
    bool EmailVerified,
    PhoneNumber PhoneNumber,
    bool PhoneNumberVerified,
    DateTime CreatedAt,
    IEnumerable<RoleName> Roles);


public class MeEndpoint(AppDbContext db) : EndpointWithoutRequest<MeResponse>
{
    public override void Configure()
    {
        Get("/auth/me");
        Claims(JwtClaims.Sub);
        Description(x => x
            .WithSummary("Get current user information")
            .Produces<MeResponse>()
            .Produces(401));

        Summary(s =>
        {
            s.Summary = "Get current user information";
            s.Description = "Returns the current authenticated user's profile information including name, email, phone number, verification status, and roles.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        var user = await db.Users
            .Include(u => u.Roles)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            ThrowError("User not found.", errorCode: "UserNotFound", statusCode: 404);
            return;
        }

        var response = new MeResponse(
            user.Id,
            user.Name,
            user.Email,
            user.EmailVerified,
            user.PhoneNumber,
            user.PhoneNumberVerified,
            user.CreatedAt,
            user.Roles.Select(r => r.Name));

        await SendAsync(response, cancellation: ct);
    }
}

