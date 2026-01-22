using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;
using BenhaScooters.Domain.Users;
using BenhaScooters.Domain.Drivers;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class ApproveDriverEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/drivers/{driverId}/approve", ApproveDriver)
            .WithName("ApproveDriverApplication")
            .WithTags("Admin - Driver Management")
            .WithSummary("Approve driver onboarding application")
            .WithDescription("Approves a driver's onboarding application, changing their status to 'Completed' and allowing them to start accepting rides. This action is irreversible.")
            .Produces<NoContent>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> ApproveDriver([FromServices] ISender sender, [FromRoute] int driverId, HttpContext ctx)
    {
        var command = new ApproveDriverCommand(UserId.From(driverId));
        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.NoContent();
    }
}
