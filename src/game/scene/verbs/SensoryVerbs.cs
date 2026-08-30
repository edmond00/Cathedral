using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Narrative.Routines;

using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Shared behaviour of the four senses — examine, contemplate, listen, smell.
///
/// <para>These are ordinary verbs, not a new kind of thing: they run the whole thinking → persona-fit
/// → dice → narration pipeline like everything else, and they cost a noetic point like everything
/// else. What makes them unusual is that they produce <b>no state change at all</b>. The narration is
/// the entire outcome, and the modus mentis granted for succeeding is the entire reward.</para>
///
/// <para>Difficulty 1 and no failure penalty, deliberately. Turning a sense on something is not a
/// feat and cannot hurt you; a failure means you looked and saw nothing worth the looking, which is
/// a real enough outcome to be worth narrating and not worth breaking a bone over.</para>
///
/// <para>Whether an object rewards a given sense is the object's business
/// (<see cref="PointOfInterest.Senses"/>), and so is what it teaches
/// (<see cref="PointOfInterest.VerbModiMentis"/>) — examining a mushroom teaches mycology where
/// examining an anvil teaches metalcraft, and both go through the same override.</para>
/// </summary>
public abstract class SensoryVerb : Verb
{
    public override int BaseDifficulty => 1;

    /// <summary>
    /// Turning a sense upon something produces no state change at all — the narration is the whole
    /// outcome — so there is nothing for an implement to make go better. Declared once for the four.
    ///
    /// <para>This is the exclusion that most wants an exception, and it is what
    /// <c>Item.MadeForVerbIds</c> is for: a glass ground to magnify bears on EXAMINE and on
    /// nothing else, and says so from the item's side rather than by softening the category.</para>
    /// </summary>
    public override ToolUsage ToolUse => ToolUsage.Excluded;

    /// <summary>
    /// Where this sense can be turned: on a point of interest in the current area, or in the spot the
    /// character is standing in. Both are needed — half the interesting objects in a building are
    /// inside a spot rather than loose in the room.
    /// </summary>
    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is SceneNpc npc) return CanSense(scene, pov, npc);

        if (target is not PointOfInterest poi) return false;
        if (!poi.RewardsSense(VerbId)) return false;

        return pov.Where.PointsOfInterest.Contains(poi);
    }

    /// <summary>
    /// Whether a sense can be turned on a living thing: it is alive, it is here now, and its kind
    /// rewards this sense (<see cref="Npc.NpcArchetype.Senses"/>).
    ///
    /// <para>This branch is why the senses reach anything alive at all. The gate used to be
    /// <c>target is not PointOfInterest → false</c>, so a scene's birds, insects, beasts and people
    /// were the only observables in the game that could not be examined, listened to or smelled —
    /// you could listen to the tree and not to the lark in it, and the only verbs a bird accepted
    /// were the ones that killed it.</para>
    ///
    /// <para>A sleeper is excluded because they are not observable as themselves: <c>SceneNpcPlacement</c>
    /// swaps them and their bed for one merged <see cref="SleepingNpcPointOfInterest"/>, which carries
    /// its own senses and its own lessons. Both routes offered at once would be two ways to the same
    /// act, differing only in which object the phase happened to open on.</para>
    /// </summary>
    private bool CanSense(Scene scene, PoV pov, SceneNpc npc)
    {
        if (!npc.IsAlive) return false;
        if (!npc.Entity.Archetype.Senses.Rewards(VerbId)) return false;
        if (npc.IsSleeping(scene, pov)) return false;

        return scene.GetNpcsAt(pov.Where, pov.When).Any(n => n.Id == npc.Id);
    }

    // No SuccessReports: the narration is the outcome. The modus-mentis grant is appended by the
    // execution pipeline from GrantedModusMentisId, as it is for every other verb.

    // Not recordable as a routine. A routine replays without narration, and the narration is all
    // there is here — a replayed "smell the flowers" would be a step that does nothing at all.

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}

/// <summary>
/// Looks closely and reasons about what is there — the scientific sense. What something is made of,
/// how it works, what it has been used for, what is wrong with it.
/// </summary>
public class ExamineVerb : SensoryVerb
{
    /// <summary>Looking closely is usually the object's own lesson. Two cases are not about the object at all.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Hostile == ThreatLevel.Audio) yield return Mm<GutFeelingModusMentis>();
        if (ctx.Target is CorpsePointOfInterest) yield return Mm<GravesightModusMentis>();
        if (ctx.Target is LandscapePointOfInterest) yield return Mm<CartographyModusMentis>();
        if (ctx.Target is StreamPointOfInterest or PoolPointOfInterest) yield return Mm<WaterlineModusMentis>();
        if (ctx.Target is ClothPointOfInterest) yield return Mm<WeaveReadingModusMentis>();
        if (ctx.Target is AnvilPointOfInterest) yield return Mm<HallmarkModusMentis>();
        if (ctx.Target is TollPointOfInterest) yield return Mm<AlgebraicAnalysisModusMentis>();
        if (ctx.Target is CradlePointOfInterest) yield return Mm<MidwiferyModusMentis>();
        if (ctx.Target is Building.DoorPointOfInterest) yield return Mm<KeywiseModusMentis>();
        if (ctx.Target is HivePointOfInterest) yield return Mm<SwarmSenseModusMentis>();
        if (ctx.Target is BreadPointOfInterest) yield return Mm<DoughcraftModusMentis>();
        if (ctx.Target is PalletPointOfInterest or BedrollPointOfInterest) yield return Mm<WearReadingModusMentis>();
        if (ctx.Target is MarkerPointOfInterest) yield return Mm<ProvenanceModusMentis>();

        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId      => "examine";
    public override string DisplayName => "Examine";

