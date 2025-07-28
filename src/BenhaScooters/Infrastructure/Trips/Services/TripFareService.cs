using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Infrastructure.Trips.Services;

public class TripFareService(IOptions<TripFareConfiguration> fareConfig) : ITripFareService
{
    private readonly TripFareConfiguration _config = fareConfig.Value;

    public Task<TripFare> CalculateActualFareAsync(Trip trip, TripRoute route, CancellationToken cancellationToken = default)
    {
        // Calculate actual distance from trip route
        var actualDistance = (decimal)GeoUtils.CalculateRouteDistance(route.Path);

        // Calculate actual duration
        var actualDuration = trip.TotalDuration ?? TimeSpan.Zero;

        // Calculate distance fare
        var distanceFare = actualDistance * _config.PricePerKm;

        // Calculate time fare
        var timeFare = (decimal)actualDuration.TotalMinutes * _config.PricePerMinute;

        return Task.FromResult(new TripFare(
            _config.BaseFare,
            distanceFare,
            timeFare,
            1 // No Surge for now
        ));
    }
}