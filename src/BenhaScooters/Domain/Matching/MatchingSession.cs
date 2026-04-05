using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching.Events;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Events;
using BenhaScooters.Domain.Users;
using ErrorOr;
using Thinktecture;


namespace BenhaScooters.Domain.Matching;



[ValueObject<int>]
public partial struct MatchingSessionId;


public enum MatchingSessionStatus
{
    Active = 1,
    Completed = 2,
    Cancelled = 3
}

public class MatchingSession : AggregateRoot
{
    public MatchingSessionId Id { get; private set; }
    public TripRequestId TripRequestId { get; private set; }

    public int NumberOfRounds { get; private set; }
    public IReadOnlyList<int> OffersPerRound { get; private set; } = [];
    public int CurrentRound { get; private set; } = 1;

    public int CurrentOffer => OffersPerRound[CurrentRound - 1];

    public MatchingSessionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime ExpiresAt { get; private set; }


    private readonly List<DriverMatchAttempt> _matchAttempts = [];
    public IReadOnlyList<DriverMatchAttempt> MatchAttempts => _matchAttempts.AsReadOnly();

    // Navigation Properties
    public TripRequest TripRequest { get; private set; } = null!;

    private MatchingSession() { } // For EF Core


    public static ErrorOr<MatchingSession> Create(TripRequestId tripRequestId, int numberOfRounds, List<int> offersPerRound)
    {
        if (numberOfRounds <= 0)
            return AppErrors.Matching.Session.InvalidNumberOfRounds();

        if (offersPerRound.Count != numberOfRounds)
            return AppErrors.Matching.Session.InvalidOffersPerRoundCount();

        if (offersPerRound.Any(o => o <= 0))
            return AppErrors.Matching.Session.InvalidOffersPerRoundValue();

        var session = new MatchingSession(tripRequestId, numberOfRounds, offersPerRound);
        return session;
    }


    private MatchingSession(TripRequestId tripRequestId, int numberOfRounds, List<int> offersPerRound)
    {
        TripRequestId = tripRequestId;

        NumberOfRounds = numberOfRounds;
        OffersPerRound = offersPerRound;

        Status = MatchingSessionStatus.Active;
        CreatedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddMinutes(10); // 10-minute session timeout
    }


    public List<UserId> GetRejectedOrPendingDrivers()
    {
        return _matchAttempts
            .Where(ma => ma.Status == MatchAttemptStatus.Rejected || ma.Status == MatchAttemptStatus.Pending)
            .Select(ma => ma.DriverUserId)
            .Distinct()
            .ToList();
    }


    public ErrorOr<Success> Complete()
    {
        if (Status != MatchingSessionStatus.Active)
            return AppErrors.Matching.Session.NotActive();

        Status = MatchingSessionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        return Result.Success;
    }

    public ErrorOr<Success> Cancel(bool isCanceledByUser, string reason)
    {
        if (Status != MatchingSessionStatus.Active)
            return AppErrors.Matching.Session.NotActive();

        Status = MatchingSessionStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;

        foreach (var attempt in _matchAttempts.Where(ma => ma.Status == MatchAttemptStatus.Pending))
        {
            attempt.Cancel();
            RaiseDomainEvent(new MatchAttemptCancelledEvent(
                attempt.Id,
                attempt.DriverUserId,
                Id,
                DateTime.UtcNow));
        }

        RaiseDomainEvent(new MatchingSessionCancelledEvent(
            Id,
            TripRequestId,
            isCanceledByUser,
            reason));

        return Result.Success;
    }

    public ErrorOr<DriverMatchAttempt> CreateDriverMatchAttempt(
        UserId driverId,
        Distance distanceToPickup,
        Duration estimatedArrivalTime,
        decimal driverScore)
    {
        if (Status != MatchingSessionStatus.Active)
            return AppErrors.Matching.Session.NotActive();

        if (IsExpired)
            return AppErrors.Matching.Session.Expired();

        var matchAttempt = new DriverMatchAttempt(
            driverId,
            Id,
            distanceToPickup,
            estimatedArrivalTime,
            driverScore,
            CurrentRound);

        _matchAttempts.Add(matchAttempt);

        return matchAttempt;
    }


    public bool IsLastRound() => CurrentRound >= NumberOfRounds;


    public ErrorOr<RoundTransitionResult> TryTransitionToNextRound()
    {
        if (Status != MatchingSessionStatus.Active)
            return AppErrors.Matching.Session.NotActive();

        if (IsExpired)
            return AppErrors.Matching.Session.Expired();
        
        if (MatchAttempts.Any(ma => ma.Status == MatchAttemptStatus.Accepted))
            return new MatchingCompleted();

        if (IsLastRound())
        {
            Cancel(false, "No match found after all rounds completed");
            return new MatchingCanceled();
        }

        // Advance to next round
        CurrentRound++;
        return new RoundTransitioned();
    }





    public ErrorOr<Success> AcceptMatch(UserId driverId)
    {
        if (Status != MatchingSessionStatus.Active)
            return AppErrors.Matching.Session.NotActive();

        if (IsExpired)
            return AppErrors.Matching.Session.Expired();

        // Find the pending match attempt for this driver
        var matchAttempt = _matchAttempts.FirstOrDefault(ma =>
            ma.DriverUserId == driverId && ma.Status == MatchAttemptStatus.Pending);

        if (matchAttempt == null)
            return AppErrors.Matching.Attempt.NotFound();

        // Accept the match attempt
        matchAttempt.Accept();

        // Cancel all other pending attempts
        foreach (var otherAttempt in _matchAttempts.Where(ma =>
            ma.Status == MatchAttemptStatus.Pending && ma.DriverUserId != driverId))
        {
            otherAttempt.Cancel();
            RaiseDomainEvent(new MatchAttemptCancelledEvent(
                otherAttempt.Id,
                otherAttempt.DriverUserId,
                Id,
                DateTime.UtcNow));
        }

        // Complete the session
        var completeResult = Complete();
        if (completeResult.IsError)
            return completeResult.Errors;

        // Raise domain event for successful match
        RaiseDomainEvent(new MatchAttemptAcceptedEvent(
            TripRequestId,
            driverId,
            matchAttempt.DistanceToPickup,
            matchAttempt.EstimatedArrivalTime,
            DateTime.UtcNow));

        return Result.Success;
    }

    public ErrorOr<Success> RejectMatch(UserId driverId, string? reason = null)
    {
        if (Status != MatchingSessionStatus.Active)
            return AppErrors.Matching.Session.NotActive();

        // Find the pending match attempt for this driver
        var matchAttempt = _matchAttempts.FirstOrDefault(ma =>
            ma.DriverUserId == driverId && ma.Status == MatchAttemptStatus.Pending);

        if (matchAttempt == null)
            return AppErrors.Matching.Attempt.NotFound();

        // Reject the match attempt
        matchAttempt.Reject(reason);

        // Raise domain event for rejection
        RaiseDomainEvent(new MatchAttemptRejectedEvent(
            TripRequestId,
            driverId,
            reason,
            DateTime.UtcNow));

        return Result.Success;
    }

    // Calculated properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive => Status == MatchingSessionStatus.Active;
}
