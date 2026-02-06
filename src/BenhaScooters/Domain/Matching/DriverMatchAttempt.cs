using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Users;
using Thinktecture;


namespace BenhaScooters.Domain.Matching;

[ValueObject<int>]
public partial struct DriverMatchAttemptId;

public enum MatchAttemptStatus
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Expired = 4,
    Cancelled = 5
}

public class DriverMatchAttempt
{
    public DriverMatchAttemptId Id { get; private set; }
    public MatchingSessionId MatchingSessionId { get; private set; }
    public UserId DriverUserId { get; private set; }  // FK to DriverProfile.UserId
    public MatchAttemptStatus Status { get; private set; }

    /// <summary>
    /// The matching round in which this attempt was made
    /// </summary>
    public int MatchingRound { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? RespondedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }
    public string? RejectionReason { get; private set; }
    
    // Matching metrics
    public double DistanceToPickup { get; private set; } // meters
    public double EstimatedArrivalTime { get; private set; } // minutes
    public decimal DriverScore { get; private set; } // Matching algorithm score

    // Navigation Properties
    public DriverProfile DriverProfile { get; private set; } = null!;
    public MatchingSession MatchingSession { get; private set; } = null!;
    
    // Semantic accessor for DriverId
    private DriverMatchAttempt() { } // For EF Core

    public DriverMatchAttempt(UserId driverId, MatchingSessionId matchingSessionId,
        double distanceToPickup, double estimatedArrivalTime, decimal driverScore, int matchingRound)
    {
        DriverUserId = driverId;
        MatchingSessionId = matchingSessionId;
        DistanceToPickup = distanceToPickup;
        EstimatedArrivalTime = estimatedArrivalTime;
        DriverScore = driverScore;
        Status = MatchAttemptStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddSeconds(30); // 30-second response window
        MatchingRound = matchingRound;
    }


    public void Accept()
    {
        if (Status != MatchAttemptStatus.Pending)
            throw new InvalidOperationException($"Cannot accept match when status is {Status}");

        if (IsExpired)
            throw new InvalidOperationException("Cannot accept expired match attempt");

        Status = MatchAttemptStatus.Accepted;
        RespondedAt = DateTime.UtcNow;
    }

    public void Reject(string? reason = null)
    {
        if (Status != MatchAttemptStatus.Pending)
            throw new InvalidOperationException($"Cannot reject match when status is {Status}");

        Status = MatchAttemptStatus.Rejected;
        RespondedAt = DateTime.UtcNow;
        RejectionReason = reason;
    }

    public void Expire()
    {
        if (Status != MatchAttemptStatus.Pending)
            return;

        Status = MatchAttemptStatus.Expired;
        RespondedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != MatchAttemptStatus.Pending)
            throw new InvalidOperationException($"Cannot cancel match when status is {Status}");

        Status = MatchAttemptStatus.Cancelled;
        RespondedAt = DateTime.UtcNow;
    }

    // Calculated properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsPending => Status == MatchAttemptStatus.Pending && !IsExpired;
    public TimeSpan? ResponseTime => RespondedAt.HasValue ? RespondedAt.Value - CreatedAt : null;
}
