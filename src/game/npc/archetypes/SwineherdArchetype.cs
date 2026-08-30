using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Farm swineherd — minds the pigs, runs them out to mast in autumn.</summary>
public class SwineherdArchetype : PeasantArchetype
{
    public override string ArchetypeId => "swineherd";

    /// <summary>What asking this person about their work teaches.</summary>
    public override string TradeModusMentisId => "husbandry";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Crop;
    public override int    ModiMentisCount => 6;

    public override string RoleNoun => "swineherd";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a thick-booted figure waves a switch over a knot of pigs, mud to the knees",
        "a stout figure drives a grunting drove between the trees",
        "someone tips a pail of scraps into a trough, pigs crowding in",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "nose", "left_arm", "right_arm", "left_leg", "right_leg", "viscera" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new DroversSwitch(), () => new FarmerSmock(), () => new HuntingKnife(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Turnip(), () => new DriedMeat(), () => new SaltPouch(), () => new Mushroom(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the swineherd — yes, you can tell, and no, I'll not apologise for it";
    public override string Workplace        => "the pig pens";
    public override string Craft            => "the drove";
    public override string DailyLabour      => "hauling scraps to the trough and driving the drove out to root";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Beasts]     = "a pig is cleverer than a dog and twice as stubborn. Anyone who calls them filthy has never watched one choose where to lie",
        [DialogueTopic.Wilds]      = "acorn-time in the wood is the best month of my year. They come back fat and I come back happy",
        [DialogueTopic.Food]       = "bacon through winter is the difference between hard and hungry. Everyone forgets that until spring",
        [DialogueTopic.Seasons]    = "autumn for the mast, and then the killing when the cold comes. That's the shape of it",
        [DialogueTopic.Neighbours] = "folk stand upwind of me and eat my pork all winter. I find that funny more days than not",
        [DialogueTopic.Health]     = "I've never been ill a day. I put it down to the pigs, which offends people",
        [DialogueTopic.Work]       = "it's mud and shouting, but nobody looks over my shoulder. There's worse trades",
        [DialogueTopic.Rest]       = "sit down anywhere near a pig pen and something will come and lean on you. It's not restful, but it's company",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the swineherd. You mind the farm's pigs — the sow and her piglets, the boar in the corner pen — and in autumn you'd take them out to root for acorns.

You speak in good-humoured grumbles, with a low voice that the pigs answer to. You smell of pigs. You know it. You don't apologise for it.

You're easy to talk to once people get past the smell — and you find that most people who get past it are worth talking to.";
}
