using System.Text.Json;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.ServiceAreas;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Data.MapData;

public class ServiceAreasPolygonSeeder(IHostEnvironment env, AppDbContext dbContext)
{
    private readonly IHostEnvironment _env = env;
    private readonly AppDbContext _dbContext = dbContext;
    private readonly GeometryFactory _geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(GeoConstants.SRID_WGS84);


    public async Task SeedStatesAsync()
    {
        if (await _dbContext.ServiceAreas.AnyAsync())
            return;

        string jsonPath = Path.Combine(_env.ContentRootPath, "Data", "MapData", "service-areas.json");


        var jsonData = File.ReadAllText(jsonPath);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        var polygons = JsonSerializer.Deserialize<MapFeatureCollection>(jsonData, options);

        if (polygons != null)
        {
            List<ServiceArea> serviceAreas = [];

            foreach (var feature in polygons.Features)
            {
                var multiPolygon = ConvertToMultiPolygon(feature.Geometry);

                var serviceArea = new ServiceArea
                {
                    ExternalId = feature.Properties.Id,
                    Name = feature.Properties.Name,
                    Area = multiPolygon
                };

                serviceAreas.Add(serviceArea);
            }

            _dbContext.ServiceAreas.AddRange(serviceAreas);
            await _dbContext.SaveChangesAsync();
        }
    }


    private MultiPolygon ConvertToMultiPolygon(Geometry geometry)
    {
        return geometry.Type switch
        {
            "Polygon" => new MultiPolygon(
                [BuildPolygon(geometry.Coordinates.Deserialize<List<List<List<double>>>>()!)],
                _geometryFactory),

            "MultiPolygon" => BuildMultiPolygon(
                geometry.Coordinates.Deserialize<List<List<List<List<double>>>>>()!),

            _ => throw new NotSupportedException($"Geometry type '{geometry.Type}' is not supported.")
        };
    }

    private MultiPolygon BuildMultiPolygon(List<List<List<List<double>>>> multiPolygonCoords)
    {
        var polygons = multiPolygonCoords
            .Select(BuildPolygon)
            .ToArray();

        return new MultiPolygon(polygons, _geometryFactory);
    }

    private Polygon BuildPolygon(List<List<List<double>>> polygonCoords)
    {
        if (polygonCoords.Count == 0)
            throw new InvalidOperationException("Polygon has no rings.");

        // First ring = outer shell
        var shell = BuildLinearRing(polygonCoords[0]);

        // Remaining rings = holes
        var holes = polygonCoords
            .Skip(1)
            .Select(BuildLinearRing)
            .ToArray();

        return new Polygon(shell, holes, _geometryFactory);
    }

    private LinearRing BuildLinearRing(List<List<double>> ringCoords)
    {
        var coordinates = ringCoords
            .Select(c => new NetTopologySuite.Geometries.Coordinate(c[0], c[1])) // GeoJSON: [lng, lat]
            .ToList();

        // Ensure ring is closed
        if (coordinates.Count == 0 ||
            !coordinates.First().Equals2D(coordinates.Last()))
        {
            coordinates.Add(new NetTopologySuite.Geometries.Coordinate(coordinates[0].X, coordinates[0].Y));
        }

        return _geometryFactory.CreateLinearRing(coordinates.ToArray());
    }


}
