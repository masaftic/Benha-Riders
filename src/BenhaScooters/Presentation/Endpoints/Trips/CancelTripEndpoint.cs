using BenhaScooters.Application.Features.Trips.Commands;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class CancelTripEndpoint : IEndpoint
{
    public record CancelTripRequestDto(
        int TripRequestId,
        string? CancellationReason = null);

    public record CancelTripResponseDto(
        int TripRequestId,
        string Message,
        DateTime CancelledAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trips/cancel", CancelTrip)
            .WithName("CancelTrip")
            .WithTags("Trips - Rider")
            .WithSummary("Cancel a trip request")
            .WithDescription("Allows riders to cancel their pending trip requests. Only pending trips can be cancelled.")
            .Produces<CancelTripResponseDto>()
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization("RiderPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> CancelTrip([FromServices] ISender sender, [FromBody] CancelTripRequestDto request, HttpContext ctx)
    {
        var riderId = ctx.GetRiderId();
        var mapper = new CancelTripEndpointMapper();
        var command = mapper.MapToCommand(request, riderId);
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
public partial class CancelTripEndpointMapper
{
    public CancelTripCommand MapToCommand(CancelTripEndpoint.CancelTripRequestDto request, Domain.Riders.RiderId riderId)
    {
        return new CancelTripCommand(
            riderId,
            TripRequestId.From(request.TripRequestId),
            request.CancellationReason);
    }

    public CancelTripEndpoint.CancelTripResponseDto MapToResponse(CancelTripResult result)
    {
        return new CancelTripEndpoint.CancelTripResponseDto(
            result.TripRequestId.Value,
            result.Message,
            result.CancelledAt);
    }
}
