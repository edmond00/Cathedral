using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village brewer — runs the alehouse and the mash-tub.</summary>
public class BrewerArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "brewer";

    /// <summary>What asking this person about their work teaches.</summary>
    public override string TradeModusMentisId => "brewcraft";
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

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_arm", "right_arm", "backbone", "nose", "tongue" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new MashPaddle(), () => new LinenTunic(), () => new DrinkingHorn(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Grain(), () => new WoodenBowl(), () => new Herb(), () => new CoinPurse(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the one who keeps the ale here — the benches by the mash-tub are mine";
    public override string Workplace        => "the alehouse";
    public override string Craft            => "brewing";
    public override string DailyLabour      => "turning malt, stirring mash, and listening to whatever the benches are saying";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Weather]    = "a hot spell sours a batch faster than a curse would. I brew small and often in summer",
        [DialogueTopic.Harvest]    = "barley first, and let the bakers argue after. A year without ale is a year of bad tempers",
        [DialogueTopic.Food]       = "ale is food, whatever anyone tells you. It's kept more folk upright through winter than pottage has",
        [DialogueTopic.Rest]       = "my house is where this place goes to stop being tired. That's a trade worth doing well",
        [DialogueTopic.Neighbours] = "drink loosens tongues, and I stand behind the barrel. I know more than I ever repeat",
        [DialogueTopic.Stories]    = "half the tales told here started as lies and got better with the telling. I don't spoil them",
        [DialogueTopic.Trade]      = "water the ale once and you've lost the village forever. It isn't worth the copper",
        [DialogueTopic.Roads]      = "travellers drink well and pay in coin instead of promises. I'm glad of the road",
        [DialogueTopic.Health]     = "a small ale in the morning never hurt a soul. It's the strong stuff before noon that undoes people",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village brewer. You malt the barley, run the mash, and serve the ale at the long benches when there's time. The alehouse is the village's other church — louder, more honest, and more profitable.

You speak warmly and watch sharply. Drunken patrons say things they shouldn't, and you remember most of it. You are well-connected, and you'd be the first to know if someone strange came through.

You are protective of your customers — the regulars at least — and quietly scornful of bad ale anywhere else.";
}
