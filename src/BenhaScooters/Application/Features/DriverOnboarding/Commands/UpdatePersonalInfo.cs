using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers.Enums;
using BenhaScooters.Domain.Drivers.ValueObjects;
using BenhaScooters.Shared.Validation;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record UpdatePersonalInfoCommand(
    UserId UserId,
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
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name must not exceed 100 characters.");

        RuleFor(x => x.NationalId)
            .NotEmpty().WithMessage("National ID is required.")
            .Matches(ValidationRegex.NationalId).WithMessage("National ID must be 14 digits.");

        RuleFor(x => x.DateOfBirth)
            .NotEmpty().WithMessage("Date of birth is required.")
            .LessThan(DateOnly.FromDateTime(DateTime.Now.AddYears(-18))).WithMessage("Driver must be at least 18 years old.")
            .GreaterThan(DateOnly.FromDateTime(DateTime.Now.AddYears(-100))).WithMessage("Invalid date of birth.");

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("Address is required.")
            .MaximumLength(500).WithMessage("Address must not exceed 500 characters.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("City is required.")
            .MaximumLength(100).WithMessage("City must not exceed 100 characters.");

        RuleFor(x => x.EmergencyContactName)
            .NotEmpty().WithMessage("Emergency contact name is required.")
            .MaximumLength(100).WithMessage("Emergency contact name must not exceed 100 characters.");

        RuleFor(x => x.EmergencyContactPhone)
            .NotEmpty().WithMessage("Emergency contact phone is required.")
            .Matches(ValidationRegex.PhoneNumber).WithMessage("Invalid phone number format.");
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
            .FirstOrDefaultAsync(dp => dp.UserId == request.UserId, cancellationToken);

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
