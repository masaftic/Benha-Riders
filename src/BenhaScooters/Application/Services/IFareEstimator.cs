using BenhaScooters.Domain.Trips.ValueObjects;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Application.Services;

public interface IFareEstimator
{
    Task<FareEstimate> EstimateFareAsync(Point pickup, Point dropoff, CancellationToken ct = default);
}

