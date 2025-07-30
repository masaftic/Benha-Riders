using BenhaScooters.Application.Features.Trips.Queries;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Presentation.Endpoints.Trips.Common;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class GetDriverTripByIdEndpoint : IEndpoint
{
    public record GetDriverTripByIdResponseDto(
        int TripId,
        string Status,
        string PickupAddress,
        string DropoffAddress,
        decimal EstimatedFare,
        decimal? FinalFare,
        bool IsPaid,
        DateTime CreatedAt,
        DateTime? StartedAt,
        DateTime? DriverArrivedAt,
        DateTime? CompletedAt,
        TimeSpan? TotalDuration,
        RiderInfoDto Rider);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/trips/{tripId}", GetTripById)
            .WithName("GetDriverTripById")
            .WithTags("Trips - Driver")
            .WithSummary("Get a specific trip by ID")
            .WithDescription("Retrieves detailed information about a specific trip for the authenticated driver.")
            .Produces<GetDriverTripByIdResponseDto>()
            .Produces(404)
            .Produces(401)
            .RequireAuthorization("DriverPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> GetTripById([FromServices] ISender sender, [FromRoute] int tripId, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new GetDriverTripByIdEndpointMapper();
        var query = mapper.MapToQuery(driverId, tripId);
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
public partial class GetDriverTripByIdEndpointMapper
{
    public GetDriverTripByIdQuery MapToQuery(Domain.Drivers.DriverId driverId, int tripId)
    {
        return new GetDriverTripByIdQuery(driverId, TripId.From(tripId));
    }

    public partial GetDriverTripByIdEndpoint.GetDriverTripByIdResponseDto MapToResponse(GetDriverTripByIdResult result);
}
