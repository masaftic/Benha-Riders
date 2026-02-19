using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.TripRequests;

namespace BenhaScooters.Contracts.TripRequests;

public record RequestTripRequest(
    Latitude PickupLatitude,
    Longitude PickupLongitude,
    Latitude DropoffLatitude,
    Longitude DropoffLongitude,
    string? DropoffAddress = null);

public record CancelTripRequestRequest(
    TripRequestId TripRequestId,
    string? CancellationReason = null);
