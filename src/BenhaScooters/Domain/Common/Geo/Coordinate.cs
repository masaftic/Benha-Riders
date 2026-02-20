using NetTopologySuite.Geometries;
using Thinktecture;

namespace BenhaScooters.Domain.Common.Geo;


class GeoConstants 
{
    public const int SRID_WGS84 = 4326;
}



[ValueObject<double>]
public partial struct Latitude
{
    static partial void ValidateFactoryArguments(ref ValidationError? validationError, ref double value)
    {
        if (value < -90 || value > 90)
        {
            validationError = new ValidationError("Latitude must be between -90 and 90 degrees.");
        }

        // normalize to 5 decimal places (approx 1 meter precision) to avoid issues with floating point precision in comparisons
        value = Math.Round(value, 5);
    }
}


[ValueObject<double>]
public partial struct Longitude
{
    static partial void ValidateFactoryArguments(ref ValidationError? validationError, ref double value)
    {
        if (value < -180 || value > 180)
        {
            validationError = new ValidationError("Longitude must be between -180 and 180 degrees.");
        }

        // normalize to 5 decimal places (approx 1 meter precision) to avoid issues with floating point precision in comparisons
        value = Math.Round(value, 5);
    }
}



[ComplexValueObject]
public partial struct Coordinate
{
    public Latitude Latitude { get; }
    public Longitude Longitude { get; }

    public static Coordinate FromPoint(Point point) => Create(Latitude.Create(point.Y), Longitude.Create(point.X));

    public Point ToPoint(GeometryFactory factory) =>
        factory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(Longitude, Latitude));

    public string ToCacheKey() => $"{(int)(Latitude * 10_000)}_{(int)(Longitude * 10_000)}"; // e.g. "374221_-1220841" for 37.4221, -122.0841
}

public static class CoordinateExtensions
{
    public static Coordinate ToCoordinate(this Point point) => Coordinate.FromPoint(point);
}
