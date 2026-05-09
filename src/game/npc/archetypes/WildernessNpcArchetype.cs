using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Abstract base for travelling wilderness workers (woodcutter, charcoal burner, miner, fisherman).
/// Sets sensible defaults: human, non-hostile, persistent, can speak.
///
/// These archetypes spend ~3 days at their wilderness site (forest/cave/coast) and
/// ~2 days at the nearest village to sell goods and resupply. The week-travel logic
/// is not yet implemented; for now they spawn at one location per scene generation.
/// </summary>
public abstract class WildernessNpcArchetype : NamedNpcArchetype
{
    public override Species Species         => SpeciesRegistry.Human;
    public override bool DefaultHostile     => false;
    public override bool DefaultPersistent  => true;
    public override int  ModiMentisCount    => 8;
    public override bool CanSpeak           => true;

    /// <summary>
    /// Number of days per week spent at the wilderness site before returning to the village.
    /// Not yet acted on by the schedule system — placeholder for future travel logic.
    /// </summary>
    public virtual int DaysAtSite => 3;

    /// <summary>
    /// Number of days per week spent at the nearest village.
    /// Not yet acted on by the schedule system — placeholder for future travel logic.
    /// </summary>
    public virtual int DaysAtVillage => 2;
}
