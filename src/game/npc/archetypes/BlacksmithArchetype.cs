using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village blacksmith — master of the forge, brave, owns his shop.</summary>
public class BlacksmithArchetype : CraftsmanArchetype
{
    public override string ArchetypeId  => "blacksmith";
    public override ItemTag? SellTag => ItemTag.Ironwork;
    public override ItemTag? BuyTag  => ItemTag.Mineral;
    public override int    ModiMentisCount => 10;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string RoleNoun => "blacksmith";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a heavy-shouldered figure looks up from the anvil, soot streaking their arms",
        "a broad figure in a scorched leather apron turns a glowing bar with the tongs",
        "someone hammers a length of iron on the anvil, sparks leaping with each blow",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_arm", "right_arm", "left_hand", "right_hand", "backbone" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new SmithTongs(), () => new BlacksmithApron(), () => new LeatherGloves(), () => new Whetstone(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Hatchet(), () => new LeatherBelt(), () => new CoinPurse(), () => new Bread(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the smith here — that's my forge you can hear from the lane";
    public override string Workplace        => "the forge";
    public override string Craft            => "ironwork";
    public override string DailyLabour      => "beating hot iron flat until it agrees to be a tool";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Weather]    = "rain I can work in — it's the damp that gets into the charcoal and makes a liar of the fire",
        [DialogueTopic.Seasons]    = "before ploughing and before harvest, everyone remembers the smith; the rest of the year they forget I eat",
        [DialogueTopic.Harvest]    = "a good harvest means blunt sickles and full purses, and I'll take both",
        [DialogueTopic.Beasts]     = "an ox that's badly shod goes lame, and a lame ox costs a family its year",
        [DialogueTopic.Work]       = "iron tells you the truth. Heat it wrong and it splits; work it right and it holds for thirty years",
        [DialogueTopic.Health]     = "my hands are half scar and my ears are half deaf, and that's the price of the trade",
        [DialogueTopic.Trade]      = "I charge what the iron and the charcoal cost me, plus my arm. Anyone who calls that dear can mend their own share",
        [DialogueTopic.Rest]       = "a cold forge is a wasted day, but a man who never lets the fire die burns himself out first",
        [DialogueTopic.Stories]    = "they say iron keeps the ill-wishing out. I've never seen it, but I've never taken the horseshoe off my door either",
        [DialogueTopic.Neighbours] = "I know who pays and who promises. It's a shorter list than you'd hope",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village blacksmith. You forge iron into tools — saws, axes, sickles, ploughshares — that everyone in the village and the surrounding country depends on. Your forge is hot, your hands are scarred, and your patience for fools is short.

You speak in clipped, certain sentences. You measure people the way you measure iron: by what they're useful for. You are proud of your craft and protective of your workshop. You distrust strangers but will warm to anyone who shows respect for honest labour.

You know who in the village owes you money for tools, who's about to need a new ploughshare, and which farmers can be trusted to pay. Your voice carries the rumble of the bellows.";
}
