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
        new(PaymentMethod.Cash, amount, null);

    public static TripPayment Online(decimal amount, string reference) =>
        new(PaymentMethod.Online, amount, reference);

    private TripPayment() { }

    private TripPayment(PaymentMethod method, decimal amount, string? externalRef)
    {
        Method = method;
        Amount = amount;
        ExternalReference = externalRef;
        Status = PaymentStatus.Pending; // Default status
    }

    public ErrorOr<Success> MarkAsPaid(decimal paidAmount)
    {
        if (Status != PaymentStatus.Pending)
        {
            return Error.Conflict("PAYMENT_ALREADY_PAID", "Payment has already been marked as paid.");
        }

        if (paidAmount <= 0)
        {
            return Error.Validation("INVALID_PAID_AMOUNT", "Paid amount must be greater than zero.");
        }

        if (paidAmount < Amount)
        {
            return Error.Validation("INSUFFICIENT_PAYMENT", "Paid amount is less than the total amount.");
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
