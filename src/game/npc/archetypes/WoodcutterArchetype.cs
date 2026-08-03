using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Forest woodcutter — fells timber and hauls logs, three days in the wood, two in the village.
/// Carries an axe, rope, and sack.
/// </summary>
public class WoodcutterArchetype : WildernessNpcArchetype
{
    public override string ArchetypeId => "woodcutter";

    /// <summary>What asking this person about their work teaches.</summary>
    public override string TradeModusMentisId => "woodcraft";
    public override ItemTag? SellTag => ItemTag.Wood;
    public override ItemTag? BuyTag  => ItemTag.Tool;
    public override int    ModiMentisCount => 8;
    public override bool   IsBrave      => true;

    public override string RoleNoun => "woodcutter";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a hard-handed figure stands beside a felled log, axe leaning against a thigh",
        "a broad figure splits a round with one clean stroke",
        "someone limbs a fallen trunk, chips scattered across the moss",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_arm", "right_arm", "backbone", "left_hand", "right_hand", "left_leg" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Hatchet(), () => new Rope(), () => new Whetstone(), () => new LeatherBoots(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new DriedMeat(), () => new Flint(), () => new LeatherCanteen(), () => new WoolCloak(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "I cut timber out in the deep wood — three days in, then down to the village with it";
    public override string Workplace        => "the deep wood";
    public override string Craft            => "the axe";
    public override string DailyLabour      => "felling, limbing, and dragging out more weight than a person ought to";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Wilds]      = "I can read a wood the way some read a book. Which oaks are cracked, which ash is sound, where the wolves run",
        [DialogueTopic.Beasts]     = "wolves keep their distance from an axe and a fire. It's the boar in spring that'll actually come at you",
        [DialogueTopic.Seasons]    = "fell in winter. The sap's down and the timber's true. Anyone cutting in summer is selling you a warp",
        [DialogueTopic.Work]       = "a tree comes down where it wants unless you've read it right. Read it wrong once and there's no second time",
        [DialogueTopic.Rest]       = "a fire and a bowl of something hot after three days out. Nothing better exists",
        [DialogueTopic.Weather]    = "wind in the canopy and I put the axe down. No timber is worth standing under that",
        [DialogueTopic.Neighbours] = "the carpenter and the cooper. Between them they're my whole trade and half my conversation",
        [DialogueTopic.Stories]    = "you hear things out there at night. It's owls. It's always owls. I still sleep near the fire",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a woodcutter who works the deep wood for three days at a time and brings the timber back to the village to sell to the carpenter and the cooper.

You speak with the quiet of someone used to long silences and the company of trees. You measure your words. You like a fire and a bowl of stew at the end of a day.

You can read a forest the way some people read a book. You know which oaks are cracked, which ash is sound, where the wolves run.";
}
