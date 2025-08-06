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

public class RejectMatchEndpoint : IEndpoint
{
    public record RejectMatchRequestDto(int DriverMatchAttemptId, string? Reason = null);


    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/matching/reject", RejectMatch)
            .WithName("RejectMatch")
            .WithTags("Matching - Driver")
            .WithSummary("Reject a trip match offer")
            .WithDescription("Allows drivers to reject a trip match offer. This will continue the matching process with other drivers.")
            .Produces<NoContent>()
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(404)
            .RequireAuthorization("DriverPolicy")
            .WithOpenApi();
    }

    public async Task<IResult> RejectMatch([FromServices] ISender sender, [FromBody] RejectMatchRequestDto request, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new RejectMatchEndpointMapper();
        var command = mapper.MapToCommand(request, driverId);
        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.NoContent();
    }
}

[Mapper]
public partial class RejectMatchEndpointMapper
{
    public RejectMatchCommand MapToCommand(RejectMatchEndpoint.RejectMatchRequestDto request, DriverId driverId) =>
        new(driverId, DriverMatchAttemptId.From(request.DriverMatchAttemptId), request.Reason);

}
