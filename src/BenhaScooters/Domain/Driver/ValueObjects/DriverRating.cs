using BenhaScooters.Domain.Common;

namespace BenhaScooters.Domain.Driver.ValueObjects;

public class DriverRating : ValueObject
{
    public decimal Rating { get; private set; }
    public int TotalRatings { get; private set; }

    private DriverRating() { } // For EF Core

    public static DriverRating Create()
    {
        return new DriverRating
        {
            Rating = 0,
            TotalRatings = 0
        };
    }

    public void AddRating(decimal newRating)
    {
        if (newRating < 1 || newRating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.", nameof(newRating));

        var totalScore = Rating * TotalRatings + newRating;
        TotalRatings++;
        Rating = totalScore / TotalRatings;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Rating;
        yield return TotalRatings;
    }

    public bool HasRatings => TotalRatings > 0;
}
