using BenhaScooters.Domain.TripRequests;

namespace BenhaScooters.Contracts.TripRequests;

public record RequestTripRequest(
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude,
    string? PickupAddress = null,
    string? DropoffAddress = null);

public record CancelTripRequestRequest(
    TripRequestId TripRequestId,
    string? CancellationReason = null);
