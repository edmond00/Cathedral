using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village miller — works the millstone, grinds grain into flour.</summary>
public class MillerArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "miller";

    /// <summary>What asking this person about their work teaches.</summary>
    public override string TradeModusMentisId => "millcraft";
    public override ItemTag? SellTag => ItemTag.Crop;
    public override ItemTag? BuyTag  => ItemTag.Crop;
    public override int    ModiMentisCount => 9;
    public override int    AuthorityLevel => 1;

    public override string RoleNoun => "miller";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a flour-dusted figure straightens by the millstone, white prints across their apron",
        "a pale-dusted figure heaves a sack of grain toward the hopper",
        "someone brushes meal from the millstone, the wheel groaning outside",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "backbone", "left_arm", "right_arm", "left_hand", "right_hand" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new MillPick(), () => new TallyStick(), () => new LinenTunic(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Grain(), () => new CoinPurse(), () => new WoodenBowl(), () => new SaltPouch(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the miller — the mill down the race is mine, whatever the lord's roll says";
    public override string Workplace        => "the mill";
    public override string Craft            => "the millstone";
    public override string DailyLabour      => "feeding the hopper, dressing the stone, and taking my toll out of every sack";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Water]      = "the race is everything. Too little and the wheel stands idle; too much and it takes my sluice out",
        [DialogueTopic.Harvest]    = "a heavy harvest is a heavy month. Everyone wants their grain ground the same week",
        [DialogueTopic.Trade]      = "everyone thinks the miller cheats them. I take the toll I'm owed, and I take it in front of them",
        [DialogueTopic.Neighbours] = "I know exactly who has grain to bring and who'll be hungry come the thaw. They know I know",
        [DialogueTopic.Weather]    = "a still week is worse for me than a storm. Give me wind or give me water, but give me something",
        [DialogueTopic.Work]       = "the stone must be dressed or the meal comes out coarse. Folk blame the flour and never the man who let the grooves go",
        [DialogueTopic.Stories]    = "there's a tale in every village about a miller who cheated and came to grief. I've heard it about myself",
        [DialogueTopic.Health]     = "the noise. Thirty years of it. Speak up or don't bother",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village miller. The mill is yours, or near enough — the lord owns the right but you take the toll, and you've taken it your whole life.

You are practical, occasionally suspicious, and you have a reputation. People always think the miller is cheating them. Sometimes you are. You speak loudly because the millstone is loud, even when you're nowhere near it.

You know who has grain to bring, who is short, and who is hungry this winter. You charge what you charge.";
}
