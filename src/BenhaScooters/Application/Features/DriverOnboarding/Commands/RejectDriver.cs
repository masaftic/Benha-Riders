using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record RejectDriverCommand(UserId DriverUserId, string Reason) : IRequest<ErrorOr<RejectDriverResponse>>;

public class RejectDriverCommandValidator : AbstractValidator<RejectDriverCommand>
{
    public RejectDriverCommandValidator()
    {
        RuleFor(x => x.DriverUserId)
            .NotEmpty().WithMessage("Driver user ID is required.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Rejection reason is required.")
            .MaximumLength(1000).WithMessage("Rejection reason must not exceed 1000 characters.");
    }
}

public record RejectDriverResponse(string Message);

public class RejectDriverCommandHandler : IRequestHandler<RejectDriverCommand, ErrorOr<RejectDriverResponse>>
{
    private readonly AppDbContext _db;

    public RejectDriverCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<RejectDriverResponse>> Handle(RejectDriverCommand request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.UserId == request.DriverUserId, cancellationToken);

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

        return new RejectDriverResponse("Driver application rejected successfully.");
    }
}
