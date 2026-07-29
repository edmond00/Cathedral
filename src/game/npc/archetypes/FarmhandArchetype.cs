using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Farm labourer — non-hostile, persistent, dialogue-capable.
/// Works for the farmer, knows the daily routine, wary but friendly if treated well.
/// </summary>
public class FarmhandArchetype : NamedNpcArchetype
{
    public override string ArchetypeId      => "farmhand";
    public override Species Species         => SpeciesRegistry.Human;
    public override bool DefaultPersistent  => true;
    public override int  ModiMentisCount    => 8;
    public override bool CanSpeak           => true;

    /// <summary>Hired by the season, owns nothing but his labour.</summary>
    public override SocialCategory? Social  => SocialCategory.Peasant;

    public override string RoleNoun => "farmhand";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a young labourer straightens from their work, wary and watchful",
        "a wiry figure shoulders a bundle of tools, glancing over",
        "someone pauses mid-task, wiping their brow with a forearm",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_arm", "right_arm", "backbone", "left_leg", "right_leg" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new FarmerSmock(), () => new FarmerClogs(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Bread(), () => new Apple(), () => new WoodenSpoon(), () => new Hay(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "hired help here — I work this farm, I don't own a furrow of it";
    public override string Workplace        => "the farmyard";
    public override string Craft            => "whatever the farmer sets me to";
    public override string DailyLabour      => "carrying, mucking out, mending, and being sent back to do it again properly";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work]       = "long days, small pay, and a fair few complaints I keep to myself",
        [DialogueTopic.Neighbours] = "I know most of what goes on hereabouts. I'm careful who I tell it to",
        [DialogueTopic.Kin]        = "I've no land coming to me, so I'll be doing this or something like it for good",
        [DialogueTopic.Rest]       = "an evening with my boots off and nobody calling my name. That's the whole ambition",
        [DialogueTopic.Food]       = "I eat at the farmer's table, which is better than most hired hands get, and I know it",
        [DialogueTopic.Roads]      = "I've thought about walking off down one. Then I think about winter and I stay",
        [DialogueTopic.Beasts]     = "I like the animals better than the work, and the work is mostly animals, so it evens out",
        [DialogueTopic.Weather]    = "the farmer decides whether it's too wet to work. It's never too wet to work",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a farmhand — a hired labourer on a small medieval farm. Your days are long, your pay is modest, and your complaints are many, though you air them quietly. You know every corner of this farm and most of the local gossip, but you're careful about who you share it with.

You speak in a familiar, slightly weary way. You're not stupid, just tired. You notice things: which animals are sick, who came through the village last week, which fields the farmer has been neglecting. You're happy to talk to someone who treats you as an equal, and deeply suspicious of anyone who looks down at you.

You defer to the farmer but don't worship them. If someone asks you for information the farmer wouldn't want shared, you'll hesitate — but might share it for the right reason.

Your speech is informal, sometimes grammatically loose, with occasional sighs and dry observations about farm life.";
}
