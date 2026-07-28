using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Field reaper / sower — seasonal title, swings sickle or scythe.</summary>
public class ReaperArchetype : PeasantArchetype
{
    public override string ArchetypeId => "reaper";
    public override ItemTag? SellTag => ItemTag.Crop;
    public override ItemTag? BuyTag  => ItemTag.Tool;
    public override int    ModiMentisCount => 7;

    public override string RoleNoun => "reaper";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a stooped figure straightens with a sickle in hand, grain dust on their sleeves",
        "a sunburnt figure swings a scythe through standing corn",
        "someone binds a sheaf and stacks it, chaff drifting around them",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "right_arm", "left_arm", "backbone", "left_hand", "right_hand" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Sickle(), () => new Whetstone(), () => new FarmerStrawHat(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Bread(), () => new DriedPeas(), () => new LeatherCanteen(), () => new Straw(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "a reaper this season — sower in the spring, whatever's wanted after";
    public override string Workplace        => "the standing corn";
    public override string Craft            => "the sickle";
    public override string DailyLabour      => "swinging from dawn to dusk and binding what I've cut behind me";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Harvest]    = "three weeks in the year decide the other forty-nine. Everything I am is bent toward them",
        [DialogueTopic.Weather]    = "rain on cut corn left lying is the worst sight there is. You watch the sky the whole time",
        [DialogueTopic.Work]       = "you find a rhythm and you don't break it. Break it and your arms remember all at once how tired they are",
        [DialogueTopic.Stories]    = "there's a song for the last sheaf and everyone sings it, even the ones who say they won't",
        [DialogueTopic.Rest]       = "noon, in the shade, with the sickle laid down. Twenty minutes and I'd swear I was a new man",
        [DialogueTopic.Health]     = "cuts on my hands from the straw, every year, and they never quite heal before it's over",
        [DialogueTopic.Seasons]    = "I'm a reaper in summer and a sower in spring. Same back, different name",
        [DialogueTopic.Food]       = "bread out of grain I cut myself. It doesn't taste better, but it sits better",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a reaper. In summer you swing the sickle from dawn to dusk; in autumn you tie sheaves; in spring you might be a sower instead. The work is bone-deep tiring and the pay is in days, not coin.

You speak with the rhythm of work — a few words, a pause, a few words. You like a song at noon. You are quick with a smile if treated like a person.

You know the strip you're on the way you know your own hand.";
}
