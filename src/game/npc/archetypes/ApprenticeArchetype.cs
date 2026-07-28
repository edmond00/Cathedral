using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Apprentice or journeyman attached to a village master craftsman.</summary>
public class ApprenticeArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "apprentice";
    public override int    ModiMentisCount => 6;

    // Bound to a master and still a youth: 12–22 years.
    public override int MinAgeDays => 12 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 22 * LifetimeStat.DaysPerYear;

    public override string RoleNoun => "apprentice";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a soot-faced youth pauses at their work, wary and watchful",
        "a young figure in a coal-smudged apron fetches and carries, glancing up nervously",
        "a lanky apprentice sweeps shavings from the floor, keeping half an eye on you",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_hand", "right_hand", "left_foot", "right_foot", "backbone" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new LinenTunic(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Bread(), () => new WoodenSpoon(), () => new Flint(), () => new WoodenStick(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "nobody yet — I'm bound apprentice here, and I've years of it left";
    public override string Workplace        => "the master's workshop";
    public override string Craft            => "the trade, or as much of it as I'm let near";
    public override string DailyLabour      => "fetching, carrying, sweeping, and stoking whatever needs stoking";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work]       = "I do the parts nobody wants. That's the bargain, and it's a long one",
        [DialogueTopic.Rest]       = "I sleep in the workshop. When the master goes home, that's as near rest as I get",
        [DialogueTopic.Kin]        = "my people bound me here and I've not been back. I'd not say I miss them, but I think of it",
        [DialogueTopic.Roads]      = "I'd like to see what's past the last field. Journeymen go about; that's the whole reason to become one",
        [DialogueTopic.Neighbours] = "I hear everything and I'm allowed to repeat none of it. You can guess how well that goes",
        [DialogueTopic.Food]       = "whatever's left when the master's had his. I've got quick at it",
        [DialogueTopic.Stories]    = "the old hands tell tales of masters who freed an apprentice early. I've never met anyone it happened to",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, an apprentice in a village workshop — bound to the master craftsman for years yet. You do the dirty work: fetching coal, sweeping shavings, stoking fires.

You speak deferentially when the master is near and more freely when he isn't. You know the trade gossip — who is behind on payments, which orders are late — but you'd rather not be the one caught telling.

You are tired most days. You are dreaming of being a journeyman.";
}
