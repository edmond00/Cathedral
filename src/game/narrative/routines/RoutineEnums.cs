using System;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// The kind of scene <see cref="Cathedral.Game.Scene.Element"/> a routine step targets.
/// Used together with a stable string key to re-resolve the live element in a freshly
/// built scene at replay time (scene element GUIDs are not stable across rebuilds).
/// </summary>
public enum RoutineTargetKind
{
    None,
    Area,
    PointOfInterest,
    Spot,
    Npc,
    Item,
}

/// <summary>
/// What an outcome report does to a routine being recorded. Every
/// <see cref="Cathedral.Game.Narrative.OutcomeReport"/> declares one, and the recorder reasons only
/// about this value — it never tests concrete report types. A new report therefore only has to
/// answer this single question to be handled correctly, both when deciding whether an unrecordable
/// verb may be skipped and when tracking which steps a later step depends on.
///
/// <para>These are <b>flags</b>, because the questions are independent: a report can relocate the
/// point of view <i>and</i> leave a one-shot change behind (unlocking a door does both). Answering
/// them with one value each forced a choice between the two, which silently dropped the door step
/// out of the movement prefix.</para>
/// </summary>
[Flags]
public enum RoutineChainEffect
{
    /// <summary>
    /// Nothing a recorded chain depends on. Character and world state that persists (items, coins,
    /// skills, wounds, humors, affinity, a triggered dialogue) lands here: it will still be true when
    /// the routine replays, and every per-step requirement is captured as a
    /// <see cref="RoutineConstraint"/> anyway. A verb whose effects are all None can be skipped
    /// silently, leaving the chain around it intact.
    /// </summary>
    None = 0,

    /// <summary>
    /// Relocates the point of view in space (<see cref="Cathedral.Game.Scene.PoV.Where"/> /
    /// <see cref="Cathedral.Game.Scene.PoV.InSpot"/>). See <see cref="RoutineChainEffectExtensions.Repositions"/>
    /// for what that means to the recorder.
    /// </summary>
    Movement = 1 << 0,

    /// <summary>
    /// Relocates the point of view in time (<see cref="Cathedral.Game.Scene.PoV.When"/>) — waiting
    /// out the morning, sleeping until dawn. Time of day is scene state exactly as location is: it
    /// decides which NPCs are present and therefore which later steps are possible at all, so a step
    /// that shifts it is a repositioning step, never a piece of work in itself.
    /// </summary>
    TimeShift = 1 << 1,

    /// <summary>
    /// Cannot be reproduced by a replayed chain: a hand-off to a phase a routine cannot contain
    /// (fight, childhood/get-up transition), or a one-shot change to the scene that later steps lean
    /// on (unlocking a door, removing an NPC). Skipping it would leave a routine that quietly assumes
    /// something replay never does, so recording stops instead.
    /// </summary>
    Breaking = 1 << 2,
}

public static class RoutineChainEffectExtensions
{
    /// <summary>
    /// True when the effect moves the point of view — in space or in time. Steps are recorded and
    /// replayed as a chain from the session's entry point, so a repositioning step both defines the
    /// where-and-when every following step happens at (it is kept in the prefix each emitted routine
    /// is built from) and breaks the chain if it is left out.
    /// </summary>
    public static bool Repositions(this RoutineChainEffect e)
        => (e & (RoutineChainEffect.Movement | RoutineChainEffect.TimeShift)) != 0;
}

/// <summary>
/// What phase a routine step transitions into when it succeeds. Only the LAST step of a routine may
/// carry a fight/dialogue value: such a step terminates the chain that contains it, and the routine
/// built from it is emitted there and then. Recording of the session continues past it — see
/// <see cref="RoutineRecorder"/>.
/// </summary>
public enum RoutinePhaseKind
{
    /// <summary>The step does not start a new phase (stays in / returns to travel after replay).</summary>
    None,

    /// <summary>The step starts a fresh narration phase (e.g. moving to a new area).</summary>
    Narration,

    /// <summary>The step starts a fight (e.g. attacking an NPC).</summary>
    Fight,

    /// <summary>The step starts a dialogue (e.g. meeting/speaking to an NPC).</summary>
    Dialogue,
}
