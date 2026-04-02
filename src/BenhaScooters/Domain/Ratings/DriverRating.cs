using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;
using ErrorOr;
using Thinktecture;

namespace BenhaScooters.Domain.Ratings;

[ValueObject<int>]
public partial struct DriverRatingId;

public class DriverRating
{
    public DriverRatingId Id { get; private set; }
    public TripId TripId { get; private set; }
    public UserId RiderId { get; private set; }
    public UserId DriverId { get; private set; }
    public int Rating { get; private set; } // 1-5 stars
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Trip Trip { get; private set; } = null!;

    private DriverRating() { } // EF Core

    public static ErrorOr<DriverRating> Create(TripId tripId, UserId riderId, UserId driverId, int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
            return AppErrors.Rating.InvalidValue();

        return new DriverRating
        {
            TripId = tripId,
            RiderId = riderId,
            DriverId = driverId,
            Rating = rating,
            Comment = comment?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
