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

public record RejectDriverCommand(DriverId DriverId, string Reason) : IRequest<ErrorOr<Success>>;

public class RejectDriverCommandValidator : AbstractValidator<RejectDriverCommand>
{
    public RejectDriverCommandValidator()
    {
        RuleFor(x => x.DriverId.Value)
            .NotEmpty().WithMessage("Driver user ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("سبب الرفض مطلوب.")
            .MaximumLength(1000).WithMessage("سبب الرفض يجب ألا يتجاوز 1000 حرف.");
    }
}


public class RejectDriverCommandHandler(AppDbContext db) : IRequestHandler<RejectDriverCommand, ErrorOr<Success>>
{
    private readonly AppDbContext _db = db;

    public async Task<ErrorOr<Success>> Handle(RejectDriverCommand request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.Id == request.DriverId, cancellationToken);

        if (driver == null)
        {
            return AdminErrors.DriverNotFound;
        }

        var rejectResult = driver.RejectOnboarding(request.Reason);
        if (rejectResult.IsError)
        {
            return rejectResult.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return Result.Success;
    }
}
