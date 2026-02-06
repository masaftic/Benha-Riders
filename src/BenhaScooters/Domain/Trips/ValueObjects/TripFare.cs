using BenhaScooters.Domain.Common;
using Thinktecture;


namespace BenhaScooters.Domain.Trips.ValueObjects;

public class TripFare : ValueObject
{
    public decimal BaseFare { get; private set; }
    public decimal DistanceFare { get; private set; }
    public decimal DurationFare { get; private set; }
    public decimal SurgeMultiplier { get; private set; }
    public decimal TotalFare { get; private set; }

    private TripFare() { }

    public TripFare(decimal baseFare, decimal distanceFare, decimal timeFare, decimal surgeMultiplier)
    {
        BaseFare = baseFare;
        DistanceFare = distanceFare;
        DurationFare = timeFare;
        SurgeMultiplier = surgeMultiplier;

        // Calculate total fare
        TotalFare = BaseFare + DistanceFare + DurationFare;
        TotalFare *= SurgeMultiplier; // Apply surge multiplier
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return BaseFare;
        yield return DistanceFare;
        yield return DurationFare;
        yield return SurgeMultiplier;
        yield return TotalFare;
    }
}
