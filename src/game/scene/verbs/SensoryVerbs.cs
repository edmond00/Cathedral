using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;

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
    /// Where this sense can be turned: on a point of interest in the current area, or in the spot the
    /// character is standing in. Both are needed — half the interesting objects in a building are
    /// inside a spot rather than loose in the room.
    /// </summary>
    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not PointOfInterest poi) return false;
        if (!poi.RewardsSense(VerbId)) return false;

        return pov.Where.PointsOfInterest.Contains(poi);
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
    public override string VerbId      => "smell";
    public override string DisplayName => "Smell";

    /// <summary>What a success teaches by default: reading a place by its smell.</summary>
    public override string? GrantedModusMentisId(Element? target) => "scenting";

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"breathe in the smell of {DefiniteTarget(target)}";
}
