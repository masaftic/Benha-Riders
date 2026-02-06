namespace BenhaScooters.Application.Abstractions;

public interface IRiderNotifications
{
    Task NotifyTripAssigned(string riderId, TripAssignedNotification notification);
    Task NotifyDriverArrived(string riderId, DriverArrivedNotification notification);
    Task NotifyTripStarted(string riderId, TripStartedNotification notification);
    Task NotifyTripCompleted(string riderId, TripCompletedNotification notification);
    Task NotifyDriverLocationUpdate(string riderId, DriverLocationUpdate locationUpdate);
}

public record TripAssignedNotification(
    int TripId,
    string DriverName,
    string? DriverPhotoUrl,
    string? VehicleModel,
    string? VehiclePlateNumber,
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

public record DriverLocationUpdate(
    double Latitude,
    double Longitude,
    DateTime Timestamp,
    double EstimatedArrivalMinutes);
