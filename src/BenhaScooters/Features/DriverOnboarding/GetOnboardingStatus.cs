using System.Security.Claims;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Driver;
using BenhaScooters.Domain.Driver.Enums;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.DriverOnboarding;

public record GetOnboardingStatusResponse(
    OnboardingStatus Status,
    OnboardingStep CurrentStep,
    int Progress,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public class GetOnboardingStatusEndpoint : EndpointWithoutRequest<GetOnboardingStatusResponse>
{
    private readonly AppDbContext _db;

    public GetOnboardingStatusEndpoint(AppDbContext db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Get("/driver/onboarding/status");
        Roles("Driver");
        Claims(JwtClaims.Sub);
        Description(x => x
            .WithSummary("Get driver onboarding status")
            .Produces<GetOnboardingStatusResponse>()
            .Produces(404));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == userId, ct);

        if (driverProfile == null)
        {
            // Create new driver profile if it doesn't exist
            driverProfile = new DriverProfile(userId);
            _db.DriverProfiles.Add(driverProfile);
            await _db.SaveChangesAsync(ct);
        }

        var response = new GetOnboardingStatusResponse(
            driverProfile.OnboardingStatus,
            driverProfile.CurrentStep,
            driverProfile.OnboardingProgress,
            driverProfile.RejectionReason,
            driverProfile.CreatedAt,
            driverProfile.CompletedAt);

        await SendOkAsync(response, ct);
    }
}
