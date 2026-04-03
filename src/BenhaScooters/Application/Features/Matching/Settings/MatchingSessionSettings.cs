using System.ComponentModel.DataAnnotations;

namespace BenhaScooters.Application.Features.Matching.Settings;

public class MatchingSessionOptions
{
    public const string SectionName = "MatchingSession";


    /// <summary>
    /// How long to wait on empty rounds before advancing to the next round.  
    /// </summary>
    public int EmptyRoundTimeoutSeconds { get; set; } = 5;


    /// <summary>
    /// How long to wait in each round before advancing to the next round
    /// </summary>
    [Range(10, 300)]
    public int RoundTimeoutSeconds { get; set; }


    [Range(1, 10)]
    public int NumberOfRounds { get; set; }

    /// <summary>
    /// Minimum time a session should stay open if it had any match attempts,
    /// even if later rounds found no drivers. Prevents premature cancellation.
    /// </summary>
    public int MinimumSessionDurationSeconds { get; set; } = 180;

    [Required]
    [MinLength(1)]
    public int[] OffersPerRound { get; set; } = null!;


    public TimeSpan EmptyRoundTimeout => TimeSpan.FromSeconds(EmptyRoundTimeoutSeconds);
    public TimeSpan MinimumSessionDuration => TimeSpan.FromSeconds(MinimumSessionDurationSeconds);
    public TimeSpan RoundTimeout => TimeSpan.FromSeconds(RoundTimeoutSeconds);
}