    /// <summary>What a success teaches by default: close, careful attention. Objects override it.</summary>
    public override string? GrantedModusMentisId(Element? target) => "scrutiny";

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"examine {DefiniteTarget(target)} closely";
}

/// <summary>
/// Looks at something as a thing worth looking at rather than a thing to use — the artistic sense.
/// The counterpart of examine: same eyes, entirely different question.
/// </summary>
public class ContemplateVerb : SensoryVerb
{
    /// <summary>The verb whose lesson depends most on what the thing MEANS, which is not what it is.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Hostile == ThreatLevel.Audio) yield return Mm<GutFeelingModusMentis>();
        if (ctx.Pov.Where is ChamberArea or ShaftArea or DenArea) yield return Mm<DreadModusMentis>();
        if (ctx.Target is CorpsePointOfInterest or GravePointOfInterest) yield return Mm<ElegyModusMentis>();
        if (ctx.Target is LandscapePointOfInterest) yield return Mm<AweModusMentis>();
        if (ctx.Target is PsalterPointOfInterest) yield return Mm<PietyModusMentis>();
        if (ctx.Target is CrossPointOfInterest) yield return Mm<ReverenceModusMentis>();
        if (ctx.Target is StocksPointOfInterest) yield return Mm<SeverityModusMentis>();
        if (ctx.Target is CairnPointOfInterest) yield return Mm<SuperstitionModusMentis>();
        if (ctx.Target is LambPointOfInterest) yield return Mm<QuickeningModusMentis>();
        if (ctx.Target is BenchPointOfInterest or LoomPointOfInterest or AnvilPointOfInterest or WorkbenchPointOfInterest) yield return Mm<JourneymanEyeModusMentis>();
        if (ctx.Target is BreadPointOfInterest or TablePointOfInterest) yield return Mm<AbstinenceModusMentis>();
        if (ctx.Target is PalletPointOfInterest) yield return Mm<ContinenceModusMentis>();
        if (ctx.IsPrivate) yield return Mm<HearthlongingModusMentis>();

        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId      => "contemplate";
    public override string DisplayName => "Contemplate";

    /// <summary>What a success teaches by default: an eye for what is worth looking at.</summary>
    public override string? GrantedModusMentisId(Element? target) => "aesthetic";

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"stand and contemplate {DefiniteTarget(target)}";
}

/// <summary>Stands still and listens: birdsong, water, a forge, the sound a building makes.</summary>
public class ListenVerb : SensoryVerb
{
    /// <summary>What a place sounds like depends more on the hour and the ground than on the object.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Hostile == ThreatLevel.Audio) yield return Mm<GutFeelingModusMentis>();
        if (ctx.Night) yield return Mm<NightEarModusMentis>();
        if (ctx.Outdoors && ctx.Pov.Where is HeathArea or CragArea or RidgeArea) yield return Mm<WeatherEarModusMentis>();
        if (ctx.Target is HivePointOfInterest) yield return Mm<SwarmSenseModusMentis>();
        if (ctx.Pov.Where is RoofArea) yield return Mm<TimberEarModusMentis>();

        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId      => "listen";
    public override string DisplayName => "Listen";

    /// <summary>What a success teaches by default: hearing a place properly.</summary>
    public override string? GrantedModusMentisId(Element? target) => "keen_ear";

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"listen to {DefiniteTarget(target)}";
}

/// <summary>Takes in a smell — the sense that carries the most and is asked for the least.</summary>
public class SmellVerb : SensoryVerb
{
    /// <summary>Mostly the object's business; the exceptions are a body in the room and a kitchen.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Hostile == ThreatLevel.Audio) yield return Mm<GutFeelingModusMentis>();
        if (ctx.Target is CorpsePointOfInterest) yield return Mm<CharnelSenseModusMentis>();
        if (ctx.Target is BreadPointOfInterest) yield return Mm<RelishModusMentis>();
        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }
    public override string VerbId      => "smell";
    public override string DisplayName => "Smell";

    /// <summary>
    /// What a success teaches by default: reading a place by its smell. Two lessons, because the two
    /// anatomies do not smell the same way — a snout follows, a nose remembers. Beast first, since
    /// scenting names a snout and keen_nose names a nose.
    /// </summary>
    public override IReadOnlyList<string> GrantedModusMentisIds(Element? target)
        => new[] { "scenting", "keen_nose" };

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"breathe in the smell of {DefiniteTarget(target)}";
}
