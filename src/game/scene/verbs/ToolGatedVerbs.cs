using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Scene.Building;

using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Shared behaviour of the verbs that need a tool in hand and take something out of the world with
/// it: fish, mine, cut wood, dig.
///
/// <para>The gate is two-stage and both halves matter. <c>RequiredToolRule</c> refuses the attempt
/// outright when nothing is combined — no amount of resolve gets ore out of rock with fingernails —
/// and the item-use critic then judges whatever <i>was</i> combined against the reference tool, so a
/// player who reaches for a rock hammer instead of a pickaxe gets a hearing rather than a rejection.
/// See <c>CriticTrees.BuildToolSubstitutionTree</c>.</para>
///
/// <para>Each yields one of the items its target holds, through the same depletion and regeneration
/// path as GATHER, so a worked-out seam stays worked out across visits.</para>
/// </summary>
public abstract class ExtractionVerb : Verb
{
    /// <summary>
    /// Every extraction verb is a tool in a hand — an axe, a pick, a rod, a spade — so the whole
    /// family is declared here. A beast digs with its claws, which is the <c>digging</c> modus mentis,
    /// not this.
    /// </summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    /// <summary>Declared once for the family: none of the four can be attempted bare-handed.</summary>
    public override ToolUsage ToolUse => ToolUsage.Required;

    /// <summary>The kind of point of interest this verb works on.</summary>
    protected abstract bool Accepts(PointOfInterest poi);

    /// <summary>The first item still available on the target, or null when it is worked out.</summary>
    protected static ItemElement? FirstAvailable(Element? target)
        => (target as PointOfInterest)?.Items.FirstOrDefault();

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not PointOfInterest poi) return false;
        if (!Accepts(poi)) return false;
        if (poi.Items.Count == 0) return false;   // worked out; the verb has nothing left to do

        return pov.Where.PointsOfInterest.Contains(poi);
    }

    /// <summary>Declared so <c>InventoryCapacityRule</c> can refuse before the roll rather than after.</summary>
    public override Item? AcquiredItem(Element? target) => FirstAvailable(target)?.Item;

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var element = FirstAvailable(target);
        return element == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new ItemAcquisitionOutcome(element) };
    }

    // Recordable: working a seam or a bank is exactly the repeatable labour a routine is for, and the
    // target resolves by item id in a rebuilt scene the way GATHER's does.

    public override bool CanRecordAsRoutine(Scene scene, PoV pov, Element target, PartyMember actor)
        => target is PointOfInterest poi && Accepts(poi) && poi.Items.Count > 0;

    public override RoutineTargetRef? RoutineTarget(Scene scene, PoV pov, Element target)
        => target is PointOfInterest poi
            ? new RoutineTargetRef(RoutineTargetKind.PointOfInterest, poi.ReferenceLemma, poi.DisplayName)
            : null;

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}

/// <summary>Takes fish out of water, with a rod or a net.</summary>
public class FishVerb : ExtractionVerb
{
    /// <summary>Line, net and tide are three ways of being a fisherman.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Pov.Where is ShoreArea or BeachArea or WatersideArea or EstuaryArea) yield return Mm<TidewatchingModusMentis>();
        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId         => "fish";
    public override string DisplayName    => "Fish";
    public override int    BaseDifficulty => 4;

    public override IReadOnlyList<string> ReferenceToolIds => new[] { "fishing_rod", "net" };

    /// <summary>What a success teaches: reading water and working a line.</summary>
    public override string? GrantedModusMentisId(Element? target) => "anglery";

    protected override bool Accepts(PointOfInterest poi)
        => poi is WaterCrossingPointOfInterest { HoldsFish: true };

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"fish {DefiniteTarget(target)}";
}

/// <summary>Breaks ore out of a seam, with a pick.</summary>
public class MineVerb : ExtractionVerb
{
    /// <summary>Reading rock, working it, and the one metal that stops a body reading anything else.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Pov.Where is SeamArea) yield return Mm<DelvingModusMentis>();
        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId         => "mine";
    public override string DisplayName    => "Mine";
    public override int    BaseDifficulty => 4;

    public override IReadOnlyList<string> ReferenceToolIds => new[] { "pick" };

    /// <summary>What a success teaches: how rock breaks, and where to hit it.</summary>
    /// <summary>
    /// What a success teaches: which way the metal runs. Not <c>stonework</c>, which is the mason's
    /// lesson and is already paid in by four separate jobs - working a seam for ore and cutting
    /// dressed stone are not the same knowledge, and one verb should not stand for both.
    /// </summary>
    public override string? GrantedModusMentisId(Element? target) => "veinsight";

    protected override bool Accepts(PointOfInterest poi) => poi is OreVeinPointOfInterest;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"work {DefiniteTarget(target)} for ore";

