using System.Security.Claims;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Driver.Enums;
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
    DateTime? DateOfBirth,
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
    string? ProfileImageUrl);

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
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == userId, ct);

        if (driverProfile == null)
        {
            ThrowError("Driver profile not found. Please get onboarding status first.", 
                errorCode: "DriverProfileNotFound", statusCode: 404);
            return;
        }

        var personalInfo = driverProfile.PersonalInfo != null ? new PersonalInfoDto(
            driverProfile.PersonalInfo.FullName,
            driverProfile.PersonalInfo.NationalId.Value,
            driverProfile.PersonalInfo.DateOfBirth,
            driverProfile.PersonalInfo.Address,
            driverProfile.PersonalInfo.City,
            driverProfile.PersonalInfo.EmergencyContactName,
            driverProfile.PersonalInfo.EmergencyContactPhone.Value) : null;

        var vehicleInfo = driverProfile.VehicleInfo != null ? new VehicleInfoDto(
            driverProfile.VehicleInfo.VehicleType,
            driverProfile.VehicleInfo.Brand,
            driverProfile.VehicleInfo.Model,
            driverProfile.VehicleInfo.Color,
            driverProfile.VehicleInfo.LicensePlate.Value,
            driverProfile.VehicleInfo.Year) : null;

        var documents = driverProfile.Documents != null ? new DocumentsDto(
            driverProfile.Documents.LicenseImageUrl,
            driverProfile.Documents.VehicleRegistrationImageUrl,
            driverProfile.Documents.ProfileImageUrl) : null;

        var response = new GetOnboardingDetailsResponse(
            driverProfile.OnboardingStatus,
            driverProfile.CurrentStep,
            driverProfile.OnboardingProgress,
            driverProfile.RejectionReason,
            driverProfile.CreatedAt,
            driverProfile.CompletedAt,
            personalInfo,
            vehicleInfo,
            documents);

        await SendOkAsync(response, ct);
    }
}
