using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using ErrorOr;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Drivers.Commands;

public record SubmitTopUpRequestCommand(
    UserId DriverId,
    decimal Amount,
    IFormFile Receipt) : IRequest<ErrorOr<SubmitTopUpRequestResponse>>;

public record SubmitTopUpRequestResponse(int RequestId, decimal Amount, string Status, DateTime CreatedAt);

public class SubmitTopUpRequestCommandValidator : AbstractValidator<SubmitTopUpRequestCommand>
{
    public SubmitTopUpRequestCommandValidator()
    {
        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("يجب أن يكون المبلغ أكبر من صفر.");

        RuleFor(x => x.Receipt)
            .NotNull().WithMessage("صورة الإيصال مطلوبة.");
    }
}

public class SubmitTopUpRequestCommandHandler
    : IRequestHandler<SubmitTopUpRequestCommand, ErrorOr<SubmitTopUpRequestResponse>>
{
    private readonly AppDbContext _db;
    private readonly Infrastructure.S3.IS3Service _s3;

    public SubmitTopUpRequestCommandHandler(AppDbContext db, Infrastructure.S3.IS3Service s3)
    {
        _db = db;
        _s3 = s3;
    }

    public async Task<ErrorOr<SubmitTopUpRequestResponse>> Handle(
        SubmitTopUpRequestCommand request,
        CancellationToken cancellationToken)
    {
        var wallet = await _db.DriverWallets
            .FirstOrDefaultAsync(w => w.DriverUserId == request.DriverId, cancellationToken);

        if (wallet is null)
            return AppErrors.Driver.Wallet.NotFound();

        var hasPending = await _db.WalletTopUpRequests
            .AnyAsync(r => r.DriverUserId == request.DriverId && r.Status == TopUpRequestStatus.Pending, cancellationToken);

        // Validate before uploading
        var validationResult = wallet.CreateTopUpRequest(request.Amount, string.Empty, hasPending);
        if (validationResult.IsError)
            return validationResult.Errors;

        // Upload receipt
        var uploadResult = await _s3.UploadFileAsync(request.Receipt, $"receipts/{request.DriverId}", cancellationToken: cancellationToken);
        if (uploadResult.IsError)
            return uploadResult.Errors;

        var topUp = new WalletTopUpRequest(wallet.Id, request.DriverId, request.Amount, uploadResult.Value);

        await _db.WalletTopUpRequests.AddAsync(topUp, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return new SubmitTopUpRequestResponse(
            topUp.Id,
            topUp.Amount,
            topUp.Status.ToString(),
            topUp.CreatedAt);
    }
}
