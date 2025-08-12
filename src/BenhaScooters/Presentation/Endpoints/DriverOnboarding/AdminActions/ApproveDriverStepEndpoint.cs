using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain.Drivers;
using FluentValidation;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class ApproveDriverStepEndpoint : IEndpoint
{
    public record ApproveStepRequest(string Step);

    public class Validator : AbstractValidator<ApproveStepRequest>
    {
        public Validator()
        {
            RuleFor(x => x.Step)
                .NotEmpty().WithMessage("Step is required.");
        }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/drivers/{driverId}/steps/approve", ApproveStep)
            .AddEndpointFilter<ValidationFilter<ApproveStepRequest>>()
            .WithName("ApproveDriverStep")
            .WithTags("Admin - Driver Management")
            .WithSummary("Approve all fields in a driver step")
            .WithDescription("Approves all pending fields in a specific step of the driver's onboarding process. Only pending fields will be approved, already reviewed fields remain unchanged.")
            .Produces(204)
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> ApproveStep([FromServices] ISender sender, [FromRoute] int driverId, [FromBody] ApproveStepRequest request, HttpContext ctx)
    {
        var command = new ApproveDriverStepCommand(DriverId.From(driverId), request.Step);
        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.NoContent();
    }
}
