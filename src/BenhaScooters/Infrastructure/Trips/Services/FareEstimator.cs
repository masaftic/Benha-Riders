using BenhaScooters.Domain.Trips.ValueObjects;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Infrastructure.Trips.Services;

public interface IFareEstimator
{
    Task<FareEstimate> EstimateFareAsync(Point pickup, Point dropoff, CancellationToken ct = default);
}

public class FareEstimator : IFareEstimator
{
    private const double AverageSpeedKmh = 25.0; // Average speed in km/h
    private const decimal BaseFare = 5.0m; // Base fare in currency units
    private const decimal PerKmRate = 1.5m; // Rate per kilometer in currency units
    private const decimal PerMinuteRate = 0.2m; // Rate per minute in currency units

    public Task<FareEstimate> EstimateFareAsync(Point pickup, Point dropoff, CancellationToken ct = default)
    {
        // Calculate distance using Haversine formula (approximate for short distances)
        var distanceKm = CalculateDistance(pickup, dropoff);

        // Estimate travel time based on distance and average speed
        var estimatedTimeMinutes = (distanceKm / AverageSpeedKmh) * 60;

        decimal amount = BaseFare 
            + ((decimal)distanceKm * PerKmRate) 
            + ((decimal)estimatedTimeMinutes * PerMinuteRate);

        // FareEstimate constructor handles the fare calculation internally
        var fareEstimate = new FareEstimate(amount, distanceKm, estimatedTimeMinutes);

        return Task.FromResult(fareEstimate);
    }

    private static double CalculateDistance(Point point1, Point point2)
    {
        const double earthRadiusKm = 6371.0;
        
        var lat1Rad = ToRadians(point1.Y);
        var lat2Rad = ToRadians(point2.Y);
        var deltaLatRad = ToRadians(point2.Y - point1.Y);
        var deltaLonRad = ToRadians(point2.X - point1.X);

        var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
