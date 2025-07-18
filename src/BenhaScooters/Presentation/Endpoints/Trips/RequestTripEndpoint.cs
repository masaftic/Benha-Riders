using BenhaScooters.Application.Features.Trips.Commands;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class RequestTripEndpoint : IEndpoint
{
    public record RequestTripRequestDto(
        double PickupLatitude,
        double PickupLongitude,
        double DropoffLatitude,
        double DropoffLongitude,
        string? PickupAddress = null,
        string? DropoffAddress = null);

    public record RequestTripResponseDto(
        int TripRequestId,
        decimal EstimatedFare,
        double EstimatedDistance,
        double EstimatedDuration,
        DateTime RequestedAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trips/request", RequestTrip)
            .WithName("RequestTrip")
            .WithTags("Trips - Rider")
            .WithSummary("Request a new trip")
            .WithDescription("Creates a new trip request with pickup and dropoff locations. Calculates estimated fare and distance using the Haversine formula.")
            .Produces<RequestTripResponseDto>()
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization("RiderPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> RequestTrip([FromServices] ISender sender, [FromBody] RequestTripRequestDto request, HttpContext ctx)
    {
        var riderId = ctx.GetRiderId();
        var mapper = new RequestTripEndpointMapper();
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
public partial class RequestTripEndpointMapper
{
    public RequestTripCommand MapToCommand(RequestTripEndpoint.RequestTripRequestDto request, Domain.Riders.RiderId riderId)
    {
        return new RequestTripCommand(
            riderId,
            request.PickupLatitude,
            request.PickupLongitude,
            request.DropoffLatitude,
            request.DropoffLongitude,
            request.PickupAddress,
            request.DropoffAddress);
    }

    public RequestTripEndpoint.RequestTripResponseDto MapToResponse(RequestTripResult result)
    {
        return new RequestTripEndpoint.RequestTripResponseDto(
            result.TripRequestId.Value,
            result.EstimatedFare,
            result.EstimatedDistance,
            result.EstimatedDuration,
            result.RequestedAt);
    }
}
