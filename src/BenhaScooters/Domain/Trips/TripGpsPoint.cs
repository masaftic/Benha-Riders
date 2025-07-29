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
    public DateTime Timestamp { get; private set; }
    public TripRouteId TripRouteId { get; private set; }

    // Navigational properties
    public TripRoute TripRoute { get; private set; } = null!;
    public Driver Driver { get; private set; } = null!;

    private TripGpsPoint() { } // For EF Core

    public TripGpsPoint(TripRouteId tripRouteId, DriverId driverId, Point location, DateTime timestamp)
    {
        TripRouteId = tripRouteId;
        DriverId = driverId;
        Location = location;
        Timestamp = timestamp;
    }

    public double Longitude => Location.X;
    public double Latitude => Location.Y;
}
