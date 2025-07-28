using BenhaScooters.Application.Features.DriverOnboarding.Queries;
using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding;

public class GetOnboardingDetailsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/driver/onboarding/details", GetOnboardingDetails)
            .WithName("GetDriverOnboardingDetails")
            .WithTags("Driver Onboarding")
            .WithSummary("Get driver onboarding details")
            .WithDescription("Retrieves comprehensive onboarding details including personal information, vehicle information, documents, and current status for the authenticated driver.")
            .Produces<GetOnboardingDetailsResponse>()
            .ProducesValidationProblem()
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Driver"))
            .WithOpenApi();
    }

    public async Task<IResult> GetOnboardingDetails([FromServices] ISender sender, HttpContext ctx)
    {
        var userId = ctx.GetCurrentUserId();
        var query = new GetOnboardingDetailsQuery(userId);
        var result = await sender.Send(query);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.Ok(result.Value);
    }
}
