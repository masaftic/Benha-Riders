using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching.Events;
using BenhaScooters.Domain.TripRequests;
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

public enum MatchingPhase
{
    Phase1_Push = 1,        // Phase 1: Push mode with best driver
    Phase2_Broadcast = 2,   // Phase 2: Broadcast mode with N best drivers
    Phase3_Broadcast = 3    // Phase 3: Broadcast mode with next N best drivers
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
    public MatchingPhase CurrentPhase { get; private set; }
    public MatchingSessionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    
    // Matching statistics
    public int TotalAttempts { get; private set; } = 0;
    public int RejectionCount { get; private set; } = 0;
    public int TimeoutCount { get; private set; } = 0;
    public int CurrentPhaseAttempts { get; private set; } = 0;

    private readonly List<DriverMatchAttempt> _matchAttempts = [];
    public IReadOnlyCollection<DriverMatchAttempt> MatchAttempts => _matchAttempts.AsReadOnly();

    // Navigation Properties
    public TripRequest TripRequest { get; private set; } = null!;

    private MatchingSession() { } // For EF Core

    public MatchingSession(TripRequestId tripRequestId)
    {
        TripRequestId = tripRequestId;
        CurrentMode = MatchingMode.Push;
        CurrentPhase = MatchingPhase.Phase1_Push;
        Status = MatchingSessionStatus.Active;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddMinutes(10); // 10-minute session timeout
    }

    public ErrorOr<Success> AdvanceToNextPhase()
    {
        if (Status != MatchingSessionStatus.Active)
            return MatchingErrors.Session.NotActive;

        switch (CurrentPhase)
        {
            case MatchingPhase.Phase1_Push:
                CurrentPhase = MatchingPhase.Phase2_Broadcast;
                CurrentMode = MatchingMode.Broadcast;
                CurrentPhaseAttempts = 0;
                break;
            
            case MatchingPhase.Phase2_Broadcast:
                CurrentPhase = MatchingPhase.Phase3_Broadcast;
                CurrentMode = MatchingMode.Broadcast; // Still broadcast
                CurrentPhaseAttempts = 0;
                break;
            
            case MatchingPhase.Phase3_Broadcast:
                // No more phases, session should be cancelled
                return Error.Validation("NO_MORE_PHASES", "No more matching phases available");
            
            default:
                return Error.Validation("INVALID_PHASE", "Invalid matching phase");
        }

        return Result.Success;
    }

    public bool ShouldAdvanceToNextPhase()
    {
        return CurrentPhase switch
        {
            MatchingPhase.Phase1_Push => true, // Always advance after Phase 1 failure
            MatchingPhase.Phase2_Broadcast => HasAllCurrentPhaseAttemptsFinished(),
            MatchingPhase.Phase3_Broadcast => false, // No next phase
            _ => false
        };
    }

    private bool HasAllCurrentPhaseAttemptsFinished()
    {
        var currentPhaseAttempts = _matchAttempts
            .Where(ma => IsAttemptFromCurrentPhase(ma))
            .ToList();

        if (!currentPhaseAttempts.Any())
            return false;

        // All attempts must be finished (not pending)
        return currentPhaseAttempts.All(ma => ma.Status != MatchAttemptStatus.Pending);
    }

    private bool IsAttemptFromCurrentPhase(DriverMatchAttempt attempt)
    {
        // Simple approach: group attempts by creation time proximity
        // In a more sophisticated implementation, you might track phase explicitly on each attempt
        var phaseStartTime = GetCurrentPhaseStartTime();
        return attempt.CreatedAt >= phaseStartTime;
    }

    private DateTime GetCurrentPhaseStartTime()
    {
        // For simplicity, we'll use the creation time of the most recent mode/phase change
        // In a more robust implementation, you'd track phase transitions explicitly
        return CurrentPhase switch
        {
            MatchingPhase.Phase1_Push => CreatedAt,
            _ => _matchAttempts.LastOrDefault()?.CreatedAt ?? CreatedAt
        };
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
        CurrentPhaseAttempts++;

        // Publish domain event for trip assignment offer
        RaiseDomainEvent(new DriverMatchOfferCreatedEvent(
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
        // Phase advancement is now handled by the service layer
    }

    public void RecordTimeout()
    {
        TimeoutCount++;
        // Phase advancement is now handled by the service layer
    }

    public ErrorOr<Success> AcceptMatch(DriverId driverId)
    {
        if (Status != MatchingSessionStatus.Active)
            return MatchingErrors.Session.NotActive;

        if (IsExpired)
            return MatchingErrors.Session.Expired;

        // Find the pending match attempt for this driver
        var matchAttempt = _matchAttempts.FirstOrDefault(ma => 
            ma.DriverId == driverId && ma.Status == MatchAttemptStatus.Pending);

        if (matchAttempt == null)
            return MatchingErrors.MatchAttempt.NotFound;

        if (matchAttempt.IsExpired)
            return MatchingErrors.MatchAttempt.Expired;

        // Accept the match attempt
        matchAttempt.Accept();

        // Cancel all other pending attempts
        foreach (var otherAttempt in _matchAttempts.Where(ma => 
            ma.Status == MatchAttemptStatus.Pending && ma.DriverId != driverId))
        {
            otherAttempt.Cancel();
        }

        // Complete the session
        var completeResult = Complete();
        if (completeResult.IsError)
            return completeResult.Errors;

        // Raise domain event for successful match
        RaiseDomainEvent(new TripMatchAcceptedEvent(
            TripRequestId,
            driverId,
            matchAttempt.DistanceToPickup,
            matchAttempt.EstimatedArrivalTime,
            DateTime.UtcNow));

        return Result.Success;
    }

    public ErrorOr<Success> RejectMatch(DriverId driverId, string? reason = null)
    {
        if (Status != MatchingSessionStatus.Active)
            return MatchingErrors.Session.NotActive;

        // Find the pending match attempt for this driver
        var matchAttempt = _matchAttempts.FirstOrDefault(ma => 
            ma.DriverId == driverId && ma.Status == MatchAttemptStatus.Pending);

        if (matchAttempt == null)
            return MatchingErrors.MatchAttempt.NotFound;

        // Reject the match attempt
        matchAttempt.Reject(reason);
        RecordRejection();

        // Raise domain event for rejection
        RaiseDomainEvent(new TripMatchRejectedEvent(
            TripRequestId,
            driverId,
            reason,
            DateTime.UtcNow));

        return Result.Success;
    }

    // Calculated properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive => Status == MatchingSessionStatus.Active && !IsExpired;
    public TimeSpan? Duration => CompletedAt.HasValue ? CompletedAt.Value - CreatedAt : null;
}
