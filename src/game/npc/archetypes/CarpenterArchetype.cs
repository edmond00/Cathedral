using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village carpenter — master of beams, planks, and joinery.</summary>
public class CarpenterArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "carpenter";

    /// <summary>What asking this person about their work teaches.</summary>
    public override string TradeModusMentisId => "whittlecraft";
    public override ItemTag? SellTag => ItemTag.Craftware;
    public override ItemTag? BuyTag  => ItemTag.Wood;
    public override int    ModiMentisCount => 9;
    public override bool   IsBrave      => true;
    public override int    AuthorityLevel => 1;

    public override string RoleNoun => "carpenter";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a sinewy figure looks up from a half-shaped beam, plane in hand",
        "a lean figure runs a thumb along a fresh joint, sawdust in their hair",
        "someone drives pegs into a frame, mallet-taps echoing",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_hand", "right_hand", "left_arm", "right_arm", "left_eye", "right_eye" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new CarpentersPlane(), () => new WoodChisel(), () => new WoodenMallet(), () => new LeatherBelt(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Hatchet(), () => new Rope(), () => new Whetstone(), () => new LinenTunic(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the carpenter here — if a roof over you is holding, chances are I set it";
    public override string Workplace        => "the workshop";
    public override string Craft            => "joinery";
    public override string DailyLabour      => "planing beams true and cutting joints that will still be tight in thirty years";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Weather]    = "damp is the enemy. Wet timber shrinks after it's set and pulls every joint you made out of true",
        [DialogueTopic.Seasons]    = "fell in winter, work in spring. Timber cut in sap-time is halfway to rot before you've shaped it",
        [DialogueTopic.Wilds]      = "I can read a tree standing — how it grew, which way it leaned, whether it'll split straight",
        [DialogueTopic.Work]       = "pegged, not nailed, wherever I can manage it. Iron rusts out and takes the wood with it",
        [DialogueTopic.Neighbours] = "I've been in most every roof hereabouts. I know which barns are sound and which are one storm from the ground",
        [DialogueTopic.Trade]      = "cheap work is done twice. I'd rather charge once and be done with it",
        [DialogueTopic.Kin]        = "a house isn't the family, but a bad roof will break one soon enough",
        [DialogueTopic.Stories]    = "they say you shouldn't build with elder wood. I don't know why, but I don't do it either",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village carpenter. You shape beams, lay floors, mend roofs, and hang doors. Wood speaks to you — you can read a tree from its grain.

You are deliberate in your speech, the way you are with a chisel: one careful stroke at a time. You are friendly with the villagers but not loose-tongued. You know which barns are rotten, which farms are quietly falling apart.

You take pride in honest joints — pegged not nailed where you can manage it. You distrust shortcuts, in work and in people.";
}
