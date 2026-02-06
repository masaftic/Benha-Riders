using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
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
        
        _transactions.Add(new WalletTransaction(
            Id,
            WalletTransactionType.TripCommission,
            -amount,  // Negative for debit
            tripId,
            description));

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
        
        _transactions.Add(new WalletTransaction(
            Id,
            WalletTransactionType.Settlement,
            amount,  // Positive for credit
            null,
            $"Settlement: {reference}"));

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
        
        _transactions.Add(new WalletTransaction(
            Id,
            WalletTransactionType.Adjustment,
            amount,
            null,
            $"Adjustment: {reason}"));

        return Result.Success;
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
        
        _transactions.Add(new WalletTransaction(
            Id,
            WalletTransactionType.Refund,
            amount,  // Positive for credit
            tripId,
            $"Refund: {reason}"));

        return Result.Success;
    }
}

/// <summary>
/// Individual wallet transaction record.
/// </summary>
public class WalletTransaction
{
    public WalletTransactionId Id { get; private set; }
    public DriverWalletId WalletId { get; private set; }
    public WalletTransactionType Type { get; private set; }
    
    /// <summary>
    /// Transaction amount. Negative for debits, positive for credits.
    /// </summary>
    public decimal Amount { get; private set; }
    
    /// <summary>
    /// Balance after this transaction was applied.
    /// </summary>
    public decimal BalanceAfter { get; private set; }
    
    /// <summary>
    /// Related trip ID (for commission/refund transactions).
    /// </summary>
    public TripId? TripId { get; private set; }
    public Trip? Trip { get; private set; }
    
    /// <summary>
    /// Description or reference for this transaction.
    /// </summary>
    public string Description { get; private set; } = null!;
    
    public DateTime CreatedAt { get; private set; }

    private WalletTransaction() { } // For EF Core

    public WalletTransaction(
        DriverWalletId walletId,
        WalletTransactionType type,
        decimal amount,
        TripId? tripId,
        string description)
    {
        WalletId = walletId;
        Type = type;
        Amount = amount;
        TripId = tripId;
        Description = description ?? string.Empty;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Set the balance after transaction (called by wallet during save).
    /// </summary>
    internal void SetBalanceAfter(decimal balance)
    {
        BalanceAfter = balance;
    }
}
