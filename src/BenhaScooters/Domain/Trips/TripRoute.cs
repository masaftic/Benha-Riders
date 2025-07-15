using NetTopologySuite.Geometries;
using Vogen;

namespace BenhaScooters.Domain.Trips;


[ValueObject<int>]
public partial struct TripRouteId;


public class TripRoute
{
    public TripRouteId Id { get; private set; }
    public TripId TripId { get; private set; }
    public LineString Path { get; private set; } = null!; // Represents the route as a line string
    public DateTime CreatedAt { get; private set; }
    public TimeSpan Duration { get; private set; }

    // Navigation properties
    public Trip Trip { get; private set; } = null!;

    private TripRoute() { } // For EF Core

    public TripRoute(TripId tripId)
    {
        TripId = tripId;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetPath(LineString path, TimeSpan duration)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path), "Path cannot be null");
        Duration = duration;
    }
}
