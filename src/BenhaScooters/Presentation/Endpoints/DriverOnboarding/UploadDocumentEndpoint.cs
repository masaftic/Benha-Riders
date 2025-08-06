using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain.Drivers.Entities;
using FluentValidation;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding;

public class UploadDocumentEndpoint : IEndpoint
{
    public record UploadDocumentRequest(
        string DocumentType,
        IFormFile File);

    public class Validator : AbstractValidator<UploadDocumentRequest>
    {
        public Validator()
        {
            RuleFor(x => x.DocumentType).NotEmpty().IsEnumName(typeof(DocumentType), caseSensitive: false).WithMessage("Invalid document type.");
            RuleFor(x => x.File).NotNull().WithMessage("File is required.").Must(file => file.Length > 0).WithMessage("File cannot be empty.");
        }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/drivers/me/onboarding/documents/upload", UploadDocument)
            .AddEndpointFilter<ValidationFilter<UploadDocumentRequest>>()
            .WithName("UploadDocument")
            .WithTags("Driver Onboarding")
            .WithSummary("Upload a document for driver onboarding.")
            .WithDescription("Allows drivers to upload required documents for onboarding. If image existed, it will be replaced if it isn't approved already. Types of documents are: DrivingLicense, VehicleRegistration, DriverPhoto")
            .Accepts<UploadDocumentRequest>("multipart/form-data")
            .Produces(204)
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization(policy => policy.RequireRole("Driver"))
            .DisableAntiforgery()
            .WithOpenApi();
    }

    public async Task<IResult> UploadDocument([FromServices] ISender sender, [FromForm] UploadDocumentRequest request, HttpContext ctx)
    {
        var command = new UploadDocumentCommand(ctx.GetDriverId(), Enum.Parse<DocumentType>(request.DocumentType), request.File);
        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.NoContent();
    }
}
