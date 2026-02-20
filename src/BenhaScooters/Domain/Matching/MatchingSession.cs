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


/*

n rounds
Ni round: make Mi offers to drivers

no phases, just rounds

input
- TripRequestId
- Number of Rounds
- Number of Offers for each round


state
- MatchingSessionId
- TripRequestId
- CurrentRound
- CurrentOffers
- Status (Active, Completed, Cancelled, Expired)


algorithm:
- 

process session
create Mi offers for round Ni


handle timeout
after sending match offers
schedule a HandleTimeOut function that looks at the session
if session is still active and no driver has accepted the match
then advance to next phase
- if current round is the last round, cancel the session, raise SessionCanceledEvent

*/


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
            return Error.Validation(
                code: "MatchingSession.InvalidNumberOfRounds",
                description: "Number of rounds must be greater than zero");

        if (offersPerRound.Count != numberOfRounds)
            return Error.Validation(
                code: "MatchingSession.InvalidOffersPerRound",
                description: "Offers per round count must match number of rounds");

        if (offersPerRound.Any(o => o <= 0))
            return Error.Validation(
                code: "MatchingSession.InvalidOffersPerRoundValue",
                description: "All offers per round values must be greater than zero");

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

        foreach (var attempt in _matchAttempts.Where(ma => ma.Status == MatchAttemptStatus.Pending))
        {
            attempt.Cancel();
        }

        RaiseDomainEvent(new MatchingSessionCancelledEvent(
            Id,
            TripRequestId,
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
            return MatchingErrors.Session.NotActive;

        if (IsExpired)
            return MatchingErrors.Session.Expired;

        var matchAttempt = new DriverMatchAttempt(
            driverId,
            Id,
            distanceToPickup,
            estimatedArrivalTime,
            driverScore,
            CurrentRound);

        _matchAttempts.Add(matchAttempt);

        // Publish domain event for trip assignment offer
        RaiseDomainEvent(new DriverMatchOfferCreatedEvent(
            TripRequestId,
            driverId,
            distanceToPickup,
            estimatedArrivalTime,
            driverScore)); // No expiration time since offers don't expire

        return matchAttempt;
    }


    public bool IsLastRound() => CurrentRound >= NumberOfRounds;
    public bool HasPendingAttemptsInCurrentRound()
    {
        return _matchAttempts.Any(ma =>
            ma.MatchingRound == CurrentRound && ma.Status == MatchAttemptStatus.Pending);
    }


    public ErrorOr<RoundTransitionResult> TryTransitionToNextRound()
    {
        if (Status != MatchingSessionStatus.Active)
            return MatchingErrors.Session.NotActive;

        if (IsExpired)
            return MatchingErrors.Session.Expired;
        
        var isLastRound = IsLastRound();

        if (isLastRound)
        {
            Cancel("لا يوجد سائقون متاحون في الوقت الحالي، يرجى المحاولة لاحقًا");
            return new MatchingCanceled();
        }

        // Advance to next round
        CurrentRound++;
        return new RoundTransitioned();
    }


    


    public ErrorOr<Success> AcceptMatch(UserId driverId)
    {
        if (Status != MatchingSessionStatus.Active)
            return MatchingErrors.Session.NotActive;

        if (IsExpired)
            return MatchingErrors.Session.Expired;

        // Find the pending match attempt for this driver
        var matchAttempt = _matchAttempts.FirstOrDefault(ma =>
            ma.DriverUserId == driverId && ma.Status == MatchAttemptStatus.Pending);

        if (matchAttempt == null)
            return MatchingErrors.MatchAttempt.NotFound;

        // Accept the match attempt
        matchAttempt.Accept();

        // Cancel all other pending attempts
        foreach (var otherAttempt in _matchAttempts.Where(ma =>
            ma.Status == MatchAttemptStatus.Pending && ma.DriverUserId != driverId))
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

    public ErrorOr<Success> RejectMatch(UserId driverId, string? reason = null)
    {
        if (Status != MatchingSessionStatus.Active)
            return MatchingErrors.Session.NotActive;

        // Find the pending match attempt for this driver
        var matchAttempt = _matchAttempts.FirstOrDefault(ma =>
            ma.DriverUserId == driverId && ma.Status == MatchAttemptStatus.Pending);

        if (matchAttempt == null)
            return MatchingErrors.MatchAttempt.NotFound;

        // Reject the match attempt
        matchAttempt.Reject(reason);

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



