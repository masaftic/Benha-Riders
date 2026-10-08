using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching.Events;
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
    Cancelled = 4
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
    public string? RejectionReason { get; private set; }
    
    // Matching metrics
    public Distance DistanceToPickup { get; private set; }
    public Duration EstimatedArrivalTime { get; private set; }
    public decimal DriverScore { get; private set; } // Matching algorithm score

    // Navigation Properties
    public DriverProfile DriverProfile { get; private set; } = null!;
    public MatchingSession MatchingSession { get; private set; } = null!;
    
    // Semantic accessor for DriverId
    private DriverMatchAttempt() { } // For EF Core

    public DriverMatchAttempt(UserId driverId, MatchingSessionId matchingSessionId,
        Distance distanceToPickup, Duration estimatedArrivalTime, decimal driverScore, int matchingRound)
    {
        DriverUserId = driverId;
        MatchingSessionId = matchingSessionId;
        DistanceToPickup = distanceToPickup;
        EstimatedArrivalTime = estimatedArrivalTime;
        DriverScore = driverScore;
        Status = MatchAttemptStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        MatchingRound = matchingRound;
    }


    public void Accept()
    {
        if (Status != MatchAttemptStatus.Pending)
            throw new InvalidOperationException($"Cannot accept match when status is {Status}");

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

    public void Cancel()
    {
        if (Status != MatchAttemptStatus.Pending)
            throw new InvalidOperationException($"Cannot cancel match when status is {Status}");

        Status = MatchAttemptStatus.Cancelled;
        RespondedAt = DateTime.UtcNow;
    }
}
