using BenhaScooters.Application.Features.Trips.Queries;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class GetTripStatusEndpoint : IEndpoint
{
    public record GetTripStatusResponseDto(
        int TripRequestId,
        TripRequestStatus Status,
        string? AssignedDriverName,
        DateTime RequestedAt,
        DateTime? AcceptedAt,
        DateTime? ExpiresAt,
        string? CancellationReason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/trips/{tripRequestId}/status", GetTripStatus)
            .WithName("GetTripStatus")
            .WithTags("Trips - Rider")
            .WithSummary("Get trip request status")
            .WithDescription("Retrieves the current status of a trip request including driver details if assigned. Only accessible by the rider who created the request.")
            .Produces<GetTripStatusResponseDto>()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization("RiderPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> GetTripStatus([FromServices] ISender sender, [FromRoute] int tripRequestId, HttpContext ctx)
    {
        var riderId = ctx.GetRiderId();
        var mapper = new GetTripStatusEndpointMapper();
        var query = mapper.MapToQuery(tripRequestId, riderId);
        var result = await sender.Send(query);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        var response = mapper.MapToResponse(result.Value);
        return Results.Ok(response);
    }
}

[Mapper]
public partial class GetTripStatusEndpointMapper
{
    public GetTripStatusQuery MapToQuery(int tripRequestId, Domain.Riders.RiderId riderId)
    {
        return new GetTripStatusQuery(
            riderId,
            TripRequestId.From(tripRequestId));
    }

    public GetTripStatusEndpoint.GetTripStatusResponseDto MapToResponse(GetTripStatusResult result)
    {
        return new GetTripStatusEndpoint.GetTripStatusResponseDto(
            result.TripRequestId.Value,
            result.Status,
            result.MatchedDriverName,
            result.RequestedAt,
            result.MatchedAt,
            result.ExpiresAt,
            result.CancellationReason);
    }
}
