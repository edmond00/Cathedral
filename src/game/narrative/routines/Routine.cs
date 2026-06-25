using System;
using System.Collections.Generic;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// One recorded step of a routine: a successfully-executed recordable verb, the stable target it
/// acted on, the constraints (item/modus-mentis/acting-member, …) it required/consumed, the cached
/// natural-language verbatim, and the phase (if any) it triggers on success.
/// </summary>
public class RoutineStep
{
    public string VerbId { get; set; } = "";
    public RoutineTargetRef Target { get; set; } = new();
    public List<RoutineConstraint> Constraints { get; set; } = new();
    public string Verbatim { get; set; } = "";
    public RoutinePhaseKind TriggeredPhase { get; set; } = RoutinePhaseKind.None;
}

/// <summary>
/// A learned routine: an ordered chain of recordable successful verbs bound to the
/// location + time period of the narration it was recorded in. Replayable automatically from the
/// travel UI, skipping narration, LLM calls, and skill checks.
/// </summary>
public class Routine
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>World vertex (location id) the routine was recorded at and may be replayed at.</summary>
    public int LocationId { get; set; }

    /// <summary>Time period the narration started in; replay forces this arrival time.</summary>
    public TimePeriod StartTime { get; set; }

    /// <summary>Display name — the verbatim of the routine's last step.</summary>
    public string Name { get; set; } = "";

    /// <summary>When true, protected from FIFO eviction when the queue overflows.</summary>
    public bool Locked { get; set; } = false;

    public List<RoutineStep> Steps { get; set; } = new();

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
