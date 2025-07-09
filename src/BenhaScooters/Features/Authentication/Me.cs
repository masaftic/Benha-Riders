using BenhaScooters.Data;
using BenhaScooters.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Authentication;

public record MeResponse(
    UserId Id,
    string Name,
    string Email,
    bool EmailVerified,
    string PhoneNumber,
    bool PhoneNumberVerified,
    DateTime CreatedAt,
    IEnumerable<string> Roles);

public class MeEndpoint(AppDbContext db) : EndpointWithoutRequest<MeResponse>
{
    public override void Configure()
    {
        Get("/auth/me");
        Claims("UserId");
        Description(x => x
            .WithSummary("Get current user information")
            .Produces<MeResponse>()
            .Produces(401));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
        
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userIdGuid))
        {
            ThrowError("Unauthorized access.", errorCode: "Unauthorized", statusCode: 401);
            return;
        }

        var userId = UserId.From(userIdGuid);
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
            user.Email.Value,
            user.EmailVerified,
            user.PhoneNumber.Value,
            user.PhoneNumberVerified,
            user.CreatedAt,
            user.Roles.Select(r => r.Name.ToString()));

        await SendAsync(response, cancellation: ct);
    }
}
