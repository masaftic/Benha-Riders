using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using Vogen;

namespace BenhaScooters.Domain.Trips;

[ValueObject<int>]
public partial struct TripRatingId;

public class TripRating
{
    public TripRatingId Id { get; private set; }
    public TripId TripId { get; private set; }
    public DriverId DriverId { get; private set; }
    public RiderId RiderId { get; private set; }
    public decimal DriverRating { get; private set; }
    public decimal RiderRating { get; private set; }
    public string? DriverComment { get; private set; }
    public string? RiderComment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigational properties
    public Driver Driver { get; private set; } = null!;
    public Rider Rider { get; private set; } = null!;
    public Trip Trip { get; private set; } = null!;


    private TripRating() { } // For EF Core

    // TODO: set this up as proper entity

    public TripRating(decimal driverRating, decimal riderRating, string? driverComment = null, string? riderComment = null)
    {
        if (driverRating < 1 || driverRating > 5)
            throw new ArgumentException("Driver rating must be between 1 and 5", nameof(driverRating));

        if (riderRating < 1 || riderRating > 5)
            throw new ArgumentException("Rider rating must be between 1 and 5", nameof(riderRating));

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
