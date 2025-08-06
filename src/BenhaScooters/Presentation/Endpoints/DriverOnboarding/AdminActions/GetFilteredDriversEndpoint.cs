using BenhaScooters.Application.Common;
using BenhaScooters.Application.Features.DriverOnboarding.Queries;
using BenhaScooters.Application.Features.DriverOnboarding.Queries.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Presentation.Endpoints;
using BenhaScooters.Presentation.Endpoints.DriverOnboarding.Common;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Riok.Mapperly.Abstractions;

namespace BenhaScooters.Presentation.Endpoints.DriverOnboarding.AdminActions;

public class GetFilteredDriversEndpoint : IEndpoint
{
    public record QueryParams(string OnboardingStatus, string OnboardingStep, int Page = 1, int PageCount = 10);

    public class Validator : AbstractValidator<QueryParams>
    {
        public Validator()
        {
            RuleFor(x => x.OnboardingStatus)
                .NotEmpty().WithMessage("Onboarding status is required.")
                .IsEnumName(typeof(OnboardingStatus), caseSensitive: false);

            RuleFor(x => x.OnboardingStep)
                .NotEmpty().WithMessage("Onboarding step is required.")
                .IsEnumName(typeof(OnboardingStep), caseSensitive: false);

            RuleFor(x => x.Page)
                .GreaterThan(0).WithMessage("Page must be greater than 0.");

            RuleFor(x => x.PageCount)
                .GreaterThan(0).WithMessage("Page count must be greater than 0.");
        }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/admin/drivers", GetPendingApplications)
            .WithName("GetFilteredDrivers")
            .WithTags("Admin - Driver Management")
            .WithSummary("Get filtered driver applications for review")
            .WithDescription("Retrieves all driver applications that match the specified filters. Includes complete application details with pre-signed document URLs valid for 15 minutes.")
            .Produces<PaginatedList<DriverSummaryDto>>()
            .Produces(401)
            .Produces(403)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> GetPendingApplications([FromServices] ISender sender, [AsParameters] QueryParams queryParams, HttpContext ctx)
    {
        var query = new ListDriversWithFilters(
            Enum.Parse<OnboardingStatus>(queryParams.OnboardingStatus),
            Enum.Parse<OnboardingStep>(queryParams.OnboardingStep),
            queryParams.Page,
            queryParams.PageCount
        );

        var result = await sender.Send(query);
        return Results.Ok(result);
    }
}
