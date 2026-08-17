using System.Collections.Generic;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Corpse;

/// <summary>
/// The body of a slain NPC, as an ordinary <see cref="PointOfInterest"/> in the area it fell in.
/// Its <see cref="PointOfInterest.Items"/> are the harvestable parts (meat, hide, fangs, claws), and
/// being this type is what makes them <c>cut</c>'s targets rather than <c>grab</c>'s.
///
/// <para>A corpse used to be an enterable <c>Spot</c> holding one PoI per body part. That cost the
/// player two narration phases to reach a fang — enter the body, then observe the part, then cut —
/// and it was the only content the whole spot/InSpot axis ever had. Flat, the body is one
/// observation whose action list already holds every part: <c>SyntheticObservationObject</c> folds a
/// PoI's items into its own sub-outcomes, so "cut the wolf fang" and "cut the animal hide" are two
/// goals under one keyword.</para>
///
/// <para>What a human was carrying is deliberately <b>not</b> in here — it goes in a plain PoI
/// beside this one, so clothing is grabbed and flesh is cut without either verb having to inspect
/// individual items.</para>
/// </summary>
public class CorpsePointOfInterest : PointOfInterest
{
    /// <summary>
    /// The entity whose death produced this body, or <c>null</c> for a body that was never a scene
    /// NPC — a <b>companion</b>, who is a bare <see cref="Narrative.PartyMember"/>: recruiting drops
    /// the <c>NpcEntity</c> that wrapped them and only the combatant walks away with the party.
    /// Nothing on the narration path reads this; it is the debug window's label.
    /// </summary>
    public INpcEntity? NpcEntity { get; }

    public CorpsePointOfInterest(
        INpcEntity? npcEntity,
        string displayName,
        List<string> descriptions,
        List<ItemElement>? parts = null,
        string[]? moods = null)
        : base(displayName, "corpse", descriptions, parts, moods)
    {
        NpcEntity = npcEntity;
    }
}
