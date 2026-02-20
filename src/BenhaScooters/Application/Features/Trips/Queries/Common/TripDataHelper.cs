using BenhaScooters.Application.Services;
using BenhaScooters.Data;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Users;
using BenhaScooters.Infrastructure.S3;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Application.Features.Trips.Queries.Common;

/// <summary>
/// Helper class to reduce duplication in trip-related queries
/// </summary>
public static class TripDataHelper
{
    /// <summary>
    /// Calculates estimated arrival time based on driver location and trip status
    /// </summary>
    public static async Task<double> CalculateEstimatedArrivalMinutesAsync(
        AppDbContext db,
        IGeoService geoService,
        UserId driverId,
        TripStatus tripStatus,
        Point pickupLocation,
        Point dropoffLocation,
        CancellationToken cancellationToken = default)
    {
        var driverLocation = await db.DriverLocations
            .AsNoTracking()
            .Where(dl => dl.UserId == driverId)
            .Select(dl => dl.Location)
            .FirstOrDefaultAsync(cancellationToken);

        if (driverLocation == null)
        {
            return 0;
        }

        // For Assigned/DriverArrived: calculate time to pickup
        // For InProgress: calculate time to dropoff
        var targetLocation = tripStatus == TripStatus.InProgress
            ? dropoffLocation
            : pickupLocation;

        var distance = geoService.CalculateDistance(driverLocation, targetLocation);
        var arrivalDuration = geoService.EstimateArrivalTime(distance);
        return arrivalDuration.ToMinutes();
    }
}
