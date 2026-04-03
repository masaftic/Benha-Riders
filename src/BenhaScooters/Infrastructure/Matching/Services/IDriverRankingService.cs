using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.Matching.Settings;
using BenhaScooters.Infrastructure.Notifications;
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
    private readonly ISignalRConnectionTracker _connectionTracker;
    private readonly ILogger<DriverRankingService> _logger;
    private readonly DriverRankingOptions _options;

    private const double DistanceWeight = 0.7; // 70% weight for distance
    private const double RatingWeight = 0.3;   // 30% weight for rating
    

    public DriverRankingService(
        AppDbContext dbContext,
        IGeoService geoService,
        ISignalRConnectionTracker connectionTracker,
        IOptions<DriverRankingOptions> options,
        ILogger<DriverRankingService> logger)
    {
        _dbContext = dbContext;
        _geoService = geoService;
        _connectionTracker = connectionTracker;
        _options = options.Value;
        _logger = logger;
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

        _logger.LogInformation("Finding top {Count} drivers within {SearchRadius} meters for round {RoundNumber}. Excluded drivers: {ExcludedDrivers}. Pickup location: {PickupLocation}",
            count, searchRadius, roundNumber, excludedUserIds != null ? string.Join(", ", excludedUserIds) : "None", pickupLocation);

        var heartbeatCutoff = DateTime.UtcNow - SignalRConnectionTracker.HeartbeatTimeout;

        var availableDrivers = await (
            from ds in _dbContext.DriverStatuses
            join dl in _dbContext.DriverLocations on ds.UserId equals dl.UserId
            join stats in _dbContext.DriverStats on ds.UserId equals stats.UserId
            where (excludedUserIds == null || !excludedUserIds.Contains(ds.UserId))
                && dl.Location.IsWithinDistance(pickupLocation, searchRadius) // PostGIS spatial index optimization
                // Broad candidate set from persisted state; refined with live SignalR state below.
                && ds.Status != DriverAvailabilityStatus.OnTrip
                && (ds.Status == DriverAvailabilityStatus.Online || ds.LastHeartbeat > heartbeatCutoff)
            select new
            {
                ds.UserId,
                ds.Status,
                CurrentLocation = dl.Location,
                stats.AverageRating
            })
            .ToListAsync(cancellationToken);

        var eligibleDrivers = new List<(UserId UserId, Point CurrentLocation, decimal AverageRating)>(availableDrivers.Count);
        foreach (var driver in availableDrivers)
        {
            if (driver.Status == DriverAvailabilityStatus.Online
                || await _connectionTracker.HasActiveConnectionAsync(driver.UserId))
            {
                eligibleDrivers.Add((driver.UserId, driver.CurrentLocation, driver.AverageRating));
            }
        }

        // Calculate actual distances using GeoService for accurate ranking
        var candidatesWithDistance = eligibleDrivers
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
}

