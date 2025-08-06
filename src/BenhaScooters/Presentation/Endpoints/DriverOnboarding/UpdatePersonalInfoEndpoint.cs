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

public class UpdatePersonalInfoEndpoint : IEndpoint
{
    public record UpdatePersonalInfoRequestDto(
        string FullName,
        string NationalId,
        DateOnly DateOfBirth,
        string Address,
        string City,
        string EmergencyContactName,
        string EmergencyContactPhone);

    public class UpdatePersonalInfoRequestValidator : AbstractValidator<UpdatePersonalInfoRequestDto>
    {
        public UpdatePersonalInfoRequestValidator()
        {
            RuleFor(x => x.FullName)
                .NotEmpty()
                .WithMessage("Full name is required.")
                .MinimumLength(2)
                .WithMessage("Full name must be at least 2 characters long.")
                .MaximumLength(100)
                .WithMessage("Full name cannot exceed 100 characters.");

            RuleFor(x => x.NationalId)
                .NotEmpty()
                .WithMessage("National ID is required.")
                .Matches(@"^[0-9]{14}$")
                .WithMessage("National ID must be exactly 14 digits.");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty()
                .WithMessage("Date of birth is required.")
                .Must(BeValidAge)
                .WithMessage("Driver must be between 18 and 80 years old.");

            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage("Address is required.")
                .MinimumLength(10)
                .WithMessage("Address must be at least 10 characters long.")
                .MaximumLength(200)
                .WithMessage("Address cannot exceed 200 characters.");

            RuleFor(x => x.City)
                .NotEmpty()
                .WithMessage("City is required.")
                .MinimumLength(2)
                .WithMessage("City must be at least 2 characters long.")
                .MaximumLength(50)
                .WithMessage("City cannot exceed 50 characters.");

            RuleFor(x => x.EmergencyContactName)
                .NotEmpty()
                .WithMessage("Emergency contact name is required.")
                .MinimumLength(2)
                .WithMessage("Emergency contact name must be at least 2 characters long.")
                .MaximumLength(100)
                .WithMessage("Emergency contact name cannot exceed 100 characters.");

            RuleFor(x => x.EmergencyContactPhone)
                .NotEmpty()
                .WithMessage("Emergency contact phone is required.")
                .Matches(@"^(\+201|01)[0-9]{9}$")
                .WithMessage("Emergency contact phone must be a valid Egyptian phone number.");
        }

        private static bool BeValidAge(DateOnly dateOfBirth)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var age = today.Year - dateOfBirth.Year;
            if (dateOfBirth > today.AddYears(-age)) age--;
            return age >= 18 && age <= 80;
        }
    }

    public record UpdatePersonalInfoResponseDto(string Message, string NextStep);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/drivers/me/onboarding/personal-info", UpdatePersonalInfo)
            .AddEndpointFilter<ValidationFilter<UpdatePersonalInfoRequestDto>>()
            .WithName("UpdateDriverPersonalInfo")
            .WithTags("Driver Onboarding")
            .WithSummary("Update driver personal information")
            .WithDescription("Updates the driver's personal information including full name, date of birth, national ID, and address during the onboarding process. National ID must be unique across all drivers.")
            .Produces<UpdatePersonalInfoResponseDto>()
            .ProducesValidationProblem()
            .Produces(400)
            .Produces(401)
            .Produces(403)
            .Produces(404)
            .RequireAuthorization(policy => policy.RequireRole("Driver"))
            .WithOpenApi();
    }

    public async Task<IResult> UpdatePersonalInfo([FromServices] ISender sender, [FromBody] UpdatePersonalInfoRequestDto personalInfoRequest, HttpContext ctx)
    {
        var driverId = ctx.GetDriverId();
        var mapper = new UpdatePersonalInfoEndpointMapper();
        var command = mapper.MapToCommand(personalInfoRequest, driverId);
        var result = await sender.Send(command);

        if (result.IsError)
        {
            return ApiProblem.HandleProblems(result.Errors, ctx);
        }

        var response = mapper.MapToResponse(result.Value);
        return Results.Ok(response);
    }
}

[Mapper]
public partial class UpdatePersonalInfoEndpointMapper
{
    public UpdatePersonalInfoCommand MapToCommand(UpdatePersonalInfoEndpoint.UpdatePersonalInfoRequestDto request, DriverId driverId)
    {
        return new UpdatePersonalInfoCommand(
            driverId,
            request.FullName,
            request.NationalId,
            request.DateOfBirth,
            request.Address,
            request.City,
            request.EmergencyContactName,
            request.EmergencyContactPhone);
    }

    public partial UpdatePersonalInfoEndpoint.UpdatePersonalInfoResponseDto MapToResponse(UpdatePersonalInfoResponse response);

    private static string OnboardingStepToString(BenhaScooters.Domain.Drivers.ValueObjects.OnboardingStep step) => step.ToString();
}
