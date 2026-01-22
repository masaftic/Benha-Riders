using System.ComponentModel.DataAnnotations;

namespace BenhaScooters.Application.Features.Matching.Settings;

public class MatchingSessionOptions
{
    public const string SectionName = "MatchingSession";

    [Range(10, 300)]
    public int DriverResponseTimeoutSeconds { get; set; }

    [Range(5, 120)]
    public int TimeAfterEmptyRoundSeconds { get; set; }


    public TimeSpan DriverResponseTimeout => TimeSpan.FromSeconds(DriverResponseTimeoutSeconds);
    public TimeSpan TimeAfterEmptyRound => TimeSpan.FromSeconds(TimeAfterEmptyRoundSeconds);
}
