using BenhaScooters.Application.Common;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Drivers.Queries;

public record GetWalletTransactionsQuery(
    UserId DriverId,
    int Page,
    int PageSize,
    DateTime? From,
    DateTime? To,
    WalletTransactionType? TypeFilter) : IRequest<ErrorOr<PaginatedList<WalletTransactionDto>>>;

public record WalletTransactionDto(
    int Id,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string Description,
    string DescriptionKey,
    string? DescriptionParams,
    DateTime CreatedAt,
    TripId? TripId);

public class GetWalletTransactionsQueryHandler
    : IRequestHandler<GetWalletTransactionsQuery, ErrorOr<PaginatedList<WalletTransactionDto>>>
{
    private readonly AppDbContext _db;

    public GetWalletTransactionsQueryHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ErrorOr<PaginatedList<WalletTransactionDto>>> Handle(
        GetWalletTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var wallet = await _db.DriverWallets
            .AsNoTracking()
            .Select(w => new { w.Id, w.DriverUserId })
            .FirstOrDefaultAsync(w => w.DriverUserId == request.DriverId, cancellationToken);

        if (wallet is null)
            return WalletErrors.NotFound;

        var query = _db.Set<WalletTransaction>()
            .AsNoTracking()
            .Where(t => t.WalletId == wallet.Id);

        if (request.From.HasValue)
            query = query.Where(t => t.CreatedAt >= request.From.Value.ToUniversalTime());

        if (request.To.HasValue)
            query = query.Where(t => t.CreatedAt <= request.To.Value.ToUniversalTime());

        if (request.TypeFilter.HasValue)
            query = query.Where(t => t.Type == request.TypeFilter.Value);

        var projected = query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new WalletTransactionDto(
                t.Id,
                t.Type.ToString(),
                t.Amount,
                t.BalanceAfter,
                t.Description,
                t.DescriptionKey,
                t.DescriptionParams,
                t.CreatedAt,
                t.TripId == null ? null : t.TripId));

        return projected.PaginateAsync(request.Page, request.PageSize);
    }
}
