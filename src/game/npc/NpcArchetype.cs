namespace Cathedral.Game.Npc;

/// <summary>
/// Abstract base for all NPC templates — both named (anatomy-bearing) and shallow (anonymous, lootable).
/// Concrete archetypes descend from <see cref="NamedNpcArchetype"/> or <see cref="ShallowNpcArchetype"/>.
/// </summary>
public abstract class NpcArchetype
{
    /// <summary>Archetype identifier (e.g. "wolf", "druid", "chicken").</summary>
    public abstract string ArchetypeId { get; }
}
