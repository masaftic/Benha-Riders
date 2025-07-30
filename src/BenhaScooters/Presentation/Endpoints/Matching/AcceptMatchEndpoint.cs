using BenhaScooters.Application.Features.Matching.Commands;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.Matching;

public class AcceptMatchEndpoint : IEndpoint
{
    public record AcceptMatchRequestDto(int DriverMatchAttemptId);

    public record AcceptMatchResponseDto(int TripId, string Message, DateTime AcceptedAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/matching/accept", AcceptMatch)
            .WithName("AcceptMatch")
            .WithTags("Matching - Driver")
            .WithSummary("Accept a trip match offer")
            .WithDescription("Allows drivers to accept a trip match offer. This will complete the matching session and trigger trip creation.")
            .Produces<AcceptMatchResponseDto>()
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization("DriverPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> AcceptMatch([FromServices] ISender sender, [FromBody] AcceptMatchRequestDto request, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new AcceptMatchEndpointMapper();
        var command = mapper.MapToCommand(request, driverId);
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
public partial class AcceptMatchEndpointMapper
{
    public AcceptMatchCommand MapToCommand(AcceptMatchEndpoint.AcceptMatchRequestDto request, DriverId driverId) =>
        new(driverId, DriverMatchAttemptId.From(request.DriverMatchAttemptId));

    public partial AcceptMatchEndpoint.AcceptMatchResponseDto MapToResponse(AcceptMatchResult result);

    private int MapTripId(TripId id) => id.Value;
}
