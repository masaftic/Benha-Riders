using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Drivers.Commands;

public record ReviewTopUpRequestCommand(
    WalletTopUpRequestId RequestId,
    UserId AdminId,
    bool Approve,
    string? Note) : IRequest<ErrorOr<Success>>;

public class ReviewTopUpRequestCommandValidator : AbstractValidator<ReviewTopUpRequestCommand>
{
    public ReviewTopUpRequestCommandValidator()
    {
        RuleFor(x => x.Note)
            .NotEmpty().WithMessage("يجب تقديم سبب الرفض.")
            .When(x => !x.Approve);
    }
}

public class ReviewTopUpRequestCommandHandler
    : IRequestHandler<ReviewTopUpRequestCommand, ErrorOr<Success>>
{
    private readonly AppDbContext _db;

    public ReviewTopUpRequestCommandHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<Success>> Handle(ReviewTopUpRequestCommand request, CancellationToken cancellationToken)
    {
        var topUp = await _db.WalletTopUpRequests
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (topUp is null)
            return WalletErrors.TopUpRequestNotFound;

        if (request.Approve)
        {
            var approveResult = topUp.Approve(request.AdminId, request.Note);
            if (approveResult.IsError)
                return approveResult.Errors;

            // Credit the driver's wallet
            var wallet = await _db.DriverWallets
                .FirstOrDefaultAsync(w => w.Id == topUp.WalletId, cancellationToken);

            if (wallet is null)
                return WalletErrors.NotFound;

            var settlementResult = wallet.RecordSettlement(topUp.Amount, $"TopUp #{topUp.Id}");
            if (settlementResult.IsError)
                return settlementResult.Errors;
        }
        else
        {
            var rejectResult = topUp.Reject(request.AdminId, request.Note ?? string.Empty);
            if (rejectResult.IsError)
                return rejectResult.Errors;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Result.Success;
    }
}
