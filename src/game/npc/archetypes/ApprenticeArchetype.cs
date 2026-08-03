using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Apprentice or journeyman attached to a village master craftsman.
///
/// <para>Set <see cref="Master"/> to bind the apprentice to a trade: they then read as "blacksmith
/// apprentice" everywhere a role is shown, and trade the master's goods. It must be set on the
/// archetype <b>instance before Spawn</b> — the persona prompt is generated once inside
/// <c>NamedNpcArchetype.Spawn</c> and frozen, so a later assignment would never reach the LLM.</para>
///
/// <para><see cref="ArchetypeId"/> stays <c>"apprentice"</c> whatever the trade. Personality trait
/// pools are keyed by that string, so a per-trade id (<c>"blacksmith_apprentice"</c>) would silently
/// deal no traits at all. Jobs likewise: <c>JobRegistry</c> has no apprentice entry, which is what
/// keeps an apprentice from granting work while still letting them trade.</para>
/// </summary>
public class ApprenticeArchetype : CraftsmanArchetype
{
    /// <summary>
    /// The master this apprentice is bound to, or null for a trade-neutral apprentice. Null must stay
    /// viable: both <c>--npc-audit</c> and <c>--dialogue-audit</c> construct every archetype through
    /// its parameterless constructor and expand every dialogue token against it.
    /// </summary>
    public CraftsmanArchetype? Master { get; init; }

    public override string ArchetypeId => "apprentice";

    /// <summary>
    /// An apprentice can present you to their own master and nobody else. Empty when the apprentice
    /// is trade-neutral, which the audits construct and which must stay viable.
    /// </summary>
    public override IReadOnlyList<string> CanIntroduceToArchetypes =>
        Master == null ? Array.Empty<string>() : new[] { Master.ArchetypeId };

    public override string IntroductionRelation => "my master";
    public override int    ModiMentisCount => 6;

    // Bound to a master and still a youth: 12–22 years.
    public override int MinAgeDays => 12 * LifetimeStat.DaysPerYear;
    public override int MaxAgeDays => 22 * LifetimeStat.DaysPerYear;

    public override string RoleNoun =>
        Master == null ? "apprentice" : $"{Master.RoleNoun} apprentice";

    // Trades the master's catalogue. NpcTradeCatalog seeds its offer list from the NPC's own id, so
    // the apprentice sells the same class of goods from their own stock rather than a copy of the
    // master's — which is the right reading for a shop boy minding the counter.
    public override ItemTag? SellTag => Master?.SellTag;
    public override ItemTag? BuyTag  => Master?.BuyTag;

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

    public override string SelfIntroduction =>
        Master == null
            ? "nobody yet — I'm bound apprentice here, and I've years of it left"
            : $"nobody yet — I'm bound apprentice to the {Master.RoleNoun}, and I've years of it left";

    public override string Workplace => Master?.Workplace ?? "the master's workshop";

    public override string Craft =>
        Master == null
            ? "the trade, or as much of it as I'm let near"
            : $"{Master.Craft} — or as much of it as I'm let near";

    public override string DailyLabour => "fetching, carrying, sweeping, and stoking whatever needs stoking";

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
        => $@"You are {name}, {(Master == null ? "an apprentice in a village workshop" : $"an apprentice {Master.RoleNoun}")} — bound to the master craftsman for years yet. You do the dirty work: fetching coal, sweeping shavings, stoking fires.

You speak deferentially when the master is near and more freely when he isn't. You know the trade gossip — who is behind on payments, which orders are late — but you'd rather not be the one caught telling.

You are tired most days. You are dreaming of being a journeyman.";
}
