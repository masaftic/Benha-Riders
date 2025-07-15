using BenhaScooters.Domain.Common;

namespace BenhaScooters.Domain.Trips.ValueObjects;

public class FareCalculation : ValueObject
{
    public decimal FinalAmount { get; private set; }
    public double ActualDistance { get; private set; } // kilometers
    public double ActualTime { get; private set; } // minutes
    public DateTime CalculatedAt { get; private set; }

    private FareCalculation() { } // For EF Core

    public FareCalculation(double actualDistance, double actualTime)
    {
        if (actualDistance < 0) throw new ArgumentException("Actual distance cannot be negative");
        if (actualTime < 0) throw new ArgumentException("Actual time cannot be negative");

        ActualDistance = actualDistance;
        ActualTime = actualTime;
        CalculatedAt = DateTime.UtcNow;
        
        // Simple calculation: 5 EGP base + 2 EGP per km + 0.5 EGP per minute
        FinalAmount = 5.0m + ((decimal)actualDistance * 2.0m) + ((decimal)actualTime * 0.5m);
        
        // Minimum fare of 10 EGP
        if (FinalAmount < 10.0m)
            FinalAmount = 10.0m;
    }

    public static FareCalculation FromEstimate(FareEstimate estimate, double actualDistance, double actualTime)
    {
        return new FareCalculation(actualDistance, actualTime);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return FinalAmount;
        yield return ActualDistance;
        yield return ActualTime;
        yield return CalculatedAt;
    }
}
