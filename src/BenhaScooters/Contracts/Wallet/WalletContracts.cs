using BenhaScooters.Domain.Drivers;

namespace BenhaScooters.Contracts.Wallet;

public record GetWalletTransactionsParams(
    int Page = 1,
    int PageSize = 20,
    DateTime? From = null,
    DateTime? To = null,
    WalletTransactionType? Type = null);

public record SubmitTopUpRequestRequest(decimal Amount);

public record ReviewTopUpRequestRequest(bool Approve, string? Note);

public record GetTopUpRequestsParams(
    int Page = 1,
    int PageSize = 20,
    TopUpRequestStatus? Status = null,
    int? DriverId = null);
