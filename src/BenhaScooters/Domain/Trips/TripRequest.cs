using NetTopologySuite.Geometries;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Trips.ValueObjects;
using BenhaScooters.Domain.Trips.Events;
using BenhaScooters.Domain.Common;
using ErrorOr;
using Vogen;

namespace BenhaScooters.Domain.Trips;

[ValueObject<int>]
public partial struct TripRequestId;

public class TripRequest : AggregateRoot
{
    public TripRequestId Id { get; private set; }
    public RiderId RiderId { get; private set; }
    public Point PickupLocation { get; private set; } = null!;
    public Point DropoffLocation { get; private set; } = null!;
    public string? PickupAddress { get; private set; }
    public string? DropoffAddress { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public TripRequestStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // Pricing
    public FareEstimate EstimatedFare { get; private set; } = null!;

    // Final matching result (set by matching system when completed)
    public DriverId? MatchedDriverId { get; private set; }
    public DateTime? MatchedAt { get; private set; }
    public string? CancellationReason { get; private set; }

    // Navigation Properties
    public Rider Rider { get; private set; } = null!;
    public Driver? MatchedDriver { get; private set; }

    private TripRequest() { } // For EF Core

    public TripRequest(RiderId riderId, Point pickupLocation, Point dropoffLocation,
        string? pickupAddress, string? dropoffAddress, FareEstimate estimatedFare)
    {
        RiderId = riderId;
        PickupLocation = pickupLocation;
        DropoffLocation = dropoffLocation;
        PickupAddress = pickupAddress;
        DropoffAddress = dropoffAddress;
        EstimatedFare = estimatedFare;
        RequestedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddMinutes(10); // 10-minute expiry
        Status = TripRequestStatus.Pending;
    }

    /// <summary>
    /// Call this method after the entity is saved to the database to publish the domain event
    /// </summary>
    public void PublishTripRequestedEvent()
    {
        RaiseDomainEvent(new TripRequestedEvent(
            Id,
            RiderId,
            PickupLocation,
            DropoffLocation,
            PickupAddress,
            DropoffAddress,
            EstimatedFare,
            RequestedAt));
    }

    /// <summary>
    /// Called by the matching system when a driver is successfully matched
    /// </summary>
    public ErrorOr<Success> MarkAsMatched(DriverId driverId)
    {
        if (Status != TripRequestStatus.Pending)
            return TripErrors.TripRequest.NotPending;

        if (IsExpired)
            return TripErrors.TripRequest.Expired;

        MatchedDriverId = driverId;
        MatchedAt = DateTime.UtcNow;
        Status = TripRequestStatus.Matched;

        return Result.Success;
    }

    public ErrorOr<Success> Cancel(string reason)
    {
        if (Status == TripRequestStatus.Cancelled)
        {
            return TripErrors.TripRequest.AlreadyCancelled;
        }

        if (Status == TripRequestStatus.Matched)
        {
            return TripErrors.TripRequest.AlreadyMatched;
        }

        if (Status != TripRequestStatus.Pending)
            return TripErrors.TripRequest.NotPending;

        Status = TripRequestStatus.Cancelled;
        CancellationReason = reason;
        return Result.Success;
    }

    public void Expire()
    {
        if (Status != TripRequestStatus.Pending)
            return;

        Status = TripRequestStatus.Expired;
        CancellationReason = "Request expired";
    }

    // Calculated properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsActive => Status == TripRequestStatus.Pending;
    public bool CanBeAssigned => Status == TripRequestStatus.Pending && !IsExpired;
}