using NetTopologySuite.Geometries;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips.ValueObjects;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Trips.Events;
using BenhaScooters.Domain.Common;
using ErrorOr;
using Vogen;

namespace BenhaScooters.Domain.Trips;

[ValueObject<int>]
public partial struct TripId;

public class Trip : AggregateRoot
{
    public TripId Id { get; private set; }
    public DriverId DriverId { get; private set; }
    public RiderId RiderId { get; private set; }

    // Trip Details
    public Point PickupLocation { get; private set; } = null!;
    public Point DropoffLocation { get; private set; } = null!;
    public string? PickupAddress { get; private set; }
    public string? DropoffAddress { get; private set; }

    // Trip Status & Timing
    public TripStatus Status { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public DateTime? DriverArrivedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public FareEstimate EstimatedFare { get; private set; } = null!;

    public TripFare? TripFare { get; private set; } // Represents fare details for the trip
    public TripPayment? TripPayment { get; private set; } // Represents payment details for the trip


    // Navigation Properties
    public Driver Driver { get; private set; } = null!;
    public Rider Rider { get; private set; } = null!;


    private Trip() { } // For EF Core

    public Trip(DriverId driverId, RiderId riderId,
        Point pickupLocation, Point dropoffLocation, string? pickupAddress, string? dropoffAddress,
        FareEstimate estimatedFare)
    {
        DriverId = driverId;
        RiderId = riderId;
        PickupLocation = pickupLocation;
        DropoffLocation = dropoffLocation;
        PickupAddress = pickupAddress?.Trim();
        DropoffAddress = dropoffAddress?.Trim();
        EstimatedFare = estimatedFare;
        Status = TripStatus.Assigned;
        AssignedAt = DateTime.UtcNow;
    }


    /// <summary>
    /// Call this method after the entity is saved to the database to publish the domain event
    /// </summary>
    public DomainEvent CreateTripCreatedEvent() => new TripCreatedEvent(Id, DriverId, RiderId);

    public ErrorOr<Success> DriverArrived()
    {
        if (Status != TripStatus.Assigned)
            return TripErrors.Trip.InvalidStatus;

        Status = TripStatus.DriverArrived;
        DriverArrivedAt = DateTime.UtcNow;

        // Publish domain event
        RaiseDomainEvent(new DriverArrivedEvent(Id, DriverId, RiderId));

        return Result.Success;
    }

    public ErrorOr<Success> StartTrip()
    {
        if (Status != TripStatus.DriverArrived)
            return TripErrors.Trip.InvalidStatus;

        Status = TripStatus.InProgress;

        // Publish domain event
        RaiseDomainEvent(new TripStartedEvent(Id, DriverId, RiderId));

        return Result.Success;
    }

    public ErrorOr<Success> CompleteTrip()
    {
        if (Status != TripStatus.InProgress)
            return TripErrors.Trip.InvalidStatus;

        Status = TripStatus.Completed;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TripCompletedEvent(Id, DriverId, RiderId));

        return Result.Success;
    }

    public ErrorOr<Success> SetTripFare(TripFare tripFare)
    {
        if (tripFare == null)
            throw new ArgumentNullException(nameof(tripFare), "Trip fare cannot be null");

        if (Status != TripStatus.Completed)
            return TripErrors.Trip.InvalidStatus;

        if (TripFare != null)
            return TripErrors.Trip.FareAlreadySet;

        TripFare = tripFare;
        return Result.Success;
    }

    public ErrorOr<Success> SetTripPayment(TripPayment payment)
    {
        if (payment == null)
            throw new ArgumentNullException(nameof(payment), "Trip payment cannot be null");

        if (Status != TripStatus.Completed)
            return TripErrors.Trip.InvalidStatus;

        if (TripPayment != null)
            return TripErrors.Trip.PaymentAlreadySet;

        TripPayment = payment;
        return Result.Success;
    }

    // Calculated properties
    public TimeSpan? TotalDuration => CompletedAt - AssignedAt;

    public bool IsActive => Status == TripStatus.InProgress;
    public bool IsCompleted => Status == TripStatus.Completed;
    public bool IsCancelled => Status == TripStatus.Cancelled;
}
