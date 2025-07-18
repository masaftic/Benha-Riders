using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding;

public class UpdatePersonalInfoEndpoint : IEndpoint
{
    public record UpdatePersonalInfoRequestDto(
        string FullName,
        string NationalId,
        DateOnly DateOfBirth,
        string Address,
        string City,
        string EmergencyContactName,
        string EmergencyContactPhone);

    public record UpdatePersonalInfoResponseDto(string Message, string NextStep);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/driver/onboarding/personal-info", UpdatePersonalInfo)
            .WithName("UpdateDriverPersonalInfo")
            .WithTags("Driver Onboarding")
            .WithSummary("Update driver personal information")
            .WithDescription("Updates the driver's personal information including full name, date of birth, national ID, and address during the onboarding process. National ID must be unique across all drivers.")
            .Produces<UpdatePersonalInfoResponseDto>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Driver"))
            .WithOpenApi();
    }

    public async Task<IResult> UpdatePersonalInfo([FromServices] ISender sender, [FromBody] UpdatePersonalInfoRequestDto personalInfoRequest, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new UpdatePersonalInfoEndpointMapper();
        var command = mapper.MapToCommand(personalInfoRequest, driverId);
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
public partial class UpdatePersonalInfoEndpointMapper
{
    public UpdatePersonalInfoCommand MapToCommand(UpdatePersonalInfoEndpoint.UpdatePersonalInfoRequestDto request, DriverId driverId)
    {
        return new UpdatePersonalInfoCommand(
            driverId,
            request.FullName,
            request.NationalId,
            request.DateOfBirth,
            request.Address,
            request.City,
            request.EmergencyContactName,
            request.EmergencyContactPhone);
    }

    public partial UpdatePersonalInfoEndpoint.UpdatePersonalInfoResponseDto MapToResponse(UpdatePersonalInfoResponse response);

    private static string OnboardingStepToString(OnboardingStep step) => step.ToString();
}
