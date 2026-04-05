using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using Microsoft.EntityFrameworkCore;

namespace BenhaScooters.Application.Features.Drivers.Queries;

internal static class DriverProfitSummaryCalculator
{
    public static async Task<List<CompletedTripProfitProjection>> GetCompletedTripsAsync(
        IQueryable<Trip> trips,
        CancellationToken cancellationToken)
    {
        return await trips
            .AsNoTracking()
            .Where(t => t.Status == TripStatus.Completed && t.CompletedAt.HasValue)
            .Select(t => new CompletedTripProfitProjection(
                t.Id,
                t.CompletedAt!.Value,
                t.FinalFare.Amount))
            .ToListAsync(cancellationToken);
    }

    public static Dictionary<TripId, decimal> GetTransactionImpactByTrip(IEnumerable<WalletTransaction> transactions)
    {
        return transactions
            .Where(t => t.TripId.HasValue && t.Type is WalletTransactionType.TripCommission or WalletTransactionType.Refund)
            .GroupBy(t => t.TripId!.Value)
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));
    }

    public static ProfitPeriodSummary BuildProfitPeriodSummary(
        IEnumerable<CompletedTripProfitProjection> completedTrips,
        IReadOnlyDictionary<TripId, decimal> transactionImpactByTrip,
        DateTime? periodStartUtc,
        DateTime periodEndUtc)
    {
        var tripsInPeriod = completedTrips
            .Where(t => !periodStartUtc.HasValue || t.CompletedAtUtc >= periodStartUtc.Value)
            .Where(t => t.CompletedAtUtc <= periodEndUtc)
            .ToList();

        var grossFare = tripsInPeriod.Sum(t => t.Fare);
        var netProfit = tripsInPeriod.Sum(t => t.Fare + transactionImpactByTrip.GetValueOrDefault(t.TripId));
        var commission = grossFare - netProfit;

        return new ProfitPeriodSummary(
            periodStartUtc ?? DateTime.MinValue,
            periodEndUtc,
            tripsInPeriod.Count,
            grossFare,
            commission,
            netProfit);
    }
}

internal sealed record CompletedTripProfitProjection(
    TripId TripId,
    DateTime CompletedAtUtc,
    decimal Fare);
