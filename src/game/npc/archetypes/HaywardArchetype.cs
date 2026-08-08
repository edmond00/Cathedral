using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Field hayward — fence and crop guard, walks the margin, watches for damage.</summary>
public class HaywardArchetype : PeasantArchetype
{
    public override string ArchetypeId => "hayward";

    /// <summary>What asking this person about their work teaches.</summary>
    public override string TradeModusMentisId => "hedgecraft";
    public override ItemTag? SellTag => ItemTag.Crop;
    public override ItemTag? BuyTag  => ItemTag.Tool;
    public override int    ModiMentisCount => 7;
    public override int    AuthorityLevel => 1;

    public override string RoleNoun => "hayward";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a stick-armed figure paces the edge of the field, eyes on the hedge-line",
        "a wiry figure mends a gap in the hedge, switch under one arm",
        "someone walks the boundary with a keen, suspicious eye",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_eye", "right_eye", "left_leg", "right_leg", "left_foot", "right_foot" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new DroversSwitch(), () => new LeatherGloves(), () => new WoolCloak(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new HuntingKnife(), () => new Rope(), () => new WalkingStaff(), () => new Bread(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the hayward — the hedges and the boundary are my charge, and I walk them every day";
    public override string Workplace        => "the field margin";
    public override string Craft            => "hedging and watching";
    public override string DailyLabour      => "walking the boundary from light to dark, mending gaps and turning stray beasts back";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Beasts]     = "one loose beast in standing corn can undo a month. That's why I'm hard about the hedges",
        [DialogueTopic.Wilds]      = "deer come out of the wood at dusk and eat what a family was counting on. I've no love for them",
        [DialogueTopic.Work]       = "everyone thinks I only walk about. Then a gap opens and they find out what I was doing",
        [DialogueTopic.Neighbours] = "I know who cuts a corner off the common and hopes nobody counted. Somebody counted",
        [DialogueTopic.Weather]    = "wind is my enemy. It takes the top off a hedge and I'm a week putting it back",
        [DialogueTopic.Harvest]    = "from the ear filling to the last sheaf carried, I don't sleep properly. That's when everything gets taken",
        [DialogueTopic.Roads]      = "strangers on the boundary at dusk. Nine times in ten it's nothing. It's the tenth I'm out here for",
        [DialogueTopic.Rest]       = "when the field's bare and there's nothing left to steal, then I rest",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the hayward. You walk the field margin from morning to evening, watching for stray animals, broken hedges, and bondmen sleeping in the shade. You report damage to the reeve.

You speak with a watchman's economy — terse, plain, ready to interrupt. You like dogs more than people. You distrust strangers near the strips.

You will challenge anyone you don't know, and you keep a stick handy.";
}
