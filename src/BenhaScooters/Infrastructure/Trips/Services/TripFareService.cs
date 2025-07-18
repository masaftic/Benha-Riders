using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Trips;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Infrastructure.Trips.Services;

public class TripFareService(IOptions<TripFareConfiguration> fareConfig) : ITripFareService
{
    private readonly TripFareConfiguration _config = fareConfig.Value;

    public Task<TripFare> CalculateActualFareAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        // Calculate actual distance from trip route
        var actualDistance = CalculateActualDistance(trip);

        // Calculate actual duration
        var actualDuration = CalculateActualDuration(trip);

        // Calculate distance fare
        var distanceFare = actualDistance * _config.PricePerKm;

        // Calculate time fare
        var timeFare = (decimal)actualDuration.TotalMinutes * _config.PricePerMinute;

        return Task.FromResult(new TripFare(
            trip.Id,
            _config.BaseFare,
            distanceFare,
            timeFare,
            1 // No Surge for now
        ));
    }

    private decimal CalculateActualDistance(Trip trip)
    {
        if (trip.TripRoute?.Path == null)
        {
            // Fallback to straight-line distance if no route available
            return CalculateStraightLineDistance(trip);
        }

        return (decimal)GeoUtils.CalculateRouteDistance(trip.TripRoute.Path);
    }

    private decimal CalculateStraightLineDistance(Trip trip)
    {
        var pickupCoord = trip.PickupLocation.Coordinate;
        var dropoffCoord = trip.DropoffLocation.Coordinate;

        var distance = GeoUtils.CalculateDistance(pickupCoord, dropoffCoord);

        return (decimal)distance;
    }

    private TimeSpan CalculateActualDuration(Trip trip)
    {
        return trip.TotalDuration ?? TimeSpan.Zero;
    }
}