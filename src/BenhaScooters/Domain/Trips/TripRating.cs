using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Users;
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

    public decimal DriverRating { get; private set; }
    public string? DriverComment { get; private set; }

    public decimal RiderRating { get; private set; }
    public string? RiderComment { get; private set; }

    public DateTime CreatedAt { get; private set; }


    private TripRating() { } // For EF Core


    public TripRating(TripId tripId, decimal driverRating, decimal riderRating, string? driverComment = null, string? riderComment = null)
    {
        if (driverRating < 1 || driverRating > 5)
            throw new ArgumentException("Driver rating must be between 1 and 5", nameof(driverRating));

        if (riderRating < 1 || riderRating > 5)
            throw new ArgumentException("Rider rating must be between 1 and 5", nameof(riderRating));

        TripId = tripId;
        DriverRating = driverRating;
        RiderRating = riderRating;
        DriverComment = driverComment?.Trim();
        RiderComment = riderComment?.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public void SetDriverRating(decimal rating, string? comment = null)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Driver rating must be between 1 and 5", nameof(rating));

        DriverRating = rating;
        DriverComment = comment?.Trim();
    }

    public void SetRiderRating(decimal rating, string? comment = null)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rider rating must be between 1 and 5", nameof(rating));

        RiderRating = rating;
        RiderComment = comment?.Trim();
    }
}
