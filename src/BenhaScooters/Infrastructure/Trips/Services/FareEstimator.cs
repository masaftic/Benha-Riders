using BenhaScooters.Application.Services;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Trips.ValueObjects;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Infrastructure.Trips.Services;


public class FareEstimator : IFareEstimator
{
    private readonly FareEstimationOptions _options;

    public FareEstimator(IOptions<FareEstimationOptions> options)
    {
        _options = options.Value;
    }

    public Task<FareEstimate> EstimateFareAsync(Point pickup, Point dropoff, CancellationToken ct = default)
    {
        // Calculate distance using Haversine formula (approximate for short distances)
        var distanceKm = GeoUtils.CalculateDistance(pickup, dropoff);

        // Estimate travel time based on distance and average speed
        var estimatedTimeMinutes = (distanceKm / _options.AverageSpeedKmh) * 60;

        decimal amount = _options.BaseFare 
            + ((decimal)distanceKm * _options.PerKmRate) 
            + ((decimal)estimatedTimeMinutes * _options.PerMinuteRate);

        var fareEstimate = FareEstimate.Create(amount, Distance.FromKilometers(distanceKm), Duration.FromMinutes(estimatedTimeMinutes));

        return Task.FromResult(fareEstimate);
    }
}
