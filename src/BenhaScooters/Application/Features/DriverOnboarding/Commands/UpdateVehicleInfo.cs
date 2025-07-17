using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Shared.Validation;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record UpdateVehicleInfoCommand(
    UserId UserId,
    VehicleType VehicleType,
    string VehicleBrand,
    string VehicleModel,
    string VehicleColor,
    string LicensePlate,
    int VehicleYear) : IRequest<ErrorOr<UpdateVehicleInfoResponse>>;

public class UpdateVehicleInfoCommandValidator : AbstractValidator<UpdateVehicleInfoCommand>
{
    public UpdateVehicleInfoCommandValidator()
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

public class UpdateVehicleInfoCommandHandler : IRequestHandler<UpdateVehicleInfoCommand, ErrorOr<UpdateVehicleInfoResponse>>
{
    private readonly AppDbContext _db;

    public UpdateVehicleInfoCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<UpdateVehicleInfoResponse>> Handle(UpdateVehicleInfoCommand request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.UserId == request.UserId, cancellationToken);

        if (driver == null)
        {
            return DriverErrors.DriverNotFound;
        }

        var updateResult = driver.UpdateVehicleInfo(
            request.VehicleType,
            request.VehicleBrand,
            request.VehicleModel,
            request.VehicleColor,
            LicensePlate.From(request.LicensePlate),
            request.VehicleYear);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateVehicleInfoResponse(
            "Vehicle information updated successfully.",
            driver.CurrentStep);
    }
}
