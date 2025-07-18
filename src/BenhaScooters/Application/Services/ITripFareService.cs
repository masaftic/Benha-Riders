using BenhaScooters.Domain.Trips;

namespace BenhaScooters.Application.Services;

public interface ITripFareService
{
    Task<TripFare> CalculateActualFareAsync(Trip trip, CancellationToken cancellationToken = default);
}