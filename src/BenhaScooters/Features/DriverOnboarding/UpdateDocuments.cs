using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Infrastructure.S3;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.DriverOnboarding;

public record UpdateDocumentsRequest(
    IFormFile LicenseImage,
    IFormFile VehicleRegistrationImage,
    IFormFile DriverImage);

public class UpdateDocumentsRequestValidator : Validator<UpdateDocumentsRequest>
{
    public UpdateDocumentsRequestValidator()
    {
        RuleFor(x => x.LicenseImage)
            .NotNull().WithMessage("License image is required.")
            .Must(BeAValidImageFile).WithMessage("License image must be a valid image file (jpg, jpeg, png) under 10MB.");

        RuleFor(x => x.VehicleRegistrationImage)
            .NotNull().WithMessage("Vehicle registration image is required.")
            .Must(BeAValidImageFile).WithMessage("Vehicle registration image must be a valid image file (jpg, jpeg, png) under 10MB.");

        RuleFor(x => x.DriverImage)
            .NotNull().WithMessage("Driver image is required.")
            .Must(BeAValidImageFile).WithMessage("Driver image must be a valid image file (jpg, jpeg, png) under 10MB.");
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

public class UpdateDocumentsEndpoint : Endpoint<UpdateDocumentsRequest, UpdateDocumentsResponse>
{
    private readonly AppDbContext _db;
    private readonly IS3Service _s3Service;

    public UpdateDocumentsEndpoint(AppDbContext db, IS3Service s3Service)
    {
        _db = db;
        _s3Service = s3Service;
    }

    public override void Configure()
    {
        Post("/driver/onboarding/documents");
        Roles("Driver");
        Claims(JwtClaims.Sub);
        AllowFileUploads();
        Description(x => x
            .WithSummary("Update driver documents")
            .Produces<UpdateDocumentsResponse>()
            .Produces(400)
            .Produces(404)
            .Accepts<UpdateDocumentsRequest>("multipart/form-data"));

        Summary(s =>
        {
            s.Summary = "Update driver documents";
            s.Description = "Uploads driver documents including license image, vehicle registration image, and driver photo. Files are stored securely in S3 storage.";
            s.RequestParam(r => r.LicenseImage, "Driver's license image file (JPG, PNG - max 10MB)");
            s.RequestParam(r => r.VehicleRegistrationImage, "Vehicle registration document image (JPG, PNG - max 10MB)");
            s.RequestParam(r => r.DriverImage, "Driver's photo (JPG, PNG - max 10MB)");
        });
    }

    public override async Task HandleAsync(UpdateDocumentsRequest req, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.UserId == userId, ct);

        if (driver == null)
        {
            ThrowError("Driver not found.",
                errorCode: "DriverNotFound", statusCode: 404);
            return;
        }

        try
        {
            // Upload files to S3 and get their keys
            var licenseImageKey = await _s3Service.UploadFileAsync(
                req.LicenseImage, 
                $"driver-documents/{userId}/license", 
                ct);

            var vehicleRegistrationImageKey = await _s3Service.UploadFileAsync(
                req.VehicleRegistrationImage, 
                $"driver-documents/{userId}/vehicle-registration", 
                ct);

            var driverImageKey = await _s3Service.UploadFileAsync(
                req.DriverImage, 
                $"driver-documents/{userId}/photo", 
                ct);

            // Update driver with S3 keys instead of URLs
            driver.UpdateDocuments(
                licenseImageKey,
                vehicleRegistrationImageKey,
                driverImageKey);

            await _db.SaveChangesAsync(ct);

            var response = new UpdateDocumentsResponse(
                "Documents uploaded successfully. Your application is now under review.",
                driver.CurrentStep);

            await SendOkAsync(response, ct);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            ThrowError(ex.Message, errorCode: "ValidationError", statusCode: 400);
        }
        catch (Exception ex)
        {
            ThrowError($"Failed to upload documents: {ex.Message}", errorCode: "UploadError", statusCode: 500);
        }
    }
}
