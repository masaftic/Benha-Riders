using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Trips.Enums;

namespace BenhaScooters.Domain.Trips.ValueObjects;

public class TripEvent : ValueObject
{
    public TripStatus Status { get; private set; }
    public DateTime Timestamp { get; private set; }

    public TripEvent(TripStatus status, DateTime timestamp)
    {
        Status = status;
        Timestamp = timestamp;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Status;
        yield return Timestamp;
    }
}
