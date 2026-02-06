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

public class BanDriverEndpoint : IEndpoint
{
    public record BanDriverRequestDto(string Reason);
    public class Validator : AbstractValidator<BanDriverRequestDto>
    {
        public Validator()
        {
            RuleFor(x => x.Reason)
                .NotEmpty().WithMessage("Ban reason is required.")
                .MaximumLength(500).WithMessage("Ban reason must not exceed 500 characters.");
        }
    }

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/admin/drivers/{driverId}/ban", BanDriver)
            .AddEndpointFilter<ValidationFilter<BanDriverRequestDto>>()
            .WithName("BanDriverApplication")
            .WithTags("Admin - Driver Management")
            .WithSummary("Ban driver onboarding application")
            .WithDescription("Ban a driver's onboarding application with a specific reason.")
            .Produces<NoContent>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Admin"))
            .WithOpenApi();
    }

    public async Task<IResult> BanDriver([FromServices] ISender sender, [FromRoute] int driverId, [FromBody] BanDriverRequestDto rejectRequest, HttpContext ctx)
    {
        var command = new BanDriverCommand(UserId.Create(driverId), rejectRequest.Reason); 
        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        return Results.NoContent();
    }
}

