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
    public TripId TripId { get; private set; }
    public Point Location { get; private set; } = null!;
    public DateTime Timestamp { get; private set; }


    // Navigation properties
    public TripRoute TripRoute { get; private set; } = null!;


    private TripGpsPoint() { } // For EF Core

    public TripGpsPoint(TripId tripId, Point location, DateTime timestamp)
    {
        TripId = tripId;
        Location = location;
        Timestamp = timestamp;
    }

    public double Longitude => Location.X;
    public double Latitude => Location.Y;
}
