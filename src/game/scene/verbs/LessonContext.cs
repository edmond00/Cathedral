using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// What a verb is told about the moment it is being performed, so that
/// <see cref="Verb.Lessons"/> can decide which modi mentis a success could teach.
///
/// <para>Everything here is <b>in the fiction</b>: what is being acted on, where, at what hour,
/// whether anybody hostile is near. Deliberately no mechanical quantity —
/// no difficulty number, no dice pool, no party size. A lesson keyed to a number the fiction never
/// mentions reads as arbitrary, because nothing in the scene tells the player why a four taught one
/// thing and a three another.</para>
/// </summary>
public sealed record LessonContext(
    Scene       Scene,
    PoV         Pov,
    PartyMember Actor,
    Element?    Target)
{
    /// <summary>The body is carrying an injury.</summary>
    public bool Wounded => Actor.Wounds.Count > 0;

    /// <summary>How close anybody who counts this body an enemy is — see <see cref="ThreatSelector"/>.</summary>
    public ThreatLevel Hostile { get; init; } = ThreatLevel.None;


    public bool Night     => Pov.When == TimePeriod.Night;
    public bool IsPrivate => Pov.Where.IsPrivate;
    public bool Outdoors  => !Pov.Where.IsPrivate;

    /// <summary>Anybody else is standing in this room — not a count, just whether the actor is alone.</summary>
    public bool Watched
    {
        get
        {
            try { return Scene.GetNpcsAt(Pov.Where, Pov.When).Any(n => n.IsAlive); }
            catch { return false; }
        }
    }

    /// <summary>The thing being acted on already counts this body an enemy.</summary>
    public bool TargetIsHostile
    {
        get
        {
            try
            {
                return Target is SceneNpc { Entity: NpcEntity npc }
                    && npc.AffinityTable.IsEnemy(Actor.AffinityKey);
            }
            catch { return false; }
        }
    }

    /// <summary>
    /// The point of interest holding <see cref="Target"/>, when the target is an item.
    ///
    /// <para>The pickup verbs — GATHER, GRAB, STEAL — target the <b>item</b>, not the thing holding
    /// it, so a condition asking whether the target is a bush can never be true for them however
    /// many bushes the world contains. That is not a hypothetical: <c>berrying</c> and
    /// <c>simpling</c> were both written that way and neither could ever fire.</para>
    ///
    /// <para>Null for a target that is not an item, and for an item whose holder is not in this
    /// area — so a condition may test it without a null check falling through wrongly.</para>
    /// </summary>
    public PointOfInterest? Holder
        => Target is ItemElement item ? ItemPickup.FindHoldingPoI(Pov, item, includeCorpse: true) : null;

    /// <summary>The thing being acted on is a person rather than an object or a beast.</summary>
    public bool TargetIsPerson => Target is SceneNpc { Entity: NpcEntity };

    /// <summary>
    /// There is deliberately no name-matching helper here any more. Every lesson condition names a
    /// <b>type</b> — a <c>PointOfInterest</c> kind or an <c>Area</c> kind — so a condition about
    /// content that does not exist fails to compile rather than matching nothing in silence.
    ///
    /// <para>The string version (<c>Named("withy")</c>) shipped six unreachable modi mentis and made
    /// half the lesson conditions in the game point at furniture that had never been built. It also
    /// went wrong in the other direction, since it matched substrings: "cross" found "Crossing" and
    /// "barrow" found "narrow". If a condition wants a kind the world lacks, build the kind.</para>
    /// </summary>
}
