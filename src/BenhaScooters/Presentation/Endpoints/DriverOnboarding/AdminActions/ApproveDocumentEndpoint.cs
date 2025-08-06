
using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Entities;
using Microsoft.AspNetCore.Mvc;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class ApproveDocumentEndpoint : IEndpoint
{
    public record ApproveDocumentRequestDto(string DocumentType, DateOnly? ExpiryDate);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPut("/admin/drivers/{driverId}/documents/approve", ApproveDocument)
            .WithName("ApproveDriverDocument")
            .WithTags("Admin - Driver Management")
            .WithSummary("Approve driver document")
            .WithDescription("Approves a driver's document, marking it as valid and setting the expiry date if provided.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> ApproveDocument([FromServices] ISender sender, [FromRoute] int driverId, [FromBody] ApproveDocumentRequestDto request, HttpContext ctx)
    {
        var command = new ApproveDocumentCommand(DriverId.From(driverId), Enum.Parse<DocumentType>(request.DocumentType), request.ExpiryDate);

        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.NoContent();
    }
}
