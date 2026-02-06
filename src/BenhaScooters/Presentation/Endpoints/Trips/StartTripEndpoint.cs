using BenhaScooters.Application.Features.Trips.Commands;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class StartTripEndpoint : IEndpoint
{
    public record StartTripResponseDto(
        int TripId,
        string Message,
        DateTime StartedAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trips/{tripId}/start", StartTrip)
            .WithName("StartTrip")
            .WithTags("Trips - Driver")
            .WithSummary("Start the trip")
            .WithDescription("Allows drivers to start the trip after arriving at pickup location and picking up the rider.")
            .Produces<StartTripResponseDto>()
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization("DriverPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> StartTrip([FromServices] ISender sender, [FromRoute] int tripId, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new StartTripEndpointMapper();
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
public partial class StartTripEndpointMapper
{
    public StartTripCommand MapToCommand(int tripId, UserId driverId)
    {
        return new StartTripCommand(
            driverId,
            TripId.Create(tripId));
    }

    public StartTripEndpoint.StartTripResponseDto MapToResponse(StartTripResult result)
    {
        return new StartTripEndpoint.StartTripResponseDto(
            result.TripId,
            result.Message,
            result.StartedAt);
    }
}
