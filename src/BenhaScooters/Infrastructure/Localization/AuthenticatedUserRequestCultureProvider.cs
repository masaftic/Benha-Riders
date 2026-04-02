using System.Security.Claims;
using BenhaScooters.Data;
using BenhaScooters.Domain.Users;
using BenhaScooters.Shared.Localization;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Infrastructure.Localization;

public sealed class AuthenticatedUserRequestCultureProvider : RequestCultureProvider
{
    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out var parsedUserId))
        {
            return null;
        }

        try
        {
            var dbContext = httpContext.RequestServices.GetRequiredService<AppDbContext>();
            var userId = UserId.Create(parsedUserId);

            var preferredLanguage = await dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == userId)
                .Select(user => user.PreferredLanguage)
                .FirstOrDefaultAsync(httpContext.RequestAborted);

            var normalizedLanguage = AppLanguages.Normalize(preferredLanguage);
            return normalizedLanguage is null
                ? null
                : new ProviderCultureResult(normalizedLanguage, normalizedLanguage);
        }
        catch
        {
            return null;
        }
    }
}
