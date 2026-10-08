using NetTopologySuite.Geometries;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips.ValueObjects;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Trips.Events;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using ErrorOr;
using Thinktecture;
using BenhaScooters.Domain.TripRequests;


namespace BenhaScooters.Domain.Trips;

[ValueObject<int>]
public partial struct TripId;

public class Trip : AggregateRoot
{
    public TripId Id { get; private set; }

    public UserId DriverId { get; private set; }
    public UserId RiderId { get; private set; }

    public TripRequestId? TripRequestId { get; private set; }

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
    

    // Final fare (locked at trip creation)
    public FareEstimate FinalFare { get; private set; } = null!;

    public TripPayment? TripPayment { get; private set; } // Payment details




    // Navigation Properties (to profiles, not old aggregates)
    public DriverProfile DriverProfile { get; private set; } = null!;
    public RiderProfile RiderProfile { get; private set; } = null!;
    public TripRequest TripRequest { get; private set; } = null!; 


    private Trip() { } // For EF Core

    public Trip(UserId driverId, UserId riderId, TripRequestId tripRequestId,
        Point pickupLocation, Point dropoffLocation, string? pickupAddress, string? dropoffAddress,
        FareEstimate finalFare)
    {
        DriverId = driverId;
        RiderId = riderId;
        TripRequestId = tripRequestId;
        PickupLocation = pickupLocation;
        DropoffLocation = dropoffLocation;
        PickupAddress = pickupAddress?.Trim();
        DropoffAddress = dropoffAddress?.Trim();
        FinalFare = finalFare;
        Status = TripStatus.Assigned;
        AssignedAt = DateTime.UtcNow;
    }

    public Trip(UserId driverId, TripRequest tripRequest)
    {
        DriverId = driverId;
        RiderId = tripRequest.RiderId;
        TripRequest = tripRequest;
        TripRequestId = tripRequest.Id;
        PickupLocation = tripRequest.PickupLocation;
        DropoffLocation = tripRequest.DropoffLocation;
        PickupAddress = tripRequest.PickupAddress?.Trim();
        DropoffAddress = tripRequest.DropoffAddress?.Trim();
        FinalFare = tripRequest.FinalFare;
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
            return AppErrors.Trip.InvalidStatus();

        Status = TripStatus.DriverArrived;
        DriverArrivedAt = DateTime.UtcNow;

        // Publish domain event
        RaiseDomainEvent(new DriverArrivedEvent(Id, DriverId, RiderId));

        return Result.Success;
    }

    public ErrorOr<Success> StartTrip()
    {
        if (Status != TripStatus.DriverArrived)
            return AppErrors.Trip.InvalidStatus();

        Status = TripStatus.InProgress;
        StartedAt = DateTime.UtcNow;

        // Publish domain event
        RaiseDomainEvent(new TripStartedEvent(Id, DriverId, RiderId));

        return Result.Success;
    }

    public ErrorOr<Success> CompleteTrip()
    {
        if (Status != TripStatus.InProgress)
            return AppErrors.Trip.InvalidStatus();

        Status = TripStatus.Completed;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TripCompletedEvent(Id, DriverId, RiderId));

        return Result.Success;
    }

    public ErrorOr<Success> CancelTrip(UserId cancelledBy, string cancellationReason)
    {
        // Can only cancel if trip hasn't started yet
        if (Status == TripStatus.InProgress || Status == TripStatus.Completed || Status == TripStatus.Cancelled)
            return AppErrors.Trip.CannotCancel();

        Status = TripStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;

        RaiseDomainEvent(new TripCancelledEvent(Id, DriverId, RiderId, cancelledBy, cancellationReason));

        return Result.Success;
    }

    /// <summary>
    /// Forces a trip to be cancelled regardless of its current state (except if already completed/cancelled).
    /// Used for cleaning up hung trips.
    /// </summary>
    public ErrorOr<Success> ForceCancel(string cancellationReason)
    {
        if (Status == TripStatus.Completed || Status == TripStatus.Cancelled)
            return AppErrors.Trip.CannotCancel();

        Status = TripStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;

        // System user ID (0)
        RaiseDomainEvent(new TripCancelledEvent(Id, DriverId, RiderId, UserId.Create(0), cancellationReason));

        return Result.Success;
    }

    public ErrorOr<Success> SetTripPayment(TripPayment payment)
    {
        if (payment == null)
            throw new ArgumentNullException(nameof(payment), "Trip payment cannot be null");

        if (Status != TripStatus.Completed)
            return AppErrors.Trip.InvalidStatus();

        if (TripPayment != null)
            return AppErrors.Trip.PaymentAlreadySet();
        
        TripPayment = payment;
        return Result.Success;
    }

    // Calculated properties
    public TimeSpan? TotalDuration => CompletedAt - AssignedAt;

    public bool IsActive => Status == TripStatus.InProgress;
    public bool IsCompleted => Status == TripStatus.Completed;
    public bool IsCancelled => Status == TripStatus.Cancelled;
}
