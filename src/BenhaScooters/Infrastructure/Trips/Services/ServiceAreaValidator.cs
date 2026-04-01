using BenhaScooters.Application.Abstractions;
using BenhaScooters.Data;
using BenhaScooters.Domain.Common.Geo;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Infrastructure.Trips.Services;

public class ServiceAreaValidator : IServiceAreaValidator
{
    private readonly GeometryFactory _geometryFactory = NetTopologySuite.NtsGeometryServices.Instance.CreateGeometryFactory(GeoConstants.SRID_WGS84);
    private readonly AppDbContext _dbContext;
    public ServiceAreaValidator(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    public Task<bool> IsLocationWithinServiceAreaAsync(
        Domain.Common.Geo.Coordinate coordinate,
        CancellationToken cancellationToken = default)
    {
        var point = coordinate.ToPoint(_geometryFactory);

        return _dbContext.ServiceAreas
            .AnyAsync(
                sa => sa.Area != null 
                   && sa.Area.Contains(point), 
                cancellationToken);
    }

    public Task<bool> AreLocationsWithinServiceAreaAsync(
        Domain.Common.Geo.Coordinate pickupCoordinate,
        Domain.Common.Geo.Coordinate dropoffCoordinate,
        CancellationToken cancellationToken = default)
    {
        var pickupPoint = pickupCoordinate.ToPoint(_geometryFactory);
        var dropoffPoint = dropoffCoordinate.ToPoint(_geometryFactory);

        return _dbContext.ServiceAreas
            .AnyAsync(
                sa => sa.Area != null
                   && sa.Area.Covers(pickupPoint)
                   && sa.Area.Covers(dropoffPoint),
                cancellationToken);
    }
}
