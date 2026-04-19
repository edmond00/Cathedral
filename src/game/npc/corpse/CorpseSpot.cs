using System.Collections.Generic;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Corpse;

/// <summary>
/// A temporary <see cref="Spot"/> representing the remains of a slain NPC.
/// Placed at the area where the NPC died; not persisted when the player leaves the scene.
///
/// Structure:
///   • 2–3 <see cref="CorpseBodyPartPoI"/>s containing harvestable items (fur, claws, meat, …).
///   • Optional wearing <see cref="PointOfInterest"/> for human NPCs, listing their equipped items.
/// </summary>
public class CorpseSpot : Spot
{
    /// <summary>The entity whose death produced this corpse.</summary>
    public INpcEntity NpcEntity { get; }

    public CorpseSpot(
        Area area,
        INpcEntity npcEntity,
        string displayName,
        List<string> descriptions,
        List<PointOfInterest> bodyParts)
        : base(area, displayName, descriptions)
    {
        NpcEntity = npcEntity;
        PointsOfInterest.AddRange(bodyParts);
    }
}
