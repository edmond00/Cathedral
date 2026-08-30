using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Village baker — bakes bread for the village; rises before dawn.</summary>
public class BakerArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "baker";

    /// <summary>What asking this person about their work teaches.</summary>
    public override string TradeModusMentisId => "doughcraft";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Crop;
    public override int    ModiMentisCount => 8;
    public override int    AuthorityLevel => 1;

    public override string RoleNoun => "baker";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a flour-dusted figure pulls a loaf from the oven, face flushed from the heat",
        "a broad figure kneads dough on a floured board, sleeves pushed back",
        "someone slides a paddle of loaves into a glowing oven, flour clouding the air",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_arm", "right_arm", "left_hand", "right_hand", "nose" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new BakersPeel(), () => new LinenTunic(), () => new Bread(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Grain(), () => new WoodenBowl(), () => new SaltPouch(), () => new CoinPurse(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the one who bakes for this place — up before the birds, abed before the songs";
    public override string Workplace        => "the oven";
    public override string Craft            => "baking";
    public override string DailyLabour      => "firing the oven in the dark and pulling loaves out of it until the light comes";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Weather]    = "wet weather and the dough sulks; dry weather and it goes off before anyone's bought it",
        [DialogueTopic.Harvest]    = "a bad harvest, and by spring I'm putting bean flour in the loaf and pretending nobody notices",
        [DialogueTopic.Food]       = "bread with nothing on it, eaten warm, is better than half the feasts I've heard boasted of",
        [DialogueTopic.Rest]       = "I sleep when the oven's cooling. That's my rest, and it's shorter than yours",
        [DialogueTopic.Kin]        = "my whole household is awake by the time yours turns over — you learn to be gentle with each other or you don't last",
        [DialogueTopic.Neighbours] = "everyone comes for bread sooner or later, so I've a fair idea who's eating well and who isn't",
        [DialogueTopic.Trade]      = "I'd rather sell cheap and sell it all than sit on loaves going hard",
        [DialogueTopic.Health]     = "burns on both arms and flour in my chest. I cough like an old dog and I'm not old yet",
        [DialogueTopic.Work]       = "the oven doesn't care that you're tired. It's hot now, so you bake now",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the village baker. You rise long before dawn to fire the oven; by the time most of the village is awake, the bread is out and the smell is rolling down the lane.

You are tired but cheerful, the way someone is when they're permanently underslept. You speak in short bursts, often interrupted by the work in front of you. You know everyone — they all come for bread sooner or later.

You are charitable to the poor when you can manage it; you are not a fool when you can't.";
}
