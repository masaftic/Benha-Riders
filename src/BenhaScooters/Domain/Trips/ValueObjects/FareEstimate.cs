using BenhaScooters.Domain.Common;

namespace BenhaScooters.Domain.Trips.ValueObjects;

public class FareEstimate : ValueObject
{
    public decimal Amount { get; private set; }
    public double Distance { get; private set; } // kilometers
    public double Time { get; private set; } // minutes

    private FareEstimate() { } // For EF Core

    public FareEstimate(decimal amount, double distance, double time)
    {
        if (distance < 0) throw new ArgumentException("Distance cannot be negative");
        if (time < 0) throw new ArgumentException("Estimated time cannot be negative");

        Distance = distance;
        Time = time;
        Amount = amount;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Distance;
        yield return Time;
    }
}
