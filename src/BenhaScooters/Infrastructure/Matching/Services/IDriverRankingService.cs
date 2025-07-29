using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Infrastructure.Matching.Services;

public record DriverCandidate(
    DriverId DriverId,
    Point CurrentLocation,
    double DistanceToPickup,
    decimal? Rating,
    decimal Score);

public interface IDriverRankingService
{
    Task<List<DriverCandidate>> FindTopNDriversAsync(Point pickupLocation, int count = 5, List<DriverId>? excludedDrivers = null, CancellationToken cancellationToken = default);
}

public class DriverRankingService : IDriverRankingService
{
    private readonly AppDbContext _dbContext;
    private const double MaxSearchRadius = 10000; // 10km in meters
    private const double DistanceWeight = 0.7; // 70% weight for distance
    private const double RatingWeight = 0.3;   // 30% weight for rating
    private const decimal DefaultRating = 4.0m; // Default rating for new drivers

    public DriverRankingService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private decimal CalculateDriverScore(double distanceMeters, decimal? rating)
    {
        // Normalize distance score (closer = higher score)
        // Use exponential decay for distance penalty
        var distanceScore = Math.Exp(-distanceMeters / 2000.0); // 2km decay factor

        // Normalize rating score (higher rating = higher score)
        var actualRating = rating ?? DefaultRating;
        var ratingScore = (double)(actualRating / 5.0m); // Normalize to 0-1 scale

        // Calculate weighted score
        var finalScore = (DistanceWeight * distanceScore) + (RatingWeight * ratingScore);

        return (decimal)finalScore;
    }

    public async Task<List<DriverCandidate>> FindTopNDriversAsync(Point pickupLocation, int count = 5, List<DriverId>? excludedDrivers = null, CancellationToken cancellationToken = default)
    {
        var availableDrivers = await (from da in _dbContext.DriverAvailabilities
                                      join dl in _dbContext.DriverLocations on da.DriverId equals dl.DriverId
                                      join dr in _dbContext.DriverRatings on da.DriverId equals dr.DriverId
                                      where da.Status == DriverStatus.Online
                                      where dl.Location.Distance(pickupLocation) <= MaxSearchRadius
                                      where excludedDrivers == null || !excludedDrivers.Contains(da.DriverId)
                                      select new
                                      {
                                          da.DriverId,
                                          CurrentLocation = dl.Location,
                                          DistanceToPickup = dl.Location.Distance(pickupLocation),
                                          Rating = dr.AverageRating
                                      })
                                      .ToListAsync(cancellationToken);

        // Calculate scores and rank drivers
        var candidates = availableDrivers
            .Select(driver => new DriverCandidate(
                driver.DriverId,
                driver.CurrentLocation,
                driver.DistanceToPickup,
                driver.Rating,
                CalculateDriverScore(driver.DistanceToPickup, driver.Rating)
            ))
            .OrderByDescending(c => c.Score) // Higher score is better
            .ToList();

        return candidates.Take(count).ToList();
    }
}



