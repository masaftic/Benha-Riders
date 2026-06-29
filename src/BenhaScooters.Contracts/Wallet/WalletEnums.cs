namespace BenhaScooters.Contracts.Wallet;

public enum WalletTransactionType
{
    TripCommission = 0,
    Settlement = 1,
    Adjustment = 2,
    Refund = 3
}

public enum TopUpRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
