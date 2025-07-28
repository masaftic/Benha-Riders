using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Common;
using ErrorOr;
using Vogen;

namespace BenhaScooters.Domain.Drivers;

[ValueObject<int>]
public partial struct DriverAvailabilityId;

public enum DriverStatus
{
    Offline,
    Online,
    OnTrip,
    Busy
}

// Separate aggregate for hot path operations with minimal invariants
public class DriverAvailability
{
    public DriverAvailabilityId Id { get; private set; }
    public DriverId DriverId { get; private set; }
    public DriverStatus Status { get; private set; }
    public DateTime LastStatusChange { get; private set; }
    
    // Trip context
    public TripId? CurrentTripId { get; private set; }
    
    // Session tracking
    public DateTime? OnlineSessionStart { get; private set; }
    public TimeSpan TotalOnlineTime { get; private set; }

    private DriverAvailability() { } // For EF Core

    public DriverAvailability(DriverId driverId)
    {
        DriverId = driverId;
        Status = DriverStatus.Offline;
        LastStatusChange = DateTime.UtcNow;
        TotalOnlineTime = TimeSpan.Zero;
    }

    public ErrorOr<Success> GoOnline()
    {
        if (Status == DriverStatus.Online)
            return Result.Success;

        Status = DriverStatus.Online;
        LastStatusChange = DateTime.UtcNow;
        OnlineSessionStart = DateTime.UtcNow;
        return Result.Success;
    }

    public ErrorOr<Success> GoOffline()
    {
        if (Status == DriverStatus.Offline)
            return Result.Success;

        if (Status == DriverStatus.OnTrip)
            return DriverErrors.Availability.InvalidStatus;

        // Add this session's time to total
        if (OnlineSessionStart.HasValue)
        {
            TotalOnlineTime += DateTime.UtcNow - OnlineSessionStart.Value;
            OnlineSessionStart = null;
        }

        Status = DriverStatus.Offline;
        LastStatusChange = DateTime.UtcNow;
        return Result.Success;
    }

    public ErrorOr<Success> StartTrip(TripId tripId)
    {
        if (Status != DriverStatus.Online)
            return DriverErrors.Availability.InvalidStatus;

        Status = DriverStatus.OnTrip;
        CurrentTripId = tripId;
        LastStatusChange = DateTime.UtcNow;
        return Result.Success;
    }

    public ErrorOr<Success> CompleteTrip()
    {
        if (Status != DriverStatus.OnTrip)
            return DriverErrors.Availability.InvalidStatus;

        Status = DriverStatus.Online;
        CurrentTripId = null;
        LastStatusChange = DateTime.UtcNow;
        return Result.Success;
    }

    public ErrorOr<Success> SetBusy()
    {
        if (Status == DriverStatus.Offline)
            return DriverErrors.Availability.InvalidStatus;

        Status = DriverStatus.Busy;
        LastStatusChange = DateTime.UtcNow;
        return Result.Success;
    }
}
