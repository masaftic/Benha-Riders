using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Matching;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Users;
using FluentAssertions;

namespace BenhaScooters.UnitTests.Matching;

public class MatchingUnitTests
{
    private static MatchingSession CreateSession(
        int numberOfRounds,
        params int[] offersPerRound)
    {
        var tripRequestId = TripRequestId.Create(1);
        var result = MatchingSession.Create(tripRequestId, numberOfRounds, offersPerRound.ToList());
        result.IsError.Should().BeFalse("session creation should succeed in tests");
        return result.Value;
    }

    [Fact]
    public void TryTransitionToNextRound_DoesNotAdvance_WhenPendingAttemptsExist()
    {
        // Arrange
        var session = CreateSession(2, 1, 1);
        var driverId = UserId.Create(1);

        var attemptResult = session.CreateDriverMatchAttempt(driverId, 100, 60, 1.0m);
        attemptResult.IsError.Should().BeFalse();

        // Act
        var transitionResult = session.TryTransitionToNextRound();

        // Assert
        transitionResult.IsError.Should().BeFalse();
        transitionResult.Value.Should().BeOfType<NoTransition>();
        session.CurrentRound.Should().Be(1);
        session.Status.Should().Be(MatchingSessionStatus.Active);
    }

    [Fact]
    public void TryTransitionToNextRound_AdvancesRound_WhenAllAttemptsResolved_AndMoreRoundsRemain()
    {
        // Arrange
        var session = CreateSession(2, 1, 1);
        var driverId = UserId.Create(1);

        var attemptResult = session.CreateDriverMatchAttempt(driverId, 100, 60, 1.0m);
        attemptResult.IsError.Should().BeFalse();

        // Resolve attempt as rejected
        var rejectResult = session.RejectMatch(driverId, "not interested");
        rejectResult.IsError.Should().BeFalse();

        // Act
        var transitionResult = session.TryTransitionToNextRound();

        // Assert
        transitionResult.IsError.Should().BeFalse();
        transitionResult.Value.Should().BeOfType<RoundTransitioned>();
        session.CurrentRound.Should().Be(2);
        session.Status.Should().Be(MatchingSessionStatus.Active);
    }

    [Fact]
    public void TryTransitionToNextRound_CancelsSession_WhenLastRoundCompletedWithNoDrivers()
    {
        // Arrange
        var session = CreateSession(1, 1);
        var driverId = UserId.Create(1);

        var attemptResult = session.CreateDriverMatchAttempt(driverId, 100, 60, 1.0m);
        attemptResult.IsError.Should().BeFalse();

        // Resolve attempt as rejected in the only round
        var rejectResult = session.RejectMatch(driverId, "not interested");
        rejectResult.IsError.Should().BeFalse();

        // Act
        var transitionResult = session.TryTransitionToNextRound();

        // Assert
        transitionResult.IsError.Should().BeFalse();
        transitionResult.Value.Should().BeOfType<MatchingCanceled>();
        session.Status.Should().Be(MatchingSessionStatus.Cancelled);
        session.CurrentRound.Should().Be(1);
    }
}
