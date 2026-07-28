using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

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

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_arm", "right_arm", "left_hand", "right_hand", "backbone", "pulmones" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new HeavyPick(), () => new MinersLamp(), () => new LeatherGloves(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Rock(), () => new Flint(), () => new DriedMeat(), () => new LeatherCanteen(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "I work a seam up in the rock — the ore the forge burns through comes down on my back";
    public override string Workplace        => "the seam";
    public override string Craft            => "the pick";
    public override string DailyLabour      => "tapping at rock by lantern-light and hauling out what's worth the carrying";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work]       = "the rock gives what it gives. You can't hurry it and you can't argue with it",
        [DialogueTopic.Omens]      = "you don't whistle in a shaft and you don't sit on the ore-pile. Laugh if you like — I've buried men who laughed",
        [DialogueTopic.Health]     = "the dust. It gets in and it stays in, and every miner I've known past fifty coughs the same",
        [DialogueTopic.Wilds]      = "underground is quieter than any wood. Different quiet, though. It listens back",
        [DialogueTopic.Weather]    = "makes no odds to me. I'm inside the hill either way",
        [DialogueTopic.Rest]       = "daylight, sat down, with my back against something warm. That's all I want out of a rest day",
        [DialogueTopic.Neighbours] = "sound carries out of the mouth of a shaft. I've learned not to say things near it",
        [DialogueTopic.Stories]    = "there's tales about what's deeper in. I've been deep enough to stop repeating them",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a miner who works a small ore-vein in a cave nearby and brings the ore down to the village forge every few days.

You speak with the gruff calm of someone who has worked alone in the dark for years. You have superstitions — you don't whistle in a shaft, you don't sit on the ore-pile.

You distrust loose talk near the entrance — sound carries, and not all of it is the kind you want returned.";
}
