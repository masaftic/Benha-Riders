using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips;
using FastEndpoints;

namespace BenhaScooters.Features.Trips.Events;

public record TripRequested(TripRequestId TripRequestId, RiderId RiderId) : IEvent;
