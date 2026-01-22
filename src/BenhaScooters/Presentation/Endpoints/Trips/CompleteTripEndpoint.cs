using BenhaScooters.Application.Features.Trips.Commands;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class CompleteTripEndpoint : IEndpoint
{
    public record CompleteTripResponseDto(
        int TripId,
        string Message,
        DateTime CompletedAt,
        TimeSpan? TotalDuration,
        decimal FinalFare);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trips/{tripId}/complete", CompleteTrip)
            .WithName("CompleteTrip")
            .WithTags("Trips - Driver")
            .WithSummary("Complete the trip")
            .WithDescription("Allows drivers to complete the trip when they reach the destination. Updates driver availability back to available.")
            .Produces<CompleteTripResponseDto>()
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization("DriverPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> CompleteTrip([FromServices] ISender sender, [FromRoute] int tripId, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new CompleteTripEndpointMapper();
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
public partial class CompleteTripEndpointMapper
{
    public CompleteTripCommand MapToCommand(int tripId, UserId driverId)
    {
        return new CompleteTripCommand(driverId, TripId.From(tripId));
    }

    public CompleteTripEndpoint.CompleteTripResponseDto MapToResponse(CompleteTripResult result)
    {
        return new CompleteTripEndpoint.CompleteTripResponseDto(
            result.TripId.Value,
            result.Message,
            result.CompletedAt,
            result.TotalDuration,
            result.FinalFare);
    }
}
