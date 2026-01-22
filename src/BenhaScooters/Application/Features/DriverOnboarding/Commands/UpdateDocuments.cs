using BenhaScooters.Application.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.S3;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DocumentType = BenhaScooters.Domain.Drivers.DocumentType;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record UpdateDocumentsCommand(
    UserId DriverId,
    IFormFile LicenseImage,
    IFormFile VehicleRegistrationImage,
    IFormFile DriverImage) : IRequest<ErrorOr<UpdateDocumentsResponse>>;


public record UpdateDocumentsResponse(string Message, DriverOnboardingStatus NextStep);

public class UpdateDocumentsCommandHandler : IRequestHandler<UpdateDocumentsCommand, ErrorOr<UpdateDocumentsResponse>>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3Service;

    public UpdateDocumentsCommandHandler(AppDbContext db, IS3Service s3Service)
    {
        _db = db;
        _s3Service = s3Service;
    }

    public async Task<ErrorOr<UpdateDocumentsResponse>> Handle(UpdateDocumentsCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId;
        
        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == userId, cancellationToken);

        if (driverProfile == null)
        {
            return DriverErrors.Profile.NotFound;
        }

        try
        {
            var driverId = driverProfile.UserId;
            
            // Upload files to S3 and get their keys
            string licenseImageKey, vehicleRegistrationImageKey, driverImageKey;

            var uploadResult = await _s3Service.UploadFileAsync(
                request.LicenseImage,
                $"drivers/{driverId}/documents/{DocumentType.DrivingLicense.ToKebabCase()}",
                useKeyPrefixAsFullUrl: true,
                cancellationToken);

            if (uploadResult.IsError) return uploadResult.Errors;
            else licenseImageKey = uploadResult.Value;

            uploadResult = await _s3Service.UploadFileAsync(
                request.VehicleRegistrationImage,
                $"drivers/{driverId}/documents/{DocumentType.VehicleRegistration.ToKebabCase()}",
                useKeyPrefixAsFullUrl: true,
                cancellationToken);

            if (uploadResult.IsError) return uploadResult.Errors;
            else vehicleRegistrationImageKey = uploadResult.Value;

            uploadResult = await _s3Service.UploadFileAsync(
                request.DriverImage,
                $"drivers/{driverId}/documents/{DocumentType.DriverPhoto.ToKebabCase()}",
                useKeyPrefixAsFullUrl: true,
                cancellationToken);

            if (uploadResult.IsError) return uploadResult.Errors;
            else driverImageKey = uploadResult.Value;

            var result = driverProfile.AddDocument(DocumentType.DrivingLicense, licenseImageKey)
                .Then(res => driverProfile.AddDocument(DocumentType.VehicleRegistration, vehicleRegistrationImageKey))
                .Then(res => driverProfile.AddDocument(DocumentType.DriverPhoto, driverImageKey));

            if (result.IsError)
            {
                return result.Errors;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new UpdateDocumentsResponse(
                "Documents uploaded successfully. Your application is now under review.",
                driverProfile.OnboardingStatus);
        }
        catch (Exception ex)
        {
            return DriverErrors.UploadError(ex.Message);
        }
    }
}
