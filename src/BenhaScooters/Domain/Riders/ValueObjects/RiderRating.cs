using BenhaScooters.Domain.Common;

namespace BenhaScooters.Domain.Riders.ValueObjects;

public class RiderRating : ValueObject
{
    public decimal Rating { get; private set; }
    public int TotalRatings { get; private set; }

    private RiderRating() { } // For EF Core

    public static RiderRating Create()
    {
        return new RiderRating
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
}
