using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village miller — works the millstone, grinds grain into flour.</summary>
public class MillerArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "miller";
    public override ItemTag? SellTag => ItemTag.Crop;
    public override ItemTag? BuyTag  => ItemTag.Crop;
    public override int    ModiMentisCount => 9;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string RoleNoun => "miller";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a flour-dusted figure straightens by the millstone, white prints across their apron",
        "a pale-dusted figure heaves a sack of grain toward the hopper",
        "someone brushes meal from the millstone, the wheel groaning outside",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village miller. The mill is yours, or near enough — the lord owns the right but you take the toll, and you've taken it your whole life.

You are practical, occasionally suspicious, and you have a reputation. People always think the miller is cheating them. Sometimes you are. You speak loudly because the millstone is loud, even when you're nowhere near it.

You know who has grain to bring, who is short, and who is hungry this winter. You charge what you charge.";
}
