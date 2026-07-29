using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Abstract base for travelling wilderness workers (woodcutter, charcoal burner, miner, fisherman).
/// Sets sensible defaults: human, non-hostile, persistent, can speak.
///
/// These archetypes work in a repeating rotation: ~3 days at their wilderness site
/// (forest/cave/coast), then ~2 days at the nearest village to sell goods and resupply. The
/// rotation logic is not yet implemented; for now they spawn at one location per scene generation.
/// </summary>
public abstract class WildernessNpcArchetype : NamedNpcArchetype
{
    public override Species Species         => SpeciesRegistry.Human;
    public override bool DefaultPersistent  => true;
    public override int  ModiMentisCount    => 8;
    public override bool CanSpeak           => true;

    /// <summary>Country people who happen to work away from the fields.</summary>
    public override SocialCategory? Social  => SocialCategory.Peasant;

    /// <summary>
    /// Days of each rotation spent at the wilderness site before returning to the village.
    /// Not yet acted on by the schedule system — placeholder for future travel logic.
    /// </summary>
    public virtual int DaysAtSite => 3;

    /// <summary>
    /// Days of each rotation spent at the nearest village.
    /// Not yet acted on by the schedule system — placeholder for future travel logic.
    /// </summary>
    public virtual int DaysAtVillage => 2;
}
