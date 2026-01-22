using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Domain.Users;
using BenhaScooters.Shared.Validation;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record UpdateVehicleInfoCommand(
    UserId DriverId,
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
        var userId = command.DriverId;
        return !await _db.DriverProfiles
            .Where(dp => dp.Vehicle != null && dp.UserId != userId)
            .AnyAsync(dp => dp.Vehicle!.VIN == VIN.From(vin), cancellationToken);
    }

    private async Task<bool> BeUniqueLicensePlate(UpdateVehicleInfoCommand command, string licensePlate, CancellationToken cancellationToken)
    {
        var userId = command.DriverId;
        return !await _db.DriverProfiles
            .Where(dp => dp.Vehicle != null && dp.UserId != userId)
            .AnyAsync(dp => dp.Vehicle!.LicensePlate == LicensePlate.From(licensePlate), cancellationToken);
    }
}

public record UpdateVehicleInfoResponse(string Message, DriverOnboardingStatus NextStep);

public class UpdateVehicleInfoCommandHandler(AppDbContext db) : IRequestHandler<UpdateVehicleInfoCommand, ErrorOr<UpdateVehicleInfoResponse>>
{
    private readonly AppDbContext _db = db;

    public async Task<ErrorOr<UpdateVehicleInfoResponse>> Handle(UpdateVehicleInfoCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId;
        
        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == userId, cancellationToken);

        if (driverProfile == null)
        {
            return DriverErrors.Profile.NotFound;
        }

        var vehicleInfo = new DriverVehicleInfo(
            request.VehicleType,
            request.VehicleBrand,
            request.VehicleModel,
            request.VehicleColor,
            LicensePlate.From(request.LicensePlate),
            request.VehicleYear,
            VIN.From(request.VIN));

        var updateResult = driverProfile.UpdateVehicle(vehicleInfo);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateVehicleInfoResponse(
            "تم تحديث معلومات المركبة بنجاح.",
            driverProfile.OnboardingStatus);
    }
}
