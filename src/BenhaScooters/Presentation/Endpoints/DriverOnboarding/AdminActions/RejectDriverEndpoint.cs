using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Presentation.Endpoints;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;
using BenhaScooters.Domain.Users;
using BenhaScooters.Domain.Drivers;
using FluentValidation;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class RejectDriverEndpoint : IEndpoint
{
    public record RejectDriverRequestDto(string Reason);
    public class Validator : AbstractValidator<RejectDriverRequestDto>
    {
        public Validator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Rejection reason is required.")
                .MaximumLength(500).WithMessage("Rejection reason must not exceed 500 characters.");
        }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/drivers/{driverId}/reject", RejectDriver)
            .AddEndpointFilter<ValidationFilter<RejectDriverRequestDto>>()
            .WithName("RejectDriverApplication")
            .WithTags("Admin - Driver Management")
            .WithSummary("Reject driver onboarding application")
            .WithDescription("Rejects a driver's onboarding application with a specific reason. The driver can resubmit their application after addressing the rejection reason. Rejection reason is required and will be visible to the driver.")
            .Produces<NoContent>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> RejectDriver([FromServices] ISender sender, [FromRoute] int driverId, [FromBody] RejectDriverRequestDto rejectRequest, HttpContext ctx)
    {
        var command = new RejectDriverCommand(DriverId.From(driverId), rejectRequest.Reason); 
        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.NoContent();
    }
}

