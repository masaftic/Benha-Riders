namespace BenhaScooters.Domain.Matching;

public record RoundTransitionResult;

/// <summary>
/// Indicates that the matching session has transitioned to the next round.
/// </summary>
public record RoundTransitioned : RoundTransitionResult;

/// <summary>
/// Indicates that the matching session has been completed.
/// </summary>
public record MatchingCompleted : RoundTransitionResult;

/// <summary>
/// Indicates that the matching session has been canceled (e.g., no drivers available in the last round).
/// </summary>
public record MatchingCanceled : RoundTransitionResult;
