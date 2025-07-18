using NetTopologySuite.Geometries;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips.ValueObjects;
using BenhaScooters.Domain.Trips.Enums;
using BenhaScooters.Domain.Common;
using ErrorOr;
using Vogen;

namespace BenhaScooters.Domain.Trips;

[ValueObject<int>]
public partial struct TripId;

public class Trip
{
    public TripId Id { get; private set; }
    public DriverId DriverId { get; private set; }
    public RiderId RiderId { get; private set; }
    public TripRequestId? TripRequestId { get; private set; }

    // Trip Details
    public Point PickupLocation { get; private set; } = null!;
    public Point DropoffLocation { get; private set; } = null!;
    public string? PickupAddress { get; private set; }
    public string? DropoffAddress { get; private set; }

    // Trip Status & Timing
    public TripStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? DriverArrivedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public FareEstimate EstimatedFare { get; private set; } = null!;

    // public TripRating? Rating { get; private set; }
    public TripRoute? TripRoute { get; private set; } // Represents the route taken during the trip
    public TripFare? TripFare { get; private set; } // Represents fare details for the trip


    // Navigation Properties
    public Driver Driver { get; private set; } = null!;
    public Rider Rider { get; private set; } = null!;


    private Trip() { } // For EF Core

    public Trip(TripRequestId tripRequestId, DriverId driverId, RiderId riderId,
        Point pickupLocation, Point dropoffLocation, string? pickupAddress, string? dropoffAddress,
        FareEstimate estimatedFare)
    {
        TripRequestId = tripRequestId;
        DriverId = driverId;
        RiderId = riderId;
        PickupLocation = pickupLocation;
        DropoffLocation = dropoffLocation;
        PickupAddress = pickupAddress?.Trim();
        DropoffAddress = dropoffAddress?.Trim();
        EstimatedFare = estimatedFare;
        Status = TripStatus.Assigned;
        CreatedAt = DateTime.UtcNow;
    }

    public ErrorOr<Success> DriverArrived()
    {
        if (Status != TripStatus.Assigned)
            return TripErrors.Trip.InvalidStatus;

        Status = TripStatus.DriverArrived;
        DriverArrivedAt = DateTime.UtcNow;
        return Result.Success;
    }

    public ErrorOr<Success> StartTrip()
    {
        if (Status != TripStatus.DriverArrived)
            return TripErrors.Trip.InvalidStatus;

        Status = TripStatus.InProgress;
        StartedAt = DateTime.UtcNow;

        if (TripRoute == null)
        {
            TripRoute = new TripRoute(Id);
        }

        return Result.Success;
    }

    public ErrorOr<Success> CompleteTrip()
    {
        if (Status != TripStatus.InProgress)
            return TripErrors.Trip.InvalidStatus;

        Status = TripStatus.Completed;
        CompletedAt = DateTime.UtcNow;

        TripRoute!.ConstructPath();
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


    // public void AddRating(decimal driverRating, decimal riderRating, string? driverComment, string? riderComment)
    // {
    //     if (Status != TripStatus.Completed)
    //         throw new InvalidOperationException("Can only rate completed trips");

    //     if (Rating != null)
    //         throw new InvalidOperationException("Trip has already been rated");

    //     Rating = new TripRating(driverRating, riderRating, driverComment, riderComment);
    // }

    // Calculated properties
    public TimeSpan? TotalDuration => CompletedAt.HasValue && CreatedAt != default
        ? CompletedAt.Value - CreatedAt
        : null;

    public bool IsActive => Status == TripStatus.InProgress;
    public bool IsCompleted => Status == TripStatus.Completed;
    public bool IsCancelled => Status == TripStatus.Cancelled;
}
