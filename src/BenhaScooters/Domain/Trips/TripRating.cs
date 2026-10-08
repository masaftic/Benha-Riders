using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using ErrorOr;
using Thinktecture;


namespace BenhaScooters.Domain.Trips;

[ValueObject<int>]
public partial struct TripRatingId;

public class TripRating : AggregateRoot
{
    public TripRatingId Id { get; private set; }
    public TripId TripId { get; private set; }
    public UserId DriverId { get; private set; }
    public UserId RiderId { get; private set; }

    public int? DriverRating { get; private set; }
    public string? DriverComment { get; private set; }
    public DateTime? DriverRatedAt { get; private set; }

    public int? RiderRating { get; private set; }
    public string? RiderComment { get; private set; }
    public DateTime? RiderRatedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public Trip Trip { get; private set; } = null!;


    private TripRating() { } // For EF Core


    public TripRating(TripId tripId, UserId driverId, UserId riderId)
    {
        TripId = tripId;
        DriverId = driverId;
        RiderId = riderId;
        CreatedAt = DateTime.UtcNow;
    }

    public ErrorOr<Success> SetDriverRating(int rating, string? comment = null)
    {
        if (rating < 1 || rating > 5)
            return AppErrors.Rating.InvalidValue();

        if (DriverRating.HasValue)
            return AppErrors.Rating.AlreadySubmitted();

        DriverRating = rating;
        DriverComment = comment?.Trim();
        DriverRatedAt = DateTime.UtcNow;
        UpdatedAt = DriverRatedAt;

        return Result.Success;
    }

    public ErrorOr<Success> SetRiderRating(int rating, string? comment = null)
    {
        if (rating < 1 || rating > 5)
            return AppErrors.Rating.InvalidValue();

        if (RiderRating.HasValue)
            return AppErrors.Rating.AlreadySubmitted();

        RiderRating = rating;
        RiderComment = comment?.Trim();
        RiderRatedAt = DateTime.UtcNow;
        UpdatedAt = RiderRatedAt;

        return Result.Success;
    }
}
