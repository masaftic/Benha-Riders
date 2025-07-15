namespace BenhaScooters.Domain.Trips.Enums;

public enum TripRequestStatus
{
    /// <summary>
    /// Request is waiting for driver assignment
    /// </summary>
    Pending = 1,
    
    /// <summary>
    /// Request has been accepted by a driver and is now in progress
    /// </summary>
    Matched = 2,
    
    /// <summary>
    /// Request was cancelled by rider or system
    /// </summary>
    Cancelled = 3,
    
    /// <summary>
    /// Request expired without being accepted
    /// </summary>
    Expired = 4
}
