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
    private readonly AppDbContext _db;

    public UpdatePersonalInfoCommandValidator(AppDbContext db)
    {
        _db = db;
        
        // Only business logic validations here, not basic input validation
        RuleFor(x => x.NationalId)
            .MustAsync(BeUniqueNationalId)
            .WithMessage("هذا الرقم القومي مسجل مع سائق آخر.")
            .When(x => !string.IsNullOrEmpty(x.NationalId));
    }

    private async Task<bool> BeUniqueNationalId(UpdatePersonalInfoCommand command, string nationalId, CancellationToken cancellationToken)
    {
        return !await _db.Drivers
            .AnyAsync(x => x.Info!.NationalId == NationalId.From(nationalId) && x.Id != command.DriverId, 
                cancellationToken);
    }
}

public record UpdatePersonalInfoResponse(string Message, BenhaScooters.Domain.Drivers.ValueObjects.OnboardingStep NextStep);

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

        // Check for duplicate national ID across all drivers
        if (await _db.Drivers.AnyAsync(x => x.Info!.NationalId == NationalId.From(request.NationalId) && x.Id != driver.Id, cancellationToken: cancellationToken))
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
            driver.OnboardingState.CurrentStep);
    }
}
