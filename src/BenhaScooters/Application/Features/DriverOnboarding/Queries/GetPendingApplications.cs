using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
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
    DriverId DriverId,
    PersonalInfoDto? PersonalInfo,
    VehicleInfoDto? VehicleInfo,
    List<DocumentDto>? Documents,
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
        var pendingApplications = await _db.Drivers // TODO: use projection to avoid loading unnecessary data
            .Include(d => d.User)
            .Include(d => d.Vehicle)
            .Include(d => d.Documents)
            .Where(dp => dp.OnboardingState.Status == OnboardingStatus.Review)
            .OrderBy(dp => dp.OnboardingState.CreatedAt)
            .ToListAsync(cancellationToken);

        var response = new List<PendingDriverApplicationDto>();

        foreach (var driver in pendingApplications)
        {
            var personalInfo = driver.Info != null ? new PersonalInfoDto(
                driver.Info.FullName,
                driver.Info.NationalId.Value,
                driver.User.PhoneNumber!.Value,
                driver.Info.DateOfBirth,
                driver.Info.Address,
                driver.Info.City,
                driver.Info.EmergencyContactName,
                driver.Info.EmergencyContactPhone.Value) : null;

            var vehicleInfo = driver.Vehicle is not null ? new VehicleInfoDto(
                driver.Vehicle.VehicleType,
                driver.Vehicle.Brand,
                driver.Vehicle.Model,
                driver.Vehicle.Color,
                driver.Vehicle.LicensePlate.Value,
                driver.Vehicle.Year,
                driver.Vehicle.VIN.Value,
                driver.Vehicle.IsActive,
                driver.Vehicle.CreatedAt) : null;

            var documentTasks = driver.Documents.Select(x => x.ToDto(_s3));
            List<DocumentDto> documents = [.. await Task.WhenAll(documentTasks)];

            response.Add(new PendingDriverApplicationDto(
                driver.Id,
                personalInfo,
                vehicleInfo,
                documents,
                driver.OnboardingState.CreatedAt));
        }

        return new GetPendingApplicationsResponse(response);
    }
}
