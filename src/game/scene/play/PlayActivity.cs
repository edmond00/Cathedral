using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

/// <summary>
/// What a thing in the world can be played at — skipping stones off a creek, swinging out on a liana,
/// scratching a dog behind the ears — as a <b>type</b>, one subclass per kind of play.
///
/// <para>PLAY is the one verb whose wording is not its own. Every other verb says what it does and the
/// target fills a slot ("cross the ford", "grab an apple"); two plays have nothing in common but being
/// done for their own sake, so the object of the play says what is done, how hard it is, what it
/// teaches and what a miss costs, and <see cref="Verbs.PlayVerb"/> only carries it out.</para>
///
/// <para>A kind of play is a type for the same reason a point of interest is (see <c>PoiKinds.cs</c>):
/// a will that wants to cheat at knucklebones names <see cref="KnucklebonesPlay"/>, and a kind nothing
/// places is something <c>--play-audit</c> can find. Instances carry no state, so one may be shared by
/// every object that offers it.</para>
///
/// <para>A success teaches and does nothing else — no item, no hour, no move. That is what play is, and
/// it is why <c>--outcome-audit</c> lists <c>play</c> among the verbs whose whole effect is the modus
/// mentis.</para>
/// </summary>
public abstract class PlayActivity
{
    /// <summary>
    /// The neutral sentence for the act, sent to the narrator. <paramref name="thing"/> names the
    /// object as the verbatim should: a definite noun phrase for a point of interest ("the creek"),
    /// an object pronoun for a creature ("it", "her") — the creature is introduced on the prompt's
    /// attention line, as every NPC verb's is.
    /// </summary>
    public abstract string Verbatim(string thing);

    /// <summary>How hard this play is (1–10), before the critic's situational modifier.</summary>
    public abstract int Difficulty { get; }

    /// <summary>
    /// What the body must be able to do: hands for a stone to skip or a castle to build, rules for a
    /// game played by them. Default none — a dog can roll down a bank as well as anyone.
    /// </summary>
    public virtual AnatomyCapability RequiredCapabilities => AnatomyCapability.None;

    /// <summary>
    /// What a success could teach, <b>most restrictive first</b> and <b>beast before human</b> (see
    /// <see cref="Verbs.Verb.Lessons"/>): the verb walks the list and grants the first one the body
    /// can hold, then falls back to the object's own declaration and to playfulness.
    /// </summary>
    public abstract IEnumerable<ModusMentis> Lessons { get; }

    /// <summary>
    /// What a failure can cost, one entry drawn (see <see cref="Verbs.Verb.FailurePenalties"/>). The
    /// default is the thread lost. Built afresh on every read, since a wound carries placement state.
    /// </summary>
    public virtual IReadOnlyList<FailurePenalty> FailurePenalties => new[] { FailurePenalty.LoseFocus };

    /// <summary>The registered template for a modus mentis, by type — as <c>Verb.Mm&lt;T&gt;</c>.</summary>
    protected static ModusMentis Mm<T>() where T : ModusMentis, new()
        => ModusMentisRegistry.Instance.GetModusMentis(typeof(T)) ?? new T();
}

/// <summary>
/// Something that can be played at. A point of interest carries its play as a property (a factory or
/// <see cref="PlayPlacement"/> sets it); a creature's comes from its archetype, since every stray cat
/// is stroked the same way.
/// </summary>
public interface IPlayable
{
    /// <summary>The play this offers, or null for none.</summary>
    PlayActivity? Play { get; }
}
