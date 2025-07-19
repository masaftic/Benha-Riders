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

    public static DriverId GetDriverId(this HttpContext context)
    {
        var driverIdClaim = context.User.FindFirst(JwtClaims.DriverId);
        if (driverIdClaim == null)
        {
            throw new InvalidOperationException("Driver ID claim not found. User is not a driver.");
        }

        return DriverId.From(int.Parse(driverIdClaim.Value));
    }
    
    public static RiderId GetRiderId(this HttpContext context)
    {
        var riderIdClaim = context.User.FindFirst(JwtClaims.RiderId);
        if (riderIdClaim == null)
        {
            throw new InvalidOperationException("Rider ID claim not found. User is not a rider.");
        }

        return RiderId.From(int.Parse(riderIdClaim.Value));
    }
}
