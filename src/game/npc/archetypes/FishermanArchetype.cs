using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Coastal fisherman — sets nets at dawn, returns mid-afternoon to dry catch and mend gear.
/// Carries net, line, hook, basket, knife.
/// </summary>
public class FishermanArchetype : WildernessNpcArchetype
{
    public override string ArchetypeId => "fisherman";

    /// <summary>What asking this person about their work teaches.</summary>
    public override string TradeModusMentisId => "anglery";
    public override ItemTag? SellTag => ItemTag.Fish;
    public override ItemTag? BuyTag  => ItemTag.Craftware;
    public override int    ModiMentisCount => 8;

    /// <summary>
    /// Master of their own working ground, and reads as one in a fight: authority is what
    /// decides who squares up rather than stands back, and how the fight AI carries itself.
    /// </summary>
    public override int    AuthorityLevel => 1;

    public override string RoleNoun => "fisherman";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a salt-bearded figure squats by a drying rack, fingers working a knot",
        "a weather-beaten figure hauls a dripping net onto the stones",
        "someone mends a line with quick, practised hands, a boat rocking nearby",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_hand", "right_hand", "left_arm", "right_arm", "left_eye", "right_eye" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new FishingNet(), () => new FishHooks(), () => new Rope(), () => new LeatherCanteen(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Knife(), () => new SaltPouch(), () => new WoolCap(), () => new DriedMeat(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "I fish this stretch — three days out, then back with whatever the water gave me";
    public override string Workplace        => "the water";
    public override string Craft            => "nets and lines";
    public override string DailyLabour      => "setting nets before light, hauling them wet, and mending what the sea tore";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Water]      = "it feeds you and it drowns you, and it doesn't change its manner between the two",
        [DialogueTopic.Weather]    = "I read the sky better than anyone inland. I've had to. Getting it wrong is the last mistake",
        [DialogueTopic.Food]       = "fish twice a day, every day. I'd trade a week of catch for a good loaf and a bowl of something hot",
        [DialogueTopic.Omens]      = "there's things you don't say on the water and things you don't bring aboard. Call it foolish; I still won't",
        [DialogueTopic.Beasts]     = "seals take fish straight out of the net and look you in the eye while they do it",
        [DialogueTopic.Trade]      = "the catch is worth most the hour it lands and nothing at all by the third day. That's the whole of my bargaining",
        [DialogueTopic.Roads]      = "I've been further along this coast than most here have been in any direction. It's not made me wiser",
        [DialogueTopic.Kin]        = "you learn to say the important things before you go out, because you might not get the chance after",
        [DialogueTopic.Health]     = "salt in every crack in my hands and a knee that tells me the weather. The knee is usually right",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a fisherman who fishes this stretch of coast for three days at a time and brings the catch back to the village to sell.

You speak in the cadence of the sea — a wave of words, then quiet. You watch the weather. You can read the sky better than anyone in the village.

You are friendly to fellow strangers but careful: you've been at the wrong end of bad weather and bad people both, and you've learned to tell early.";
}
