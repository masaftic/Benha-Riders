using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
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
        var pendingApplications = await _db.DriverProfiles
            .Include(d => d.User)
            .Include(d => d.Documents)
            .Where(dp => dp.OnboardingStatus == DriverOnboardingStatus.UnderReview)
            .OrderBy(dp => dp.CreatedAt)
            .ToListAsync(cancellationToken);

        var response = new List<PendingDriverApplicationDto>();

        foreach (var driverProfile in pendingApplications)
        {
            var personalInfo = driverProfile.PersonalInfo != null ? new PersonalInfoDto(
                driverProfile.PersonalInfo.FullName,
                driverProfile.PersonalInfo.NationalId.Value,
                driverProfile.User.PhoneNumber!.Value,
                driverProfile.PersonalInfo.DateOfBirth,
                driverProfile.PersonalInfo.Address,
                driverProfile.PersonalInfo.City,
                driverProfile.PersonalInfo.EmergencyContactName,
                driverProfile.PersonalInfo.EmergencyContactPhone.Value) : null;

            var vehicleInfo = driverProfile.Vehicle is not null ? new VehicleInfoDto(
                driverProfile.Vehicle.VehicleType,
                driverProfile.Vehicle.Brand,
                driverProfile.Vehicle.Model,
                driverProfile.Vehicle.Color,
                driverProfile.Vehicle.LicensePlate.Value,
                driverProfile.Vehicle.Year,
                driverProfile.Vehicle.VIN.Value,
                true,
                driverProfile.CreatedAt) : null;

            var documentTasks = driverProfile.Documents.Select(doc => ToDocumentDto(doc, _s3));
            List<DocumentDto> documents = [.. await Task.WhenAll(documentTasks)];

            response.Add(new PendingDriverApplicationDto(
                driverProfile.GetDriverId(),
                personalInfo,
                vehicleInfo,
                documents,
                driverProfile.CreatedAt));
        }

        return new GetPendingApplicationsResponse(response);
    }
    
    private static async Task<DocumentDto> ToDocumentDto(DriverDocument doc, IS3Service s3)
    {
        var url = await s3.GetPreSignedUrlAsync(doc.ImageUrl, TimeSpan.FromMinutes(15));
        return new DocumentDto(doc.Type.ToString(), url, doc.UploadedAt, doc.ExpiryDate);
    }
}
