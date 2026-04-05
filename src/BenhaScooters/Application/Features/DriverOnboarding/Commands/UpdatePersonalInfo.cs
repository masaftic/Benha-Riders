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
    UserId DriverId,
    string FullName,
    string NationalId) : IRequest<ErrorOr<UpdatePersonalInfoResponse>>;


public record UpdatePersonalInfoResponse(string Message, DriverOnboardingStatus Status);

public class UpdatePersonalInfoCommandHandler(AppDbContext db) : IRequestHandler<UpdatePersonalInfoCommand, ErrorOr<UpdatePersonalInfoResponse>>
{
    private readonly AppDbContext _db = db;

    public async Task<ErrorOr<UpdatePersonalInfoResponse>> Handle(UpdatePersonalInfoCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId;
        
        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == userId, cancellationToken);

        if (driverProfile == null)
        {
            return AppErrors.Driver.NotFound();
        }

        if (await _db.DriverProfiles
            .AnyAsync(x => x.PersonalInfo != null && x.PersonalInfo.NationalId == NationalId.Create(request.NationalId) && x.UserId != userId, 
                cancellationToken))
        {
            return AppErrors.Driver.Profile.NationalIdAlreadyExists();
        }

        var personalInfo = new DriverPersonalInfo(
            request.FullName,
            NationalId.Create(request.NationalId));

        var updateResult = driverProfile.UpdatePersonalInfo(personalInfo);

        if (updateResult.IsError)
        {
            return updateResult.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new UpdatePersonalInfoResponse(
            "Personal information updated successfully.",
            driverProfile.OnboardingStatus);
    }
}
