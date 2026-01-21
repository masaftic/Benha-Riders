namespace BenhaScooters.Domain.TripRequests.Enums;

public enum TripRequestStatus
{
    /// <summary>
    /// Request has been created but not yet confirmed by rider
    /// </summary>
    NotConfirmed,

    /// <summary>
    /// Request is waiting for driver assignment
    /// </summary>
    Pending,
    
    /// <summary>
    /// Request has been accepted by a driver and is now in progress
    /// </summary>
    Matched,
    
    /// <summary>
    /// Request was cancelled by rider or system
    /// </summary>
    Cancelled,
    
    /// <summary>
    /// Request expired without being accepted
    /// </summary>
    Expired
}
