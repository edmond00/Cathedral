using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Forest charcoal burner — tends the slow-burning mound that turns logs into coal for the village forge.
/// </summary>
public class CharcoalBurnerArchetype : WildernessNpcArchetype
{
    public override string ArchetypeId => "charcoal_burner";
    public override ItemTag? SellTag => ItemTag.Mineral;
    public override ItemTag? BuyTag  => ItemTag.Wood;
    public override int    ModiMentisCount => 7;

    public override string RoleNoun => "charcoal burner";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a soot-blackened figure tends a smouldering mound of earth, smoke curling",
        "a grimed figure rakes ash from a cooling clamp",
        "someone stacks cordwood into a dome, face streaked with black",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "pulmones", "nose", "left_arm", "right_arm", "left_hand", "right_hand" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new CharcoalRake(), () => new LeatherGloves(), () => new Flint(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new DriedMeat(), () => new LeatherCanteen(), () => new WoolCloak(), () => new Torch(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the one who burns the coal for the forge — that smoke through the trees is mine";
    public override string Workplace        => "the clamp";
    public override string Craft            => "charcoal";
    public override string DailyLabour      => "watching a smouldering mound day and night for a week, and sleeping in snatches beside it";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work]       = "a week awake beside the mound. Let air in once and you've a bonfire and nothing to sell",
        [DialogueTopic.Wilds]      = "the wood is my whole world. I know the smell of every kind of it when it chars",
        [DialogueTopic.Weather]    = "wind is the danger. It finds a hole in the turf and eats the whole clamp before I can smother it",
        [DialogueTopic.Rest]       = "I don't rest while it burns. After — I sleep two days and folk think I've died",
        [DialogueTopic.Neighbours] = "I'm alone so long that when I do meet someone I talk too much. Stop me when you've had enough",
        [DialogueTopic.Health]     = "black in my chest and black under my nails, and neither of them comes out",
        [DialogueTopic.Omens]      = "smoke that leans against the wind. I've seen it twice. I don't know what it means and I don't like it",
        [DialogueTopic.Stories]    = "there's things move about at night out here. Mostly deer. Mostly",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a charcoal burner. You tend the great smouldering mound day and night for a week at a time, turning logs to charcoal that the village forge will burn.

You speak rarely, in a slow voice, as if measuring smoke. You are alone for long stretches and when you do meet someone you talk a little too much, then catch yourself.

You know the smell of every kind of wood when it chars.";
}
