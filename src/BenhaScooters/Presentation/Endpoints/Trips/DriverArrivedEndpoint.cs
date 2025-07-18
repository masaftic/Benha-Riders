using BenhaScooters.Application.Features.Trips.Commands;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class DriverArrivedEndpoint : IEndpoint
{
    public record DriverArrivedResponseDto(
        int TripId,
        string Message,
        DateTime ArrivedAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trips/{tripId}/driver-arrived", DriverArrived)
            .WithName("DriverArrived")
            .WithTags("Trips - Driver")
            .WithSummary("Mark driver as arrived at pickup location")
            .WithDescription("Allows drivers to mark themselves as arrived at the pickup location for an assigned trip.")
            .Produces<DriverArrivedResponseDto>()
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization("DriverPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> DriverArrived([FromServices] ISender sender, [FromRoute] int tripId, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new DriverArrivedEndpointMapper();
        var command = mapper.MapToCommand(tripId, driverId);
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
public partial class DriverArrivedEndpointMapper
{
    public DriverArrivedCommand MapToCommand(int tripId, Domain.Drivers.DriverId driverId)
    {
        return new DriverArrivedCommand(
            driverId,
            TripId.From(tripId));
    }

    public DriverArrivedEndpoint.DriverArrivedResponseDto MapToResponse(DriverArrivedResult result)
    {
        return new DriverArrivedEndpoint.DriverArrivedResponseDto(
            result.TripId.Value,
            result.Message,
            result.ArrivedAt);
    }
}
