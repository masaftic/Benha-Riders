using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Infrastructure.S3;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record UpdateDocumentsCommand(
    DriverId DriverId,
    IFormFile LicenseImage,
    IFormFile VehicleRegistrationImage,
    IFormFile DriverImage) : IRequest<ErrorOr<UpdateDocumentsResponse>>;

public class UpdateDocumentsCommandValidator : AbstractValidator<UpdateDocumentsCommand>
{
    public UpdateDocumentsCommandValidator()
    {
        RuleFor(x => x.LicenseImage)
            .NotNull().WithMessage("صورة الرخصة مطلوبة.")
            .Must(BeAValidImageFile).WithMessage("صورة الرخصة يجب أن تكون ملف صورة صحيح (jpg, jpeg, png) أقل من 10 ميجابايت.");

        RuleFor(x => x.VehicleRegistrationImage)
            .NotNull().WithMessage("صورة تسجيل المركبة مطلوبة.")
            .Must(BeAValidImageFile).WithMessage("صورة تسجيل المركبة يجب أن تكون ملف صورة صحيح (jpg, jpeg, png) أقل من 10 ميجابايت.");

        RuleFor(x => x.DriverImage)
            .NotNull().WithMessage("صورة السائق مطلوبة.")
            .Must(BeAValidImageFile).WithMessage("صورة السائق يجب أن تكون ملف صورة صحيح (jpg, jpeg, png) أقل من 10 ميجابايت.");
    }

    private static bool BeAValidImageFile(IFormFile? file)
    {
        if (file == null || file.Length == 0)
            return false;

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        if (!allowedExtensions.Contains(extension))
            return false;

        var maxFileSize = 10 * 1024 * 1024; // 10MB
        return file.Length <= maxFileSize;
    }
}

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
            var updateResult = driver.UpdateDocuments(
                licenseImageKey,
                vehicleRegistrationImageKey,
                driverImageKey);

            if (updateResult.IsError)
            {
                return updateResult.Errors;
            }

            await _db.SaveChangesAsync(cancellationToken);

            return new UpdateDocumentsResponse(
                "Documents uploaded successfully. Your application is now under review.",
                driver.CurrentStep);
        }
        catch (Exception ex)
        {
            return DriverErrors.UploadError(ex.Message);
        }
    }
}
