using BenhaScooters.Data;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Infrastructure.Matching.Services;

public record DriverCandidate(
    DriverId DriverId,
    Point CurrentLocation,
    double DistanceToPickup,
    decimal Rating,
    decimal Score);

public interface IDriverRankingService
{
    Task<List<DriverCandidate>> FindTopNDriversAsync(
        Point pickupLocation, 
        int count = 5, 
        List<DriverId>? excludedDrivers = null, 
        CancellationToken cancellationToken = default);
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

    private static decimal CalculateDriverScore(double distanceMeters, decimal rating)
    {
        // Normalize distance score (closer = higher score)
        // Use exponential decay for distance penalty
        var distanceScore = Math.Exp(-distanceMeters / 2000.0); // 2km decay factor

        // Normalize rating score (higher rating = higher score)
        var ratingScore = (double)(rating / 5.0m); // Normalize to 0-1 scale

        // Calculate weighted score
        var finalScore = (DistanceWeight * distanceScore) + (RatingWeight * ratingScore);

        return (decimal)finalScore;
    }

    public async Task<List<DriverCandidate>> FindTopNDriversAsync(
        Point pickupLocation, 
        int count = 5, 
        List<DriverId>? excludedDrivers = null, 
        CancellationToken cancellationToken = default)
    {
        // Convert excluded DriverIds to UserIds for query
        var excludedUserIds = excludedDrivers?.Select(d => d.ToUserId()).ToList();

        // Query using new tiered structure: DriverStatus + DriverLocation + DriverStats
        // All tables are keyed by UserId
        var availableDrivers = await (
            from ds in _dbContext.DriverStatuses
            join dl in _dbContext.DriverLocations on ds.UserId equals dl.UserId
            join stats in _dbContext.DriverStats on ds.UserId equals stats.UserId
            where ds.Status == DriverAvailabilityStatus.Online
            where dl.Location.Distance(pickupLocation) <= MaxSearchRadius
            where excludedUserIds == null || !excludedUserIds.Contains(ds.UserId)
            select new
            {
                ds.UserId,
                CurrentLocation = dl.Location,
                DistanceToPickup = dl.Location.Distance(pickupLocation),
                stats.AverageRating
            })
            .ToListAsync(cancellationToken);

        // Calculate scores and rank drivers
        var candidates = availableDrivers
            .Select(driver => new DriverCandidate(
                DriverId.From(driver.UserId.Value),  // Convert UserId back to DriverId
                driver.CurrentLocation,
                driver.DistanceToPickup,
                driver.AverageRating,
                CalculateDriverScore(driver.DistanceToPickup, driver.AverageRating)
            ))
            .OrderByDescending(c => c.Score) // Higher score is better
            .Take(count)
            .ToList();

        return candidates;
    }
}



