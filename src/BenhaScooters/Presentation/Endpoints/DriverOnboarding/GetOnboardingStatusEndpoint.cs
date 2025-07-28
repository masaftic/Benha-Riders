using BenhaScooters.Application.Features.DriverOnboarding.Queries;
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

public class GetOnboardingStatusEndpoint : IEndpoint
{
    public record GetOnboardingStatusResponseDto(
        string Status,
        string CurrentStep,
        int Progress,
        string? RejectionReason,
        DateTime CreatedAt,
        DateTime? CompletedAt);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/driver/onboarding/status", GetOnboardingStatus)
            .WithName("GetDriverOnboardingStatus")
            .WithTags("Driver Onboarding")
            .WithSummary("Get driver onboarding status")
            .WithDescription("Retrieves the current onboarding status of the driver including progress and any rejection reasons. Creates a new driver profile if none exists.")
            .Produces<GetOnboardingStatusResponseDto>()
            .Produces(401)
            .Produces(403)
            .RequireAuthorization(policy => policy.RequireRole("Driver"))
            .WithOpenApi();
    }

    public async Task<IResult> GetOnboardingStatus([FromServices] ISender sender, HttpContext ctx)
    {
        var userId = ctx.GetCurrentUserId();
        var query = new GetOnboardingStatusQuery(userId);
        var result = await sender.Send(query);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        var mapper = new GetOnboardingStatusEndpointMapper();
        var response = mapper.MapToResponse(result.Value);
        return Results.Ok(response);
    }
}

[Mapper]
public partial class GetOnboardingStatusEndpointMapper
{
    public partial GetOnboardingStatusEndpoint.GetOnboardingStatusResponseDto MapToResponse(GetOnboardingStatusResponse response);

    private static string OnboardingStatusToString(BenhaScooters.Domain.Drivers.ValueObjects.OnboardingStatus status) => status.ToString();
    private static string OnboardingStepToString(BenhaScooters.Domain.Drivers.ValueObjects.OnboardingStep step) => step.ToString();
}
