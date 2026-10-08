using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using FluentAssertions;

namespace BenhaScooters.UnitTests.Drivers;

public class DriverWalletUnitTests
{
    private readonly UserId _driverId = UserId.Create(10);
    private readonly TripId _tripId = TripId.Create(500);

    [Fact]
    public void NewWallet_StartsAtZeroBalance_AndHasNoDebt()
    {
        var wallet = new DriverWallet(_driverId);

        wallet.Balance.Should().Be(0m);
        wallet.GetDebt().Should().Be(0m);
        wallet.CanAcceptMatch(debtLimit: 50m).Should().BeTrue();
        wallet.Transactions.Should().BeEmpty();
    }

    [Fact]
    public void ChargeCommission_DecreasesBalance_AndIncreasesDebt()
    {
        var wallet = new DriverWallet(_driverId);

        var result = wallet.ChargeCommission(15.5m, _tripId, "Commission for trip 500");

        result.IsError.Should().BeFalse();
        wallet.Balance.Should().Be(-15.5m);
        wallet.GetDebt().Should().Be(15.5m);
        wallet.Transactions.Should().HaveCount(1);
        wallet.Transactions[0].Type.Should().Be(WalletTransactionType.TripCommission);
        wallet.Transactions[0].Amount.Should().Be(-15.5m);
        wallet.Transactions[0].BalanceAfter.Should().Be(-15.5m);
    }

    [Fact]
    public void ChargeCommission_RejectsZeroOrNegativeAmounts()
    {
        var wallet = new DriverWallet(_driverId);

        var zeroResult = wallet.ChargeCommission(0m, _tripId, "Invalid 0");
        var negResult = wallet.ChargeCommission(-10m, _tripId, "Invalid negative");

        zeroResult.IsError.Should().BeTrue();
        negResult.IsError.Should().BeTrue();
        wallet.Balance.Should().Be(0m);
    }

    [Fact]
    public void CanAcceptMatch_ReturnsFalse_WhenDebtExceedsLimit()
    {
        var wallet = new DriverWallet(_driverId);
        wallet.ChargeCommission(60m, _tripId, "Trip commission");

        // Balance is -60, Debt is 60
        wallet.CanAcceptMatch(debtLimit: 50m).Should().BeFalse("debt of 60 exceeds limit of 50");
        wallet.CanAcceptMatch(debtLimit: 60m).Should().BeTrue("debt of 60 is exactly at limit of 60");
        wallet.CanAcceptMatch(debtLimit: 100m).Should().BeTrue("debt of 60 is within limit of 100");
    }

    [Fact]
    public void RecordSettlement_CreditsBalance_AndReducesDebt()
    {
        var wallet = new DriverWallet(_driverId);
        wallet.ChargeCommission(50m, _tripId, "Commission");

        var settleResult = wallet.RecordSettlement(30m, "REF-12345");

        settleResult.IsError.Should().BeFalse();
        wallet.Balance.Should().Be(-20m);
        wallet.GetDebt().Should().Be(20m);
        wallet.Transactions.Should().HaveCount(2);
        wallet.Transactions[1].Type.Should().Be(WalletTransactionType.Settlement);
        wallet.Transactions[1].Amount.Should().Be(30m);
    }

    [Fact]
    public void RefundCommission_CreditsBalanceBack()
    {
        var wallet = new DriverWallet(_driverId);
        wallet.ChargeCommission(20m, _tripId, "Trip start");

        var refundResult = wallet.RefundCommission(20m, _tripId, "Trip cancelled by rider");

        refundResult.IsError.Should().BeFalse();
        wallet.Balance.Should().Be(0m);
        wallet.GetDebt().Should().Be(0m);
        wallet.Transactions[1].Type.Should().Be(WalletTransactionType.Refund);
    }

    [Fact]
    public void CreateTopUpRequest_ReturnsError_WhenAlreadyHasPendingRequest()
    {
        var wallet = new DriverWallet(_driverId);

        var errorResult = wallet.CreateTopUpRequest(100m, "http://receipt.png", hasPendingRequest: true);
        errorResult.IsError.Should().BeTrue();

        var okResult = wallet.CreateTopUpRequest(100m, "http://receipt.png", hasPendingRequest: false);
        okResult.IsError.Should().BeFalse();
        okResult.Value.Amount.Should().Be(100m);
    }
}
