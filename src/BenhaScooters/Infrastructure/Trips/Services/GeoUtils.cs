using NetTopologySuite.Geometries;

namespace BenhaScooters.Infrastructure.Trips.Services;

public static class GeoUtils
{
    public static double CalculateDistance(Coordinate coord1, Coordinate coord2)
    {
        const double earthRadiusKm = 6371.0;

        var lat1Rad = ToRadians(coord1.Y);
        var lat2Rad = ToRadians(coord2.Y);
        var deltaLatRad = ToRadians(coord2.Y - coord1.Y);
        var deltaLonRad = ToRadians(coord2.X - coord1.X);

        var a = Math.Sin(deltaLatRad / 2) * Math.Sin(deltaLatRad / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLonRad / 2) * Math.Sin(deltaLonRad / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }

    // Overload for Point objects for convenience
    public static double CalculateDistance(Point point1, Point point2)
    {
        return CalculateDistance(point1.Coordinate, point2.Coordinate);
    }

    // Calculate total distance along a route
    public static double CalculateRouteDistance(LineString route)
    {
        if (route.Coordinates.Length < 2)
            return 0;

        double totalDistance = 0;
        for (int i = 0; i < route.Coordinates.Length - 1; i++)
        {
            totalDistance += CalculateDistance(route.Coordinates[i], route.Coordinates[i + 1]);
        }
        return totalDistance;
    }

    public static double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180.0;
    }
}
