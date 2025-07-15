using NetTopologySuite.Geometries;
using BenhaScooters.Domain.Drivers;
using Vogen;

namespace BenhaScooters.Domain.Trips;

[ValueObject<int>]
public partial struct TripGpsPointId;

// Historical record of all pings (for trip tracking and analytics)
public class TripGpsPoint
{
    public TripGpsPointId Id { get; private set; }
    public DriverId DriverId { get; private set; }
    public Point Location { get; private set; } = null!;
    public double Heading { get; private set; } // Direction in degrees (0-360)
    public double Speed { get; private set; } // Speed in km/h
    public DateTime Timestamp { get; private set; }
    public TripId TripId { get; private set; }


    // Navigational properties
    public Driver Driver { get; private set; } = null!;
    public Trip Trip { get; private set; } = null!;

    private TripGpsPoint() { } // For EF Core

    public TripGpsPoint(TripId tripId, DriverId driverId, Point location, double heading, double speed, DateTime timestamp)
    {
        if (heading < 0 || heading > 360)
            throw new ArgumentException("Heading must be between 0 and 360 degrees", nameof(heading));

        if (speed < 0)
            throw new ArgumentException("Speed cannot be negative", nameof(speed));

        TripId = tripId;
        DriverId = driverId;
        Location = location;
        Heading = heading;
        Speed = speed;
        Timestamp = timestamp;
    }

    public double Longitude => Location.X;
    public double Latitude => Location.Y;
}
