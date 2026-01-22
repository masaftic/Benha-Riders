using BenhaScooters.Application.Features.TripRequests.Commands;
using BenhaScooters.Application.Features.Trips.Commands;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Users;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.TripRequests;

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
        app.MapPost("/trip-requests", RequestTrip)
            .WithName("RequestTrip")
            .WithTags("Trip Requests - Rider")
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
    public RequestTripCommand MapToCommand(RequestTripEndpoint.RequestTripRequestDto request, UserId riderId)
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

    public partial RequestTripEndpoint.RequestTripResponseDto MapToResponse(RequestTripResult result);

    private int MapTripRequestId(TripRequestId id) => id.Value;
}
