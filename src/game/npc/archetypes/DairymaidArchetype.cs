using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Farm dairymaid — milks cows, churns butter, presses cheese.</summary>
public class DairymaidArchetype : PeasantArchetype
{
    public override string ArchetypeId => "dairymaid";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Craftware;
    public override int    ModiMentisCount => 7;

    protected override NameGender? GenderBias => NameGender.Female;

    public override string RoleNoun => "dairymaid";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a quick figure leans against the churn, sleeves rolled, hands red",
        "a brisk figure carries two brimming pails on a yoke",
        "someone skims cream from a shallow pan, apron spotted with milk",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_hand", "right_hand", "left_arm", "right_arm", "nose" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new MilkPail(), () => new ChurnPaddle(), () => new LinenTunic(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Cheese(), () => new WoodenBowl(), () => new WoolStockings(), () => new SaltPouch(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the one who keeps the dairy here — the cows know me better than the household does";
    public override string Workplace        => "the dairy";
    public override string Craft            => "butter and cheese";
    public override string DailyLabour      => "milking before light, then churning until my arms are wood";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Beasts]     = "a cow milks best for a quiet voice. Shout at her once and she'll hold it back all week out of spite",
        [DialogueTopic.Food]       = "fresh butter on warm bread. There's nothing on any lord's table better than that, and I'd argue it",
        [DialogueTopic.Weather]    = "thundery weather turns the milk before you can get it into the pan. I hate a close sky",
        [DialogueTopic.Seasons]    = "spring grass makes yellow butter and summer's the best cheese. Winter milk is thin, sad stuff",
        [DialogueTopic.Health]     = "my hands are red raw half the year and my back went years ago. Nobody warns you about the back",
        [DialogueTopic.Rest]       = "I'm done by noon, and that's the one mercy in it",
        [DialogueTopic.Kin]        = "I'd like a house of my own one day, with two cows in it and nobody's schedule but mine",
        [DialogueTopic.Stories]    = "they say a cheese won't set if there's a quarrel in the house. Laugh, but I've seen it hold true",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the dairymaid. You start before dawn — milking the cows, then churning, then pressing. By noon the work is mostly done and you are mostly tired.

You speak warmly, with a country directness. You like having someone to talk to while you work. You know which cow is going dry, which is in season, which butter went to which household this week.

You are not a gossip — quite — but you are very current.";
}
