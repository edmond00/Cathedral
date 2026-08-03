using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village weaver — master of the loom, makes cloth and linen.</summary>
public class WeaverArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "weaver";

    /// <summary>What asking this person about their work teaches.</summary>
    public override string TradeModusMentisId => "threadwork";
    public override ItemTag? SellTag => ItemTag.Clothing;
    public override ItemTag? BuyTag  => ItemTag.Textile;
    public override int    ModiMentisCount => 8;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string RoleNoun => "weaver";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a small figure leans into the loom's clatter, fingers flying through the warp",
        "a slight figure winds thread onto a shuttle, eyes on the pattern",
        "someone works a treadle loom, cloth inching out beneath their hands",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_hand", "right_hand", "left_eye", "right_eye", "cerebellum" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Spindle(), () => new BoneNeedle(), () => new LinenTunic(), () => new MendingKit(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Wool(), () => new WoolCloak(), () => new WoolCap(), () => new Candle(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the weaver here — that clatter down the row is my loom";
    public override string Workplace        => "the loom";
    public override string Craft            => "weaving";
    public override string DailyLabour      => "counting threads through the warp until the light goes and my eyes give out";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Weather]    = "grey days are the ruin of me. I can't see the pattern and I won't weave a fault into good cloth",
        [DialogueTopic.Beasts]     = "I can tell you which farm a fleece came off before you name it. Thin sheep make thin thread",
        [DialogueTopic.Work]       = "one careless hand at my loom undoes a day. That's why I'm sharp with people at it",
        [DialogueTopic.Seasons]    = "shearing, then retting the flax, then the long dark half of the year at the treadles. That's my year",
        [DialogueTopic.Kin]        = "everyone in my house winds thread from the moment they can hold a spindle. It isn't cruelty, it's cloth",
        [DialogueTopic.Neighbours] = "I see whose cloak is fraying and whose is new. It tells you more about a household than talking to them does",
        [DialogueTopic.Health]     = "my eyes and my back. Both going, both from the same chair",
        [DialogueTopic.Stories]    = "the old women say a fault woven in on purpose keeps the ill luck out. I've never dared leave one in",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village weaver. You take wool from the farmers and flax from the field and turn them into cloth — undyed for the poor, sometimes dyed for the better-off.

You speak softly but precisely, like someone counting threads. You miss very little. You know which farms have skinny sheep, who has been sneaking flax past the reeve, and whose cloak is fraying.

You are wary of strangers — your loom is your livelihood, and a clumsy hand could undo a day's work in a moment.";
}
