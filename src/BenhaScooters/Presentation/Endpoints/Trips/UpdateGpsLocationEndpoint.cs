using BenhaScooters.Application.Features.Trips.Commands;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Trips;

public class UpdateGpsLocationEndpoint : IEndpoint
{
    public record UpdateGpsLocationRequestDto(
        double Latitude,
        double Longitude);

    public record UpdateGpsLocationResponseDto(
        int TripId,
        string Message,
        DateTime Timestamp);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trips/{tripId}/gps-location", UpdateGpsLocation)
            .WithName("UpdateGpsLocation")
            .WithTags("Trips - Driver")
            .WithSummary("Update GPS location during trip")
            .WithDescription("Allows drivers to send GPS location updates during an active trip. This builds the trip route in real-time.")
            .Produces<UpdateGpsLocationResponseDto>()
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization("DriverPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> UpdateGpsLocation([FromServices] ISender sender, [FromRoute] int tripId, [FromBody] UpdateGpsLocationRequestDto request, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new UpdateGpsLocationEndpointMapper();
        var command = mapper.MapToCommand(request, tripId, driverId);
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
public partial class UpdateGpsLocationEndpointMapper
{
    public UpdateGpsLocationCommand MapToCommand(UpdateGpsLocationEndpoint.UpdateGpsLocationRequestDto request, int tripId, Domain.Drivers.DriverId driverId)
    {
        return new UpdateGpsLocationCommand(
            driverId,
            TripId.From(tripId),
            request.Latitude,
            request.Longitude);
    }

    public UpdateGpsLocationEndpoint.UpdateGpsLocationResponseDto MapToResponse(UpdateGpsLocationResult result)
    {
        return new UpdateGpsLocationEndpoint.UpdateGpsLocationResponseDto(
            result.TripId.Value,
            result.Message,
            result.Timestamp);
    }
}
