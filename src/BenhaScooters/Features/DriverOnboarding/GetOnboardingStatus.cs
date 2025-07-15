using System.Security.Claims;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
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

        Summary(s =>
        {
            s.Summary = "Get driver onboarding status";
            s.Description = "Retrieves the current onboarding status of the driver including progress and any rejection reasons. Only accessible by the driver.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.UserId == userId, ct);

        if (driver == null)
        {
            // Create new driver  if it doesn't exist
            driver = new Driver(userId);
            _db.Drivers.Add(driver);
            await _db.SaveChangesAsync(ct);
        }

        var response = new GetOnboardingStatusResponse(
            driver.OnboardingStatus,
            driver.CurrentStep,
            driver.OnboardingProgress,
            driver.RejectionReason,
            driver.CreatedAt,
            driver.CompletedAt);

        await SendOkAsync(response, ct);
    }
}
