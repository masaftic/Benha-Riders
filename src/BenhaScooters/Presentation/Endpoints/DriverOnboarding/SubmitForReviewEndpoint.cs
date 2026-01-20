using BenhaScooters.Application.Features.DriverOnboarding.Commands;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers;
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

public class SubmitForReviewEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/drivers/me/onboarding/submit-for-review", SubmitForReview)
            .WithName("SubmitForReview")
            .WithTags("Driver Onboarding")
            .WithSummary("Sends driver application to admins to review, driver can freely edit his onboarding info before submission or if rejected")
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .RequireAuthorization(policy => policy.RequireRole("Driver"))
            .WithOpenApi();
    }

    private async Task<IResult> SubmitForReview([FromServices] ISender sender, HttpContext context)
    {
        var command = new SubmitForReviewCommand(context.GetDriverId());
        var result = await sender.Send(command);

        if (result.IsError)
            return ApiProblem.HandleProblems(result.Errors, context);

        return Results.Ok();
    }
}
