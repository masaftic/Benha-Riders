using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Users;

namespace BenhaScooters.Domain.Drivers;

public enum DriverAvailabilityStatus
{
    /// <summary>
    /// Driver does not wish to receive ride offers AFTER they close the app.
    /// </summary>
    Offline = 0,

    /// <summary>
    /// Driver wishes to receive ride offers even if they close the app. 
    /// We will use LastHeartbeat to track active drivers and avoid sending offers to inactive ones.
    /// </summary>
    Online = 1,
    OnTrip = 2
}

/// <summary>
/// Tracks driver availability status. Medium-frequency updates (per session/trip).
/// Uses UserId as primary key.
/// </summary>
public class DriverStatus
{
    public UserId UserId { get; private set; }  // PK & FK
    public DriverAvailabilityStatus Status { get; private set; }

    /// <summary>
    /// Will be used for reliably tracking online drivers (the ones who are actively using the app)
    /// Status.Online is the intent to receive ride offers, even if they close the app
    /// </summary>
    public DateTime? LastHeartbeat { get; private set; } 
    public TripId? CurrentTripId { get; private set; }
    public DateTime LastStatusChange { get; private set; }
    public DateTime? OnlineSessionStart { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    private DriverStatus() { } // For EF Core

    public DriverStatus(UserId userId)
    {
        UserId = userId;
        Status = DriverAvailabilityStatus.Offline;
        LastStatusChange = DateTime.UtcNow;
    }

    public ErrorOr<Success> GoOnline()
    {
        if (Status == DriverAvailabilityStatus.Online)
            return Result.Success;

        if (Status == DriverAvailabilityStatus.OnTrip)
            return AppErrors.Driver.Status.CannotGoOnlineWhileOnTrip();

        Status = DriverAvailabilityStatus.Online;
        LastStatusChange = DateTime.UtcNow;
        OnlineSessionStart = DateTime.UtcNow;
        return Result.Success;
    }

    public ErrorOr<Success> GoOffline()
    {
        if (Status == DriverAvailabilityStatus.Offline)
            return Result.Success;

        if (Status == DriverAvailabilityStatus.OnTrip)
            return AppErrors.Driver.Status.CannotGoOfflineWhileOnTrip();

        Status = DriverAvailabilityStatus.Offline;
        LastStatusChange = DateTime.UtcNow;
        OnlineSessionStart = null;
        return Result.Success;
    }

    public ErrorOr<Success> StartTrip(TripId tripId)
    {
        if (Status == DriverAvailabilityStatus.OnTrip)
            return AppErrors.Driver.Status.AlreadyOnTrip();

        Status = DriverAvailabilityStatus.OnTrip;
        CurrentTripId = tripId;
        LastStatusChange = DateTime.UtcNow;
        return Result.Success;
    }

    public ErrorOr<Success> CompleteTrip()
    {
        if (Status != DriverAvailabilityStatus.OnTrip)
            return AppErrors.Driver.Status.NotOnTrip();

        Status = DriverAvailabilityStatus.Online;
        CurrentTripId = null;
        LastStatusChange = DateTime.UtcNow;
        return Result.Success;
    }

    public void UpdateStatus(DriverAvailabilityStatus newStatus, TripId? tripId = null)
    {
        Status = newStatus;
        CurrentTripId = tripId;
        LastStatusChange = DateTime.UtcNow;

        if (newStatus == DriverAvailabilityStatus.Online && OnlineSessionStart == null)
        {
            OnlineSessionStart = DateTime.UtcNow;
        }
        else if (newStatus != DriverAvailabilityStatus.Online)
        {
            OnlineSessionStart = null;
        }
    }
}
