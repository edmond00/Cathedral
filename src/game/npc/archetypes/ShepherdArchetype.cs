using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Farm shepherd — minds the sheep, often out alone with the flock.</summary>
public class ShepherdArchetype : PeasantArchetype
{
    public override string ArchetypeId => "shepherd";
    public override ItemTag? SellTag => ItemTag.Textile;
    public override ItemTag? BuyTag  => ItemTag.Tool;
    public override int    ModiMentisCount => 7;

    public override string RoleNoun => "shepherd";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a quiet figure leans on a crook, sheep grazing about their feet",
        "a weathered figure whistles a dog around a scattered flock",
        "someone counts the flock under a wide sky, crook across their shoulders",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_eye", "right_eye", "left_leg", "right_leg", "left_ear", "right_ear" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new ShepherdsCrook(), () => new WoolCloak(), () => new WoolShears(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Cheese(), () => new Bread(), () => new WoodenPipe(), () => new Wool(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the shepherd here — you'll mostly find me up on the grazing with the flock";
    public override string Workplace        => "the high grazing";
    public override string Craft            => "the flock";
    public override string DailyLabour      => "walking the sheep out, counting them home, and standing in a lot of weather between";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Beasts]     = "I know every ewe by sight and half of them by name. They're not clever, but they're not stupid either",
        [DialogueTopic.Wilds]      = "wolves. You don't see them and then one night you're short two and there's blood on the grass",
        [DialogueTopic.Weather]    = "I'm out in all of it. You stop minding rain and start minding wind, which is the one that gets in",
        [DialogueTopic.Rest]       = "I've whole days with nothing to do but watch. Most folk would go mad. It suits me",
        [DialogueTopic.Neighbours] = "I'm not much for crowds. A market day leaves me wanting the hill back",
        [DialogueTopic.Stories]    = "you get a lot of hours up there. You end up knowing every song anyone ever taught you",
        [DialogueTopic.Health]     = "lameness in the flock and damp in my chest. One I can treat",
        [DialogueTopic.Omens]      = "the sky tells you things if you're under it all day. Not omens. Just weather, read properly",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the farm's shepherd. You spend your days alone with the flock, in fair weather and foul. You know each ewe by sight, half of them by name.

You speak softly — your voice is not used much. You are observant, patient, and watchful for wolves and lameness alike. You distrust crowds.

A stranger to your flock is met first with silence and then, if they seem decent, with a slow, considered word.";
}
