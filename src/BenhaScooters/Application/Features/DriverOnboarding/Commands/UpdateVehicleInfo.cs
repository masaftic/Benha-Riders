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
    string VehicleColor,
    string LicensePlate,
    int VehicleYear) : IRequest<ErrorOr<UpdateVehicleInfoResponse>>;



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
            return AppErrors.Driver.Profile.NotFound();
        }

        if (!await _db.DriverProfiles
            .Where(dp => dp.Vehicle != null && dp.UserId != userId)
            .AnyAsync(dp => dp.Vehicle!.LicensePlate == LicensePlate.Create(request.LicensePlate), cancellationToken))
        {
            return AppErrors.Driver.Profile.LicensePlateAlreadyExists();
        }

        var vehicleInfo = new DriverVehicleInfo(
            request.VehicleType,
            request.VehicleBrand,
            request.VehicleColor,
            LicensePlate.Create(request.LicensePlate),
            request.VehicleYear);

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
