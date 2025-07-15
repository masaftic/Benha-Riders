using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using FastEndpoints;

namespace BenhaScooters.Features.Trips.Events;

public record DriverMatchAttemptCreated(
    TripRequestId TripRequestId,
    DriverId DriverId,
    double Distance,
    double EstimatedTime,
    int Score
) : IEvent;