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
                .WithMessage("Valid vehicle type is required.");

            RuleFor(x => x.VehicleBrand)
                .NotEmpty()
                .WithMessage("Vehicle brand is required.")
                .MinimumLength(2)
                .WithMessage("Vehicle brand must be at least 2 characters long.")
                .MaximumLength(50)
                .WithMessage("Vehicle brand cannot exceed 50 characters.");

            RuleFor(x => x.VehicleModel)
                .NotEmpty()
                .WithMessage("Vehicle model is required.")
                .MinimumLength(1)
                .WithMessage("Vehicle model must be at least 1 character long.")
                .MaximumLength(50)
                .WithMessage("Vehicle model cannot exceed 50 characters.");

            RuleFor(x => x.VehicleColor)
                .NotEmpty()
                .WithMessage("Vehicle color is required.")
                .MinimumLength(2)
                .WithMessage("Vehicle color must be at least 2 characters long.")
                .MaximumLength(30)
                .WithMessage("Vehicle color cannot exceed 30 characters.");

            RuleFor(x => x.LicensePlate)
                .NotEmpty()
                .WithMessage("License plate is required.")
                .Length(3, 10)
                .WithMessage("License plate must be between 3 and 10 characters.");

            RuleFor(x => x.VehicleYear)
                .InclusiveBetween(1980, DateTime.Now.Year + 1)
                .WithMessage($"Vehicle year must be between 1980 and {DateTime.Now.Year + 1}.");

            RuleFor(x => x.VIN)
                .NotEmpty()
                .WithMessage("VIN is required.")
                .Length(17)
                .WithMessage("VIN must be exactly 17 characters.");
        }
    }

    public record UpdateVehicleInfoResponseDto(string Message, string NextStep);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/driver/onboarding/vehicle-info", UpdateVehicleInfo)
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

    private static string OnboardingStepToString(BenhaScooters.Domain.Drivers.ValueObjects.OnboardingStep step) => step.ToString();
}
