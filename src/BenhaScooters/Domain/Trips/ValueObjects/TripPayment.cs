using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Trips.Enums;
using ErrorOr;

namespace BenhaScooters.Domain.Trips.ValueObjects;

public class TripPayment : ValueObject
{
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }
    public decimal Amount { get; private set; }
    public decimal PaidAmount { get; private set; }
    public string? ExternalReference { get; private set; } // null for cash

    public bool IsPaid => Status == PaymentStatus.Paid;

    public static TripPayment Cash(decimal amount) =>
        new(PaymentMethod.Cash, amount, null, PaymentStatus.Paid); // Cash payments are considered paid immediately

    public static TripPayment Online(decimal amount, string reference) =>
        new(PaymentMethod.Online, amount, reference, PaymentStatus.Pending); // Online payments start as pending until confirmed

    private TripPayment() { }

    private TripPayment(PaymentMethod method, decimal amount, string? externalRef, PaymentStatus status = PaymentStatus.Pending)
    {
        Method = method;
        Amount = amount;
        ExternalReference = externalRef;
        Status = status; 
    }

    public ErrorOr<Success> MarkAsPaid(decimal paidAmount)
    {
        if (Status != PaymentStatus.Pending)
        {
            return AppErrors.Payment.AlreadyPaid();
        }

        if (paidAmount <= 0)
        {
            return AppErrors.Payment.InvalidPaidAmount();
        }

        if (paidAmount < Amount)
        {
            return AppErrors.Payment.InsufficientAmount();
        }

        PaidAmount = paidAmount;
        Status = PaymentStatus.Paid;
        return Result.Success;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Method;
        yield return Amount;
        yield return ExternalReference ?? string.Empty; // treat null as empty for equality
        yield return Status;
    }
}