    /// <summary>Rock comes down as well as out. Rarely, it comes down on you.</summary>
    public override IReadOnlyList<Wound?> FailurePenalties(Element? target) => new Wound?[]
    {
        null, null, null, null,
        new ContusionWound(), new BrokenHandLeftWound(),
    };
}

/// <summary>Takes timber out of a standing or fallen tree, with an axe.</summary>
public class CutWoodVerb : ExtractionVerb
{
    /// <summary>Dropping a standing tree and cutting a stool so it grows back are opposite crafts.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Target is TreePointOfInterest) yield return Mm<FellingModusMentis>();
        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId         => "cut_wood";
    public override string DisplayName    => "Cut Wood";
    public override int    BaseDifficulty => 3;

    public override IReadOnlyList<string> ReferenceToolIds => new[] { "axe" };

    /// <summary>What a success teaches: how a tree comes apart, and which way it falls.</summary>
    public override string? GrantedModusMentisId(Element? target) => "woodcraft";

    /// <summary>
    /// Any wooded point of interest — the terrain builders all anchor trees and logs on the lemmas
    /// "tree", "log" and "stump", so this rides on content that already exists rather than needing
    /// its own PoI type placed everywhere.
    /// </summary>
    protected override bool Accepts(PointOfInterest poi)
        => poi is TreePointOfInterest or LogPointOfInterest or StumpPointOfInterest
                or DeadfallPointOfInterest;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"cut wood from {DefiniteTarget(target)}";

    /// <summary>An axe that skips off wet bark goes somewhere. Usually into the ground.</summary>
    public override IReadOnlyList<Wound?> FailurePenalties(Element? target) => new Wound?[]
    {
        null, null, null, null,
        new CutWound(), new BrokenFootLeftWound(),
    };
}

/// <summary>Turns over ground worth turning over, with a shovel.</summary>
public class DigVerb : ExtractionVerb
{
    /// <summary>Peat, a garden bed and a grave are one class and three trades.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Target is GravePointOfInterest) yield return Mm<GraveworkModusMentis>();
        if (ctx.Target is MossPointOfInterest || ctx.Pov.Where is BogArea) yield return Mm<TurfcuttingModusMentis>();
        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId         => "dig";
    public override string DisplayName    => "Dig";
    public override int    BaseDifficulty => 3;

    public override IReadOnlyList<string> ReferenceToolIds => new[] { "shovel" };

    /// <summary>
    /// What a success teaches: what is under the surface, and how to get at it — by paw or by spade.
    /// Beast first, since digging names a beast's limbs and spadework names arms.
    /// </summary>
    public override IReadOnlyList<string> GrantedModusMentisIds(Element? target)
        => new[] { "digging", "spadework" };

    protected override bool Accepts(PointOfInterest poi) => poi is DiggableGroundPointOfInterest;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"dig into {DefiniteTarget(target)}";
}

/// <summary>
/// Breaks something up on purpose, with a hammer.
///
/// <para>Illegal, and unlike the other four it destroys rather than harvests: the object is replaced
/// by its wreckage, which holds whatever can be picked out of it. The room afterwards shows what was
/// done to it, which is the difference between theft and vandalism.</para>
/// </summary>
public class BreakVerb : Verb
{
    /// <summary>Breaking a chest and breaking an altar are not the same act.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId         => "break";
    public override string DisplayName    => "Break";
    public override int    BaseDifficulty => 3;

    /// <summary>A hammer or an axe swung at a thing. No hands, no verb.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    /// <summary>Breaking a thing up takes something heavier than a fist.</summary>
    public override ToolUsage ToolUse => ToolUsage.Required;

    /// <summary>
    /// Wrecking somebody's property is a crime; wrecking a thing standing in a public place is only
    /// bad manners. The sealed half already covers doing it inside a private area, so what is left is
    /// the object that belongs to one while being reachable from outside it.
    /// </summary>
    protected override bool IsIllegalFor(Scene scene, PoV pov, Element? target, PartyMember? actor)
        => PrivacyModel.ReachesPrivateArea(scene, target);

    public override IReadOnlyList<string> ReferenceToolIds => new[] { "hammer", "axe" };

    /// <summary>What a success teaches: applying more force than finesse, accurately.</summary>
    public override string? GrantedModusMentisId(Element? target) => "brute_force";

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not BreakablePointOfInterest breakable) return false;

        return pov.Where.PointsOfInterest.Contains(breakable);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"break {DefiniteTarget(target)} apart";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
        => target is BreakablePointOfInterest breakable
            ? new Outcome[] { new PoiReplacementOutcome(breakable, breakable.BrokenVariant) }
            : System.Array.Empty<Outcome>();

    /// <summary>Swinging a hammer at furniture sends pieces of it back at you.</summary>
    public override IReadOnlyList<Wound?> FailurePenalties(Element? target) => new Wound?[]
    {
        null, null, null,
        new CutWound(), new ContusionWound(), new BrokenHandRightWound(),
    };

    // Not recordable: a replayed routine rebuilds the scene with the furniture whole again.

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
