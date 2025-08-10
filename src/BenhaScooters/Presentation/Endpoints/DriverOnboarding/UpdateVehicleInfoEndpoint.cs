using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding;

public class UpdateVehicleInfoEndpoint : IEndpoint
{
    public record UpdateVehicleInfoRequestDto(
        VehicleType VehicleType,
        string VehicleBrand,
        string VehicleModel,
        string VehicleColor,
        string LicensePlate,
        int VehicleYear,
        string VIN);

    public class UpdateVehicleInfoRequestValidator : AbstractValidator<UpdateVehicleInfoRequestDto>
    {
        public UpdateVehicleInfoRequestValidator()
        {
            RuleFor(x => x.VehicleType)
                .IsInEnum()
                .WithMessage("نوع المركبة صحيح مطلوب.");

            RuleFor(x => x.VehicleBrand)
                .NotEmpty()
                .WithMessage("ماركة المركبة مطلوبة.")
                .MinimumLength(2)
                .WithMessage("ماركة المركبة يجب أن تكون على الأقل حرفين.")
                .MaximumLength(50)
                .WithMessage("ماركة المركبة لا يمكن أن تتجاوز 50 حرف.");

            RuleFor(x => x.VehicleModel)
                .NotEmpty()
                .WithMessage("موديل المركبة مطلوب.")
                .MinimumLength(1)
                .WithMessage("موديل المركبة يجب أن يكون على الأقل حرف واحد.")
                .MaximumLength(50)
                .WithMessage("موديل المركبة لا يمكن أن يتجاوز 50 حرف.");

            RuleFor(x => x.VehicleColor)
                .NotEmpty()
                .WithMessage("لون المركبة مطلوب.")
                .MinimumLength(2)
                .WithMessage("لون المركبة يجب أن يكون على الأقل حرفين.")
                .MaximumLength(30)
                .WithMessage("لون المركبة لا يمكن أن يتجاوز 30 حرف.");

            RuleFor(x => x.LicensePlate)
                .NotEmpty()
                .WithMessage("لوحة الترخيص مطلوبة.")
                .Length(3, 10)
                .WithMessage("لوحة الترخيص يجب أن تكون بين 3 و 10 أحرف.");

            RuleFor(x => x.VehicleYear)
                .InclusiveBetween(1980, DateTime.Now.Year + 1)
                .WithMessage($"سنة المركبة يجب أن تكون بين 1980 و {DateTime.Now.Year + 1}.");

            RuleFor(x => x.VIN)
                .NotEmpty()
                .WithMessage("رقم الهيكل (VIN) مطلوب.")
                .Length(17)
                .WithMessage("رقم الهيكل (VIN) يجب أن يكون 17 حرف بالضبط.");
        }
    }

    public record UpdateVehicleInfoResponseDto(string Message, string NextStep);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/drivers/me/onboarding/vehicle-info", UpdateVehicleInfo)
            .AddEndpointFilter<ValidationFilter<UpdateVehicleInfoRequestDto>>()
            .WithName("UpdateDriverVehicleInfo")
            .WithTags("Driver Onboarding")
            .WithSummary("Update driver vehicle information")
            .WithDescription("Updates the driver's vehicle information including make, model, year, license plate, and color during the onboarding process. Vehicle year must be between 1980 and current year + 1.")
            .Produces<UpdateVehicleInfoResponseDto>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Driver"))
            .WithOpenApi();
    }

    public async Task<IResult> UpdateVehicleInfo([FromServices] ISender sender, [FromBody] UpdateVehicleInfoRequestDto vehicleInfoRequest, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new UpdateVehicleInfoEndpointMapper();
        var command = mapper.MapToCommand(vehicleInfoRequest, driverId);
        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        var response = mapper.MapToResponse(result.Value);
        return Results.Ok(response);
    }
}

[Mapper]
public partial class UpdateVehicleInfoEndpointMapper
{
    public UpdateVehicleInfoCommand MapToCommand(UpdateVehicleInfoEndpoint.UpdateVehicleInfoRequestDto request, DriverId driverId)
    {
        return new UpdateVehicleInfoCommand(
            driverId,
            request.VehicleType,
            request.VehicleBrand,
            request.VehicleModel,
            request.VehicleColor,
            request.LicensePlate,
            request.VehicleYear,
            request.VIN);
    }

    public partial UpdateVehicleInfoEndpoint.UpdateVehicleInfoResponseDto MapToResponse(UpdateVehicleInfoResponse response);
}
