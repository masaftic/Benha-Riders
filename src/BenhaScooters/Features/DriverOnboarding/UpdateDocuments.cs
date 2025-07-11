using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Driver.Enums;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FastEndpoints.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.DriverOnboarding;

public record UpdateDocumentsRequest(
    string LicenseImageUrl,
    string VehicleRegistrationImageUrl,
    string ProfileImageUrl);

public class UpdateDocumentsRequestValidator : Validator<UpdateDocumentsRequest>
{
    public UpdateDocumentsRequestValidator()
    {
        RuleFor(x => x.LicenseImageUrl)
            .NotEmpty().WithMessage("License image is required.")
            .Must(BeAValidUrl).WithMessage("License image must be a valid URL.")
            .MaximumLength(500).WithMessage("License image URL must not exceed 500 characters.");

        RuleFor(x => x.VehicleRegistrationImageUrl)
            .NotEmpty().WithMessage("Vehicle registration image is required.")
            .Must(BeAValidUrl).WithMessage("Vehicle registration image must be a valid URL.")
            .MaximumLength(500).WithMessage("Vehicle registration image URL must not exceed 500 characters.");

        RuleFor(x => x.ProfileImageUrl)
            .NotEmpty().WithMessage("Profile image is required.")
            .Must(BeAValidUrl).WithMessage("Profile image must be a valid URL.")
            .MaximumLength(500).WithMessage("Profile image URL must not exceed 500 characters.");
    }

    private static bool BeAValidUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var result) && 
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }
}

public record UpdateDocumentsResponse(string Message, OnboardingStep NextStep);

public class UpdateDocumentsEndpoint : Endpoint<UpdateDocumentsRequest, UpdateDocumentsResponse>
{
    private readonly AppDbContext _db;

    public UpdateDocumentsEndpoint(AppDbContext db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Post("/driver/onboarding/documents");
        Roles("Driver");
        Claims(JwtClaims.Sub);
        Description(x => x
            .WithSummary("Update driver documents")
            .Produces<UpdateDocumentsResponse>()
            .Produces(400)
            .Produces(404));
    }

    public override async Task HandleAsync(UpdateDocumentsRequest req, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == userId, ct);

        if (driverProfile == null)
        {
            ThrowError("Driver profile not found.", 
                errorCode: "DriverProfileNotFound", statusCode: 404);
            return;
        }

        try
        {
            driverProfile.UpdateDocuments(
                req.LicenseImageUrl,
                req.VehicleRegistrationImageUrl,
                req.ProfileImageUrl);

            await _db.SaveChangesAsync(ct);

            var response = new UpdateDocumentsResponse(
                "Documents updated successfully. Your application is now under review.",
                driverProfile.CurrentStep);

            await SendOkAsync(response, ct);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            ThrowError(ex.Message, errorCode: "ValidationError", statusCode: 400);
        }
    }
}
