using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
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
    int VehicleYear) : IRequest<ErrorOr<UpdateVehicleInfoResponse>>;

public class UpdateVehicleInfoCommandValidator : AbstractValidator<UpdateVehicleInfoCommand>
{
    public UpdateVehicleInfoCommandValidator()
    {
        RuleFor(x => x.VehicleType)
            .IsInEnum().WithMessage("نوع المركبة مطلوب.");

        RuleFor(x => x.VehicleBrand)
            .NotEmpty().WithMessage("ماركة المركبة مطلوبة.")
            .MaximumLength(50).WithMessage("يجب ألا تتجاوز ماركة المركبة 50 حرفًا.");

        RuleFor(x => x.VehicleModel)
            .NotEmpty().WithMessage("طراز المركبة مطلوب.")
            .MaximumLength(50).WithMessage("يجب ألا يتجاوز طراز المركبة 50 حرفًا.");

        RuleFor(x => x.VehicleColor)
            .NotEmpty().WithMessage("لون المركبة مطلوب.")
            .MaximumLength(30).WithMessage("يجب ألا يتجاوز لون المركبة 30 حرفًا.");

        RuleFor(x => x.LicensePlate)
            .NotEmpty().WithMessage("رقم لوحة الترخيص مطلوب.")
            .Matches(ValidationRegex.LicensePlate).WithMessage("تنسيق رقم لوحة الترخيص غير صالح.");

        RuleFor(x => x.VehicleYear)
            .GreaterThanOrEqualTo(1980).WithMessage("يجب أن يكون سنة المركبة 1980 أو أحدث.")
            .LessThanOrEqualTo(DateTime.Now.Year + 1).WithMessage("لا يمكن أن تكون سنة المركبة في المستقبل.");
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
            .FirstOrDefaultAsync(dp => dp.Id == request.DriverId, cancellationToken);

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
