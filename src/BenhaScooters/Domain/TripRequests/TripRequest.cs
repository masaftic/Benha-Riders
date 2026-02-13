using NetTopologySuite.Geometries;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips.ValueObjects;
using BenhaScooters.Domain.Common;
using BenhaScooters.Domain.Users;
using ErrorOr;
using Thinktecture;
using BenhaScooters.Domain.TripRequests.Enums;
using BenhaScooters.Domain.TripRequests.Events;

namespace BenhaScooters.Domain.TripRequests;

[ValueObject<int>]
public partial struct TripRequestId;

public class TripRequest : AggregateRoot
{
    public TripRequestId Id { get; private set; }
    public UserId RiderId { get; private set; }
    public Point PickupLocation { get; private set; } = null!;
    public Point DropoffLocation { get; private set; } = null!;
    public string? PickupAddress { get; private set; }
    public string? DropoffAddress { get; private set; }
    public DateTime RequestedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public TripRequestStatus Status { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    // Final price (locked at request creation - this becomes the trip's FinalFare)
    public FareEstimate FinalFare { get; private set; } = null!;

    // Final matching result (set by matching system when completed)
    public UserId? MatchedDriverId { get; private set; }

    public DateTime? MatchedAt { get; private set; }
    public string? CancellationReason { get; private set; }

    // Navigation Properties
    public RiderProfile RiderProfile { get; private set; } = null!;
    public DriverProfile? MatchedDriverProfile { get; private set; }



    public Domain.Common.Geo.Coordinate PickupCoordinate => Domain.Common.Geo.Coordinate.FromPoint(PickupLocation);
    public Domain.Common.Geo.Coordinate DropoffCoordinate => Domain.Common.Geo.Coordinate.FromPoint(DropoffLocation);



    private TripRequest() { } // For EF Core

    public TripRequest(UserId riderId, Point pickupLocation, Point dropoffLocation,
        string? pickupAddress, string? dropoffAddress, FareEstimate finalFare)
    {
        RiderId = riderId;
        PickupLocation = pickupLocation;
        DropoffLocation = dropoffLocation;
        PickupAddress = pickupAddress;
        DropoffAddress = dropoffAddress;
        FinalFare = finalFare;
        RequestedAt = DateTime.UtcNow;
        ExpiresAt = DateTime.UtcNow.AddMinutes(10); // 10-minute expiry
        Status = TripRequestStatus.NotConfirmed;
    }

    /// <summary>
    /// Call this method after the entity is saved to the database to publish the domain event
    /// </summary>
    public DomainEvent CreateTripRequestedEvent()
    {
        return new TripRequestedEvent(
            Id,
            RiderId,
            PickupLocation,
            DropoffLocation,
            PickupAddress,
            DropoffAddress,
            FinalFare,
            RequestedAt);
    }

    public DomainEvent CreateTripRequestConfirmedEvent()
    {
        return new TripRequestConfirmedEvent(
            Id,
            RiderId,
            PickupLocation,
            DropoffLocation,
            PickupAddress,
            DropoffAddress,
            FinalFare,
            DateTime.UtcNow);
    }

    public ErrorOr<Success> Confirm()
    {
        if (Status != TripRequestStatus.NotConfirmed)
            return TripErrors.TripRequest.AlreadyConfirmed;

        if (IsExpired)
            return TripErrors.TripRequest.Expired;

        Status = TripRequestStatus.Pending;
        ConfirmedAt = DateTime.UtcNow;

        RaiseDomainEvent(CreateTripRequestConfirmedEvent());

        return Result.Success;
    }

    /// <summary>
    /// Called by the matching system when a driver is successfully matched
    /// </summary>
    public ErrorOr<Success> MarkAsMatched(UserId driverId)
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
        if (Status == TripRequestStatus.Matched)
        {
            return TripErrors.TripRequest.AlreadyMatched;
        }

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