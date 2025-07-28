using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Infrastructure.S3;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Application.Features.DriverOnboarding.Queries;

public record GetOnboardingDetailsQuery(UserId UserId) : IRequest<ErrorOr<GetOnboardingDetailsResponse>>;

public record GetOnboardingDetailsResponse(
    OnboardingStateDto OnboardingState,
    PersonalInfoDto? PersonalInfo,
    VehicleInfoDto? VehicleInfo,
    DocumentsDto Documents);



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
            .Include(d => d.Documents)
            .Include(d => d.Vehicles)
            .FirstOrDefaultAsync(dp => dp.UserId == request.UserId, cancellationToken);

        if (driver == null)
        {
            return DriverErrors.DriverNotFound;
        }

        var personalInfo = driver.Info != null ? new PersonalInfoDto(
            driver.Info.FullName,
            driver.Info.NationalId.Value,
            driver.Info.DateOfBirth,
            driver.Info.Address,
            driver.Info.City,
            driver.Info.EmergencyContactName,
            driver.Info.EmergencyContactPhone.Value) : null;

        var activeVehicle = driver.ActiveVehicle;
        var vehicleInfo = activeVehicle != null ? new VehicleInfoDto(
            activeVehicle.VehicleType,
            activeVehicle.Brand,
            activeVehicle.Model,
            activeVehicle.Color,
            activeVehicle.LicensePlate.Value,
            activeVehicle.Year,
            activeVehicle.VIN.Value,
            activeVehicle.IsActive,
            activeVehicle.CreatedAt) : null;

        // Create document DTOs with enhanced information
        var licenseDoc = driver.Documents.FirstOrDefault(d => d.Type == BenhaScooters.Domain.Drivers.Entities.DocumentType.DrivingLicense);
        var registrationDoc = driver.Documents.FirstOrDefault(d => d.Type == BenhaScooters.Domain.Drivers.Entities.DocumentType.VehicleRegistration);
        var photoDoc = driver.Documents.FirstOrDefault(d => d.Type == BenhaScooters.Domain.Drivers.Entities.DocumentType.DriverPhoto);

        var documents = new DocumentsDto(
            licenseDoc != null ? new DocumentDto(
                licenseDoc.Type.ToString(),
                licenseDoc.Status.ToString(),
                await _s3.GetPreSignedUrlAsync(licenseDoc.ImageUrl, TimeSpan.FromMinutes(10), cancellationToken),
                licenseDoc.UploadedAt,
                licenseDoc.ExpiryDate,
                licenseDoc.RejectionReason,
                licenseDoc.IsValid,
                licenseDoc.IsExpired) : null,
            registrationDoc != null ? new DocumentDto(
                registrationDoc.Type.ToString(),
                registrationDoc.Status.ToString(),
                await _s3.GetPreSignedUrlAsync(registrationDoc.ImageUrl, TimeSpan.FromMinutes(10), cancellationToken),
                registrationDoc.UploadedAt,
                registrationDoc.ExpiryDate,
                registrationDoc.RejectionReason,
                registrationDoc.IsValid,
                registrationDoc.IsExpired) : null,
            photoDoc != null ? new DocumentDto(
                photoDoc.Type.ToString(),
                photoDoc.Status.ToString(),
                await _s3.GetPreSignedUrlAsync(photoDoc.ImageUrl, TimeSpan.FromMinutes(10), cancellationToken),
                photoDoc.UploadedAt,
                photoDoc.ExpiryDate,
                photoDoc.RejectionReason,
                photoDoc.IsValid,
                photoDoc.IsExpired) : null);

        var onboardingState = new OnboardingStateDto(
            driver.OnboardingState.Status.ToString(),
            driver.OnboardingState.CurrentStep.ToString(),
            driver.OnboardingProgress,
            driver.OnboardingState.RejectionReason,
            driver.OnboardingState.CreatedAt,
            driver.OnboardingState.CompletedAt,
            driver.OnboardingState.IsCompleted,
            driver.OnboardingState.CurrentStep != BenhaScooters.Domain.Drivers.ValueObjects.OnboardingStep.Completed);

        var response = new GetOnboardingDetailsResponse(
            onboardingState,
            personalInfo,
            vehicleInfo,
            documents);

        return response;
    }
}
