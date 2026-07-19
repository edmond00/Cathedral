using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Cave miner — picks ore from the seam, hauls it to the village forge.
/// Carries pick, shovel, sack, lantern.
/// </summary>
public class MinerArchetype : WildernessNpcArchetype
{
    public override string ArchetypeId => "miner";
    public override ItemTag? SellTag => ItemTag.Mineral;
    public override ItemTag? BuyTag  => ItemTag.Tool;
    public override int    ModiMentisCount => 8;
    public override bool   IsBrave      => true;

    public override string RoleNoun => "miner";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a soot-faced figure straightens, pick in hand, lantern at the hip",
        "a grimed figure wheels a barrow of ore from a dark mouth in the rock",
        "someone taps at a seam by lantern-light, dust hanging in the air",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a miner who works a small ore-vein in a cave nearby and brings the ore down to the village forge every few days.

You speak with the gruff calm of someone who has worked alone in the dark for years. You have superstitions — you don't whistle in a shaft, you don't sit on the ore-pile.

You distrust loose talk near the entrance — sound carries, and not all of it is the kind you want returned.";
}
