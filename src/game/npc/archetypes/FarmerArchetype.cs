using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Farm owner — non-hostile, persistent, dialogue-capable.
/// Runs the holding, knows the land, suspicious of strangers.
/// </summary>
public class FarmerArchetype : NamedNpcArchetype
{
    public override string ArchetypeId      => "farmer";

    /// <summary>What asking this person about their work teaches.</summary>
    public override string TradeModusMentisId => "tillage";
    public override ItemTag? SellTag        => ItemTag.Crop;
    public override ItemTag? BuyTag         => ItemTag.Tool;
    public override Species Species         => SpeciesRegistry.Human;
    public override bool DefaultPersistent  => true;
    public override int  ModiMentisCount    => 10;
    public override bool CanSpeak           => true;

    /// <summary>Holds his own land, but works it with his own hands.</summary>
    public override SocialCategory? Social  => SocialCategory.Peasant;
    public override bool IsBrave            => true;   // owns the land, will demand a fight
    public override int  AuthorityLevel     => 1;      // landowner

    public override string RoleNoun => "farmer";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a broad-shouldered figure in a mud-stained smock watches you",
        "a weathered figure straightens from the soil, hands caked with earth",
        "someone leans on a hoe at the field's edge, taking your measure",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "backbone", "left_arm", "right_arm", "cerebrum", "left_eye", "right_eye" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new FarmerSmock(), () => new FarmerBreeches(), () => new LeatherBoots(), () => new TallyStick(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new CoinPurse(), () => new DroversSwitch(), () => new Bread(), () => new Cheese(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the one who holds this land — the fields you walked in are mine and my father's before";
    public override string Workplace        => "the holding";
    public override string Craft            => "the land";
    public override string DailyLabour      => "up before light, round the beasts, round the fields, and settling accounts after dark";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Harvest]    = "a farm is one bad year from ruin and two good years from nothing much. Nobody believes that until it happens to them",
        [DialogueTopic.Weather]    = "I've watched this sky my whole life and it still surprises me. Anyone who claims to predict it is selling something",
        [DialogueTopic.Beasts]     = "the beasts eat first, then the household. Get that order wrong once and you'll not get it wrong twice",
        [DialogueTopic.Work]       = "there's no idle season. There's only the season where the work is indoors",
        [DialogueTopic.Kin]        = "this land goes to my children and it'll take the same out of them it took out of me",
        [DialogueTopic.Neighbours] = "I lend a team and I expect a team lent back. That's the whole of how a village stands up",
        [DialogueTopic.Trade]      = "I sell in autumn when everyone's selling, so I sell cheap. Anyone who tells you farmers grow rich has never held a farm",
        [DialogueTopic.Roads]      = "strangers on my land want work, want food, or want taking hold of. I find out which, quickly",
        [DialogueTopic.Seasons]    = "the year is a wheel and you're under it. Keep up or it rolls over you",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a medieval farmer who has worked this land your whole life. You rise before dawn, you know every slope of your fields and every habit of your animals. You have no patience for idleness or fancy talk.

You are not unkind, but you are direct — sometimes to the point of rudeness. You speak in plain, short sentences about practical things: the weather, the harvest, the state of the soil, the price of grain at the market. You distrust anyone whose hands are clean.

You may warm to someone who respects the land and shows common sense. You grow cold and terse with anyone who seems lazy, dishonest, or entitled. If pushed, you will order them off your holding without hesitation.

You have a family and farmhands depending on you. Everything you say and do is coloured by that responsibility.";
}
