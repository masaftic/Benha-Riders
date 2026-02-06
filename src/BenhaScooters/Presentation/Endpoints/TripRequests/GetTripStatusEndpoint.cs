using BenhaScooters.Application.Features.TripRequests.Queries;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.TripRequests.Enums;
using BenhaScooters.Domain.Users;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class GetTripStatusEndpoint : IEndpoint
{
    public record GetTripStatusResponseDto(
        TripRequestId TripRequestId,
        TripRequestStatus Status,
        string? MatchedDriverName,
        DateTime RequestedAt,
        DateTime? MatchedAt,
        DateTime? ExpiresAt,
        string? CancellationReason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/trip-requests/{tripRequestId}/status", GetTripStatus)
            .WithName("GetTripStatus")
            .WithTags("Trip Requests - Rider")
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
    public GetTripStatusQuery MapToQuery(int tripRequestId, UserId riderId)
    {
        return new GetTripStatusQuery(
            riderId,
            TripRequestId.Create(tripRequestId));
    }

    public partial GetTripStatusEndpoint.GetTripStatusResponseDto MapToResponse(GetTripStatusResult result);
}
