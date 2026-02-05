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

public record RejectDriverCommand(UserId DriverId, string RejectionReason, UserId AdminId) : IRequest<ErrorOr<Success>>;

public class RejectDriverCommandValidator : AbstractValidator<RejectDriverCommand>
{
    public RejectDriverCommandValidator()
    {
        RuleFor(x => x.DriverId.Value)
            .NotEmpty().WithMessage("Driver user ID is required.");

        RuleFor(x => x.RejectionReason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MaximumLength(1000).WithMessage("Rejection reason cannot exceed 1000 characters.");

        RuleFor(x => x.AdminId.Value)
            .NotEmpty().WithMessage("Admin user ID is required.");
    }
}

public class RejectDriverCommandHandler : IRequestHandler<RejectDriverCommand, ErrorOr<Success>>
{
    private readonly AppDbContext _db;

    public RejectDriverCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<Success>> Handle(RejectDriverCommand request, CancellationToken cancellationToken)
    {
        var userId = request.DriverId;
        
        var driverProfile = await _db.DriverProfiles
            .FirstOrDefaultAsync(dp => dp.UserId == userId, cancellationToken);

        if (driverProfile == null)
        {
            return AdminErrors.DriverNotFound;
        }

        var rejectResult = driverProfile.Reject(request.RejectionReason, request.AdminId);
        if (rejectResult.IsError)
        {
            return rejectResult.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
