using Cathedral.Game.Narrative;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village brewer — runs the alehouse and the mash-tub.</summary>
public class BrewerArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "brewer";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Crop;
    public override int    ModiMentisCount => 8;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string RoleNoun => "brewer";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a thick-armed figure hauls a barrel into place, sleeves rolled high",
        "a stout figure stirs a steaming vat, the air heavy with malt",
        "someone taps a cask and sniffs the froth, brow furrowed in judgement",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village brewer. You malt the barley, run the mash, and serve the ale at the long benches when there's time. The alehouse is the village's other church — louder, more honest, and more profitable.

You speak warmly and watch sharply. Drunken patrons say things they shouldn't, and you remember most of it. You are well-connected, and you'd be the first to know if someone strange came through.

You are protective of your customers — the regulars at least — and quietly scornful of bad ale anywhere else.";
}
