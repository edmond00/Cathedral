using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Field plowman — drives the team, opens the strips, hard physical labour.</summary>
public class PlowmanArchetype : PeasantArchetype
{
    public override string ArchetypeId => "plowman";
    public override int    ModiMentisCount => 7;

    public override string RoleNoun => "plowman";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a heavy figure leans on the plough's stilt, oxen breathing hard ahead",
        "a broad figure guides the share through heavy soil, reins looped over a shoulder",
        "someone turns a straight dark furrow, mud clotted on their boots",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_arm", "right_arm", "backbone", "left_leg", "right_leg" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new FarmerSmock(), () => new FarmerClogs(), () => new DroversSwitch(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Bread(), () => new Cheese(), () => new DrinkingHorn(), () => new Whetstone(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "a plowman — that's my team standing over there, and that furrow's mine";
    public override string Workplace        => "the open strips";
    public override string Craft            => "the plough";
    public override string DailyLabour      => "walking behind the team from first light, holding the share true through heavy ground";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Beasts]     = "I know my oxen better than I know most people, and I'd say that's the right way round",
        [DialogueTopic.Weather]    = "you can't plough wet clay. It rolls up on the share and you spend the day scraping instead of turning",
        [DialogueTopic.Seasons]    = "the ploughing weeks are the hardest of my year and the ones I'd not give up",
        [DialogueTopic.Work]       = "a straight furrow is the one thing I've got that nobody can argue with",
        [DialogueTopic.Health]     = "my back. Everyone who's held the stilts more than ten years says the same word",
        [DialogueTopic.Rest]       = "a quiet word at noon and a sit-down out of the wind. That'll do me",
        [DialogueTopic.Food]       = "bring me ale at noon and you can talk at me as long as you like",
        [DialogueTopic.Stories]    = "there's an old song for the ploughing. I sing it badly and the oxen don't complain",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a plowman. You work the heavy team, opening the strips at first light, sweating through the day. Your back is sore. Your boots are heavy with mud.

You speak slowly and not at length. You like a quiet word at noon. You know oxen better than you know people, and you suspect that's the right way round.

You'll talk to anyone who isn't above you, especially if they bring ale.";
}
