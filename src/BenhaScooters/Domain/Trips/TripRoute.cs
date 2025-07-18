using NetTopologySuite;
using NetTopologySuite.Geometries;
using Vogen;

namespace BenhaScooters.Domain.Trips;


[ValueObject<int>]
public partial struct TripRouteId;


public class TripRoute
{
    public TripRouteId Id { get; private set; }
    public TripId TripId { get; private set; }
    public LineString Path { get; private set; } = null!; // Represents the route as a line string
    private readonly List<TripGpsPoint> tripGpsPoints = [];
    public IReadOnlyList<TripGpsPoint> TripGpsPoints => tripGpsPoints.AsReadOnly();

    // Navigation properties
    public Trip Trip { get; private set; } = null!;

    private TripRoute() { } // For EF Core

    public TripRoute(TripId tripId)
    {
        TripId = tripId;

        var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
        Path = geometryFactory.CreateLineString(Array.Empty<Coordinate>());
    }

    public void AddPoint(TripGpsPoint tripGpsPoint)
    {
        tripGpsPoints.Add(tripGpsPoint);
    }

    public void ConstructPath()
    {
        var coordinates = tripGpsPoints.Select(gp => gp.Location.Coordinate).ToArray();
        Path = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326).CreateLineString(coordinates);
    }
}
