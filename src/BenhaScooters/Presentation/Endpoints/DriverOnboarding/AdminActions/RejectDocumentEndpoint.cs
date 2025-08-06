using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class RejectDocumentEndpoint : IEndpoint
{
    public record RejectDocumentRequestDto(string DocumentType, string Reason);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/admin/drivers/{driverId}/documents/reject", RejectDocument)
            .WithName("RejectDriverDocument")
            .WithTags("Admin - Driver Management")
            .WithSummary("Reject driver document")
            .WithDescription("Rejects a driver's document, marking it as invalid and providing a reason for rejection.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> RejectDocument([FromServices] ISender sender, [FromRoute] int driverId, [FromBody] RejectDocumentRequestDto request, HttpContext ctx)
    {
        var command = new RejectDocumentCommand(DriverId.From(driverId), Enum.Parse<DocumentType>(request.DocumentType), request.Reason);

        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.NoContent();
    }
}
