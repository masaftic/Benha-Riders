using NetTopologySuite.Geometries;
using Vogen;

namespace BenhaScooters.Domain.Drivers;

[ValueObject<int>]
public partial struct DriverLocationId;

// Separate aggregate for hot path operations with minimal invariants
// Represents the current location of a driver for real-time tracking
public class DriverLocation
{
    public DriverLocationId Id { get; private set; }
    public DriverId DriverId { get; private set; }
    public Point Location { get; private set; } = null!; // PostGIS geography point
    public double Heading { get; private set; } = 0; // Direction in degrees
    public double Speed { get; private set; } = 0; // Speed in km/h
    public DateTime Timestamp { get; private set; } = DateTime.UtcNow; // Last update time

    private DriverLocation() { } // For EF Core

    public DriverLocation(DriverId driverId, Point location, double heading, double speed, DateTime timestamp)
    {
        DriverId = driverId;
        Location = location;
        Heading = heading;
        Speed = speed;
        Timestamp = timestamp;
    }

    public void UpdateLocation(Point newLocation, double newHeading, double newSpeed)
    {
        Location = newLocation;
        Heading = newHeading;
        Speed = newSpeed;
        Timestamp = DateTime.UtcNow;
    }
}
