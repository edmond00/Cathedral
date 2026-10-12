using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Npc.Archetypes;

// The keepers of the great buildings' stores. A great building is self-sufficient and keeps its own
// stores, and for a long time everyone in one traded out of them: the lord sold ironwork, the guards
// bought food, the priests sold herbs and the monks ale. Trading is now done on premises (see
// TradeGate), and these three are who a great building trades through — each at its entrance, where a
// caller with something to sell is met.

/// <summary>Quartermaster - keeps the arms and the stores of a garrison, and buys in its food.</summary>
public class QuartermasterArchetype : SettledArchetype
{
    public override string ArchetypeId => "quartermaster";
    public override string TradeModusMentisId => "provisioning";
    public override SocialCategory? Social => SocialCategory.Military;
    public override string RoleNoun => "quartermaster";
    public override ItemTag? SellTag => ItemTag.Ironwork;
    public override ItemTag? BuyTag  => ItemTag.Foodstuff;
    public override int AuthorityLevel => 1;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "captain" };
    public override string IntroductionRelation => "the captain";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a thickset figure counts spearheads into a crate, lips moving",
        "someone with a tally stick and a ring of keys checks a cart against a list",
        "a grey-haired soldier in a leather apron tests the edge of a blade with a thumb",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "cerebrum", "eyes", "arms" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Tabard(), () => new KeyRing(), () => new Ledgerbook(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new ShortSword(), () => new Whetstone(), () => new CoinPurse(), () => new Bread(),
    };

    public override string SelfIntroduction => "the quartermaster. Every spear, every boot and every sack of meal in this place passes through my hands";
    public override string Workplace        => "the stores and the gate";
    public override string Craft            => "the stores";
    public override string DailyLabour      => "counting out the arms, counting in the food, and finding out which of the two has gone missing this week";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Food]  = "a garrison marches on its bread. A garrison that runs out of it does not march anywhere",
        [DialogueTopic.Trade] = "I pay fair and I pay late. Everyone who deals with soldiers learns that",
        [DialogueTopic.Work]  = "the captain wins the battles. I make sure there is something to win them with",
        [DialogueTopic.Roads] = "every cart that comes up that road I have to count, and every one that goes down it",
        [DialogueTopic.Neighbours] = "the farmers round here sell to us because they have to. They do not have to like it",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, quartermaster of this garrison. You keep the arms, the armour and the stores, and you buy in what the soldiers eat.

You are practical, suspicious of anyone who wants something for nothing, and you know the price of everything to the copper. You respect a hard bargain and despise waste.";
}

/// <summary>Sacristan - keeps the vessels, candles and vestments of a temple, and sells candles and charms to the faithful.</summary>
public class SacristanArchetype : SettledArchetype
{
    public override string ArchetypeId => "sacristan";
    public override string TradeModusMentisId => "liturgy";
    public override SocialCategory? Social => SocialCategory.Religious;
    public override string RoleNoun => "sacristan";
    public override ItemTag? SellTag => ItemTag.Craftware;
    public override ItemTag? BuyTag  => ItemTag.Herb;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "priest" };
    public override string IntroductionRelation => "the priest";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a stooped figure trims a row of candles, scraping the drips into a pot",
        "someone in a plain robe polishes a censer with slow circular strokes",
        "a quiet figure stands by a tray of votive candles, watching who takes one",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "hands", "eyes", "nose" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Habit(), () => new KeyRing(), () => new VotiveCandle(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new PrayerBeads(), () => new Incense(), () => new Candle(), () => new Bread(),
    };

    public override string SelfIntroduction => "the sacristan. I keep the candles lit and the plate polished, and I sell the faithful what they need to pray with";
    public override string Workplace        => "the sacristy and the temple doors";
    public override string Craft            => "the vessels and the candles";
    public override string DailyLabour      => "lighting, trimming and snuffing candles, airing the vestments, counting the plate, and keeping the candle tray by the door";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work]   = "the priests pray. Somebody has to make sure there is light to pray by",
        [DialogueTopic.Trade]  = "a candle is a prayer you can hold. I do not think it wrong to ask a copper for it",
        [DialogueTopic.Omens]  = "a candle that gutters at the altar means something. I have never agreed with anyone about what",
        [DialogueTopic.Health] = "half the pilgrims who come in coughing want a candle and half want a physician. I sell the first",
        [DialogueTopic.Stories] = "I have heard every confession that was ever whispered too loud. I tell none of them",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, sacristan of this temple. You keep its vessels, candles and vestments and you sell candles and small devotions to the faithful at the door.

You are devout in a practical, unshowy way, fussy about order, and a little proud of how much depends on you. You are polite to everyone and trust nobody near the plate.";
}

/// <summary>Cellarer - keeps a monastery's cellars, brewhouse and larder, and sells what the brothers make.</summary>
public class CellarerArchetype : SettledArchetype
{
    public override string ArchetypeId => "cellarer";
    public override string TradeModusMentisId => "cellarcraft";
    public override SocialCategory? Social => SocialCategory.Religious;
    public override string RoleNoun => "cellarer";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Crop;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "priest" };
    public override string IntroductionRelation => "the abbot";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a stout brother with a bunch of keys taps a cask and listens to it",
        "a robed figure with flour on one sleeve tallies cheeses on a shelf",
        "someone in a habit hefts a sack of grain, judging it by the weight",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "tongue", "nose", "cerebrum" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Habit(), () => new KeyRing(), () => new Apron(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Ale(), () => new Cheese(), () => new Ledgerbook(), () => new Bread(),
    };

    public override string SelfIntroduction => "the cellarer of this house. The brothers pray, and I see that they eat — and what we make over, I sell";
    public override string Workplace        => "the cellars and the gate";
    public override string Craft            => "the cellar and the brewhouse";
    public override string DailyLabour      => "the brewing, the cheeses, the bread, the stores, and the accounts of all of it";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Food]    = "plain food, well made. A brother who eats badly prays badly",
        [DialogueTopic.Trade]   = "our ale is the best for three valleys, and we sell it to keep the roof on",
        [DialogueTopic.Harvest] = "the tithe comes in at harvest and I have to make it last until the next one",
        [DialogueTopic.Work]    = "the abbot keeps the rule. I keep the larder. Between us the house stands",
        [DialogueTopic.Seasons] = "Lent is lean and Easter is not. I plan the year around both",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, cellarer of this monastery: you keep its cellars, its brewhouse and its larder, and you sell what the brothers make beyond their own needs.

You are cheerful, worldly for a monk, shrewd at a bargain and genuinely proud of your ale and cheese. You keep the rule, but loosely around the edges.";
}
