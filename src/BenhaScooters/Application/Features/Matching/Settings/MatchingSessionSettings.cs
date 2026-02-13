using System.ComponentModel.DataAnnotations;

namespace BenhaScooters.Application.Features.Matching.Settings;

public class MatchingSessionOptions
{
    public const string SectionName = "MatchingSession";

    /// <summary>
    /// How long to wait in each round before advancing to the next round
    /// </summary>
    [Range(10, 300)]
    public int RoundTimeoutSeconds { get; set; }

    /// <summary>
    /// How long to wait after the last round before cancelling the session if no one accepted
    /// </summary>
    [Range(10, 600)]
    public int FinalRoundWaitSeconds { get; set; }

    [Range(1, 10)]
    public int NumberOfRounds { get; set; }

    [Required]
    [MinLength(1)]
    public int[] OffersPerRound { get; set; } = null!;


    public TimeSpan RoundTimeout => TimeSpan.FromSeconds(RoundTimeoutSeconds);
    public TimeSpan FinalRoundWait => TimeSpan.FromSeconds(FinalRoundWaitSeconds);
}
