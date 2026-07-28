using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Druid NPC — nature keeper, dialogue-capable, can trade herbs. Hostile if disrespected.</summary>
public class DruidArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "druid";
    public override ItemTag? SellTag => ItemTag.Herb;
    public override Species Species => SpeciesRegistry.Human;
    public override bool DefaultPersistent => true;
    public override int ModiMentisCount => 12;
    public override bool CanSpeak => true;

    public override string RoleNoun => "druid";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a robed figure leans against a gnarled staff",
        "a hooded figure traces a sign in the air, beads and bone at their belt",
        "someone stands motionless among the trees, eyes half-closed",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "anamnesis", "cerebrum", "hippocampus", "nose", "left_eye", "right_eye" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new PlainRobe(), () => new Herb(), () => new WalkingStaff(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Mushroom(), () => new Moss(), () => new Poppy(), () => new WoodenBowl(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "I keep these woods, and have since before most of what you can see was standing";
    public override string Workplace        => "the grove";
    public override string Craft            => "root and leaf";
    public override string DailyLabour      => "walking the wood, gathering what it offers, and taking nothing it did not";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Wilds]      = "the wood is not a place I live in. It is the thing I belong to, and it has no need of me at all",
        [DialogueTopic.Weather]    = "rain is not weather. It is the wood drinking, and it drinks when it chooses",
        [DialogueTopic.Seasons]    = "the year does not turn. It breathes — in through spring, out through autumn — and we are inside it",
        [DialogueTopic.Beasts]     = "the fox and the hare keep an older bargain than any village council ever wrote down",
        [DialogueTopic.Health]     = "most sickness is a body out of step with the year. Nettle, willow-bark and patience mend more than anyone credits",
        [DialogueTopic.Omens]      = "the moss remembers what the stone forgets. Signs are only memory, spoken by something with no mouth",
        [DialogueTopic.Stories]    = "the old tales are the wood's own account of itself, badly copied by people in a hurry",
        [DialogueTopic.Work]       = "you cannot make a wood. You can only stop making it worse, and that is work enough",
        [DialogueTopic.Roads]      = "roads are cuts. They heal over if you let them, and nobody ever lets them",
        [DialogueTopic.Neighbours] = "the village fears me a little, which is convenient, and needs me a little, which is safer",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a druid who has lived in these woods for decades. The trees are your congregation, the fungi your messengers, the rain your hymn. You distrust outsiders on principle — not from malice, but because most who come here take without asking.

You speak slowly and deliberately, often in metaphor drawn from the living world. You might say 'the birch does not bend for strangers' or 'the moss remembers what the stone forgets.' You are patient, but firm. You share knowledge of plants, fungi, weather signs, and animal behavior — but only once trust is established.

If someone shows genuine respect for the forest, you warm to them considerably. If they speak of cutting, burning, or taking carelessly, you grow cold and curt. You will not attack unprovoked, but you make your displeasure clear.

Your speech is unhurried, slightly archaic, and full of nature imagery. You never raise your voice.";
}
