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

    /// <summary>
    /// Player-facing name of this step, from <see cref="Cathedral.Game.Scene.Verbs.Verb.RoutineLabel"/>
    /// — the verbatim made explicit out of its prompt context ("meet Aldith to talk", not "meet her to
    /// talk"). Kept separate from <see cref="Verbatim"/>, which replay still feeds back to the LLM.
    /// </summary>
    public string Label { get; set; } = "";

    /// <summary>The label to show, falling back to the verbatim for steps recorded before labels existed.</summary>
    public string DisplayLabel => string.IsNullOrWhiteSpace(Label) ? Verbatim : Label;

    /// <summary>
    /// True when this step relocates the point of view — in space or in time (one of its reports
    /// declared <see cref="RoutineChainEffect.Movement"/> or <see cref="RoutineChainEffect.TimeShift"/>).
    /// Repositioning steps are what every later step depends on, so they form the prefix each routine
    /// emitted from a session is built from, and they are never a routine's reason for existing:
    /// walking to the slope and waiting for noon are how you get somewhere, not what you went to do.
    /// </summary>
    public bool RepositionsPointOfView { get; set; }

    /// <summary>
    /// An independent copy. One session can emit several routines that share a movement prefix, and
    /// each routine owns its steps outright — a step instance is never live in two routines at once.
    /// </summary>
    public RoutineStep Clone() => new()
    {
        VerbId           = VerbId,
        Target           = Target,
        Constraints      = new List<RoutineConstraint>(Constraints),
        Verbatim         = Verbatim,
        Label            = Label,
        TriggeredPhase   = TriggeredPhase,
        VariantKey       = VariantKey,
        ActionModusMentisId = ActionModusMentisId,
        RepositionsPointOfView = RepositionsPointOfView,
    };

    /// <summary>
    /// Stable key for the chosen <see cref="Cathedral.Game.Scene.Variant"/> (e.g. the
    /// requested job id), so replay rebuilds the same view. Empty when the verb has no variant.
    /// </summary>
    public string VariantKey { get; set; } = "";

    /// <summary>
    /// The action modus mentis the step was performed with — <b>recorded, not required</b>.
    ///
    /// <para>It was a <c>RoutineConstraint</c>, which made it both a precondition of replay and a
    /// line in the routine's requirements list. Neither is right. A routine is a thing you learned to
    /// do, not a thing one particular skill learned to do: forgetting the modus mentis you happened
    /// to use the first time is not a reason to be unable to walk the same walk again, and naming it
    /// in the menu described the recording rather than what the player must bring. An item is
    /// different — it is spent, and without it the step genuinely cannot happen — so
    /// <see cref="ItemConstraint"/> stays a constraint and stays listed.</para>
    ///
    /// <para>It is still kept because replay reruns the coded action rules, and morality is read off
    /// the acting modus mentis (see <c>ActionRuleContext.ActionModusMentis</c>, which already resolves
    /// to null when the actor no longer holds it — a forgotten skill degrades the rule check, it does
    /// not block the step).</para>
    /// </summary>
    public string ActionModusMentisId { get; set; } = "";
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

    /// <summary>Display name — the <see cref="RoutineStep.DisplayLabel"/> of the routine's last step.</summary>
    public string Name { get; set; } = "";

    /// <summary>When true, protected from FIFO eviction when the queue overflows.</summary>
    public bool Locked { get; set; } = false;

    public List<RoutineStep> Steps { get; set; } = new();

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
