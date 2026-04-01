using BenhaScooters.Domain.Common.Geo;

namespace BenhaScooters.Application.Abstractions;

public interface IServiceAreaValidator
{
    Task<bool> IsLocationWithinServiceAreaAsync(
        Coordinate coordinate,
        CancellationToken cancellationToken = default);
    
    Task<bool> AreLocationsWithinServiceAreaAsync(
        Coordinate pickupCoordinate,
        Coordinate dropoffCoordinate,
        CancellationToken cancellationToken = default);
}
