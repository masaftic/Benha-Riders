using NetTopologySuite.Geometries;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Trips.ValueObjects;
using BenhaScooters.Domain.Common;
using ErrorOr;
using Vogen;

namespace BenhaScooters.Domain.Trips;

[ValueObject<int>]
public partial struct TripRequestId;

public class TripRequest
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

    // Matching
    public DriverId? AssignedDriverId { get; private set; }
    public DateTime? AssignedAt { get; private set; }
    public int AttemptCount { get; private set; } = 0; // Number of times drivers rejected this request
    public string? CancellationReason { get; private set; }

    // Navigation Properties
    public Rider Rider { get; private set; } = null!;
    public Driver? AssignedDriver { get; private set; }

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

    public ErrorOr<Success> AcceptByDriver(DriverId driverId)
    {
        if (Status != TripRequestStatus.Pending)
            return TripErrors.TripRequest.NotPending;

        if (IsExpired)
            return TripErrors.TripRequest.Expired;

        AssignedDriverId = driverId;
        AssignedAt = DateTime.UtcNow;
        Status = TripRequestStatus.Matched;
        return Result.Success;
    }

    public ErrorOr<Success> Reject(string? reason = null)
    {
        if (Status != TripRequestStatus.Pending)
            return TripErrors.TripRequest.NotPending;

        Status = TripRequestStatus.Pending;
        AssignedDriverId = null;
        AssignedAt = null;
        AttemptCount++;

        if (AttemptCount >= 3) // After 3 rejections, cancel the request
        {
            var cancelResult = Cancel("Too many driver rejections");
            if (cancelResult.IsError)
                return cancelResult.FirstError;
        }

        return Result.Success;
    }

    public ErrorOr<Success> Cancel(string reason)
    {
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