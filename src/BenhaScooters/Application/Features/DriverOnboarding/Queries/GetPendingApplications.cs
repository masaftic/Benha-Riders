using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Infrastructure.S3;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

public record GetPendingApplicationsQuery : IRequest<ErrorOr<GetPendingApplicationsResponse>>;

public record GetPendingApplicationsResponse(List<PendingDriverApplicationDto> Applications);

public record PendingDriverApplicationDto(
    int UserId,
    PersonalInfoDto? PersonalInfo,
    VehicleInfoDto? VehicleInfo,
    DocumentsDto? Documents,
    DateTime CreatedAt);

public class GetPendingApplicationsQueryHandler : IRequestHandler<GetPendingApplicationsQuery, ErrorOr<GetPendingApplicationsResponse>>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3;

    public GetPendingApplicationsQueryHandler(AppDbContext db, IS3Service s3)
    {
        _db = db;
        _s3 = s3;
    }

    public async Task<ErrorOr<GetPendingApplicationsResponse>> Handle(GetPendingApplicationsQuery request, CancellationToken cancellationToken)
    {
        var pendingApplications = await _db.Drivers
            .Where(dp =>
                dp.OnboardingStatus == OnboardingStatus.InProgress &&
                dp.CurrentStep == OnboardingStep.Review)
            .OrderBy(dp => dp.CreatedAt)
            .ToListAsync(cancellationToken);

        var response = new List<PendingDriverApplicationDto>();

        foreach (var driver in pendingApplications)
        {
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
                await _s3.GetPreSignedUrlAsync(driver.Documents.LicenseImageUrl, TimeSpan.FromMinutes(15), cancellationToken),
                await _s3.GetPreSignedUrlAsync(driver.Documents.VehicleRegistrationImageUrl, TimeSpan.FromMinutes(15), cancellationToken),
                await _s3.GetPreSignedUrlAsync(driver.Documents.ImageUrl, TimeSpan.FromMinutes(15), cancellationToken)) : null;

            response.Add(new PendingDriverApplicationDto(
                driver.UserId.Value,
                personalInfo,
                vehicleInfo,
                documents,
                driver.CreatedAt));
        }

        return new GetPendingApplicationsResponse(response);
    }
}
