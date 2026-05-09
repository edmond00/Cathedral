using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Abstract base for village workshop workers (blacksmith, weaver, miller, baker, etc.).
/// Sets sensible defaults: human, non-hostile, persistent, can speak.
/// Master craftsmen are typically <see cref="IsBrave"/> = true (will defend their workshop);
/// apprentices and journeymen leave <see cref="IsBrave"/> at the default.
/// </summary>
public abstract class CraftsmanArchetype : NamedNpcArchetype
{
    public override Species Species         => SpeciesRegistry.Human;
    public override bool DefaultHostile     => false;
    public override bool DefaultPersistent  => true;
    public override int  ModiMentisCount    => 9;
    public override bool CanSpeak           => true;
}
