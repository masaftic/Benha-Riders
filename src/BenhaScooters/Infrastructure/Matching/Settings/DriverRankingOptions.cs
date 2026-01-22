using System.ComponentModel.DataAnnotations;

namespace BenhaScooters.Infrastructure.Matching.Settings;

public class DriverRankingOptions
{
    public const string SectionName = "DriverRanking";

    // Maximum distance in meters to search for drivers around the pickup location
    [Range(1000, 50000)]
    public double MaxSearchRadiusMeters { get; set; } 

    // Distance increment in meters per round
    [Range(100, 5000)]
    public double RadiusIncrementMeters { get; set; }
}
