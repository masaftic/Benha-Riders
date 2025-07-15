using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Shared.Security;
using BenhaScooters.Shared.Validation;
using FastEndpoints;
using FastEndpoints.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Features.DriverOnboarding;

public record UpdateVehicleInfoRequest(
    VehicleType VehicleType,
    string VehicleBrand,
    string VehicleModel,
    string VehicleColor,
    string LicensePlate,
    int VehicleYear);

public class UpdateVehicleInfoRequestValidator : Validator<UpdateVehicleInfoRequest>
{
    public UpdateVehicleInfoRequestValidator()
    {
        RuleFor(x => x.VehicleType)
            .IsInEnum().WithMessage("Invalid vehicle type.");

        RuleFor(x => x.VehicleBrand)
            .NotEmpty().WithMessage("Vehicle brand is required.")
            .MaximumLength(50).WithMessage("Vehicle brand must not exceed 50 characters.");

        RuleFor(x => x.VehicleModel)
            .NotEmpty().WithMessage("Vehicle model is required.")
            .MaximumLength(50).WithMessage("Vehicle model must not exceed 50 characters.");

        RuleFor(x => x.VehicleColor)
            .NotEmpty().WithMessage("Vehicle color is required.")
            .MaximumLength(30).WithMessage("Vehicle color must not exceed 30 characters.");

        RuleFor(x => x.LicensePlate)
            .NotEmpty().WithMessage("License plate is required.")
            .Matches(ValidationRegex.LicensePlate).WithMessage("Invalid license plate format.");

        RuleFor(x => x.VehicleYear)
            .GreaterThanOrEqualTo(1980).WithMessage("Vehicle year must be 1980 or later.")
            .LessThanOrEqualTo(DateTime.Now.Year + 1).WithMessage("Vehicle year cannot be in the future.");
    }
}

public record UpdateVehicleInfoResponse(string Message, OnboardingStep NextStep);

public class UpdateVehicleInfoEndpoint : Endpoint<UpdateVehicleInfoRequest, UpdateVehicleInfoResponse>
{
    private readonly AppDbContext _db;

    public UpdateVehicleInfoEndpoint(AppDbContext db)
    {
        _db = db;
    }

    public override void Configure()
    {
        Post("/driver/onboarding/vehicle-info");
        Roles("Driver");
        Claims(JwtClaims.Sub);
        Description(x => x
            .WithSummary("Update driver vehicle information")
            .Produces<UpdateVehicleInfoResponse>()
            .Produces(400)
            .Produces(404));
    }

    public override async Task HandleAsync(UpdateVehicleInfoRequest req, CancellationToken ct)
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
            driver.UpdateVehicleInfo(
                req.VehicleType,
                req.VehicleBrand,
                req.VehicleModel,
                req.VehicleColor,
                LicensePlate.From(req.LicensePlate),
                req.VehicleYear);

            await _db.SaveChangesAsync(ct);

            var response = new UpdateVehicleInfoResponse(
                "Vehicle information updated successfully.",
                driver.CurrentStep);

            await SendOkAsync(response, ct);
        }
        catch (Exception ex) when (ex is ArgumentException || ex is InvalidOperationException)
        {
            ThrowError(ex.Message, errorCode: "ValidationError", statusCode: 400);
        }
    }
}
