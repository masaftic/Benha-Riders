using BenhaScooters.Application.Features.Trips.Queries;
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
        app.MapGet("/trips/available", GetAvailableTrips)
            .WithName("GetAvailableTrips")
            .WithTags("Trips - Driver")
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
    public GetAvailableTripsQuery MapToQuery(Domain.Drivers.DriverId driverId)
    {
        return new GetAvailableTripsQuery(driverId);
    }

    public GetAvailableTripsEndpoint.GetAvailableTripsResponseDto MapToResponse(GetAvailableTripsResult result)
    {
        return new GetAvailableTripsEndpoint.GetAvailableTripsResponseDto(
            result.AvailableTrips.Select(MapToDto).ToList());
    }

    private GetAvailableTripsEndpoint.AvailableTripDto MapToDto(AvailableTripDto source)
    {
        return new GetAvailableTripsEndpoint.AvailableTripDto(
            source.TripRequestId,
            source.PickupLatitude,
            source.PickupLongitude,
            source.DropoffLatitude,
            source.DropoffLongitude,
            source.PickupAddress,
            source.DropoffAddress,
            source.EstimatedFare,
            source.EstimatedDistance,
            source.EstimatedDuration,
            source.RequestedAt,
            source.ExpiresAt);
    }
}
