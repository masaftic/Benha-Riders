using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Domain.Users;
using BenhaScooters.Domain.Drivers;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record BanDriverCommand(DriverId DriverId, string Reason) : IRequest<ErrorOr<Success>>;

public class BanDriverCommandValidator : AbstractValidator<BanDriverCommand>
{
    public BanDriverCommandValidator()
    {
        RuleFor(x => x.DriverId.Value)
            .NotEmpty().WithMessage("Driver user ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("سبب الرفض مطلوب.")
            .MaximumLength(1000).WithMessage("سبب الرفض يجب ألا يتجاوز 1000 حرف.");
    }
}


public class RejectDriverCommandHandler(AppDbContext db) : IRequestHandler<BanDriverCommand, ErrorOr<Success>>
{
    private readonly AppDbContext _db = db;

    public async Task<ErrorOr<Success>> Handle(BanDriverCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId.ToUserId();
        
        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == userId, cancellationToken);

        if (driverProfile == null)
        {
            return AdminErrors.DriverNotFound;
        }

        var rejectResult = driverProfile.Suspend(request.Reason);
        if (rejectResult.IsError)
        {
            return rejectResult.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
