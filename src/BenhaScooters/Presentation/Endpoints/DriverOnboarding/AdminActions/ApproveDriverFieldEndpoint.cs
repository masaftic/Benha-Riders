using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain.Drivers;
using FluentValidation;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class ApproveDriverFieldEndpoint : IEndpoint
{
    public record ApproveFieldRequest(string Step, string FieldName);

    public class Validator : AbstractValidator<ApproveFieldRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Step)
                .NotEmpty().WithMessage("Step is required.");
            RuleFor(x => x.FieldName)
                .NotEmpty().WithMessage("Field name is required.");
        }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/drivers/{driverId}/fields/approve", ApproveField)
            .AddEndpointFilter<ValidationFilter<ApproveFieldRequest>>()
            .WithName("ApproveDriverField")
            .WithTags("Admin - Driver Management")
            .WithSummary("Approve a specific driver field")
            .WithDescription("Approves a specific field in the driver's onboarding process. Fields that are already reviewed cannot be changed again.")
            .Produces(204)
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> ApproveField([FromServices] ISender sender, [FromRoute] int driverId, [FromBody] ApproveFieldRequest request, HttpContext ctx)
    {
        var command = new ApproveDriverFieldCommand(DriverId.From(driverId), request.Step, request.FieldName);
        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.NoContent();
    }
}
