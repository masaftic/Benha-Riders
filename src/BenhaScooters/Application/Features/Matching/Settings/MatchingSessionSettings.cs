using System.ComponentModel.DataAnnotations;

namespace BenhaScooters.Application.Features.Matching.Settings;

public class MatchingSessionOptions
{
    public const string SectionName = "MatchingSession";

    [Range(10, 3000)]
    public int DriverResponseTimeoutSeconds { get; set; }

    [Range(5, 120)]
    public int TimeAfterEmptyRoundSeconds { get; set; }

    [Range(1, 10)]
    public int NumberOfRounds { get; set; }

    [Required]
    [MinLength(1)]
    public int[] OffersPerRound { get; set; } = null!;


    public TimeSpan DriverResponseTimeout => TimeSpan.FromSeconds(DriverResponseTimeoutSeconds);
    public TimeSpan TimeAfterEmptyRound => TimeSpan.FromSeconds(TimeAfterEmptyRoundSeconds);
}
