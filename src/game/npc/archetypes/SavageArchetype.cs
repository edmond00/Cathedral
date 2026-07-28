using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Savage NPC — territorial wild human, initially hostile, can be befriended or fought.</summary>
public class SavageArchetype : NamedNpcArchetype
{
    public override string ArchetypeId => "savage";
    public override Species Species => SpeciesRegistry.Human;
    public override bool DefaultEnemy => true;
    public override bool DefaultPersistent => true;
    public override int ModiMentisCount => 10;
    public override bool CanSpeak => true;

    public override string RoleNoun => "savage";
    protected override bool LabelMentionsLocation => false;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a wild, paint-streaked figure crouches nearby, eyeing you with suspicion",
        "a matted, half-clad figure bares its teeth from behind a rock",
        "someone daubed in ochre watches from the brush, spear held low",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_leg", "right_leg", "left_arm", "right_arm", "teeths", "nose" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new HuntingSpear(), () => new AnimalHide(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Flint(), () => new DriedMeat(), () => new Rock(), () => new Bark(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "I am here. Was here before you. That is all the name you need";
    public override string Workplace        => "this ground";
    public override string Craft            => "taking what the land has";
    public override string DailyLabour      => "hunt. Eat. Watch. Sleep short";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Wilds]      = "this is not wild. This is home. Your fields are the strange thing",
        [DialogueTopic.Beasts]     = "the beast does not lie about what it wants. I like the beast better",
        [DialogueTopic.Food]       = "meat. Root. What I take, I eat. What I do not take, I do not eat",
        [DialogueTopic.Weather]    = "cold comes. You get low, you get out of the wind, you live. Simple",
        [DialogueTopic.Work]       = "you people work for another person's food. I do not understand it and I do not want to",
        [DialogueTopic.Rest]       = "sleep short. Wake if something moves. Always",
        [DialogueTopic.Kin]        = "had people once. Gone. Do not ask more",
        [DialogueTopic.Roads]      = "roads bring more of you. Never fewer",
        [DialogueTopic.Health]     = "hurt heals or it does not. No herbs, no fussing",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a wild human who has lived outside civilization for as long as you can remember. You speak in broken, clipped sentences — grammar is an afterthought. You rely on actions more than words. You are territorial and suspicious of soft-handed strangers.

You communicate bluntly: 'You. Why here.' or 'This place mine. Go.' or 'Strong? Show.' You respect strength, endurance, and directness. Flattery confuses you. Weakness disgusts you. But if someone proves themselves — through courage, honesty, or an offering of food — you may grudgingly accept their presence.

You know the wild intimately: animal tracks, edible roots, shelter spots, danger signs. You might share this knowledge if trust is established, but always in your own terse way. You never apologize.";
}
