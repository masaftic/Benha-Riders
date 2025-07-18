using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding;

public class UpdateDocumentsEndpoint : IEndpoint
{
    public record UpdateDocumentsRequestDto(
        IFormFile LicenseImage,
        IFormFile VehicleRegistrationImage,
        IFormFile DriverImage);
        
    public record UpdateDocumentsResponseDto(string Message, string NextStep);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/driver/onboarding/documents", UpdateDocuments)
            .WithName("UpdateDriverDocuments")
            .WithTags("Driver Onboarding")
            .WithSummary("Update driver documents")
            .WithDescription("Uploads driver documents including license image, vehicle registration image, and driver photo. Files are stored securely in S3 storage. Accepted formats: JPG, JPEG, PNG (max 10MB each).")
            .Produces<UpdateDocumentsResponseDto>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .Produces(500)
            .Accepts<UpdateDocumentsRequestDto>("multipart/form-data")
            .RequireAuthorization(policy => policy.RequireRole("Driver"))
            .DisableAntiforgery() // Required for file uploads
            .WithOpenApi();
    }

    public async Task<IResult> UpdateDocuments([FromServices] ISender sender, [FromForm] UpdateDocumentsRequestDto request, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();

        var command = new UpdateDocumentsCommand(
            driverId,
            request.LicenseImage,
            request.VehicleRegistrationImage,
            request.DriverImage);

        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        var mapper = new UpdateDocumentsEndpointMapper();
        var response = mapper.MapToResponse(result.Value);
        return Results.Ok(response);
    }
}

[Mapper]
public partial class UpdateDocumentsEndpointMapper
{
    public partial UpdateDocumentsEndpoint.UpdateDocumentsResponseDto MapToResponse(UpdateDocumentsResponse response);

    private static string OnboardingStepToString(OnboardingStep step) => step.ToString();
}
