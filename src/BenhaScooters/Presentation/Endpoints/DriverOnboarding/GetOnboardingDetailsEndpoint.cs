using BenhaScooters.Application.Features.DriverOnboarding.Queries;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Presentation.Endpoints.DriverOnboarding.Common;
using BenhaScooters.Shared.Security;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding;

public class GetOnboardingDetailsEndpoint : IEndpoint
{
    public record GetOnboardingDetailsResponseDto(
        string Status,
        string CurrentStep,
        int Progress,
        string? RejectionReason,
        DateTime CreatedAt,
        DateTime? CompletedAt,
        PersonalInfoDto? PersonalInfo,
        VehicleInfoDto? VehicleInfo,
        DocumentsDto? Documents);


    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/driver/onboarding/details", GetOnboardingDetails)
            .WithName("GetDriverOnboardingDetails")
            .WithTags("Driver Onboarding")
            .WithSummary("Get detailed driver onboarding information")
            .WithDescription("Retrieves comprehensive onboarding details including personal information, vehicle details, documents, and current progress status. Document URLs are pre-signed and valid for 10 minutes.")
            .Produces<GetOnboardingDetailsResponseDto>()
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

        var mapper = new GetOnboardingDetailsEndpointMapper();
        var response = mapper.MapToResponse(result.Value);
        return Results.Ok(response);
    }
}

[Mapper]
public partial class GetOnboardingDetailsEndpointMapper
{
    public partial GetOnboardingDetailsEndpoint.GetOnboardingDetailsResponseDto MapToResponse(GetOnboardingDetailsResponse response);

    private static string OnboardingStatusToString(OnboardingStatus status) => status.ToString();
    private static string OnboardingStepToString(OnboardingStep step) => step.ToString();
    private static string? VehicleTypeToString(VehicleType? vehicleType) => vehicleType?.ToString();
}
