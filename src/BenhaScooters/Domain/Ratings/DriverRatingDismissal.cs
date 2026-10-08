using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using Thinktecture;

namespace BenhaScooters.Domain.Ratings;

[ValueObject<int>]
public partial struct DriverRatingDismissalId;

public class DriverRatingDismissal
{
    public DriverRatingDismissalId Id { get; private set; }
    public TripId TripId { get; private set; }
    public UserId RiderId { get; private set; }
    public DateTime DismissedAt { get; private set; }

    public Trip Trip { get; private set; } = null!;

    private DriverRatingDismissal() { }

    public DriverRatingDismissal(TripId tripId, UserId riderId)
    {
        TripId = tripId;
        RiderId = riderId;
        DismissedAt = DateTime.UtcNow;
    }
}
