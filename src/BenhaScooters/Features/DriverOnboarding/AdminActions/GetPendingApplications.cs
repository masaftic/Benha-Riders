using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Infrastructure.S3;
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
    private readonly IS3Service _s3;

    public GetPendingApplicationsEndpoint(AppDbContext db, IS3Service s3)
    {
        _db = db;
        _s3 = s3;
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

        for (int i = 0; i < response.Count; i++)
        {
            var app = response[i];
            if (app.Documents is not null)
            {
                var docs = app.Documents;
                var updatedDocs = new DocumentsDto(
                    LicenseImageUrl: await _s3.GetPreSignedUrlAsync(docs.LicenseImageUrl, TimeSpan.FromMinutes(15)),
                    VehicleRegistrationImageUrl: await _s3.GetPreSignedUrlAsync(docs.VehicleRegistrationImageUrl, TimeSpan.FromMinutes(15)),
                    ImageUrl: await _s3.GetPreSignedUrlAsync(docs.ImageUrl, TimeSpan.FromMinutes(15))
                );
                response[i] = app with { Documents = updatedDocs };
            }
        }

        await SendOkAsync(new GetPendingApplicationsResponse(response), ct);
    }
}
