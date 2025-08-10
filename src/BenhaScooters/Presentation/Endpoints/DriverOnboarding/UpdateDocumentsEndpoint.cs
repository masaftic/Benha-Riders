using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using FluentValidation;
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

    public class UpdateDocumentsRequestValidator : AbstractValidator<UpdateDocumentsRequestDto>
    {
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png" };
        private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

        public UpdateDocumentsRequestValidator()
        {
            RuleFor(x => x.LicenseImage)
                .NotNull().WithMessage("صورة الرخصة مطلوبة.")
                .Must(BeAValidImageFile).WithMessage("صورة الرخصة يجب أن تكون ملف صورة صحيح (jpg, jpeg, png) أقل من 10 ميجابايت.");

            RuleFor(x => x.VehicleRegistrationImage)
                .NotNull().WithMessage("صورة تسجيل المركبة مطلوبة.")
                .Must(BeAValidImageFile).WithMessage("صورة تسجيل المركبة يجب أن تكون ملف صورة صحيح (jpg, jpeg, png) أقل من 10 ميجابايت.");

            RuleFor(x => x.DriverImage)
                .NotNull().WithMessage("صورة السائق مطلوبة.")
                .Must(BeAValidImageFile).WithMessage("صورة السائق يجب أن تكون ملف صورة صحيح (jpg, jpeg, png) أقل من 10 ميجابايت.");
        }

        private static bool BeAValidImageFile(IFormFile? file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > MaxFileSize)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return AllowedExtensions.Contains(extension);
        }
    }

    public record UpdateDocumentsResponseDto(string Message, string NextStep);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/drivers/me/onboarding/documents", UpdateDocuments)
            .AddEndpointFilter<ValidationFilter<UpdateDocumentsRequestDto>>()
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
}
