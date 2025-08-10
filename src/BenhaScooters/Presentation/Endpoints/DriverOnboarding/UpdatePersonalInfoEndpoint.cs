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
                .WithMessage("الاسم الكامل مطلوب.")
                .MinimumLength(2)
                .WithMessage("الاسم الكامل يجب أن يكون على الأقل حرفين.")
                .MaximumLength(100)
                .WithMessage("الاسم الكامل لا يمكن أن يتجاوز 100 حرف.");

            RuleFor(x => x.NationalId)
                .NotEmpty()
                .WithMessage("الرقم القومي مطلوب.")
                .Matches(@"^[0-9]{14}$")
                .WithMessage("الرقم القومي يجب أن يكون 14 رقم بالضبط.");

            RuleFor(x => x.DateOfBirth)
                .NotEmpty()
                .WithMessage("تاريخ الميلاد مطلوب.")
                .Must(BeValidAge)
                .WithMessage("يجب أن يكون عمر السائق بين 18 و 80 سنة.");

            RuleFor(x => x.Address)
                .NotEmpty()
                .WithMessage("العنوان مطلوب.")
                .MinimumLength(10)
                .WithMessage("العنوان يجب أن يكون على الأقل 10 أحرف.")
                .MaximumLength(200)
                .WithMessage("العنوان لا يمكن أن يتجاوز 200 حرف.");

            RuleFor(x => x.City)
                .NotEmpty()
                .WithMessage("المدينة مطلوبة.")
                .MinimumLength(2)
                .WithMessage("المدينة يجب أن تكون على الأقل حرفين.")
                .MaximumLength(50)
                .WithMessage("المدينة لا يمكن أن تتجاوز 50 حرف.");

            RuleFor(x => x.EmergencyContactName)
                .NotEmpty()
                .WithMessage("اسم جهة الاتصال في حالة الطوارئ مطلوب.")
                .MinimumLength(2)
                .WithMessage("اسم جهة الاتصال في حالة الطوارئ يجب أن يكون على الأقل حرفين.")
                .MaximumLength(100)
                .WithMessage("اسم جهة الاتصال في حالة الطوارئ لا يمكن أن يتجاوز 100 حرف.");

            RuleFor(x => x.EmergencyContactPhone)
                .NotEmpty()
                .WithMessage("رقم هاتف جهة الاتصال في حالة الطوارئ مطلوب.")
                .Matches(@"^(\+201|01)[0-9]{9}$")
                .WithMessage("رقم هاتف جهة الاتصال في حالة الطوارئ يجب أن يكون رقم هاتف مصري صحيح.");
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
}
