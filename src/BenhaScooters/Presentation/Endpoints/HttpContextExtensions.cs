using System.Security.Claims;
using BenhaScooters.Domain;
using BenhaScooters.Shared.Security;

namespace BenhaScooters.Presentation.Endpoints;

public static class HttpContextExtensions
{
    public static UserId GetCurrentUserId(this HttpContext context)
    {
        // Exceptions are ok here because this is an exceptional state, not error flows.

        if (context.User.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null)
        {
            throw new InvalidOperationException("Name identifier claim not found.");
        }

        return UserId.From(int.Parse(userIdClaim.Value));
    } 
}
