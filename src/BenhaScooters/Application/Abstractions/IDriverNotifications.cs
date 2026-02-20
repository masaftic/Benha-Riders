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
    double EstimatedDistance, // kilometers
    double DistanceToPickup, // kilometers
    double EstimatedArrivalTime, // minutes
    DateTime OfferedAt);

public record RideOfferExpiredNotification(
    string DriverMatchAttemptId,
    DateTime ExpiredAt);
