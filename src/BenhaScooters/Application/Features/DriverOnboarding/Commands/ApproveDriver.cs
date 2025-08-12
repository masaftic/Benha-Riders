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
        var driver = await _db.Drivers
            .Include(dp => dp.Documents)
            .Include(dp => dp.Vehicle)
            .Include(dp => dp.Fields)
            .FirstOrDefaultAsync(dp => dp.Id == request.DriverId, cancellationToken);

        if (driver == null)
        {
            return AdminErrors.DriverNotFound;
        }

        var completeResult = driver.CompleteOnboarding();
        if (completeResult.IsError)
        {
            return completeResult.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
