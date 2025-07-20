using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Shared.Validation;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record UpdatePersonalInfoCommand(
    DriverId DriverId,
    string FullName,
    string NationalId,
    DateOnly DateOfBirth,
    string Address,
    string City,
    string EmergencyContactName,
    string EmergencyContactPhone) : IRequest<ErrorOr<UpdatePersonalInfoResponse>>;

public class UpdatePersonalInfoCommandValidator : AbstractValidator<UpdatePersonalInfoCommand>
{
    public UpdatePersonalInfoCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("الاسم الكامل مطلوب.")
            .MaximumLength(100).WithMessage("الاسم الكامل يجب ألا يتجاوز 100 حرف.");

        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("رقم الهوية الوطنية مطلوب.")
            .Matches(ValidationRegex.NationalId).WithMessage("رقم الهوية الوطنية يجب أن يكون 14 رقم.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("تاريخ الميلاد مطلوب.")
            .LessThan(DateOnly.FromDateTime(DateTime.Now.AddYears(-18))).WithMessage("يجب أن يكون عمر السائق 18 سنة على الأقل.")
            .GreaterThan(DateOnly.FromDateTime(DateTime.Now.AddYears(-100))).WithMessage("تاريخ ميلاد غير صحيح.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("العنوان مطلوب.")
            .MaximumLength(500).WithMessage("العنوان يجب ألا يتجاوز 500 حرف.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("المدينة مطلوبة.")
            .MaximumLength(100).WithMessage("المدينة يجب ألا تتجاوز 100 حرف.");

        RuleFor(x => x.EmergencyContactName)
            .NotEmpty().WithMessage("اسم جهة الاتصال الطارئ مطلوب.")
            .MaximumLength(100).WithMessage("اسم جهة الاتصال الطارئ يجب ألا يتجاوز 100 حرف.");

        RuleFor(x => x.EmergencyContactPhone)
            .NotEmpty().WithMessage("رقم هاتف جهة الاتصال الطارئ مطلوب.")
            .Matches(ValidationRegex.PhoneNumber).WithMessage("تنسيق رقم الهاتف غير صحيح.");
    }
}

public record UpdatePersonalInfoResponse(string Message, OnboardingStep NextStep);

public class UpdatePersonalInfoCommandHandler : IRequestHandler<UpdatePersonalInfoCommand, ErrorOr<UpdatePersonalInfoResponse>>
{
    private readonly AppDbContext _db;

    public UpdatePersonalInfoCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<UpdatePersonalInfoResponse>> Handle(UpdatePersonalInfoCommand request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.Id == request.DriverId, cancellationToken);

        if (driver == null)
        {
            return DriverErrors.DriverNotFound;
        }

        if (await _db.Drivers.AnyAsync(x => x.PersonalInfo!.NationalId == NationalId.From(request.NationalId) && x.Id != driver.Id, cancellationToken: cancellationToken))
        {
            return DriverErrors.DuplicateNationalId;
        }

        var updateResult = driver.UpdatePersonalInfo(
            request.FullName,
            NationalId.From(request.NationalId),
            request.DateOfBirth,
            request.Address,
            request.City,
            request.EmergencyContactName,
            PhoneNumber.From(request.EmergencyContactPhone));

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdatePersonalInfoResponse(
            "Personal information updated successfully.",
            driver.CurrentStep);
    }
}
