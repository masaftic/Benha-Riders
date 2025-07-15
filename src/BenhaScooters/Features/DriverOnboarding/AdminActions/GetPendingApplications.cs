using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using FastEndpoints;
using FastEndpoints.Security;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.DriverOnboarding.AdminActions;

public record PersonalInfoDto(
    string FullName,
    NationalId NationalId,
    DateOnly DateOfBirth,
    string Address,
    string City,
    string EmergencyContactName,
    PhoneNumber EmergencyContactPhone);

public record VehicleInfoDto(
    VehicleType VehicleType,
    string Brand,
    string Model,
    string Color,
    LicensePlate LicensePlate,
    int Year);

public record DocumentsDto(
    string LicenseImageUrl,
    string VehicleRegistrationImageUrl,
    string ImageUrl);

public record PendingDriverApplicationDto(
    UserId UserId,
    PersonalInfoDto? PersonalInfo,
    VehicleInfoDto? VehicleInfo,
    DocumentsDto? Documents,
    DateTime CreatedAt);

public record GetPendingApplicationsResponse(List<PendingDriverApplicationDto> Applications);

public class GetPendingApplicationsEndpoint : EndpointWithoutRequest<GetPendingApplicationsResponse>
{
    private readonly AppDbContext _db;

    public GetPendingApplicationsEndpoint(AppDbContext db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Get("/admin/driver/pending-applications");
        Roles("Admin");
        Description(x => x
            .WithSummary("Get pending driver applications for review")
            .Produces<GetPendingApplicationsResponse>());
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var pendingApplications = await _db.Drivers
            .Where(dp =>
                dp.OnboardingStatus == OnboardingStatus.InProgress &&
                dp.CurrentStep == OnboardingStep.Review)
            .OrderBy(dp => dp.CreatedAt)
            .ToListAsync(ct);

        // TODO: do projection

        var response = pendingApplications.Select(dp => new PendingDriverApplicationDto(
            UserId: dp.UserId,
            PersonalInfo: dp.PersonalInfo is null ? null : new PersonalInfoDto(
                FullName: dp.PersonalInfo.FullName,
                NationalId: dp.PersonalInfo.NationalId,
                DateOfBirth: dp.PersonalInfo.DateOfBirth,
                Address: dp.PersonalInfo.Address,
                City: dp.PersonalInfo.City,
                EmergencyContactName: dp.PersonalInfo.EmergencyContactName,
                EmergencyContactPhone: dp.PersonalInfo.EmergencyContactPhone),
            VehicleInfo: dp.VehicleInfo is null ? null : new VehicleInfoDto(
                VehicleType: dp.VehicleInfo.VehicleType,
                Brand: dp.VehicleInfo.Brand,
                Model: dp.VehicleInfo.Model,
                Color: dp.VehicleInfo.Color,
                LicensePlate: dp.VehicleInfo.LicensePlate,
                Year: dp.VehicleInfo.Year),
            Documents: dp.Documents is null ? null : new DocumentsDto(
                LicenseImageUrl: dp.Documents.LicenseImageUrl,
                VehicleRegistrationImageUrl: dp.Documents.VehicleRegistrationImageUrl,
                ImageUrl: dp.Documents.ImageUrl),
            CreatedAt: dp.CreatedAt))
            .ToList();

        await SendOkAsync(new GetPendingApplicationsResponse(response), ct);
    }
}
