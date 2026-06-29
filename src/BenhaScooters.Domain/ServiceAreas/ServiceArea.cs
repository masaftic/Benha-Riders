using BenhaScooters.Domain.Common.Geo;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Domain.ServiceAreas;

public class ServiceArea
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public MultiPolygon Area { get; set; } = null!;



    public bool Contains(Point location)
    {
        return Area.Contains(location);
    }

    public bool Intersects(LineString route)
    {
        return Area.Intersects(route);
    }

    public bool Contains(Common.Geo.Coordinate coordinate)
    {
        var geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(GeoConstants.SRID_WGS84);
        var point = coordinate.ToPoint(geometryFactory);
        return Area.Contains(point);
    }
}
