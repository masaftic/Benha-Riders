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

public record ApproveDriverCommand(DriverId DriverId) : IRequest<ErrorOr<Success>>;

public class ApproveDriverCommandValidator : AbstractValidator<ApproveDriverCommand>
{
    public ApproveDriverCommandValidator()
    {
        RuleFor(x => x.DriverId.Value)
            .NotEmpty().WithMessage("Driver user ID is required.");
    }
}

public class ApproveDriverCommandHandler : IRequestHandler<ApproveDriverCommand, ErrorOr<Success>>
{
    private readonly AppDbContext _db;

    public ApproveDriverCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<Success>> Handle(ApproveDriverCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId.ToUserId();
        
        var driverProfile = await _db.DriverProfiles
            .Include(dp => dp.Documents)
            .FirstOrDefaultAsync(dp => dp.UserId == userId, cancellationToken);

        if (driverProfile == null)
        {
            return AdminErrors.DriverNotFound;
        }

        // Admin approves the driver profile
        var adminUserId = UserId.From(1); // TODO: Get from current user context
        var approveResult = driverProfile.Approve(adminUserId);
        if (approveResult.IsError)
        {
            return approveResult.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
