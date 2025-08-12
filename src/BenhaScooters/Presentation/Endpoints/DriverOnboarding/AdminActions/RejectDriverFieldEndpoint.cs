using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain.Drivers;
using FluentValidation;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class RejectDriverFieldEndpoint : IEndpoint
{
    public record RejectFieldRequest(string Step, string FieldName, string Reason);

    public class Validator : AbstractValidator<RejectFieldRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Step)
                .NotEmpty().WithMessage("Step is required.");
            RuleFor(x => x.FieldName)
                .NotEmpty().WithMessage("Field name is required.");
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Rejection reason is required.")
                .MaximumLength(500).WithMessage("Rejection reason must not exceed 500 characters.");
        }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/drivers/{driverId}/fields/reject", RejectField)
            .AddEndpointFilter<ValidationFilter<RejectFieldRequest>>()
            .WithName("RejectDriverField")
            .WithTags("Admin - Driver Management")
            .WithSummary("Reject a specific driver field")
            .WithDescription("Rejects a specific field in the driver's onboarding process with a reason. The rejection reason will be visible to the driver so they can address the issue.")
            .Produces(204)
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> RejectField([FromServices] ISender sender, [FromRoute] int driverId, [FromBody] RejectFieldRequest request, HttpContext ctx)
    {
        var command = new RejectDriverFieldCommand(DriverId.From(driverId), request.Step, request.FieldName, request.Reason);
        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.NoContent();
    }
}
