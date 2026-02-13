

using BenhaScooters.Domain.Common.Geo;

public static class TestValueObjects
{
    public static Distance TestDistance = Distance.FromMeters(1500); // 1.5 km
    public static Duration TestDuration = Duration.FromMinutes(5); // 5 minutes

    public static Coordinate TestCoordinate = Coordinate.Create(
        Latitude.Create(30.0444), // Example: Cairo latitude
        Longitude.Create(31.2357) // Example: Cairo longitude
    );
}
