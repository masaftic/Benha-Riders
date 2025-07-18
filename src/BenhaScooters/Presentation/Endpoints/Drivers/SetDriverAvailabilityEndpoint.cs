using BenhaScooters.Application.Features.Drivers.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Drivers;

public class SetDriverAvailabilityEndpoint : IEndpoint
{
    public record SetDriverAvailabilityRequestDto(string Status);

    public record SetDriverAvailabilityResponseDto(
        string Status,
        DateTime LastStatusChange,
        string Message);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/driver/availability", SetDriverAvailability)
            .WithName("SetDriverAvailability")
            .WithTags("Driver Management")
            .WithSummary("Set driver availability status")
            .WithDescription("Updates the driver's availability status (Online, Offline, Busy). Drivers must be online to receive trip requests. OnTrip status is set automatically and cannot be manually assigned. Only accessible by completed onboarded drivers.")
            .Produces<SetDriverAvailabilityResponseDto>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization("OnboardedDriver")
            .WithOpenApi();
    }

    public async Task<IResult> SetDriverAvailability([FromServices] ISender sender, [FromBody] SetDriverAvailabilityRequestDto availabilityRequest, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new SetDriverAvailabilityEndpointMapper();
        var command = mapper.MapToCommand(availabilityRequest, driverId);
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
public partial class SetDriverAvailabilityEndpointMapper
{
    public SetDriverAvailabilityCommand MapToCommand(SetDriverAvailabilityEndpoint.SetDriverAvailabilityRequestDto request, DriverId driverId)
    {
        return new SetDriverAvailabilityCommand(driverId, request.Status);
    }

    public partial SetDriverAvailabilityEndpoint.SetDriverAvailabilityResponseDto MapToResponse(SetDriverAvailabilityResponse response);

    private static string DriverStatusToString(DriverStatus status) => status.ToString();
}
