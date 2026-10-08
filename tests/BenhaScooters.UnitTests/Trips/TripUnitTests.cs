using BenhaScooters.Domain.Common.Geo;
using BenhaScooters.Domain.TripRequests;
using BenhaScooters.Domain.Trips;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Trips.ValueObjects;
using BenhaScooters.Domain.Users;
using FluentAssertions;
using NetTopologySuite.Geometries;

namespace BenhaScooters.UnitTests.Trips;

public class TripUnitTests
{
    private static Trip CreateTrip()
    {
        var driverId = UserId.Create(1);
        var riderId = UserId.Create(2);
        var tripRequestId = TripRequestId.Create(100);
        var fare = FareEstimate.Create(30m, Distance.FromKilometers(3), Duration.FromMinutes(8));

        return new Trip(
            driverId,
            riderId,
            tripRequestId,
            new Point(31.18463, 30.46629),
            new Point(31.17891, 30.46983),
            "Benha Station",
            "Benha University",
            fare);
    }

    [Fact]
    public void NewTrip_StartsWithAssignedStatus()
    {
        var trip = CreateTrip();

        trip.Status.Should().Be(TripStatus.Assigned);
        trip.DriverArrivedAt.Should().BeNull();
        trip.StartedAt.Should().BeNull();
        trip.CompletedAt.Should().BeNull();
        trip.IsActive.Should().BeFalse();
        trip.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public void DriverArrived_TransitionsStatusToDriverArrived()
    {
        var trip = CreateTrip();

        var result = trip.DriverArrived();

        result.IsError.Should().BeFalse();
        trip.Status.Should().Be(TripStatus.DriverArrived);
        trip.DriverArrivedAt.Should().NotBeNull();
    }

    [Fact]
    public void StartTrip_Fails_IfDriverHasNotArrived()
    {
        var trip = CreateTrip();

        // Still in Assigned status
        var result = trip.StartTrip();

        result.IsError.Should().BeTrue("driver cannot start a trip before marking arrival at pickup");
        trip.Status.Should().Be(TripStatus.Assigned);
        trip.StartedAt.Should().BeNull();
    }

    [Fact]
    public void StartTrip_TransitionsStatusToInProgress_WhenDriverArrived()
    {
        var trip = CreateTrip();
        trip.DriverArrived();

        var result = trip.StartTrip();

        result.IsError.Should().BeFalse();
        trip.Status.Should().Be(TripStatus.InProgress);
        trip.StartedAt.Should().NotBeNull();
        trip.IsActive.Should().BeTrue();
    }

    [Fact]
    public void CompleteTrip_TransitionsStatusToCompleted_WhenInProgress()
    {
        var trip = CreateTrip();
        trip.DriverArrived();
        trip.StartTrip();

        var result = trip.CompleteTrip();

        result.IsError.Should().BeFalse();
        trip.Status.Should().Be(TripStatus.Completed);
        trip.CompletedAt.Should().NotBeNull();
        trip.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public void CompleteTrip_Fails_IfNotInProgress()
    {
        var trip = CreateTrip();
        trip.DriverArrived();

        var result = trip.CompleteTrip();

        result.IsError.Should().BeTrue("cannot complete trip before starting it");
        trip.Status.Should().Be(TripStatus.DriverArrived);
    }

    [Fact]
    public void CancelTrip_Succeeds_BeforeTripStarts()
    {
        var trip = CreateTrip();

        var result = trip.CancelTrip(UserId.Create(2), "Changed mind");

        result.IsError.Should().BeFalse();
        trip.Status.Should().Be(TripStatus.Cancelled);
        trip.IsCancelled.Should().BeTrue();
    }

    [Fact]
    public void CancelTrip_Fails_OnceTripIsInProgress()
    {
        var trip = CreateTrip();
        trip.DriverArrived();
        trip.StartTrip();

        var result = trip.CancelTrip(UserId.Create(2), "Want to cancel");

        result.IsError.Should().BeTrue("in-progress trip cannot be cancelled through regular flow");
        trip.Status.Should().Be(TripStatus.InProgress);
    }
}
