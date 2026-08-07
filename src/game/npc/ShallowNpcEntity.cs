using System.Collections.Generic;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc;

/// <summary>
/// A shallow NPC instance — anonymous, has no anatomy, cannot fight or converse.
/// Can be slayed to yield a harvestable <see cref="CorpsePointOfInterest"/>.
/// </summary>
public class ShallowNpcEntity : INpcEntity
{
    public string NpcId    { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// Shallow wildlife has no generated name to derive an id from, but its <see cref="NpcId"/> is
    /// drawn from the location-seeded spawn rng and so already comes out the same on every build —
    /// which is exactly what a persistent id has to be.
    /// </remarks>
    public string PersistentId => NpcId;

    public string DisplayName { get; }
    public bool   IsAlive     { get; set; } = true;

    public string             ObservationHint            { get; }
    public NpcArchetype       Archetype                  { get; }

    public string SpeciesName => "(none)";

    public ShallowNpcEntity(
        string npcId,
        string displayName,
        ShallowNpcArchetype archetype,
        string observationHint)
    {
        NpcId                      = npcId;
        DisplayName                = displayName;
        Archetype                  = archetype;
        ObservationHint            = observationHint;
    }

    public List<PointOfInterest> GenerateCorpse()
        => ((ShallowNpcArchetype)Archetype).CreateCorpse(this);
}
