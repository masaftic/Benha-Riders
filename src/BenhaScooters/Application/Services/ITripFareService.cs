using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.ValueObjects;

namespace BenhaScooters.Application.Services;

public interface ITripFareService
{
    Task<TripFare> CalculateActualFareAsync(Trip trip, TripRoute route, CancellationToken cancellationToken = default);
}