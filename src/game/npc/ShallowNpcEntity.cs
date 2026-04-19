using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc;

/// <summary>
/// A shallow NPC instance — anonymous, has no anatomy, cannot fight or converse.
/// Can be slayed to yield a lootable <see cref="CorpseSpot"/>.
/// </summary>
public class ShallowNpcEntity : INpcEntity
{
    public string NpcId    { get; }
    public string DisplayName { get; }
    public bool   IsHostile   { get; }
    public bool   IsAlive     { get; set; } = true;

    public string             ObservationHint            { get; }
    public NpcArchetype       Archetype                  { get; }

    public string SpeciesName => "(none)";

    public ShallowNpcEntity(
        string npcId,
        string displayName,
        ShallowNpcArchetype archetype,
        bool isHostile,
        string observationHint)
    {
        NpcId                      = npcId;
        DisplayName                = displayName;
        Archetype                  = archetype;
        IsHostile                  = isHostile;
        ObservationHint            = observationHint;
    }

    public CorpseSpot GenerateCorpse(Area area)
        => ((ShallowNpcArchetype)Archetype).CreateCorpse(this, area);
}
