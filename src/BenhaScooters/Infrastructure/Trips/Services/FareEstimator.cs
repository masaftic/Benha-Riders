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
    private readonly IGeoService _geoService;

    public FareEstimator(IOptions<FareEstimationOptions> options, IGoogleMapsService googleMapsService, IGeoService geoService)
    {
        _options = options.Value;
        _googleMapsService = googleMapsService;
        _geoService = geoService;
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
            var distance = _geoService.CalculateDistance(pickup, dropoff);
            var estimatedDuration = _geoService.EstimateArrivalTime(distance, _options.AverageSpeedKmh);

            decimal amount = _options.BaseFare 
                + ((decimal)distance.ToKilometers() * _options.PerKmRate) 
                + ((decimal)estimatedDuration.ToMinutes() * _options.PerMinuteRate);

            return FareEstimate.Create(amount, distance, estimatedDuration);
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
