using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using Thinktecture;

namespace BenhaScooters.Domain.Drivers;

[ValueObject<int>]
public partial struct WalletTopUpRequestId;

public enum TopUpRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

/// <summary>
/// A driver-initiated top-up request. The driver submits a cash receipt
/// and the admin manually approves or rejects it to credit the wallet.
/// </summary>
public class WalletTopUpRequest
{
    public WalletTopUpRequestId Id { get; private set; }
    public DriverWalletId WalletId { get; private set; }
    public UserId DriverUserId { get; private set; }

    /// <summary>Amount the driver claims to have deposited.</summary>
    public decimal Amount { get; private set; }

    /// <summary>S3 key / URL of the uploaded receipt image.</summary>
    public string ReceiptUrl { get; private set; } = null!;

    public TopUpRequestStatus Status { get; private set; }

    /// <summary>Note left by the admin when approving or rejecting.</summary>
    public string? ReviewNote { get; private set; }

    public UserId? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    // Navigation
    public DriverWallet Wallet { get; private set; } = null!;
    public User Driver { get; private set; } = null!;

    private WalletTopUpRequest() { } // EF Core

    public WalletTopUpRequest(DriverWalletId walletId, UserId driverUserId, decimal amount, string receiptUrl)
    {
        WalletId = walletId;
        DriverUserId = driverUserId;
        Amount = amount;
        ReceiptUrl = receiptUrl;
        Status = TopUpRequestStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public ErrorOr<Success> Approve(UserId reviewedBy, string? note = null)
    {
        if (Status != TopUpRequestStatus.Pending)
            return WalletErrors.TopUpRequestAlreadyReviewed;

        Status = TopUpRequestStatus.Approved;
        ReviewedByUserId = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
        ReviewNote = note;
        return Result.Success;
    }

    public ErrorOr<Success> Reject(UserId reviewedBy, string reason)
    {
        if (Status != TopUpRequestStatus.Pending)
            return WalletErrors.TopUpRequestAlreadyReviewed;

        if (string.IsNullOrWhiteSpace(reason))
            return WalletErrors.RejectionReasonRequired;

        Status = TopUpRequestStatus.Rejected;
        ReviewedByUserId = reviewedBy;
        ReviewedAt = DateTime.UtcNow;
        ReviewNote = reason;
        return Result.Success;
    }
}
