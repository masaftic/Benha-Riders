using System.Text.Json;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.Drivers.Common;

public class OnboardedProcessor<TRequest> : IPreProcessor<TRequest>
{
    public async Task PreProcessAsync(IPreProcessorContext<TRequest> context, CancellationToken ct)
    {
        var userId = (context.HttpContext.User?.Claims
            .FirstOrDefault(c => c.Type == JwtClaims.Sub)?.Value) ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();

        var isOnboardingComplete = await db
            .Drivers
            .AsNoTracking()
            .Where(d => d.UserId == UserId.From(int.Parse(userId)))
            .Select(d => d.OnboardingStatus == OnboardingStatus.Completed)
            .FirstOrDefaultAsync(ct);

        if (isOnboardingComplete != true) {
            context.HttpContext.Response.StatusCode = 403; // Forbidden
            context.HttpContext.Response.ContentType = "application/problem+json";
            await context.HttpContext.Response.WriteAsync(
                JsonSerializer.Serialize(new { message = "Onboarding is not complete." }),
                cancellationToken: ct
            );
        }
    }
}
