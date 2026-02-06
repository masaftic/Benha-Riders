namespace BenhaScooters.Application.Abstractions;

public interface IRiderNotifications
{
    Task NotifyTripAssignedAsync(string riderId, TripAssignedNotification notification);
    Task NotifyDriverArrivedAsync(string riderId, DriverArrivedNotification notification);
    Task NotifyTripStartedAsync(string riderId, TripStartedNotification notification);
    Task NotifyTripCompletedAsync(string riderId, TripCompletedNotification notification);
    Task NotifyDriverLocationUpdateAsync(string riderId, DriverLocationUpdate locationUpdate);
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
