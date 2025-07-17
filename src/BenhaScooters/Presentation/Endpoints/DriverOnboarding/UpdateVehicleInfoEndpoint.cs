using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
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
        int VehicleYear);

    public record UpdateVehicleInfoResponseDto(string Message, string NextStep);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/driver/onboarding/vehicle-info", UpdateVehicleInfo)
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
        var userId = ctx.GetCurrentUserId();
        var mapper = new UpdateVehicleInfoEndpointMapper();
        var command = mapper.MapToCommand(vehicleInfoRequest, userId);
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
    public UpdateVehicleInfoCommand MapToCommand(UpdateVehicleInfoEndpoint.UpdateVehicleInfoRequestDto request, UserId userId)
    {
        return new UpdateVehicleInfoCommand(
            userId,
            request.VehicleType,
            request.VehicleBrand,
            request.VehicleModel,
            request.VehicleColor,
            request.LicensePlate,
            request.VehicleYear);
    }

    public partial UpdateVehicleInfoEndpoint.UpdateVehicleInfoResponseDto MapToResponse(UpdateVehicleInfoResponse response);

    private static string OnboardingStepToString(OnboardingStep step) => step.ToString();
}
