using BenhaScooters.Application.Abstractions;
using BenhaScooters.Application.Services;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Trips.ValueObjects;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Infrastructure.Trips.Services;


public class FareEstimator : IFareEstimator
{
    private readonly FareEstimationOptions _options;
    private readonly IGoogleMapsService _googleMapsService;

    public FareEstimator(IOptions<FareEstimationOptions> options, IGoogleMapsService googleMapsService)
    {
        _options = options.Value;
        _googleMapsService = googleMapsService;
    }

    public async Task<FareEstimate> EstimateFareAsync(Point pickup, Point dropoff, CancellationToken ct = default)
    {
        // Get actual route information from Google Maps
        var directionsResult = await _googleMapsService.GetDirectionsAsync(
            pickup.Y, // latitude
            pickup.X, // longitude
            dropoff.Y,
            dropoff.X,
            language: "ar",
            mode: "driving",
            alternatives: false,
            cancellationToken: ct);

        // Fallback to Haversine if Google Maps fails
        if (directionsResult.IsError || directionsResult.Value.Routes.Count == 0)
        {
            var distanceKm = GeoUtils.CalculateDistance(pickup, dropoff);
            var estimatedTimeMinutes = (distanceKm / _options.AverageSpeedKmh) * 60;

            decimal amount = _options.BaseFare 
                + ((decimal)distanceKm * _options.PerKmRate) 
                + ((decimal)estimatedTimeMinutes * _options.PerMinuteRate);

            return FareEstimate.Create(amount, Distance.FromKilometers(distanceKm), Duration.FromMinutes(estimatedTimeMinutes));
        }

        // Extract actual distance and duration from the first route's first leg
        var leg = directionsResult.Value.Routes[0].Legs[0];
        var actualDistanceKm = leg.Distance.Value / 1000.0; // Convert meters to kilometers
        var actualDurationMinutes = leg.Duration.Value / 60.0; // Convert seconds to minutes

        // Calculate actual fare: base fare + (distance multiplier * distance) + (time multiplier * time)
        decimal actualFare = _options.BaseFare 
            + ((decimal)actualDistanceKm * _options.PerKmRate) 
            + ((decimal)actualDurationMinutes * _options.PerMinuteRate);

        var fareEstimate = FareEstimate.Create(
            actualFare, 
            Distance.FromKilometers(actualDistanceKm), 
            Duration.FromMinutes(actualDurationMinutes));

        return fareEstimate;
    }
}
