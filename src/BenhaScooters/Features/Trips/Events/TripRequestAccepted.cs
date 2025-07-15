using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using FastEndpoints;

namespace BenhaScooters.Features.Trips.Events;

public record TripRequestAccepted(
    TripRequestId TripRequestId,
    DriverId DriverId,
    DateTime AcceptedAt
) : IEvent;
