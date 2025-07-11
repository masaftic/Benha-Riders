using BenhaScooters.Domain;
using FastEndpoints;
using FastEndpoints.Security;

namespace BenhaScooters.Shared.Security;

public static class EndpointExtensions
{
    public static UserId GetCurrentUserId(this IEndpoint endpoint)
    {
        var userIdClaim = endpoint.HttpContext.User?.Claims.FirstOrDefault(c => c.Type == JwtClaims.Sub);
        if (userIdClaim == null)
            throw new UnauthorizedAccessException("User is not authenticated.");

        var userId = UserId.From(int.Parse(userIdClaim.Value));
        return userId;
    }
}
