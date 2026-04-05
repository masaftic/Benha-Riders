using BenhaScooters.Application.Common.Settings;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BenhaScooters.Application.Features.Drivers.Queries;

public record GetWalletSummaryQuery(UserId DriverId) : IRequest<ErrorOr<WalletSummaryResponse>>;

public record ProfitPeriodSummary(
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    int CompletedTrips,
    decimal GrossFare,
    decimal Commission,
    decimal NetProfit);

public record WalletSummaryResponse(
    decimal Balance,
    decimal Debt,
    bool CanAcceptMatch,
    decimal TotalCommissionsCharged,
    decimal TotalAmountPaidIn,
    int TotalTripsCharged,
    int PendingTopUpRequests,
    DateTime? LastTransactionAt,
    decimal DebtLimit,
    string StatusLabel,
    ProfitPeriodSummary TodayProfit,
    ProfitPeriodSummary ThisWeekProfit,
    ProfitPeriodSummary ThisMonthProfit,
    ProfitPeriodSummary LifetimeProfit,
    DateTime CalculatedAtUtc);


public static class WalletStatusLabels
{
    public const string Positive = "positive";
    public const string NoDebt = "no_debt";
    public const string DebtCanAccept = "debt_can_accept";
    public const string DebtExceeded = "debt_exceeded";
}


public class GetWalletSummaryQueryHandler : IRequestHandler<GetWalletSummaryQuery, ErrorOr<WalletSummaryResponse>>
{
    private readonly AppDbContext _db;
    private readonly DriverWalletOptions _walletOptions;


    public GetWalletSummaryQueryHandler(AppDbContext db, IOptions<DriverWalletOptions> walletOptions)
    {
        _db = db;
        _walletOptions = walletOptions.Value;
    }

    public async Task<ErrorOr<WalletSummaryResponse>> Handle(GetWalletSummaryQuery request, CancellationToken cancellationToken)
    {
        var wallet = await _db.DriverWallets
            .AsNoTracking()
            .Include(w => w.Transactions)
            .FirstOrDefaultAsync(w => w.DriverUserId == request.DriverId, cancellationToken);

        if (wallet is null)
            return AppErrors.Driver.Wallet.NotFound();

        var transactions = wallet.Transactions;
        var nowUtc = DateTime.UtcNow;
        var todayStartUtc = nowUtc.Date;
        var weekStartUtc = GetStartOfWeekUtc(nowUtc);
        var monthStartUtc = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var totalCommissions = transactions
            .Where(t => t.Type == WalletTransactionType.TripCommission)
            .Sum(t => Math.Abs(t.Amount));

        var totalPaidIn = transactions
            .Where(t => t.Type is WalletTransactionType.Settlement or WalletTransactionType.Refund)
            .Sum(t => t.Amount);

        var tripsCharged = transactions.Count(t => t.Type == WalletTransactionType.TripCommission);

        var lastTx = transactions.MaxBy(t => t.CreatedAt);

        var pendingTopUps = await _db.WalletTopUpRequests
            .CountAsync(r => r.DriverUserId == request.DriverId && r.Status == TopUpRequestStatus.Pending, cancellationToken);

        var completedTrips = await DriverProfitSummaryCalculator.GetCompletedTripsAsync(
            _db.Trips.Where(t => t.DriverId == request.DriverId),
            cancellationToken);

        var transactionImpactByTrip = DriverProfitSummaryCalculator.GetTransactionImpactByTrip(transactions);

        var canAccept = wallet.CanAcceptMatch(_walletOptions.DebtLimitEgp);

        var statusLabel = wallet.Balance switch
        {
            > 0 => WalletStatusLabels.Positive,
            0 => WalletStatusLabels.NoDebt,
            < 0 when wallet.GetDebt() <= _walletOptions.DebtLimitEgp => WalletStatusLabels.DebtCanAccept,
            _ => WalletStatusLabels.DebtExceeded
        };

        return new WalletSummaryResponse(
            wallet.Balance,
            wallet.GetDebt(),
            canAccept,
            totalCommissions,
            totalPaidIn,
            tripsCharged,
            pendingTopUps,
            lastTx?.CreatedAt,
            _walletOptions.DebtLimitEgp,
            statusLabel,
            DriverProfitSummaryCalculator.BuildProfitPeriodSummary(completedTrips, transactionImpactByTrip, todayStartUtc, nowUtc),
            DriverProfitSummaryCalculator.BuildProfitPeriodSummary(completedTrips, transactionImpactByTrip, weekStartUtc, nowUtc),
            DriverProfitSummaryCalculator.BuildProfitPeriodSummary(completedTrips, transactionImpactByTrip, monthStartUtc, nowUtc),
            DriverProfitSummaryCalculator.BuildProfitPeriodSummary(completedTrips, transactionImpactByTrip, null, nowUtc),
            nowUtc);
    }

    private static DateTime GetStartOfWeekUtc(DateTime utcDateTime)
    {
        var daysSinceMonday = ((int)utcDateTime.DayOfWeek + 6) % 7;
        return utcDateTime.Date.AddDays(-daysSinceMonday);
    }
}
