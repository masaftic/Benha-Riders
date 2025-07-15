using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Shared.Security;
using BenhaScooters.Shared.Validation;
using FastEndpoints;
using FastEndpoints.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.DriverOnboarding;

public record UpdatePersonalInfoRequest(
    string FullName,
    string NationalId,
    DateOnly DateOfBirth,
    string Address,
    string City,
    string EmergencyContactName,
    string EmergencyContactPhone);

public class UpdatePersonalInfoRequestValidator : Validator<UpdatePersonalInfoRequest>
{
    public UpdatePersonalInfoRequestValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("National ID is required.")
            .Matches(ValidationRegex.NationalId).WithMessage("National ID must be 14 digits.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateOnly.FromDateTime(DateTime.Now.AddYears(-18))).WithMessage("Driver must be at least 18 years old.")
            .GreaterThan(DateOnly.FromDateTime(DateTime.Now.AddYears(-100))).WithMessage("Invalid date of birth.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.EmergencyContactName)
            .NotEmpty().WithMessage("Emergency contact name is required.")
            .MaximumLength(100).WithMessage("Emergency contact name must not exceed 100 characters.");

        RuleFor(x => x.EmergencyContactPhone)
            .NotEmpty().WithMessage("Emergency contact phone is required.")
            .Matches(ValidationRegex.PhoneNumber).WithMessage("Invalid phone number format.");
    }
}

public record UpdatePersonalInfoResponse(string Message, OnboardingStep NextStep);

public class UpdatePersonalInfoEndpoint : Endpoint<UpdatePersonalInfoRequest, UpdatePersonalInfoResponse>
{
    private readonly AppDbContext _db;

    public UpdatePersonalInfoEndpoint(AppDbContext db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Post("/driver/onboarding/personal-info");
        Roles("Driver");
        Claims(JwtClaims.Sub);
        Description(x => x
            .WithSummary("Update driver personal information")
            .Produces<UpdatePersonalInfoResponse>()
            .Produces(400)
            .Produces(404));

        Summary(s =>
        {
            s.Summary = "Update driver personal information";
            s.Description = "Updates the driver's personal information including full name, date of birth, national ID, and address during the onboarding process.";
            s.ExampleRequest = new UpdatePersonalInfoRequest("John Doe", "12345678901234", new DateOnly(1990, 1, 1), "123 Main St", "New York", "Jane Doe", "+201012345678");
        });
    }

    public override async Task HandleAsync(UpdatePersonalInfoRequest req, CancellationToken ct)
    {
        var userId = this.GetCurrentUserId();

        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.UserId == userId, ct);

        if (driver == null)
        {
            ThrowError("Driver not found. Please get onboarding status first.", 
                errorCode: "DriverNotFound", statusCode: 404);
            return;
        }

        if (await _db.Drivers.AnyAsync(x => x.PersonalInfo!.NationalId == NationalId.From(req.NationalId) && x.Id != driver.Id, cancellationToken: ct))
        {
            ThrowError("A driver with this national ID already exists.", 
                errorCode: "DuplicateNationalId", statusCode: 400);
            return;
        }

        try
        {
            driver.UpdatePersonalInfo(
                req.FullName,
                NationalId.From(req.NationalId),
                req.DateOfBirth,
                req.Address,
                req.City,
                req.EmergencyContactName,
                PhoneNumber.From(req.EmergencyContactPhone));

            await _db.SaveChangesAsync(ct);

            var response = new UpdatePersonalInfoResponse(
                "Personal information updated successfully.",
                driver.CurrentStep);

            await SendOkAsync(response, ct);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            ThrowError(ex.Message, errorCode: "ValidationError", statusCode: 400);
        }
    }
}
