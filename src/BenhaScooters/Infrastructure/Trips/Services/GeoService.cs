using BenhaScooters.Application.Services;
using BenhaScooters.Domain.Common.Geo;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Infrastructure.Trips.Services;

public class GeoService : IGeoService
{
    private const double EarthRadiusKm = 6371.0;

    public Distance CalculateDistance(Point from, Point to)
    {
        var distanceKm = CalculateHaversineDistance(from.Coordinate, to.Coordinate);
        return Distance.FromKilometers(distanceKm);
    }

    public Distance CalculateDistance(BenhaScooters.Domain.Common.Geo.Coordinate from, BenhaScooters.Domain.Common.Geo.Coordinate to, GeometryFactory factory)
    {
        var fromPoint = from.ToPoint(factory);
        var toPoint = to.ToPoint(factory);
        return CalculateDistance(fromPoint, toPoint);
    }

    public Distance CalculateRouteDistance(LineString route)
    {
        if (route.Coordinates.Length < 2)
            return Distance.FromKilometers(0);

        double totalDistanceKm = 0;
        for (int i = 0; i < route.Coordinates.Length - 1; i++)
        {
            totalDistanceKm += CalculateHaversineDistance(route.Coordinates[i], route.Coordinates[i + 1]);
        }
        
        return Distance.FromKilometers(totalDistanceKm);
    }

    public Duration EstimateArrivalTime(Distance distance, double averageSpeedKmh = 30.0)
    {
        if (averageSpeedKmh <= 0)
            throw new ArgumentException("Average speed must be positive", nameof(averageSpeedKmh));

        var hours = distance.ToKilometers() / averageSpeedKmh;
        return Duration.FromHours(hours);
    }

    private static double CalculateHaversineDistance(NetTopologySuite.Geometries.Coordinate coord1, NetTopologySuite.Geometries.Coordinate coord2)
    {
        var lat1Rad = ToRadians(coord1.Y);
        var lat2Rad = ToRadians(coord2.Y);
        var deltaLatRad = ToRadians(coord2.Y - coord1.Y);
        var deltaLonRad = ToRadians(coord2.X - coord1.X);

        var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return EarthRadiusKm * c;
    }

    private static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
