using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using FluentValidation;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding;

public class PatchDriverFieldsEndpoint : IEndpoint
{
    public record PatchFieldsRequest(List<FieldPatchDto> Fields);
    public record FieldPatchDto(string Step, string FieldName, string Value);

    public class Validator : AbstractValidator<PatchFieldsRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Fields)
                .NotEmpty().WithMessage("At least one field is required.");
            
            RuleForEach(x => x.Fields)
                .ChildRules(field =>
                {
                    field.RuleFor(f => f.Step)
                        .NotEmpty().WithMessage("Step is required.");
                    field.RuleFor(f => f.FieldName)
                        .NotEmpty().WithMessage("Field name is required.");
                    field.RuleFor(f => f.Value)
                        .NotEmpty().WithMessage("Field value is required.");
                });
        }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPatch("/drivers/me/onboarding/fields", PatchFields)
            .AddEndpointFilter<ValidationFilter<PatchFieldsRequest>>()
            .WithName("PatchDriverFields")
            .WithTags("Driver Onboarding")
            .WithSummary("Update specific driver fields")
            .WithDescription("Updates specific fields in the driver's onboarding information. Only available when the driver's application has been rejected and requires corrections. If all required fields are corrected, the driver will automatically transition to review status.")
            .Produces(204)
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .RequireAuthorization(policy => policy.RequireRole("Driver"))
            .WithOpenApi();
    }

    public async Task<IResult> PatchFields([FromServices] ISender sender, [FromBody] PatchFieldsRequest request, HttpContext ctx)
    {
        var fields = request.Fields.Select(f => new FieldDto(f.Step, f.FieldName, f.Value)).ToList();
        var command = new PatchDriverFieldsCommand(ctx.GetDriverId(), fields);
        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.NoContent();
    }
}
