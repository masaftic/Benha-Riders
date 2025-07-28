using Amazon.Runtime.Documents;
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


public record UpdateDocumentsResponse(string Message, BenhaScooters.Domain.Drivers.ValueObjects.OnboardingStep NextStep);

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
            var licenseImageKey = await _s3Service.UploadFileAsync(
                request.LicenseImage,
                $"driver-documents/{request.DriverId}/license",
                cancellationToken);

            var vehicleRegistrationImageKey = await _s3Service.UploadFileAsync(
                request.VehicleRegistrationImage,
                $"driver-documents/{request.DriverId}/vehicle-registration",
                cancellationToken);

            var driverImageKey = await _s3Service.UploadFileAsync(
                request.DriverImage,
                $"driver-documents/{request.DriverId}/photo",
                cancellationToken);

            // Update driver with S3 keys instead of URLs
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
