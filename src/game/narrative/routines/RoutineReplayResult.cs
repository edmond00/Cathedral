using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// Outcome of a routine replay attempt. For virtual replay only <see cref="Replayable"/> (and the
/// failure info) is meaningful; for full replay <see cref="Outcomes"/>, <see cref="ExtraLines"/>
/// and <see cref="FinalTransition"/> describe the applied results and what happens next.
/// </summary>
public class RoutineReplayResult
{
    public bool Replayable { get; set; }

    /// <summary>Index of the step that failed (-1 when fully replayable).</summary>
    public int FailedStepIndex { get; set; } = -1;

    /// <summary>Human-readable reason the routine could not be replayed.</summary>
    public string? FailReason { get; set; }

    /// <summary>UI-visible outcome reports emitted during full replay (acquired items, etc.).</summary>
    public List<OutcomeReport> Outcomes { get; set; } = new();

    /// <summary>Extra outcome lines contributed by constraints (e.g. "Used: Torch").</summary>
    public List<string> ExtraLines { get; set; } = new();

    /// <summary>What phase to enter after the player clicks CONTINUE on the outcome popup.</summary>
    public PhaseTransition FinalTransition { get; set; } = ReturnToTravelTransition.Instance;
}
