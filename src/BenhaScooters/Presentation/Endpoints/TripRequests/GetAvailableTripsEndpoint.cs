using BenhaScooters.Application.Features.TripRequests.Queries;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Users;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class GetAvailableTripsEndpoint : IEndpoint
{
    public record AvailableTripDto(
        int TripRequestId,
        double PickupLatitude,
        double PickupLongitude,
        double DropoffLatitude,
        double DropoffLongitude,
        string? PickupAddress,
        string? DropoffAddress,
        decimal EstimatedFare,
        double EstimatedDistance,
        double EstimatedDuration,
        DateTime RequestedAt,
        DateTime ExpiresAt);

    public record GetAvailableTripsResponseDto(List<AvailableTripDto> AvailableTrips);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/trip-requests/available", GetAvailableTrips)
            .WithName("GetAvailableTrips")
            .WithTags("Trip Requests - Driver")
            .WithSummary("Get available trip requests for drivers")
            .WithDescription("Returns a list of pending trip requests that drivers can accept. Only shows trips if the driver is available for requests.")
            .Produces<GetAvailableTripsResponseDto>()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization("DriverPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> GetAvailableTrips([FromServices] ISender sender, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new GetAvailableTripsEndpointMapper();
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
public partial class GetAvailableTripsEndpointMapper
{
    public GetAvailableTripsQuery MapToQuery(UserId driverId)
    {
        return new GetAvailableTripsQuery(driverId);
    }

    public partial GetAvailableTripsEndpoint.GetAvailableTripsResponseDto MapToResponse(GetAvailableTripsResult result);

    private int MapTripRequestId(TripRequestId id) => id.Value;
}
