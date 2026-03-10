using BenhaScooters.Domain.Trips;


namespace BenhaScooters.Domain.Drivers;

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
    /// Description or reference for this transaction (human-readable, for backward compatibility).
    /// </summary>
    public string Description { get; private set; } = null!;
    
    /// <summary>
    /// Translation key for the description (e.g., "trip_commission", "settlement", "adjustment", "refund").
    /// Used by mobile app to lookup translations.
    /// </summary>
    public string DescriptionKey { get; private set; } = null!;
    
    /// <summary>
    /// Optional JSON string containing parameters for the description.
    /// Example: {"reference": "TopUp #123"}, {"reason": "driver error"}
    /// </summary>
    public string? DescriptionParams { get; private set; }
    
    public DateTime CreatedAt { get; private set; }

    private WalletTransaction() { } // For EF Core

    public WalletTransaction(
        DriverWalletId walletId,
        WalletTransactionType type,
        decimal amount,
        TripId? tripId,
        string description,
        string descriptionKey,
        string? descriptionParams = null)
    {
        WalletId = walletId;
        Type = type;
        Amount = amount;
        TripId = tripId;
        Description = description ?? string.Empty;
        DescriptionKey = descriptionKey;
        DescriptionParams = descriptionParams;
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
