using System.Security.Claims;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.DriverOnboarding;

public record GetOnboardingDetailsResponse(
    OnboardingStatus Status,
    OnboardingStep CurrentStep,
    int Progress,
    string? RejectionReason,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    PersonalInfoDto? PersonalInfo,
    VehicleInfoDto? VehicleInfo,
    DocumentsDto? Documents);

public record PersonalInfoDto(
    string? FullName,
    string? NationalId,
    DateOnly? DateOfBirth,
    string? Address,
    string? City,
    string? EmergencyContactName,
    string? EmergencyContactPhone);

public record VehicleInfoDto(
    VehicleType? VehicleType,
    string? VehicleBrand,
    string? VehicleModel,
    string? VehicleColor,
    string? LicensePlate,
    int? VehicleYear);

public record DocumentsDto(
    string? LicenseImageUrl,
    string? VehicleRegistrationImageUrl,
    string? ImageUrl);

public class GetOnboardingDetailsEndpoint : EndpointWithoutRequest<GetOnboardingDetailsResponse>
{
    private readonly AppDbContext _db;

    public GetOnboardingDetailsEndpoint(AppDbContext db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Get("/driver/onboarding/details");
        Roles("Driver");
        Claims(JwtClaims.Sub);
        Description(x => x
            .WithSummary("Get detailed driver onboarding information")
            .Produces<GetOnboardingDetailsResponse>()
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Get detailed driver onboarding information";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.UserId == userId, ct);

        if (driver == null)
        {
            ThrowError("Driver  not found. Please get onboarding status first.", 
                errorCode: "DriverNotFound", statusCode: 404);
            return;
        }

        var personalInfo = driver.PersonalInfo != null ? new PersonalInfoDto(
            driver.PersonalInfo.FullName,
            driver.PersonalInfo.NationalId.Value,
            driver.PersonalInfo.DateOfBirth,
            driver.PersonalInfo.Address,
            driver.PersonalInfo.City,
            driver.PersonalInfo.EmergencyContactName,
            driver.PersonalInfo.EmergencyContactPhone.Value) : null;

        var vehicleInfo = driver.VehicleInfo != null ? new VehicleInfoDto(
            driver.VehicleInfo.VehicleType,
            driver.VehicleInfo.Brand,
            driver.VehicleInfo.Model,
            driver.VehicleInfo.Color,
            driver.VehicleInfo.LicensePlate.Value,
            driver.VehicleInfo.Year) : null;

        var documents = driver.Documents != null ? new DocumentsDto(
            driver.Documents.LicenseImageUrl,
            driver.Documents.VehicleRegistrationImageUrl,
            driver.Documents.ImageUrl) : null;

        var response = new GetOnboardingDetailsResponse(
            driver.OnboardingStatus,
            driver.CurrentStep,
            driver.OnboardingProgress,
            driver.RejectionReason,
            driver.CreatedAt,
            driver.CompletedAt,
            personalInfo,
            vehicleInfo,
            documents);

        await SendOkAsync(response, ct);
    }
}
