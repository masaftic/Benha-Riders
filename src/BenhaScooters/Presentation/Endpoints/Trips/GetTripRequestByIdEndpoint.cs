using BenhaScooters.Application.Features.Trips.Queries;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class GetTripRequestByIdEndpoint : IEndpoint
{
    public record GetTripRequestByIdResponseDto(
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
        DateTime ExpiresAt,
        TripRequestStatus Status);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/trips/requests/{tripRequestId}", GetTripRequestById)
            .WithName("GetTripRequestById")
            .WithTags("Trips - Driver")
            .WithSummary("Get trip request details by ID")
            .WithDescription("Retrieves detailed information about a specific trip request for drivers to review before accepting.")
            .Produces<GetTripRequestByIdResponseDto>()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization("DriverPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> GetTripRequestById([FromServices] ISender sender, [FromRoute] int tripRequestId, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new GetTripRequestByIdEndpointMapper();
        var query = mapper.MapToQuery(tripRequestId, driverId);
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
public partial class GetTripRequestByIdEndpointMapper
{
    public GetTripRequestByIdQuery MapToQuery(int tripRequestId, Domain.Drivers.DriverId driverId)
    {
        return new GetTripRequestByIdQuery(
            driverId,
            TripRequestId.From(tripRequestId));
    }

    public GetTripRequestByIdEndpoint.GetTripRequestByIdResponseDto MapToResponse(GetTripRequestByIdResult result)
    {
        return new GetTripRequestByIdEndpoint.GetTripRequestByIdResponseDto(
            result.TripRequestId,
            result.PickupLatitude,
            result.PickupLongitude,
            result.DropoffLatitude,
            result.DropoffLongitude,
            result.PickupAddress,
            result.DropoffAddress,
            result.EstimatedFare,
            result.EstimatedDistance,
            result.EstimatedDuration,
            result.RequestedAt,
            result.ExpiresAt,
            result.Status);
    }
}
