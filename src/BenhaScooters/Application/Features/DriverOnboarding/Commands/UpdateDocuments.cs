using Amazon.Runtime.Documents;
using BenhaScooters.Application.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Infrastructure.S3;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

using DocumentType = BenhaScooters.Domain.Drivers.Entities.DocumentType;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record UpdateDocumentsCommand(
    DriverId DriverId,
    IFormFile LicenseImage,
    IFormFile VehicleRegistrationImage,
    IFormFile DriverImage) : IRequest<ErrorOr<UpdateDocumentsResponse>>;


public record UpdateDocumentsResponse(string Message, OnboardingStep NextStep);

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
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.Id == request.DriverId, cancellationToken);

        if (driver == null)
        {
            return DriverErrors.DriverNotFound;
        }

        try
        {
            // Upload files to S3 and get their keys
            string licenseImageKey, vehicleRegistrationImageKey, driverImageKey;

            var uploadResult = await _s3Service.UploadFileAsync(
                request.LicenseImage,
                $"drivers/{driver.Id}/documents/{DocumentType.DrivingLicense.ToKebabCase()}",
                useKeyPrefixAsFullUrl: true,
                cancellationToken);

            if (uploadResult.IsError) return uploadResult.Errors;
            else licenseImageKey = uploadResult.Value;

            uploadResult = await _s3Service.UploadFileAsync(
                request.VehicleRegistrationImage,
                $"drivers/{driver.Id}/documents/{DocumentType.VehicleRegistration.ToKebabCase()}",
                useKeyPrefixAsFullUrl: true,
                cancellationToken);

            if (uploadResult.IsError) return uploadResult.Errors;
            else vehicleRegistrationImageKey = uploadResult.Value;

            uploadResult = await _s3Service.UploadFileAsync(
                request.DriverImage,
                $"drivers/{driver.Id}/documents/{DocumentType.DriverPhoto.ToKebabCase()}",
                useKeyPrefixAsFullUrl: true,
                cancellationToken);

            if (uploadResult.IsError) return uploadResult.Errors;
            else driverImageKey = uploadResult.Value;

            var result = driver.AddDocument(DocumentType.DrivingLicense, licenseImageKey)
                .Then(res => driver.AddDocument(DocumentType.VehicleRegistration, vehicleRegistrationImageKey))
                .Then(res => driver.AddDocument(DocumentType.DriverPhoto, driverImageKey));

            if (result.IsError)
            {
                return result.Errors;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new UpdateDocumentsResponse(
                "Documents uploaded successfully. Your application is now under review.",
                driver.OnboardingState.CurrentStep);
        }
        catch (Exception ex)
        {
            return DriverErrors.UploadError(ex.Message);
        }
    }
}
