using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Abstract base for field/farm peasant roles (reeve, plowman, reaper, shepherd, etc.).
/// Sets sensible defaults: human, non-hostile, persistent, can speak, civilian.
/// Concrete subclasses set <see cref="ArchetypeId"/>, <see cref="NamePool"/>,
/// observation hints, and dialogue prompt text.
/// </summary>
public abstract class PeasantArchetype : NamedNpcArchetype
{
    public override Species Species         => SpeciesRegistry.Human;
    public override bool DefaultHostile     => false;
    public override bool DefaultPersistent  => true;
    public override int  ModiMentisCount    => 8;
    public override bool CanSpeak           => true;
}
