using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;

using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Verb for moving the PoV to a different area.
/// Possible when the target is a reachable <see cref="Area"/> connected via the scene's directed graph.
/// </summary>
public class MoveToAreaVerb : Verb
{
    /// <summary>Walking is the most general act there is, so this is where the broad dispositions
    /// belong — the ones that are about the person rather than about any particular craft.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Target is HearthPointOfInterest) yield return Mm<HomingModusMentis>();
        if (ctx.IsPrivate) yield return Mm<TerritorialityModusMentis>();
        if (ctx.Target is TunnelArea or GalleryArea) yield return Mm<CuriosityModusMentis>();
        if (ctx.Hostile == ThreatLevel.Visual) yield return Mm<RecklessnessModusMentis>();
        if (ctx.Night) yield return Mm<ResolveModusMentis>();

        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId         => "move";
    public override string DisplayName    => "Move";
    public override int    BaseDifficulty => 1;

    /// <summary>Walking from one room to the next. Nothing carried makes a body better at it, and at difficulty 1 the dice an implement would lend are spent on a roll already won.</summary>
    public override ToolUsage ToolUse => ToolUsage.Excluded;

    /// <summary>What a success teaches: walking somewhere on purpose is the whole of wayfaring.</summary>
    public override string? GrantedModusMentisId(Element? target) => "wayfaring";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not Area targetArea) return false;
        if (targetArea.Id == pov.Where.Id) return false; // can't move to same area

        return scene.AreaGraph.TryGetValue(pov.Where.Id, out var reachable)
            && reachable.Contains(targetArea.Id);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        if (target is Area area && !string.IsNullOrWhiteSpace(area.TransitionDescription))
            return area.TransitionDescription;
        return $"move to {DefiniteTarget(target)}";
    }

    // A routine step always names the destination: the area's TransitionDescription describes the
    // walk ("push through the gap in the hedge") and can leave a step list saying where you went
    // through without saying where you ended up.
    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"move to {DefiniteTarget(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not Area targetArea) return System.Array.Empty<Outcome>();
        return new[] { new AreaMoveOutcome(targetArea) };
    }

    // ── Routine recording ─────────────────────────────────────────────────────
    // Moving between areas is the first recordable verb. It can later decline for special areas
    // (e.g. one-way/event areas) by inspecting the target here.

    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => target is Area;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
        => target is Area area
            ? new RoutineTargetRef(RoutineTargetKind.Area, area.ReferenceLemma, area.DisplayName)
            : null;

    // Moving starts a fresh narration phase at the destination area.
    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
