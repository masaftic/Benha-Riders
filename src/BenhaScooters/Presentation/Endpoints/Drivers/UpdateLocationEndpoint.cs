using BenhaScooters.Application.Features.Drivers.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Drivers;

public class UpdateLocationEndpoint : IEndpoint
{
    public record UpdateLocationRequestDto(double Latitude, double Longitude);
    
    public record UpdateLocationResponseDto(double Latitude, double Longitude);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/driver/location", UpdateLocation)
            .WithName("UpdateDriverLocation")
            .WithTags("Driver Management")
            .WithSummary("Update current driver location")
            .WithDescription("Updates the driver's current GPS location, heading, and speed. Used for real-time tracking and trip monitoring. Latitude must be between -90 and 90, longitude between -180 and 180, heading between 0 and 360 degrees, and speed must be non-negative. Only accessible by completed onboarded drivers.")
            .Produces<UpdateLocationResponseDto>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization("OnboardedDriver")
            .WithOpenApi();
    }

    public async Task<IResult> UpdateLocation([FromServices] ISender sender, [FromBody] UpdateLocationRequestDto locationRequest, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new UpdateLocationEndpointMapper();
        var command = mapper.MapToCommand(locationRequest, driverId);
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
public partial class UpdateLocationEndpointMapper
{
    public UpdateLocationCommand MapToCommand(UpdateLocationEndpoint.UpdateLocationRequestDto request, UserId driverId)
    {
        return new UpdateLocationCommand(driverId, request.Latitude, request.Longitude);
    }

    public partial UpdateLocationEndpoint.UpdateLocationResponseDto MapToResponse(UpdateLocationResponse response);
}
