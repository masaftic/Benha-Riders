using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Events;
using ErrorOr;
using Vogen;

namespace BenhaScooters.Domain.Matching;

[ValueObject<int>]
public partial struct MatchingSessionId;

public enum MatchingMode
{
    Push = 1,    // Send offer to one driver at a time
    Broadcast = 2 // Send offer to multiple drivers simultaneously
}

public enum MatchingSessionStatus
{
    Active = 1,
    Completed = 2,
    Cancelled = 3,
    Expired = 4
}

public class MatchingSession : AggregateRoot
{
    public MatchingSessionId Id { get; private set; }
    public TripRequestId TripRequestId { get; private set; }
    public MatchingMode CurrentMode { get; private set; }
    public MatchingSessionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    
    // Matching statistics
    public int TotalAttempts { get; private set; } = 0;
    public int RejectionCount { get; private set; } = 0;
    public int TimeoutCount { get; private set; } = 0;

    private readonly List<DriverMatchAttempt> _matchAttempts = [];

    // Navigation Properties
    public TripRequest TripRequest { get; private set; } = null!;
    public IReadOnlyCollection<DriverMatchAttempt> MatchAttempts => _matchAttempts.AsReadOnly();

    private MatchingSession() { } // For EF Core

    public MatchingSession(TripRequestId tripRequestId)
    {
        TripRequestId = tripRequestId;
        CurrentMode = MatchingMode.Push;
        Status = MatchingSessionStatus.Active;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddMinutes(10); // 10-minute session timeout
    }

    public ErrorOr<Success> SwitchToBroadcastMode()
    {
        if (Status != MatchingSessionStatus.Active)
            return MatchingErrors.Session.NotActive;

        if (CurrentMode == MatchingMode.Broadcast)
            return Result.Success; // Already in broadcast mode

        CurrentMode = MatchingMode.Broadcast;
        return Result.Success;
    }

    public ErrorOr<Success> Complete()
    {
        if (Status != MatchingSessionStatus.Active)
            return MatchingErrors.Session.NotActive;

        Status = MatchingSessionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        return Result.Success;
    }

    public ErrorOr<Success> Cancel(string reason)
    {
        if (Status != MatchingSessionStatus.Active)
            return MatchingErrors.Session.NotActive;

        Status = MatchingSessionStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        return Result.Success;
    }

    public void Expire()
    {
        if (Status != MatchingSessionStatus.Active)
            return;

        Status = MatchingSessionStatus.Expired;
        CompletedAt = DateTime.UtcNow;
    }

    public ErrorOr<DriverMatchAttempt> CreateDriverMatchAttempt(
        DriverId driverId,
        double distanceToPickup,
        double estimatedArrivalTime,
        decimal driverScore)
    {
        if (Status != MatchingSessionStatus.Active)
            return MatchingErrors.Session.NotActive;

        if (IsExpired)
            return MatchingErrors.Session.Expired;

        var matchAttempt = new DriverMatchAttempt(
            TripRequestId,
            driverId,
            Id,
            distanceToPickup,
            estimatedArrivalTime,
            driverScore);

        _matchAttempts.Add(matchAttempt);
        TotalAttempts++;

        // Publish domain event for trip assignment offer
        RaiseDomainEvent(new TripAssignmentOfferCreatedEvent(
            TripRequestId,
            driverId,
            distanceToPickup,
            estimatedArrivalTime,
            driverScore,
            matchAttempt.ExpiresAt));

        return matchAttempt;
    }

    public void RecordRejection()
    {
        RejectionCount++;
        
        // Switch to broadcast mode after 2 rejections in push mode
        if (CurrentMode == MatchingMode.Push && RejectionCount >= 2)
        {
            SwitchToBroadcastMode();
        }
    }

    public void RecordTimeout()
    {
        TimeoutCount++;
        
        // Switch to broadcast mode after 1 timeout in push mode
        if (CurrentMode == MatchingMode.Push && TimeoutCount >= 1)
        {
            SwitchToBroadcastMode();
        }
    }

    // Calculated properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive => Status == MatchingSessionStatus.Active && !IsExpired;
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - CreatedAt : null;
}
