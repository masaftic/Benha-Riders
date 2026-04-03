using BenhaScooters.Domain.Users;

namespace BenhaScooters.Application.Abstractions;

public interface ILocalizedPushNotificationService
{
    Task NotifyDriverApprovedAsync(UserId driverId, CancellationToken cancellationToken = default);

    Task NotifyTripAssignedToRiderAsync(
        UserId riderId,
        string tripId,
        string driverName,
        string vehicleLicensePlate,
        CancellationToken cancellationToken = default);

    Task NotifyTripRequestCanceledAsync(
        UserId riderId,
        string tripRequestId,
        CancellationToken cancellationToken = default);

    Task NotifyRideRequestOfferToDriverAsync(
        UserId driverId,
        string matchAttemptId,
        string riderName,
        decimal estimatedFare,
        string pickupLocation,
        string dropoffLocation,
        string? pickupAddress,
        string? dropoffAddress,
        double distanceToPickupKm,
        double estimatedArrivalMinutes,
        CancellationToken cancellationToken = default);

    Task NotifyDriverArrivedToRiderAsync(
        UserId riderId,
        string tripId,
        CancellationToken cancellationToken = default);

    Task NotifyTripCancelledToRiderAsync(
        UserId riderId,
        string tripId,
        CancellationToken cancellationToken = default);

    Task NotifyTripCancelledToDriverAsync(
        UserId driverId,
        string tripId,
        CancellationToken cancellationToken = default);
}
