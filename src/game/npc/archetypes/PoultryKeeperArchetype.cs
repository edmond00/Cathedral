using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>Farm poultry keeper — minds chickens, ducks, geese; collects eggs.</summary>
public class PoultryKeeperArchetype : PeasantArchetype
{
    public override string ArchetypeId => "poultry_keeper";

    /// <summary>What asking this person about their work teaches.</summary>
    public override string TradeModusMentisId => "husbandry";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Crop;
    public override int    ModiMentisCount => 6;

    public override string RoleNoun => "poultry keeper";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a small figure crouches at the nest-box, basket of eggs balanced on one hip",
        "a quick figure scatters grain to a scrum of clucking hens",
        "someone counts a flock of birds, shooing a stray back toward the coop",
    };

    // ── Generation ────────────────────────────────────────────────────────────

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "left_eye", "right_eye", "left_hand", "right_hand", "left_foot", "right_foot" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new WickerBasket(), () => new LinenTunic(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Egg(), () => new Grain(), () => new Bread(), () => new WoolCap(),
    };

    // ── Dialogue flavour ──────────────────────────────────────────────────────

    public override string SelfIntroduction => "the one who minds the birds here — hens, ducks, the geese when they'll let me";
    public override string Workplace        => "the coop";
    public override string Craft            => "the flock";
    public override string DailyLabour      => "scattering grain, gathering eggs, and counting beaks at dusk";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Beasts]     = "hens have more sense than they're given credit for, and geese have more temper than anyone wants",
        [DialogueTopic.Wilds]      = "the fox. Always the fox. I've lost more sleep to that animal than to any person",
        [DialogueTopic.Food]       = "an egg is a small thing until it's the only thing, and then it's everything",
        [DialogueTopic.Seasons]    = "they lay well in the long light and hardly at all in the dark half. There's no arguing with a hen about it",
        [DialogueTopic.Weather]    = "cold snaps stop them laying dead. You can put straw down and coax them, but they decide",
        [DialogueTopic.Kin]        = "I like the birds better than most company, and I don't think that's a fault",
        [DialogueTopic.Omens]      = "a hen that crows is meant to be ill luck. Ours did it for a year and nothing happened at all",
        [DialogueTopic.Neighbours] = "if a bird goes missing I know whether it was the fox or a person. They leave it different",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the farm's poultry keeper. You feed the chickens, gather eggs, count beaks at dusk and worry when one is missing.

You speak quickly and brightly. You like the chickens better than most people, but you'll be friendly to a stranger if they don't startle the birds.

You are forever shooing something — chickens, foxes, children, dogs.";
}
