using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village cooper — barrel maker, works with staves and iron hoops.</summary>
public class CooperArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "cooper";

    /// <summary>What asking this person about their work teaches.</summary>
    public override string TradeModusMentisId => "cellarcraft";
    public override ItemTag? SellTag => ItemTag.Craftware;
    public override ItemTag? BuyTag  => ItemTag.Wood;
    public override int    ModiMentisCount => 8;
    public override int    AuthorityLevel => 1;

    public override string RoleNoun => "cooper";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a stocky figure works iron over a half-built barrel, hammer-taps ringing",
        "a broad figure bends a stave around a mould, muscles taut",
        "someone hoops a cask tight, shavings curling underfoot",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_hand", "right_hand", "left_arm", "right_arm", "backbone" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Drawknife(), () => new WoodenMallet(), () => new LeatherBelt(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Rope(), () => new Whetstone(), () => new WoodChisel(), () => new DrinkingHorn(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the cooper here — every barrel in this place has passed through my hands";
    public override string Workplace        => "the cooperage";
    public override string Craft            => "barrel-making";
    public override string DailyLabour      => "bending staves round a mould and driving hoops until the cask holds water";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Weather]    = "dry weather shrinks a cask and it weeps. Wet it once and it swells back tight — folk think that's magic",
        [DialogueTopic.Wilds]      = "oak for casks, and nothing else. Pine warps and pine tastes; I'll not have it in my yard",
        [DialogueTopic.Work]       = "a barrel that leaks is my shame walking about the village with my name on it",
        [DialogueTopic.Food]       = "everything worth keeping through winter is kept in something I made. Remember that at the table",
        [DialogueTopic.Neighbours] = "nobody minds the cooper. I'm in every cellar and every yard, and I hear the lot of it",
        [DialogueTopic.Rest]       = "ale, a bench and someone talking nonsense at me — that's an evening well spent",
        [DialogueTopic.Trade]      = "the brewer and the miller keep me fed. I'd not price myself out of either",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village cooper. You shape staves, fit hoops, and bind barrels for the brewer, the miller, and the farms. A barrel that leaks is your shame.

You speak with the cadence of a hammer — steady, unhurried. You like to talk about wood: which oak holds, which pine warps. You aren't loud, but you are observed: nobody minds the cooper, and so you hear most everything.

You like ale and good company. You think gentry are noisy.";
}
