using NetTopologySuite.Geometries;
using Vogen;

namespace BenhaScooters.Domain.Drivers;

[ValueObject<int>]
public partial struct DriverLocationId;

// Separate aggregate for hot path operations with minimal invariants
// Represents the current location of a driver for real-time tracking
public class DriverLocation
{
    public DriverId DriverId { get; private set; }
    public Point Location { get; private set; } = null!; // PostGIS geography point
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow; // Last update time

    public Driver Driver { get; private set; } = null!; 

    private DriverLocation() { } // For EF Core

    public DriverLocation(DriverId driverId, Point location, DateTime timestamp)
    {
        DriverId = driverId;
        Location = location;
        Timestamp = timestamp;
    }

    public void UpdateLocation(Point newLocation)
    {
        Location = newLocation;
        Timestamp = DateTime.UtcNow;
    }
}
