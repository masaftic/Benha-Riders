using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Matching.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Infrastructure.Matching.Services;

public record DriverCandidate(
    UserId DriverId,
    Point CurrentLocation,
    Distance DistanceToPickup,
    decimal Rating,
    decimal Score);

public interface IDriverRankingService
{
    Task<List<DriverCandidate>> FindTopNDriversAsync(
        Point pickupLocation, 
        int count, 
        int roundNumber,
        List<UserId>? excludedDrivers = null, 
        CancellationToken cancellationToken = default);
}

public class DriverRankingService : IDriverRankingService
{
    private readonly AppDbContext _dbContext;
    private readonly IGeoService _geoService;
    private readonly DriverRankingOptions _options;
    private const double DistanceWeight = 0.7; // 70% weight for distance
    private const double RatingWeight = 0.3;   // 30% weight for rating
    private const decimal DefaultRating = 4.0m; // Default rating for new drivers

    public DriverRankingService(AppDbContext dbContext, IGeoService geoService, IOptions<DriverRankingOptions> options)
    {
        _dbContext = dbContext;
        _geoService = geoService;
        _options = options.Value;
    }

    private static decimal CalculateDriverScore(Distance distanceMeters, decimal rating)
    {
        // Normalize distance score (closer = higher score)
        // Use exponential decay for distance penalty
        var distanceScore = Math.Exp(-distanceMeters / 3000.0); // 3km decay factor

        // Normalize rating score (higher rating = higher score)
        var ratingScore = (double)(rating / 5.0m); // Normalize to 0-1 scale

        // Calculate weighted score
        var finalScore = (DistanceWeight * distanceScore) + (RatingWeight * ratingScore);

        return (decimal)finalScore;
    }

    public async Task<List<DriverCandidate>> FindTopNDriversAsync(
        Point pickupLocation, 
        int count, 
        int roundNumber,
        List<UserId>? excludedDrivers = null,
        CancellationToken cancellationToken = default)
    {
        // Convert excluded DriverIds to UserIds for query
        var excludedUserIds = excludedDrivers?.ToList();
        // Query using new tiered structure: DriverStatus + DriverLocation + DriverStats
        
        var searchRadius = _options.MaxSearchRadiusMeters + roundNumber * _options.RadiusIncrementMeters;

        // Use PostGIS ST_DWithin for efficient spatial filtering (3km base radius + progressive expansion)
        var availableDrivers = await (
            from ds in _dbContext.DriverStatuses
            join dl in _dbContext.DriverLocations on ds.UserId equals dl.UserId
            join stats in _dbContext.DriverStats on ds.UserId equals stats.UserId
            where ds.Status == DriverAvailabilityStatus.Online
                && (excludedUserIds == null || !excludedUserIds.Contains(ds.UserId))
                && dl.Location.IsWithinDistance(pickupLocation, searchRadius) // PostGIS spatial index optimization
            select new
            {
                ds.UserId,
                CurrentLocation = dl.Location,
                stats.AverageRating
            })
            .ToListAsync(cancellationToken);

        // Calculate actual distances using GeoService for accurate ranking
        var candidatesWithDistance = availableDrivers
            .Select(driver => 
            {
                var distance = _geoService.CalculateDistance(driver.CurrentLocation, pickupLocation);
                return new
                {
                    driver.UserId,
                    driver.CurrentLocation,
                    DistanceToPickup = distance,
                    driver.AverageRating
                };
            })
            .ToList();

        // Calculate scores and rank drivers
        var candidates = candidatesWithDistance
            .Select(driver => new DriverCandidate(
                driver.UserId, 
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



