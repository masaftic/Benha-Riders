using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Presentation.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class RejectDriverEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/drivers/{driverId}/reject", RejectDriver)
            .WithName("RejectDriverApplication")
            .WithTags("Admin - Driver Management")
            .WithSummary("Reject driver onboarding application")
            .WithDescription("Rejects a driver's onboarding application with a reason. Driver can fix issues and resubmit.")
            .Produces(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> RejectDriver(
        [FromServices] ISender sender, 
        [FromRoute] int driverId, 
        [FromBody] RejectDriverRequest request,
        HttpContext ctx)
    {
        var adminId = ctx.GetCurrentUserId();


        var command = new RejectDriverCommand(
            UserId.From(driverId),
            request.RejectionReason,
            adminId
        );

        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.Ok(new { message = "Driver rejected successfully" });
    }
}

public record RejectDriverRequest(string RejectionReason);
