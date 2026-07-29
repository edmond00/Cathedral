using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Hermit NPC — reclusive sage, dialogue-capable, knows mountain secrets. Generally peaceful.</summary>
public class HermitArchetype : NamedNpcArchetype
{
    // Someone who has withdrawn from the world has usually lived in it first: 40–80 years.
    public override int MinAgeDays => 40 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 80 * LifetimeStat.DaysPerYear;

    public override string ArchetypeId => "hermit";
    public override ItemTag? SellTag => ItemTag.Forage;
    public override ItemTag? BuyTag  => ItemTag.Foodstuff;
    public override Species Species => SpeciesRegistry.Human;
    public override bool DefaultPersistent => true;
    public override int ModiMentisCount => 12;
    public override bool CanSpeak => true;

    /// <summary>Withdrew from the village on principle, and is respected for it by some.</summary>
    public override SocialCategory? Social  => SocialCategory.Religious;

    public override string RoleNoun => "hermit";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "an old solitary figure sits by a smouldering fire",
        "a gaunt, bearded figure watches from the mouth of a rough shelter",
        "someone in patched rags stirs a small pot, muttering to themselves",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "anamnesis", "hippocampus", "backbone", "viscera", "left_foot", "right_foot" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new PlainRobe(), () => new WoodenBowl(), () => new WalkingStaff(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Moss(), () => new Mushroom(), () => new Flint(), () => new WildBerry(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "nobody. That was the point of coming up here";
    public override string Workplace        => "the mountain";
    public override string Craft            => "keeping alive up here";
    public override string DailyLabour      => "water, fire, food, sleep. There is nothing else and there does not need to be";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Rest]       = "down there, rest is what's left over. Up here it's the whole day, and it took me years to bear it",
        [DialogueTopic.Neighbours] = "three moons since anyone came this way. What does that tell you?",
        [DialogueTopic.Food]       = "what the rock gives. Less than you'd think. Enough, so far",
        [DialogueTopic.Weather]    = "the mountain makes its own. It doesn't consult the valley and it doesn't consult me",
        [DialogueTopic.Kin]        = "I had people. That's all I'll say on it",
        [DialogueTopic.Stories]    = "there's marks cut in the stone up here older than any tale anyone's still telling. Nobody comes to read them",
        [DialogueTopic.Omens]      = "signs. Everything is a sign if you're lonely enough. That's the danger of it",
        [DialogueTopic.Health]     = "the cold gets into the joints and stays. I've stopped calling it an ailment. It's just the shape of me now",
        [DialogueTopic.Wilds]      = "the peak doesn't care about your name. That's the first comfortable thing I ever learned",
        [DialogueTopic.Roads]      = "every road goes back. That's what's wrong with them",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a hermit who retreated from civilization long ago. You live alone in the mountains, eating what the rock gives you, sleeping where the wind allows. Visitors are rare — and rarely welcome.

You speak in fragments and riddles. Your sentences are short, sometimes incomplete. You might trail off mid-sentence. You often answer questions with other questions. You are not hostile — just deeply uninterested in small talk. If someone persists with patience and genuine curiosity, however, you may share fragments of hard-won knowledge about the mountain paths, hidden caves, weather patterns, or old stories carved into the stone.

Your speech is sparse and cryptic: 'The peak doesn't care about your name.' or 'Three moons since anyone came this way. What does that tell you?' You do not volunteer information. You test whether the visitor is worth speaking to.";
}
