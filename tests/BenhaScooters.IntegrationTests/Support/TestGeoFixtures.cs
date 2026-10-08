using BenhaScooters.Data;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.ServiceAreas;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using DomainCoordinate = BenhaScooters.Domain.Common.Geo.Coordinate;

namespace BenhaScooters.IntegrationTests.Support;

public readonly record struct TestCoordinate(double Lat, double Lng)
{
    public DomainCoordinate ToDomainCoordinate() => DomainCoordinate.Create(Latitude.Create(Lat), Longitude.Create(Lng));
}

public static class TestGeoFixtures
{
    private const double EarthRadiusMeters = 6371000d;
    private static readonly GeometryFactory GeometryFactory =
        NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(GeoConstants.SRID_WGS84);

    public static readonly TestCoordinate BenhaStation = new(30.46629, 31.18463);
    public static readonly TestCoordinate BenhaUniversity = new(30.46983, 31.17891);
    public static readonly TestCoordinate BenhaHospital = new(30.46255, 31.18742);
    public static readonly TestCoordinate OutsideBenha = new(30.61000, 31.35000);

    public static Point ToPoint(TestCoordinate coordinate)
    {
        return GeometryFactory.CreatePoint(new NetTopologySuite.Geometries.Coordinate(
            coordinate.Lng,
            coordinate.Lat));
    }

    public static async Task SeedBenhaServiceAreaAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        if (await db.ServiceAreas.AnyAsync(sa => sa.ExternalId == "test-benha", cancellationToken))
            return;

        var shell = GeometryFactory.CreateLinearRing(
        [
            new NetTopologySuite.Geometries.Coordinate(31.1300, 30.4300),
            new NetTopologySuite.Geometries.Coordinate(31.2400, 30.4300),
            new NetTopologySuite.Geometries.Coordinate(31.2400, 30.5100),
            new NetTopologySuite.Geometries.Coordinate(31.1300, 30.5100),
            new NetTopologySuite.Geometries.Coordinate(31.1300, 30.4300)
        ]);

        var polygon = GeometryFactory.CreatePolygon(shell);
        var area = new ServiceArea
        {
            ExternalId = "test-benha",
            Name = "Test Benha Service Area",
            Area = GeometryFactory.CreateMultiPolygon([polygon])
        };

        db.ServiceAreas.Add(area);
        await db.SaveChangesAsync(cancellationToken);
    }

    public static double HaversineMeters(
        double originLatitude,
        double originLongitude,
        double destinationLatitude,
        double destinationLongitude)
    {
        var lat1 = ToRadians(originLatitude);
        var lat2 = ToRadians(destinationLatitude);
        var deltaLat = ToRadians(destinationLatitude - originLatitude);
        var deltaLon = ToRadians(destinationLongitude - originLongitude);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)
            + Math.Cos(lat1) * Math.Cos(lat2)
            * Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusMeters * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180d;
}
