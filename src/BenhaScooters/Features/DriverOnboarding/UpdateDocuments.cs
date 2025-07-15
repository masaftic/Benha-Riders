using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Shared.Security;
using FastEndpoints;
using FastEndpoints.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.DriverOnboarding;

public record UpdateDocumentsRequest(
    string LicenseImageUrl,
    string VehicleRegistrationImageUrl,
    string ImageUrl);

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

        RuleFor(x => x.ImageUrl)
            .NotEmpty().WithMessage("image is required.")
            .Must(BeAValidUrl).WithMessage("image must be a valid URL.")
            .MaximumLength(500).WithMessage("image URL must not exceed 500 characters.");
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

        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.UserId == userId, ct);

        if (driver == null)
        {
            ThrowError("Driver  not found.",
                errorCode: "DriverNotFound", statusCode: 404);
            return;
        }

        try
        {
            driver.UpdateDocuments(
                req.LicenseImageUrl,
                req.VehicleRegistrationImageUrl,
                req.ImageUrl);

            await _db.SaveChangesAsync(ct);

            var response = new UpdateDocumentsResponse(
                "Documents updated successfully. Your application is now under review.",
                driver.CurrentStep);

            await SendOkAsync(response, ct);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            ThrowError(ex.Message, errorCode: "ValidationError", statusCode: 400);
        }
    }
}
