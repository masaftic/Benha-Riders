using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class RejectDriverEndpoint : IEndpoint
{
    public record RejectDriverRequestDto(int UserId, string Reason);

    public record RejectDriverResponseDto(string Message);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/driver/reject", RejectDriver)
            .WithName("RejectDriverApplication")
            .WithTags("Admin - Driver Management")
            .WithSummary("Reject driver onboarding application")
            .WithDescription("Rejects a driver's onboarding application with a specific reason. The driver can resubmit their application after addressing the rejection reason. Rejection reason is required and will be visible to the driver.")
            .Produces<RejectDriverResponseDto>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> RejectDriver([FromServices] ISender sender, [FromBody] RejectDriverRequestDto rejectRequest, HttpContext ctx)
    {
        var mapper = new RejectDriverEndpointMapper();
        var command = mapper.MapToCommand(rejectRequest);
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
public partial class RejectDriverEndpointMapper
{
    public RejectDriverCommand MapToCommand(RejectDriverEndpoint.RejectDriverRequestDto request)
    {
        return new RejectDriverCommand(UserId.From(request.UserId), request.Reason);
    }

    public partial RejectDriverEndpoint.RejectDriverResponseDto MapToResponse(RejectDriverResponse response);
}
