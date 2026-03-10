using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using System.Text.Json;
using Thinktecture;


namespace BenhaScooters.Domain.Drivers;

[ValueObject<int>]
public partial struct DriverWalletId;

[ValueObject<int>]
public partial struct WalletTransactionId;

public enum WalletTransactionType
{
    TripCommission = 0,    // Charged when trip starts (negative)
    Settlement = 1,        // Driver pays back debt (positive)
    Adjustment = 2,        // Admin adjustment (positive or negative)
    Refund = 3             // Refund for cancelled trip (positive)
}

/// <summary>
/// Driver wallet that tracks balance and transaction history.
/// Balance is typically negative (debt) as drivers collect cash and owe us commission.
/// When balance falls below -DebtLimit, driver cannot accept new matches.
/// </summary>
public class DriverWallet : AggregateRoot
{
    public DriverWalletId Id { get; private set; }
    public UserId DriverUserId { get; private set; }
    
    /// <summary>
    /// Current balance. Negative means driver owes money (debt).
    /// Positive means driver has credit (rare, only after overpayment).
    /// </summary>
    public decimal Balance { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    private readonly List<WalletTransaction> _transactions = [];
    public IReadOnlyList<WalletTransaction> Transactions => _transactions.AsReadOnly();
    
    // Navigation
    public User Driver { get; private set; } = null!;

    private DriverWallet() { } // For EF Core

    public DriverWallet(UserId driverUserId)
    {
        DriverUserId = driverUserId;
        Balance = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if driver can accept a match based on debt limit.
    /// </summary>
    /// <param name="debtLimit">Maximum allowed debt (positive number, e.g., 500)</param>
    /// <returns>True if balance >= -debtLimit</returns>
    public bool CanAcceptMatch(decimal debtLimit) => Balance >= -debtLimit;

    /// <summary>
    /// Get current debt amount (positive number representing how much driver owes).
    /// Returns 0 if balance is positive or zero.
    /// </summary>
    public decimal GetDebt() => Balance < 0 ? -Balance : 0;

    /// <summary>
    /// Charge commission when trip starts. Decreases balance (increases debt).
    /// </summary>
    public ErrorOr<Success> ChargeCommission(decimal amount, TripId tripId, string description)
    {
        if (amount <= 0)
            return WalletErrors.InvalidAmount;

        Balance -= amount;
        UpdatedAt = DateTime.UtcNow;

        var trx = new WalletTransaction(
            Id,
            WalletTransactionType.TripCommission,
            -amount,  // Negative for debit
            tripId,
            description,
            "trip_commission",
            JsonSerializer.Serialize(new { tripId }));
        
        _transactions.Add(trx);

        trx.SetBalanceAfter(Balance); // Set balance after applying transaction

        return Result.Success;
    }

    /// <summary>
    /// Record a settlement payment from driver. Increases balance (reduces debt).
    /// </summary>
    public ErrorOr<Success> RecordSettlement(decimal amount, string reference)
    {
        if (amount <= 0)
            return WalletErrors.InvalidAmount;

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;

        var trx = new WalletTransaction(
            Id,
            WalletTransactionType.Settlement,
            amount,  // Positive for credit
            null,
            $"Settlement: {reference}",
            "settlement",
            JsonSerializer.Serialize(new { reference }));
        
        _transactions.Add(trx);

        trx.SetBalanceAfter(Balance); // Set balance after applying transaction

        return Result.Success;
    }

    /// <summary>
    /// Apply an admin adjustment (can be positive or negative).
    /// </summary>
    public ErrorOr<Success> ApplyAdjustment(decimal amount, string reason)
    {
        if (amount == 0)
            return WalletErrors.InvalidAmount;

        if (string.IsNullOrWhiteSpace(reason))
            return WalletErrors.AdjustmentReasonRequired;

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
        
        var trx = new WalletTransaction(
            Id,
            WalletTransactionType.Adjustment,
            amount,
            null,
            $"Adjustment: {reason}",
            "adjustment",
            JsonSerializer.Serialize(new { reason }));
        
        _transactions.Add(trx);

        trx.SetBalanceAfter(Balance); // Set balance after applying transaction

        return Result.Success;
    }

    /// <summary>
    /// Create a top-up request for admin review.
    /// Returns an error if there is already a pending top-up request.
    /// </summary>
    public ErrorOr<WalletTopUpRequest> CreateTopUpRequest(decimal amount, string receiptUrl, bool hasPendingRequest)
    {
        if (amount <= 0)
            return WalletErrors.InvalidAmount;

        if (hasPendingRequest)
            return WalletErrors.PendingTopUpExists;

        return new WalletTopUpRequest(Id, DriverUserId, amount, receiptUrl);
    }

    /// <summary>
    /// Refund a commission for a cancelled trip.
    /// </summary>
    public ErrorOr<Success> RefundCommission(decimal amount, TripId tripId, string reason)
    {
        if (amount <= 0)
            return WalletErrors.InvalidAmount;

        Balance += amount;
        UpdatedAt = DateTime.UtcNow;
        
        var trx = new WalletTransaction(
            Id,
            WalletTransactionType.Refund,
            amount,  // Positive for credit
            tripId,
            $"Refund: {reason}",
            "refund",
            JsonSerializer.Serialize(new { reason, tripId }));

        _transactions.Add(trx);

        trx.SetBalanceAfter(Balance); // Set balance after applying transaction

        return Result.Success;
    }
}
