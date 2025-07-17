using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Infrastructure.S3;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

public record GetOnboardingDetailsQuery(UserId UserId) : IRequest<ErrorOr<GetOnboardingDetailsResponse>>;

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



public class GetOnboardingDetailsQueryHandler : IRequestHandler<GetOnboardingDetailsQuery, ErrorOr<GetOnboardingDetailsResponse>>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3;

    public GetOnboardingDetailsQueryHandler(AppDbContext db, IS3Service s3)
    {
        _db = db;
        _s3 = s3;
    }

    public async Task<ErrorOr<GetOnboardingDetailsResponse>> Handle(GetOnboardingDetailsQuery request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.UserId == request.UserId, cancellationToken);

        if (driver == null)
        {
            return DriverErrors.DriverNotFound;
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
            await _s3.GetPreSignedUrlAsync(driver.Documents.LicenseImageUrl, TimeSpan.FromMinutes(10), cancellationToken),
            await _s3.GetPreSignedUrlAsync(driver.Documents.VehicleRegistrationImageUrl, TimeSpan.FromMinutes(10), cancellationToken),
            await _s3.GetPreSignedUrlAsync(driver.Documents.ImageUrl, TimeSpan.FromMinutes(10), cancellationToken)) : null;

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

        return response;
    }
}
