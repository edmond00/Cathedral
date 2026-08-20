using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Npc;

using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Shared gate for the two things you can do to something too small to fight: it must be a live,
/// tiny shallow creature, and it must be here now.
/// </summary>
public abstract class TinyCreatureVerb : Verb
{
    /// <summary>The live tiny creature behind a target, or null when the target is not one.</summary>
    protected static ShallowNpcEntity? Tiny(Scene scene, PoV pov, Element target)
    {
        if (target is not SceneNpc sceneNpc) return null;
        if (sceneNpc.Entity is not ShallowNpcEntity shallow) return null;
        if (!shallow.IsAlive) return null;
        if (shallow.Archetype is not ShallowNpcArchetype { IsTiny: true }) return null;

        return scene.GetNpcsAt(pov.Where, pov.When).Any(n => n.Id == sceneNpc.Id) ? shallow : null;
    }

    /// <summary>The creature's name in lower case, for verbatims: "the butterfly".</summary>
    protected static string TinyName(Element target)
        => (target as SceneNpc)?.Entity.DisplayName?.ToLowerInvariant() ?? "it";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Tiny(scene, pov, target) != null;

    // Not recordable. Which insects are where is rolled fresh on every visit, so a routine step
    // pointing at "the beetle" would resolve to nothing the next time through.

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}

/// <summary>
/// Closes a hand on a tiny creature without harming it, and keeps what is worth keeping.
///
/// <para>The harder of the two — difficulty 3, because a butterfly is a butterfly — and the one that
/// yields anything. What it teaches is a soft grip: the ability to hold something living without
/// breaking it, which is the same skill whether the thing in the hand is a moth or a hawk.</para>
/// </summary>
public class CatchVerb : TinyCreatureVerb
{

    public override string VerbId         => "catch";
    public override string DisplayName    => "Catch";
    public override int    BaseDifficulty => 3;

    /// <summary>
    /// What a success teaches: holding something alive without crushing it — in a mouth or in a hand.
    /// Beast first, since soft_mouth names a muzzle and snatch names hands.
    /// </summary>
    public override IReadOnlyList<string> GrantedModusMentisIds(Element? target)
        => new[] { "soft_mouth", "snatch" };

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"catch the {TinyName(target)} in my hands";

    /// <summary>
    /// The specimen a catch produces, so <c>InventoryCapacityRule</c> can refuse the action when
    /// there is nowhere to put it rather than letting it silently evaporate.
    /// </summary>
    public override Item? AcquiredItem(Element? target)
        => (target as SceneNpc)?.Entity.Archetype is ShallowNpcArchetype arch
            ? arch.BuildCatchYield().FirstOrDefault()
            : null;

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not ShallowNpcEntity shallow)
            return System.Array.Empty<Outcome>();
        if (shallow.Archetype is not ShallowNpcArchetype arch) return System.Array.Empty<Outcome>();

        var reports = new List<Outcome> { new TinyCreatureRemovedOutcome(sceneNpc, caught: true) };
        foreach (var item in arch.BuildCatchYield())
            reports.Add(new ItemGrantOutcome(item));

        return reports;
    }
}

/// <summary>
/// Puts a foot or a thumb on a tiny creature. Easy, instant, yields nothing.
///
/// <para>Difficulty 1 against catch's 3, and that asymmetry is the design: destroying the thing is
/// always the easier option and never the rewarding one. What it teaches is cruelty, which is a
/// modus mentis like any other and will colour every narration it is later chosen for.</para>
/// </summary>
public class CrushVerb : TinyCreatureVerb
{
    /// <summary>What lives closest to people, and what its numbers say about the house.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Target is SceneNpc { Entity.Archetype: Npc.Archetypes.RatArchetype or Npc.Archetypes.HouseMouseArchetype or Npc.Archetypes.CockroachArchetype }) yield return Mm<RatcatcherModusMentis>();
        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId         => "crush";
    public override string DisplayName    => "Crush";
    public override int    BaseDifficulty => 1;

    // Left legal deliberately: nobody prosecutes the death of a snail. Killing something that could
    // not have hurt you is a matter for the persona, and `cruelty` is what it teaches.

    /// <summary>What a success teaches: doing harm because it was easy.</summary>
    public override string? GrantedModusMentisId(Element? target) => "cruelty";

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"crush the {TinyName(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
        => target is SceneNpc sceneNpc
            ? new Outcome[] { new TinyCreatureRemovedOutcome(sceneNpc, caught: false) }
            : System.Array.Empty<Outcome>();
}
