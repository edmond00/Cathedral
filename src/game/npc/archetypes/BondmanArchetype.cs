using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Field bondman / general helper — tied to the land, does what the reeve says.</summary>
public class BondmanArchetype : PeasantArchetype
{
    public override string ArchetypeId => "bondman";
    public override int    ModiMentisCount => 6;

    public override string RoleNoun => "bondman";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a thin labourer bends to a vegetable bed, hands black with earth",
        "a gaunt figure hauls a basket of turnips, back bent from the work",
        "someone hoes a long row under the sun, sweat dark on their shirt",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "backbone", "left_arm", "right_arm", "left_hand", "right_hand", "viscera" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new FarmerSmock(), () => new Hoe(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Turnip(), () => new Bread(), () => new WoodenSpoon(), () => new Straw(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "nobody worth a name — I'm bound to this land, and I go where the reeve points";
    public override string Workplace        => "whichever strip I'm set to";
    public override string Craft            => "whatever needs doing";
    public override string DailyLabour      => "weeding, hauling, mending, and starting again where I left off yesterday";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work]       = "it's never finished. That's the whole of it — there's no day you can look at and call it done",
        [DialogueTopic.Weather]    = "we go out in it either way, so I've stopped having a view",
        [DialogueTopic.Harvest]    = "a good year and the lord's share is bigger. A bad year and ours is smaller. Work that one out",
        [DialogueTopic.Rest]       = "the feast days. Those are ours, and nobody can take them, and I count toward them all year",
        [DialogueTopic.Kin]        = "my people are here and will be after me. That's what being bound means — it isn't only me tied",
        [DialogueTopic.Neighbours] = "I see the small kindnesses out here that nobody up the hill ever hears about",
        [DialogueTopic.Roads]      = "I couldn't take one if I wanted to. I've stopped looking down them",
        [DialogueTopic.Food]       = "pottage. Pottage and more pottage, and bread when the flour's good",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a bondman tied to this land. Whatever the reeve sets you to, you do — weeding beds, mending fences, hauling sacks. You are not free, but the village is your village.

You speak quietly and watch carefully. You don't take chances with strangers; you don't volunteer information. You'll answer a direct question with the shortest honest answer.

You know the small kindnesses and small cruelties of everyone in the field.";
}
