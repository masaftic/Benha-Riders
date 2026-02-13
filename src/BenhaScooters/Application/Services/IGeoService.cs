using BenhaScooters.Domain.Common.Geo;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Application.Services;

/// <summary>
/// Service for geographic calculations with type-safe distance and duration measurements.
/// </summary>
public interface IGeoService
{
    /// <summary>
    /// Calculates the distance between two points using the Haversine formula.
    /// </summary>
    /// <param name="from">Starting point</param>
    /// <param name="to">Destination point</param>
    /// <returns>Distance between the two points</returns>
    Distance CalculateDistance(Point from, Point to);
    
    /// <summary>
    /// Calculates the distance between two coordinates using the Haversine formula.
    /// </summary>
    /// <param name="from">Starting coordinate</param>
    /// <param name="to">Destination coordinate</param>
    /// <param name="factory">Geometry factory for creating points</param>
    /// <returns>Distance between the two coordinates</returns>
    Distance CalculateDistance(BenhaScooters.Domain.Common.Geo.Coordinate from, BenhaScooters.Domain.Common.Geo.Coordinate to, GeometryFactory factory);
    
    /// <summary>
    /// Calculates the total distance along a route (LineString).
    /// </summary>
    /// <param name="route">Route as a LineString</param>
    /// <returns>Total distance along the route</returns>
    Distance CalculateRouteDistance(LineString route);
    
    /// <summary>
    /// Estimates arrival time based on distance and average speed.
    /// </summary>
    /// <param name="distance">Distance to travel</param>
    /// <param name="averageSpeedKmh">Average speed in kilometers per hour (default: 30 km/h for city driving)</param>
    /// <returns>Estimated duration to reach destination</returns>
    Duration EstimateArrivalTime(Distance distance, double averageSpeedKmh = 30.0);
}
