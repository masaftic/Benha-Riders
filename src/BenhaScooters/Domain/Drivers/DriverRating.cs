using Vogen;

namespace BenhaScooters.Domain.Drivers;

[ValueObject<int>]
public partial struct DriverRatingId;

// Separate aggregate for rating history and calculations
public class DriverRating
{
    public DriverId DriverId { get; private set; }
    public decimal AverageRating { get; private set; }
    public int TotalRatings { get; private set; }
    public DateTime LastUpdated { get; private set; }

    // Navigation properties
    public Driver Driver { get; private set; } = null!;

    private DriverRating() { } // For EF Core

    public DriverRating(DriverId driverId)
    {
        DriverId = driverId;
        AverageRating = 4.0m; // Default rating for new drivers
        TotalRatings = 1;
        LastUpdated = DateTime.UtcNow;
    }

    public void AddRating(decimal newRating)
    {
        if (newRating < 1 || newRating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.", nameof(newRating));

        var totalScore = AverageRating * TotalRatings + newRating;
        TotalRatings++;
        AverageRating = totalScore / TotalRatings;
        LastUpdated = DateTime.UtcNow;
    }

    public bool HasRatings => TotalRatings > 0;
}