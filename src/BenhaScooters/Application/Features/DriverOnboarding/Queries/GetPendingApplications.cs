using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.Entities;
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
            .Include(d => d.Vehicles)
            .Include(d => d.Documents)
            .Where(dp =>
                dp.OnboardingState.Status == OnboardingStatus.InProgress &&
                dp.OnboardingState.CurrentStep == OnboardingStep.Review)
            .OrderBy(dp => dp.OnboardingState.CreatedAt)
            .ToListAsync(cancellationToken);

        var response = new List<PendingDriverApplicationDto>();

        foreach (var driver in pendingApplications)
        {
            var personalInfo = driver.Info != null ? new PersonalInfoDto(
                driver.Info.FullName,
                driver.Info.NationalId.Value,
                driver.Info.DateOfBirth,
                driver.Info.Address,
                driver.Info.City,
                driver.Info.EmergencyContactName,
                driver.Info.EmergencyContactPhone.Value) : null;

            var vehicleInfo = new VehicleInfoDto(
                driver.Vehicles.First().VehicleType,
                driver.Vehicles.First().Brand,
                driver.Vehicles.First().Model,
                driver.Vehicles.First().Color,
                driver.Vehicles.First().LicensePlate.Value,
                driver.Vehicles.First().Year,
                driver.Vehicles.First().VIN.Value,
                driver.Vehicles.First().IsActive,
                driver.Vehicles.First().CreatedAt);

            var documents = new DocumentsDto(
                await driver.Documents.FirstOrDefault(d => d.Type == DocumentType.DrivingLicense)?.ToDto(_s3),
                await driver.Documents.FirstOrDefault(d => d.Type == DocumentType.VehicleRegistration)?.ToDto(_s3),
                await driver.Documents.FirstOrDefault(d => d.Type == DocumentType.DriverPhoto)?.ToDto(_s3));

            response.Add(new PendingDriverApplicationDto(
                driver.UserId.Value,
                personalInfo,
                vehicleInfo,
                documents,
                driver.OnboardingState.CreatedAt));
        }

        return new GetPendingApplicationsResponse(response);
    }
}
