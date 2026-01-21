using BenhaScooters.Application.Features.TripRequests.Commands;
using BenhaScooters.Domain.TripRequests;

namespace BenhaScooters.Presentation.Endpoints.TripRequests;

public class ConfirmTripRequestEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/trip-requests/{tripRequestId}/confirm", ConfirmTripRequest)
            .WithName("ConfirmTripRequest")
            .WithTags("Trip Requests - Rider")
            .WithSummary("Confirm a trip request")
            .WithDescription("Confirms a trip request after the rider has seen the estimated fare.")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization("RiderPolicy")
            .WithOpenApi();
    }


    public async Task<IResult> ConfirmTripRequest([FromServices] ISender sender, [FromRoute] int tripRequestId, HttpContext ctx)
    {
        var riderId = ctx.GetRiderId();
        var command = new ConfirmTripRequestCommand(TripRequestId.From(tripRequestId), riderId);
        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.Ok();
    }
}
