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

public record UnbanDriverCommand(UserId DriverId, UserId AdminId) : IRequest<ErrorOr<Success>>;

public class UnbanDriverCommandValidator : AbstractValidator<UnbanDriverCommand>
{
    public UnbanDriverCommandValidator()
    {
        RuleFor(x => x.DriverId.Value)
            .NotEmpty().WithMessage("Driver user ID is required.");

        RuleFor(x => x.AdminId.Value)
            .NotEmpty().WithMessage("Admin user ID is required.");
    }
}

public class UnbanDriverCommandHandler : IRequestHandler<UnbanDriverCommand, ErrorOr<Success>>
{
    private readonly AppDbContext _db;

    public UnbanDriverCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<Success>> Handle(UnbanDriverCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId;
        
        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == userId, cancellationToken);

        if (driverProfile == null)
        {
            return AdminErrors.DriverNotFound;
        }

        var unsuspendResult = driverProfile.Unsuspend(request.AdminId);
        if (unsuspendResult.IsError)
        {
            return unsuspendResult.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
