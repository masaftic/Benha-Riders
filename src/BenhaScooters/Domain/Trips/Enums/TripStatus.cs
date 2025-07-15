namespace BenhaScooters.Domain.Trips.Enums;

public enum TripStatus
{
    /// <summary>
    /// Driver has been assigned to the trip
    /// </summary>
    Assigned = 2,
    
    /// <summary>
    /// Driver has arrived at pickup location
    /// </summary>
    DriverArrived = 3,
    
    /// <summary>
    /// Trip is in progress (rider picked up)
    /// </summary>
    InProgress = 4,
    
    /// <summary>
    /// Trip has been completed successfully
    /// </summary>
    Completed = 5,
    
    /// <summary>
    /// Trip was cancelled by rider, driver, or system
    /// </summary>
    Cancelled = 6
}
