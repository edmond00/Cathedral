using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;

using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Sits down somewhere and lets one period of the day go by.
///
/// <para>The first verb in the game that moves the clock. <c>TimeShiftOutcome</c> and everything
/// downstream of it — the controller noticing the period changed, re-placing NPCs for the new time,
/// re-gating every verb — has been in place and unused; this is what finally produces one.</para>
///
/// <para>Difficulty 1 and no failure penalty: sitting still is not a feat. A failure is having your
/// mind wander, or being moved along, and it costs only the attempt.</para>
/// </summary>
public class SitAndWaitVerb : Verb
{
    /// <summary>Waiting takes its lesson from where the waiting is done.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Hostile == ThreatLevel.Visual) yield return Mm<SelfCommandModusMentis>();
        if (ctx.Pov.Where is HallArea or GreenArea) yield return Mm<GossipModusMentis>();
        if (ctx.Pov.Where is MarketArea or SquareArea) yield return Mm<StreetwiseModusMentis>();
        if (ctx.Pov.Where is MeadowArea or CropArea or GrasslandArea) yield return Mm<WhistlingModusMentis>();
        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId         => "sit_and_wait";
    public override string DisplayName    => "Sit and Wait";
    public override int    BaseDifficulty => 1;

    /// <summary>Sitting still. There is no work here for a thing to be held against.</summary>
    public override ToolUsage ToolUse => ToolUsage.Excluded;

    /// <summary>What a success teaches: letting time pass without filling it.</summary>
    public override string? GrantedModusMentisId(Element? target) => "patience";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => target is SitSpotPointOfInterest && pov.Where.PointsOfInterest.Contains(target);

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"sit on {DefiniteTarget(target)} and let the time pass";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
        => new Outcome[] { new TimeShiftOutcome(pov.When.Next()) };

    // ── Routine recording ─────────────────────────────────────────────────────
    // Recordable, but TimeShiftOutcome declares RoutineChainEffect.TimeShift, so the recorder treats
    // it as repositioning rather than as a piece of work — which is exactly right: waiting until
    // afternoon is a prefix to whatever the character actually came to do.

    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => target is SitSpotPointOfInterest;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
        => target is PointOfInterest poi
            ? new RoutineTargetRef(RoutineTargetKind.PointOfInterest, poi.ReferenceLemma, poi.DisplayName)
            : null;

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
