using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Entities;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Shared.Validation;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record UpdateVehicleInfoCommand(
    DriverId DriverId,
    VehicleType VehicleType,
    string VehicleBrand,
    string VehicleModel,
    string VehicleColor,
    string LicensePlate,
    int VehicleYear,
    string VIN) : IRequest<ErrorOr<UpdateVehicleInfoResponse>>;

public class UpdateVehicleInfoCommandValidator : AbstractValidator<UpdateVehicleInfoCommand>
{
    private readonly AppDbContext _db;

    public UpdateVehicleInfoCommandValidator(AppDbContext db)
    {
        _db = db;
        
        // Only business logic validations here
        RuleFor(x => x.VIN)
            .MustAsync(BeUniqueVIN)
            .WithMessage("هذا الرقم التسلسلي للمركبة مسجل مع سائق آخر.")
            .When(x => !string.IsNullOrEmpty(x.VIN));

        RuleFor(x => x.LicensePlate)
            .MustAsync(BeUniqueLicensePlate)
            .WithMessage("رقم لوحة الترخيص هذا مسجل مع مركبة أخرى.")
            .When(x => !string.IsNullOrEmpty(x.LicensePlate));
    }

    private async Task<bool> BeUniqueVIN(UpdateVehicleInfoCommand command, string vin, CancellationToken cancellationToken)
    {
        return !await _db.Vehicles
            .AnyAsync(v => v.VIN == VIN.From(vin) && v.DriverId != command.DriverId && v.IsActive, 
                cancellationToken);
    }

    private async Task<bool> BeUniqueLicensePlate(UpdateVehicleInfoCommand command, string licensePlate, CancellationToken cancellationToken)
    {
        return !await _db.Vehicles
            .AnyAsync(v => v.LicensePlate == LicensePlate.From(licensePlate) && v.DriverId != command.DriverId && v.IsActive, 
                cancellationToken);
    }
}

public record UpdateVehicleInfoResponse(string Message, OnboardingStatus NextStep);

public class UpdateVehicleInfoCommandHandler(AppDbContext db) : IRequestHandler<UpdateVehicleInfoCommand, ErrorOr<UpdateVehicleInfoResponse>>
{
    private readonly AppDbContext _db = db;

    public async Task<ErrorOr<UpdateVehicleInfoResponse>> Handle(UpdateVehicleInfoCommand request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.Id == request.DriverId, cancellationToken);

        if (driver == null)
        {
            return DriverErrors.DriverNotFound;
        }

        var updateResult = driver.EnrollVehicle(
            request.VehicleType,
            request.VehicleBrand,
            request.VehicleModel,
            request.VehicleColor,
            LicensePlate.From(request.LicensePlate),
            request.VehicleYear,
            VIN.From(request.VIN));

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateVehicleInfoResponse(
            "تم تحديث معلومات المركبة بنجاح.",
            driver.OnboardingState.Status);
    }
}
