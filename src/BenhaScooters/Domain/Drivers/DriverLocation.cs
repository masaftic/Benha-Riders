using BenhaScooters.Domain.Users;
using NetTopologySuite.Geometries;

namespace BenhaScooters.Domain.Drivers;

/// <summary>
/// Driver's current location. High-frequency updates (every 5-10 seconds while online).
/// Uses UserId as primary key for simplicity and performance.
/// </summary>
public class DriverLocation
{
    public UserId UserId { get; private set; }  // PK & FK
    public Point Location { get; private set; } = null!;  // PostGIS geography point
    public DateTime Timestamp { get; private set; }
    public float? Heading { get; private set; }  // Direction in degrees (0-360)
    public float? Speed { get; private set; }    // Speed in km/h

    // Navigation
    public User User { get; private set; } = null!;

    private DriverLocation() { } // For EF Core

    public DriverLocation(UserId userId, Point location)
    {
        UserId = userId;
        Location = location;
        Timestamp = DateTime.UtcNow;
    }

    public void UpdateLocation(Point newLocation, float? heading = null, float? speed = null)
    {
        Location = newLocation;
        Heading = heading;
        Speed = speed;
        Timestamp = DateTime.UtcNow;
    }

    public bool IsStale(TimeSpan maxAge) => DateTime.UtcNow - Timestamp > maxAge;
    
    public bool IsRecent => !IsStale(TimeSpan.FromMinutes(5));
}
