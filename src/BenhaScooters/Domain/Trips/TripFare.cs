using Vogen;

namespace BenhaScooters.Domain.Trips;

[ValueObject<int>]
public partial struct TripFareId;

public class TripFare
{
    public TripFareId Id { get; private set; }
    public TripId TripId { get; private set; }
    public decimal BaseFare { get; private set; }
    public decimal DistanceFare { get; private set; }
    public decimal TimeFare { get; private set; }
    public decimal SurgeMultiplier { get; private set; }
    public decimal TotalFare { get; private set; }

    private TripFare() { }

    public TripFare(TripId tripId, decimal baseFare, decimal distanceFare, decimal timeFare, decimal surgeMultiplier)
    {
        TripId = tripId;
        BaseFare = baseFare;
        DistanceFare = distanceFare;
        TimeFare = timeFare;
        SurgeMultiplier = surgeMultiplier;

        // Calculate total fare
        TotalFare = BaseFare + DistanceFare + TimeFare;
        TotalFare *= SurgeMultiplier; // Apply surge multiplier
    }
}
