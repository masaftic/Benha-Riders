using BenhaScooters.Data;
using BenhaScooters.Domain;
using BenhaScooters.Domain.Common;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.DriverOnboarding.Commands;

public record ApproveDriverCommand(UserId DriverUserId) : IRequest<ErrorOr<ApproveDriverResponse>>;

public class ApproveDriverCommandValidator : AbstractValidator<ApproveDriverCommand>
{
    public ApproveDriverCommandValidator()
    {
        RuleFor(x => x.DriverUserId)
            .NotEmpty().WithMessage("Driver user ID is required.");
    }
}

public record ApproveDriverResponse(string Message);

public class ApproveDriverCommandHandler : IRequestHandler<ApproveDriverCommand, ErrorOr<ApproveDriverResponse>>
{
    private readonly AppDbContext _db;

    public ApproveDriverCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<ApproveDriverResponse>> Handle(ApproveDriverCommand request, CancellationToken cancellationToken)
    {
        var driver = await _db.Drivers
            .FirstOrDefaultAsync(dp => dp.UserId == request.DriverUserId, cancellationToken);

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

        return new ApproveDriverResponse("Driver application approved successfully.");
    }
}
