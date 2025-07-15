using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using Vogen;

namespace BenhaScooters.Domain.Drivers;

[ValueObject<int>]
public partial struct DriverAvailabilityId;

public enum DriverStatus
{
    Offline = 1,
    Online = 2,
    OnTrip = 3,
    Busy = 4
}

public class DriverAvailability
{
    public DriverAvailabilityId Id { get; private set; }
    public DriverId DriverId { get; private set; }
    public DriverStatus Status { get; private set; }
    public DateTime LastStatusChange { get; private set; }
    public DateTime LastLocationUpdate { get; private set; }
    
    // Trip context
    public TripId? CurrentTripId { get; private set; }
    
    // Session tracking
    public DateTime? OnlineSessionStart { get; private set; }
    public TimeSpan TotalOnlineTime { get; private set; }

    // Navigation Properties
    public Driver Driver { get; private set; } = null!;

    private DriverAvailability() { } // For EF Core

    public DriverAvailability(DriverId driverId)
    {
        DriverId = driverId;
        Status = DriverStatus.Offline;
        LastStatusChange = DateTime.UtcNow;
        LastLocationUpdate = DateTime.UtcNow;
        TotalOnlineTime = TimeSpan.Zero;
    }

    public void GoOnline()
    {
        if (Status == DriverStatus.Online)
            return;

        Status = DriverStatus.Online;
        LastStatusChange = DateTime.UtcNow;
        OnlineSessionStart = DateTime.UtcNow;
    }

    public void GoOffline()
    {
        if (Status == DriverStatus.Offline)
            return;

        if (Status == DriverStatus.OnTrip)
            throw new InvalidOperationException("Cannot go offline while on a trip");

        // Add this session's time to total
        if (OnlineSessionStart.HasValue)
        {
            TotalOnlineTime += DateTime.UtcNow - OnlineSessionStart.Value;
            OnlineSessionStart = null;
        }

        Status = DriverStatus.Offline;
        LastStatusChange = DateTime.UtcNow;
    }

    public void StartTrip(TripId tripId)
    {
        if (Status != DriverStatus.Online)
            throw new InvalidOperationException($"Cannot start trip when status is {Status}");

        Status = DriverStatus.OnTrip;
        CurrentTripId = tripId;
        LastStatusChange = DateTime.UtcNow;
    }

    public void CompleteTrip()
    {
        if (Status != DriverStatus.OnTrip)
            throw new InvalidOperationException("Driver is not currently on a trip");

        Status = DriverStatus.Online;
        CurrentTripId = null;
        LastStatusChange = DateTime.UtcNow;
    }

    public void SetBusy()
    {
        if (Status == DriverStatus.Offline)
            throw new InvalidOperationException("Cannot set busy status when offline");

        Status = DriverStatus.Busy;
        LastStatusChange = DateTime.UtcNow;
    }

    public void UpdateLocationTimestamp()
    {
        LastLocationUpdate = DateTime.UtcNow;
    }

    // Calculated properties

    public bool IsAvailableForRequests => Status == DriverStatus.Online;
    public bool IsActive => Status != DriverStatus.Offline;
    public TimeSpan TimeSinceLastLocation => DateTime.UtcNow - LastLocationUpdate;
    public bool IsLocationStale => TimeSinceLastLocation > TimeSpan.FromMinutes(5);
}
