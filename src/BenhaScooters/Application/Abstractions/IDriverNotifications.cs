using BenhaScooters.Application.Abstractions;

namespace BenhaScooters.Application.Abstractions;

public interface IDriverNotifications
{
    Task NotifyRideRequestOffer(string driverId, RideRequestOfferNotification notification);
    Task NotifyRideRequestOfferExpired(string driverId, RideOfferExpiredNotification notification);
    Task NotifyTripCancelled(string driverId, TripCancelledNotification notification);
}

public record RideRequestOfferNotification(
    string DriverMatchAttemptId,
    string RiderName,
    double PickupLatitude,
    double PickupLongitude,
    double DropoffLatitude,
    double DropoffLongitude,
    string? PickupAddress,
    string? DropoffAddress,
    decimal EstimatedFare,
    double EstimatedDistance,
    double DistanceToPickup,
    double EstimatedArrivalTime,
    DateTime OfferedAt,
    DateTime ExpiresAt);

public record RideOfferExpiredNotification(
    string DriverMatchAttemptId,
    DateTime ExpiredAt);
