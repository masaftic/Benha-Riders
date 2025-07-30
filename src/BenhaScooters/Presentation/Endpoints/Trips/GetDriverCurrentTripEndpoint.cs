using BenhaScooters.Application.Features.Trips.Queries;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Presentation.Endpoints.Trips.Common;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class GetDriverCurrentTripEndpoint : IEndpoint
{
    public record GetDriverCurrentTripResponseDto(
        int TripId,
        string Status,
        string PickupAddress,
        string DropoffAddress,
        decimal EstimatedFare,
        DateTime CreatedAt,
        DateTime? StartedAt,
        DateTime? DriverArrivedAt,
        RiderInfoDto Rider);


    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/trips/current", GetCurrentTrip)
            .WithName("GetDriverCurrentTrip")
            .WithTags("Trips - Driver")
            .WithSummary("Get driver's current active trip")
            .WithDescription("Retrieves the driver's current active trip (assigned, driver arrived, or in progress).")
            .Produces<GetDriverCurrentTripResponseDto>()
            .Produces(404)
            .Produces(401)
            .RequireAuthorization("DriverPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> GetCurrentTrip([FromServices] ISender sender, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new GetDriverCurrentTripEndpointMapper();
        var query = mapper.MapToQuery(driverId);
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
public partial class GetDriverCurrentTripEndpointMapper
{
    public GetDriverCurrentTripQuery MapToQuery(Domain.Drivers.DriverId driverId)
    {
        return new GetDriverCurrentTripQuery(driverId);
    }

    public partial GetDriverCurrentTripEndpoint.GetDriverCurrentTripResponseDto MapToResponse(GetDriverCurrentTripResult result);
}
