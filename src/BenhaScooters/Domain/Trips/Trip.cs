using NetTopologySuite.Geometries;
using BenhaScooters.Domain.Drivers;
using BenhaScooters.Domain.Riders;
using BenhaScooters.Domain.Trips.ValueObjects;
using BenhaScooters.Domain.Trips.Enums;
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
    public TripRoute TripRoute { get; private set; } = null!; // Represents the route taken during the trip
    public TripFare TripFare { get; private set; } = null!; // Represents fare details for the trip

    
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

    public void DriverArrived()
    {
        if (Status != TripStatus.Assigned)
            throw new InvalidOperationException($"Cannot mark driver as arrived when trip status is {Status}");

        Status = TripStatus.DriverArrived;
        DriverArrivedAt = DateTime.UtcNow;
    }

    public void StartTrip()
    {
        if (Status != TripStatus.DriverArrived)
            throw new InvalidOperationException($"Cannot start trip when status is {Status}");

        Status = TripStatus.InProgress;
        StartedAt = DateTime.UtcNow;
    }

    public void CompleteTrip(Point finalLocation)
    {
        if (Status != TripStatus.InProgress)
            throw new InvalidOperationException($"Cannot complete trip when status is {Status}");

        Status = TripStatus.Completed;
        CompletedAt = DateTime.UtcNow;
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
