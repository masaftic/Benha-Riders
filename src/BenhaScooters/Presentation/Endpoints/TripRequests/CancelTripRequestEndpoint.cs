using BenhaScooters.Application.Features.Trips.Commands;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class CancelTripRequestEndpoint : IEndpoint
{
    public record CancelTripRequestRequestDto(
        int TripRequestId,
        string? CancellationReason = null);

    public record CancelTripRequestResponseDto(
        int TripRequestId,
        string Message,
        DateTime CancelledAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trip-requests/cancel", CancelTripRequest)
            .WithName("CancelTripRequest")
            .WithTags("Trip Requests - Rider")
            .WithSummary("Cancel a trip request")
            .WithDescription("Allows riders to cancel their pending trip requests. Only pending trips can be cancelled.")
            .Produces<CancelTripRequestResponseDto>()
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization("RiderPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> CancelTripRequest([FromServices] ISender sender, [FromBody] CancelTripRequestRequestDto request, HttpContext ctx)
    {
        var riderId = ctx.GetRiderId();
        var mapper = new CancelTripRequestEndpointMapper();
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
public partial class CancelTripRequestEndpointMapper
{
    public CancelTripRequestCommand MapToCommand(CancelTripRequestEndpoint.CancelTripRequestRequestDto request, Domain.Riders.RiderId riderId)
    {
        return new CancelTripRequestCommand(
            riderId,
            TripRequestId.From(request.TripRequestId),
            request.CancellationReason);
    }

    public partial CancelTripRequestEndpoint.CancelTripRequestResponseDto MapToResponse(CancelTripRequestResult result);

    private int MapTripRequestId(TripRequestId id) => id.Value;
}
