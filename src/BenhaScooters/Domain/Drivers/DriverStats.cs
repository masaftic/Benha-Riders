using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Drivers;

/// <summary>
/// Aggregated driver statistics. Medium-frequency updates (per trip completion).
/// Uses UserId as primary key.
/// </summary>
public class DriverStats
{
    public UserId UserId { get; private set; }  // PK & FK
    
    // Rating
    public decimal AverageRating { get; private set; }
    public int TotalRatings { get; private set; }
    
    // Trip counts
    public int TotalTrips { get; private set; }
    public int CompletedTrips { get; private set; }
    public int CancelledTrips { get; private set; }
    
    // Engagement metrics
    public int CurrentStreak { get; private set; }  // Consecutive days with completed trips
    public TimeSpan TotalOnlineTime { get; private set; }
    public DateTime? LastTripAt { get; private set; }
    public DateTime? LastOnlineAt { get; private set; }
    public DateTime? LastStreakDate { get; private set; }  // Track last day counted for streak

    // Navigation
    public User User { get; private set; } = null!;

    private DriverStats() { } // For EF Core

    public DriverStats(UserId userId)
    {
        UserId = userId;
        AverageRating = 4.0m;  // Default rating for new drivers
        TotalRatings = 1;      // Start with 1 to avoid division by zero and give benefit of doubt
        TotalTrips = 0;
        CompletedTrips = 0;
        CancelledTrips = 0;
        CurrentStreak = 0;
        TotalOnlineTime = TimeSpan.Zero;
    }

    public DriverId GetDriverId() => DriverId.FromUserId(UserId);

    public void AddRating(decimal rating)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.", nameof(rating));

        var totalScore = AverageRating * TotalRatings + rating;
        TotalRatings++;
        AverageRating = Math.Round(totalScore / TotalRatings, 2);
    }

    public void RecordTripStarted()
    {
        TotalTrips++;
    }

    public void RecordTripCompleted()
    {
        CompletedTrips++;
        LastTripAt = DateTime.UtcNow;
        UpdateStreak();
    }

    public void RecordTripCancelled()
    {
        CancelledTrips++;
        // Optionally reset streak on cancellation
        // CurrentStreak = 0;
    }

    public void AddOnlineTime(TimeSpan duration)
    {
        TotalOnlineTime += duration;
        LastOnlineAt = DateTime.UtcNow;
    }

    private void UpdateStreak()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        
        if (LastStreakDate == null)
        {
            // First trip ever
            CurrentStreak = 1;
            LastStreakDate = DateTime.UtcNow;
            return;
        }

        var lastDate = DateOnly.FromDateTime(LastStreakDate.Value);
        var daysDiff = today.DayNumber - lastDate.DayNumber;

        if (daysDiff == 0)
        {
            // Same day, streak already counted
            return;
        }
        else if (daysDiff == 1)
        {
            // Consecutive day
            CurrentStreak++;
            LastStreakDate = DateTime.UtcNow;
        }
        else
        {
            // Streak broken
            CurrentStreak = 1;
            LastStreakDate = DateTime.UtcNow;
        }
    }

    // Calculated properties
    public decimal CompletionRate => TotalTrips > 0 
        ? Math.Round((decimal)CompletedTrips / TotalTrips * 100, 1) 
        : 100m;

    public bool HasRatings => TotalRatings > 1;  // > 1 because we start with 1 default
    public bool IsNewDriver => CompletedTrips < 10;
    public bool IsExperienced => CompletedTrips >= 100;
}
