using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class ApproveDriverEndpoint : IEndpoint
{
    public record ApproveDriverRequestDto(int UserId);

    public record ApproveDriverResponseDto(string Message);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/driver/approve", ApproveDriver)
            .WithName("ApproveDriverApplication")
            .WithTags("Admin - Driver Management")
            .WithSummary("Approve driver onboarding application")
            .WithDescription("Approves a driver's onboarding application, changing their status to 'Completed' and allowing them to start accepting rides. This action is irreversible.")
            .Produces<ApproveDriverResponseDto>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> ApproveDriver([FromServices] ISender sender, [FromBody] ApproveDriverRequestDto approveRequest, HttpContext ctx)
    {
        var mapper = new ApproveDriverEndpointMapper();
        var command = mapper.MapToCommand(approveRequest);
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
public partial class ApproveDriverEndpointMapper
{
    public ApproveDriverCommand MapToCommand(ApproveDriverEndpoint.ApproveDriverRequestDto request)
    {
        return new ApproveDriverCommand(UserId.From(request.UserId));
    }

    public partial ApproveDriverEndpoint.ApproveDriverResponseDto MapToResponse(ApproveDriverResponse response);
}
