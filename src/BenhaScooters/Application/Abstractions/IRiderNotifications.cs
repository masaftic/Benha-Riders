using BenhaScooters.Application.Features.Trips.Queries.Common;

namespace BenhaScooters.Application.Abstractions;

public interface IRiderNotifications
{
    Task NotifyTripAssigned(string riderId, TripAssignedNotification notification);
    Task NotifyDriverArrived(string riderId, DriverArrivedNotification notification);
    Task NotifyTripStarted(string riderId, TripStartedNotification notification);
    Task NotifyTripCompleted(string riderId, TripCompletedNotification notification);
    Task NotifyTripCancelled(string riderId, TripCancelledNotification notification);
    Task NotifyDriverLocationUpdate(string riderId, DriverLocationUpdate locationUpdate);
}

public record TripAssignedNotification(
    int TripId,
    DriverInfo Driver,
    double EstimatedArrivalMinutes,
    DateTime AssignedAt);

public record DriverArrivedNotification(
    int TripId,
    DateTime ArrivedAt);

public record TripStartedNotification(
    int TripId,
    DateTime StartedAt);

public record TripCompletedNotification(
    int TripId,
    DateTime CompletedAt);

public record TripCancelledNotification(
    int TripId,
    string? CancellationReason,
    DateTime CancelledAt);

public record DriverLocationUpdate(
    double Latitude,
    double Longitude,
    DateTime Timestamp,
    double EstimatedArrivalMinutes);
