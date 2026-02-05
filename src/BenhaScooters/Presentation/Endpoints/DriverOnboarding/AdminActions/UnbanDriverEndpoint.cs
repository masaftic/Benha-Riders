using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Presentation.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class UnbanDriverEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/drivers/{driverId}/unban", UnbanDriver)
            .WithName("UnbanDriver")
            .WithTags("Admin - Driver Management")
            .WithSummary("Unban/unsuspend a driver")
            .WithDescription("Unsuspends a banned driver, restoring them to approved status.")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> UnbanDriver(
        [FromServices] ISender sender, 
        [FromRoute] int driverId,
        HttpContext ctx)
    {
        var adminId = ctx.GetCurrentUserId();

        var command = new UnbanDriverCommand(
            UserId.From(driverId),
            adminId
        );

        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.Ok(new { message = "Driver unbanned successfully" });
    }
}
