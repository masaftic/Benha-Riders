using System.Security.Claims;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Shared.Security;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Presentation.Endpoints;

public static class HttpContextExtensions
{
    public static UserId GetCurrentUserId(this HttpContext context)
    {
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

    public static UserId GetDriverId(this HttpContext context)
    {
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
    
    public static UserId GetRiderId(this HttpContext context)
    {
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
