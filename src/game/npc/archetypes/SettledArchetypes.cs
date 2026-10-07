using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Npc.Archetypes;

/// <summary>
/// Abstract base for the people of the settled country: the orchards and plantations, the stables and
/// drove roads, the towns, the ports and the great buildings. Human, persistent and speaking, like a
/// <see cref="PeasantArchetype"/>; what sets them apart is that each names its own social standing, since
/// these are the archetypes that finally fill the Military, Religious, Urban and Aristocrat rows.
/// </summary>
public abstract class SettledArchetype : NamedNpcArchetype
{
    public override Species Species        => SpeciesRegistry.Human;
    public override bool DefaultPersistent => true;
    public override int  ModiMentisCount   => 8;
    public override bool CanSpeak          => true;
}

/// <summary>Orchard and grove keeper - tends the fruit and nut trees, prunes, grafts and picks.</summary>
public class OrchardistArchetype : SettledArchetype
{
    public override string ArchetypeId => "orchardist";
    public override string TradeModusMentisId => "pomology";
    public override SocialCategory? Social => SocialCategory.Peasant;
    public override string RoleNoun => "orchardist";
    public override ItemTag? SellTag => ItemTag.Crop;
    public override ItemTag? BuyTag  => ItemTag.Tool;
    public override int ModiMentisCount => 7;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "planter" };
    public override string IntroductionRelation => "the master of the place";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a weathered figure up a ladder among the branches, a basket on one hip",
        "someone walks the rows touching bark as if greeting old friends",
        "a stooped figure with a pruning hook studies a tree for a long time before cutting",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "hands", "eyes", "arms" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new PruningHook(), () => new PickingBasket(), () => new LinenTunic(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new GraftingKnife(), () => new Bread(), () => new LeatherGloves(), () => new WoodenPipe(),
    };

    public override string SelfIntroduction => "I keep the trees here - every one of them, and some of them are older than my grandmother";
    public override string Workplace        => "the orchard rows";
    public override string Craft            => "the pruning hook and the grafting knife";
    public override string DailyLabour      => "pruning in winter, grafting in spring, propping branches in summer and picking until my shoulders give out";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Harvest] = "a tree remembers a bad year for two more. You pick what it gives and you thank it",
        [DialogueTopic.Seasons] = "winter is when the real work is done - you cut, and spring tells you if you cut right",
        [DialogueTopic.Work] = "there is no hurrying a tree. Anybody who tries ends up with firewood",
        [DialogueTopic.Weather] = "a late frost on the blossom and that is the year gone, whatever happens after",
        [DialogueTopic.Wilds] = "deer at the bark, birds at the fruit, wasps in the windfalls. The wild wants my trees as much as I do",
        [DialogueTopic.Kin] = "my father planted the far rows. I will plant some my children will pick",
        [DialogueTopic.Food] = "there is nothing like a pear eaten off the branch on the day it is ready. One day, and it is gone",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, keeper of these fruit trees. You speak slowly, in seasons rather than days, and you think of each tree as a long-lived household with its own temper and history.

You are patient, a little stubborn, and suspicious of anyone in a hurry. You know which trees are failing and grieve them quietly. Strangers who admire the trees properly are welcome; strangers who pick without asking are not.";
}

/// <summary>Vintner - master of a vineyard and its press, maker and seller of wine.</summary>
public class VintnerArchetype : SettledArchetype
{
    public override string ArchetypeId => "vintner";
    public override string TradeModusMentisId => "viniculture";
    public override SocialCategory? Social => SocialCategory.Bourgeois;
    public override string RoleNoun => "vintner";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Craftware;
    public override int ModiMentisCount => 9;
    public override int AuthorityLevel => 1;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a stout figure in a purple-stained apron walks the rows, tasting a grape here and there",
        "someone stands by the press sniffing the air with their eyes closed",
        "a figure with a vine knife inspects a bunch against the light",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "nose", "tongue", "hands" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new VintnerApron(), () => new VineKnife(), () => new LeatherBoots(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Wine(), () => new CoinPurse(), () => new Raisins(), () => new DrinkingHorn(),
    };

    public override string SelfIntroduction => "the vintner here - the vines are mine, and so is whatever comes out of the press";
    public override string Workplace        => "the vineyard and the press-house";
    public override string Craft            => "the vine and the press";
    public override string DailyLabour      => "walking the rows, pruning to two buds, watching the sugar come up, and arguing with the press crew";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Harvest] = "the vintage is decided in the last fortnight. All the year before is only a promise",
        [DialogueTopic.Weather] = "rain at the vintage swells the grapes and thins the wine. I pray for dry and I am never answered",
        [DialogueTopic.Food] = "wine is food. Anyone who says otherwise has been drinking bad wine",
        [DialogueTopic.Trade] = "the merchants come up from the coast and lie to me about the price. I lie back. We both know it",
        [DialogueTopic.Work] = "two buds, no more. Leave three and you have more grapes and less wine",
        [DialogueTopic.Rest] = "after the vintage, a month of nothing. I have earned it and so has the vine",
        [DialogueTopic.Neighbours] = "the next valley says their wine is better. Their wine is vinegar with ambitions",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, master of this vineyard. You speak with the confidence of someone who owns land and makes something people pay well for, and you are vain about your wine and a little jealous of every neighbour's.

You are generous with a cup and shrewd with a price. You judge a stranger partly by how they hold a glass.";
}

/// <summary>Planter - master of a plantation, grove, paddy or tea garden, who sets the gangs to work.</summary>
public class PlanterArchetype : SettledArchetype
{
    public override string ArchetypeId => "planter";
    public override string TradeModusMentisId => "plantership";
    public override SocialCategory? Social => SocialCategory.Bourgeois;
    public override string RoleNoun => "planter";
    public override ItemTag? SellTag => ItemTag.Crop;
    public override ItemTag? BuyTag  => ItemTag.Tool;
    public override int ModiMentisCount => 9;
    public override int AuthorityLevel => 1;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a figure in a wide hat watches the rows from a shaded veranda, a ledger on their knee",
        "someone rides slowly along the edge of the field, counting",
        "a figure in good boots taps a cane against their leg as the workers pass",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "cerebrum", "eyes", "tongue" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new PlanterHat(), () => new Ledgerbook(), () => new LeatherBoots(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new CoinPurse(), () => new Cinnamon(), () => new WalkingStaff(), () => new SugarLoaf(),
    };

    public override string SelfIntroduction => "the master of this ground. The rows, the crop and the hands working it all answer to me";
    public override string Workplace        => "the plantation house";
    public override string Craft            => "the rows and the ledger";
    public override string DailyLabour      => "setting the gangs, counting the rows, keeping the books, and sending the crop down to the coast";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work] = "every hand must earn their keep. That is not cruelty, it is arithmetic",
        [DialogueTopic.Trade] = "the price at the coast decides everything. I do not set it and I cannot argue with it",
        [DialogueTopic.Harvest] = "a good harvest means the gangs work harder, not less. Ripe fruit does not wait",
        [DialogueTopic.Weather] = "the rains are late and every day late is money I will never see",
        [DialogueTopic.Neighbours] = "they're all planters like me, and we all watch each other's prices",
        [DialogueTopic.Roads] = "the road to the coast is the only road that matters. Without it the crop rots where it stands",
        [DialogueTopic.Health] = "fever takes a hand a week in the wet season. I count them and I hire more",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, master of this plantation. You think in rows, hands and prices at the coast, and you are not sentimental about any of them.

You are courteous to people of standing and curt with everyone else. You can be generous when it costs you nothing and you believe, sincerely, that you are a fair master.";
}

/// <summary>Picker - labourer of the plantations, groves, paddies and gardens.</summary>
public class PickerArchetype : SettledArchetype
{
    public override string ArchetypeId => "picker";
    public override string TradeModusMentisId => "harvestry";
    public override SocialCategory? Social => SocialCategory.Peasant;
    public override string RoleNoun => "picker";
    public override ItemTag? SellTag => ItemTag.Crop;
    public override ItemTag? BuyTag  => ItemTag.Foodstuff;
    public override int ModiMentisCount => 6;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "planter", "orchardist" };
    public override string IntroductionRelation => "the master here";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a bent figure works along the row, filling a basket without looking up",
        "someone pauses to straighten their back, a hand pressed to the base of the spine",
        "a figure with a basket on their back moves steadily down the row",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "hands", "backbone", "legs" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new PickingBasket(), () => new LinenTunic(), () => new PlanterHat(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Machete(), () => new Bread(), () => new LeatherCanteen(), () => new Oatcake(),
    };

    public override string SelfIntroduction => "one of the pickers - you will find me in the rows, the same as all the others";
    public override string Workplace        => "the rows";
    public override string Craft            => "the basket";
    public override string DailyLabour      => "picking from first light, carrying to the weigh-house, and picking again until the light goes";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work] = "so many baskets a day, or no pay for the day. That is all there is to it",
        [DialogueTopic.Rest] = "rest is the hour after dark before you fall asleep. It is not long enough",
        [DialogueTopic.Food] = "we eat what the master sends down. It is enough. Mostly",
        [DialogueTopic.Kin] = "my children pick beside me. Smaller baskets",
        [DialogueTopic.Weather] = "the heat at noon is the worst. People fall down in the rows",
        [DialogueTopic.Health] = "my back, my hands, my eyes. Pick for twenty years and you will know",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a picker on this ground. You work long hours for little and you are tired in a way sleep does not fix.

You are guarded with strangers, especially well-dressed ones. You are warm with your fellow workers and quick to share food. You know far more about the crop than the master does and nobody asks you.";
}

/// <summary>Groom - keeper of a stable, its horses and their tack.</summary>
public class GroomArchetype : SettledArchetype
{
    public override string ArchetypeId => "groom";
    public override string TradeModusMentisId => "horsemanship";
    public override SocialCategory? Social => SocialCategory.Peasant;
    public override string RoleNoun => "groom";
    public override ItemTag? BuyTag  => ItemTag.Crop;
    public override int ModiMentisCount => 7;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a figure brushes down a horse with long steady strokes, talking quietly to it",
        "someone carries two buckets of water across the yard, horses calling after them",
        "a lean figure leans on a stall door, watching a horse eat",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "hands", "heart", "legs" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Currycomb(), () => new Jerkin(), () => new LeatherBoots(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new HoofPick(), () => new Bridle(), () => new Bread(), () => new Horsehair(),
    };

    public override string SelfIntroduction => "the groom - the horses here are in my care, and I would rather you asked before you touched them";
    public override string Workplace        => "the stable";
    public override string Craft            => "the currycomb and the halter";
    public override string DailyLabour      => "mucking out, feeding, watering, grooming and walking every horse in the yard, twice a day";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Beasts] = "a horse is honest. It will tell you exactly what it thinks of you, if you look",
        [DialogueTopic.Work] = "mucking out is most of it. Nobody tells you that when you start",
        [DialogueTopic.Rest] = "I sleep in the loft above them. I hear them all night and I sleep better for it",
        [DialogueTopic.Roads] = "a horse comes back from a long road and tells me how it was treated. I can see it",
        [DialogueTopic.Health] = "colic is the one I fear. A horse can be dead by morning",
        [DialogueTopic.Neighbours] = "people mostly. I prefer horses",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, groom of this stable. You are quiet, gentle with animals and a little awkward with people. You smell of horse and straw and you do not mind.

You judge a stranger by how they approach a horse. You are fiercely protective of the animals in your care and will contradict anyone, even a lord, on their welfare.";
}

/// <summary>Drover - herder of cattle, sheep and goats on the ranges and the drove roads.</summary>
public class DroverArchetype : SettledArchetype
{
    public override string ArchetypeId => "drover";
    public override string TradeModusMentisId => "droving";
    public override SocialCategory? Social => SocialCategory.Peasant;
    public override string RoleNoun => "drover";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Tool;
    public override int ModiMentisCount => 7;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a weathered figure leans on a long stick, watching the herd graze",
        "someone with a coiled rope on their shoulder whistles a dog round the stock",
        "a figure in a battered hat walks slowly through the herd, checking feet",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "legs", "eyes", "ears" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new DroverHat(), () => new StockWhip(), () => new LeatherBoots(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Lariat(), () => new DriedMeat(), () => new LeatherCanteen(), () => new Cowbell(),
    };

    public override string SelfIntroduction => "one of the drovers - I am mostly out with the herd, but you have caught me in";
    public override string Workplace        => "the range";
    public override string Craft            => "the herd and the whip";
    public override string DailyLabour      => "walking the herd out, keeping it together, finding water and grass, and bringing it home whole";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Beasts] = "a herd is one animal with two hundred legs and no sense. You learn to think like it",
        [DialogueTopic.Wilds] = "wolves on the range, cats in the hills, and a dry summer worse than both",
        [DialogueTopic.Water] = "I know every spring for three days walk. You do not survive out there otherwise",
        [DialogueTopic.Roads] = "the drove roads are older than the empire. Older than anything",
        [DialogueTopic.Weather] = "a thunderstorm on the range and the herd runs. You run with it or you lose it",
        [DialogueTopic.Rest] = "a fire, a dog, a sky. I do not need a roof",
        [DialogueTopic.Trade] = "at market I find out what the season was worth. Usually less than I hoped",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a drover. You spend most of your life out on the range with the herd, under the sky, and you are more at ease with animals and weather than with people.

You are laconic, practical and hard to impress. You are generous at a campfire and suspicious in a town.";
}

/// <summary>Gravedigger - sexton of a burial ground, keeper of the dead and their register.</summary>
public class GravediggerArchetype : SettledArchetype
{
    public override string ArchetypeId => "gravedigger";
    public override string TradeModusMentisId => "sextonry";
    public override SocialCategory? Social => SocialCategory.Peasant;
    public override string RoleNoun => "gravedigger";
    public override ItemTag? SellTag => ItemTag.Craftware;
    public override ItemTag? BuyTag  => ItemTag.Tool;
    public override int ModiMentisCount => 7;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "priest" };
    public override string IntroductionRelation => "the priest";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a figure in a muddy hood leans on a spade beside a fresh-cut hole",
        "someone moves slowly between the graves, straightening a marker here and there",
        "a stooped figure scythes the long grass between the mounds",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "arms", "backbone", "hands" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new GraveSpade(), () => new Hood(), () => new LeatherBoots(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Lantern(), () => new Bread(), () => new WoodenPipe(), () => new Shroud(),
    };

    public override string SelfIntroduction => "I dig the graves here, and I keep them - somebody has to, and the dead do not complain";
    public override string Workplace        => "the burial ground";
    public override string Craft            => "the grave spade";
    public override string DailyLabour      => "digging, filling, scything the grass, minding the gate and writing down who went where";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work] = "six feet down, straight sides. Do it right once and nobody thinks about it for a hundred years",
        [DialogueTopic.Kin] = "I have buried most of mine. I know where every one of them is",
        [DialogueTopic.Omens] = "people think a graveyard is full of spirits. It is full of grass",
        [DialogueTopic.Health] = "I know what people die of. Mostly it is the winter",
        [DialogueTopic.Stories] = "every grave has one. Most of them are sad and short",
        [DialogueTopic.Weather] = "frozen ground is the worst. In a hard winter the dead wait in the shed till spring",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, gravedigger. You are calm about death in a way that unsettles people, and you have a dry, dark humour.

You are respectful of the dead and unimpressed by the living. You know a great deal of local history, because you know who is buried where and how they got there.";
}

/// <summary>Guard - man-at-arms of a fort, castle, garrison or gate.</summary>
public class GuardArchetype : SettledArchetype
{
    public override string ArchetypeId => "guard";
    public override string TradeModusMentisId => "soldiery";
    public override SocialCategory? Social => SocialCategory.Military;
    public override string RoleNoun => "guard";
    public override ItemTag? BuyTag  => ItemTag.Foodstuff;
    public override int ModiMentisCount => 8;
    public override int AuthorityLevel => 1;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "captain", "steward" };
    public override string IntroductionRelation => "my captain";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a figure in a faded tabard leans on a spear by the gate, watching the road",
        "someone in an iron cap walks the wall, pausing at each crenel",
        "a bored-looking soldier picks at their teeth beside the guardroom door",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "arms", "eyes", "backbone" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Tabard(), () => new WarSpear(), () => new Helm(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new ShortSword(), () => new Bread(), () => new Dice(), () => new LeatherCanteen(),
    };

    public override string SelfIntroduction => "one of the guard here. I'd stay on the right side of the gate if I were you";
    public override string Workplace        => "the gate and the wall";
    public override string Craft            => "the spear and the watch";
    public override string DailyLabour      => "standing watches, walking the wall, drilling in the yard and waiting for something to happen";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work] = "most of it is standing about. Nobody tells you that it is the standing that kills you",
        [DialogueTopic.Food] = "barracks food. If you eat it fast you do not taste it",
        [DialogueTopic.Roads] = "I watch everyone who comes up that road. Most of them are nobody",
        [DialogueTopic.Neighbours] = "the townsfolk look at us like we are dogs, until there is trouble",
        [DialogueTopic.Rest] = "four hours off, eight on. You sleep when you can and where you fall",
        [DialogueTopic.Trade] = "a guard's pay is a joke. Everybody knows how we make it up",
        [DialogueTopic.Omens] = "never look at the moon on the wall at night. Old guard's rule",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a guard. You are bored most of the time and dangerous some of it, and you have stopped expecting anything interesting to happen.

You are suspicious of strangers by habit and duty. You grumble about the pay, the food and the officers, but you are loyal to the people beside you on the wall.";
}

/// <summary>Captain - commander of a fort, fortress or commandery garrison.</summary>
public class CaptainArchetype : SettledArchetype
{
    public override string ArchetypeId => "captain";
    public override string TradeModusMentisId => "siegecraft";
    public override SocialCategory? Social => SocialCategory.Military;
    public override string RoleNoun => "captain";
    public override ItemTag? BuyTag  => ItemTag.Ironwork;
    public override int ModiMentisCount => 10;
    public override int AuthorityLevel => 2;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "an upright figure in mail stands at the head of the yard, watching the drill",
        "someone with a cloak over their armour studies a map pinned to a table",
        "a hard-faced officer speaks quietly to a sergeant, who hurries off",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "cerebrum", "backbone", "tongue" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new ArmingSword(), () => new PaddedGambeson(), () => new ArmingBoots(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Chart(), () => new Whistle(), () => new CoinPurse(), () => new Writ(),
    };

    public override string SelfIntroduction => "I command here. If you have business with this garrison, you have it with me";
    public override string Workplace        => "the command room";
    public override string Craft            => "the garrison";
    public override string DailyLabour      => "keeping the walls manned, the men paid, the stores full and the commander above me satisfied";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work] = "a garrison that is not drilling is a garrison that is drinking. I prefer them drilling",
        [DialogueTopic.Roads] = "I need to know who is on every road within a day's march. I usually do",
        [DialogueTopic.Trade] = "the merchants want protection and do not want to pay for it. Nobody does",
        [DialogueTopic.Neighbours] = "the lord of the valley and I are civil. That is the most that can be said",
        [DialogueTopic.Food] = "stores for ninety days. That is the rule, and I keep it",
        [DialogueTopic.Omens] = "I do not believe in omens. I believe in scouts",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, captain of this garrison. You are used to command, and you speak briefly and expect to be obeyed.

You are watchful, pragmatic and tired. You weigh every stranger as a possible threat and every request as a possible cost. You respect competence wherever you find it.";
}

/// <summary>Priest - keeper of a temple, its rites and its people.</summary>
public class PriestArchetype : SettledArchetype
{
    public override string ArchetypeId => "priest";
    public override string TradeModusMentisId => "liturgy";
    public override SocialCategory? Social => SocialCategory.Religious;
    public override string RoleNoun => "priest";
    public override ItemTag? SellTag => ItemTag.Herb;
    public override ItemTag? BuyTag  => ItemTag.Foodstuff;
    public override int ModiMentisCount => 9;
    public override int AuthorityLevel => 1;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a robed figure trims the candles before the altar, lips moving",
        "someone in a vestment sweeps the temple steps, nodding to passers-by",
        "a grey-haired figure sits by the door with a book open on their knee",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "tongue", "heart", "anamnesis" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Vestment(), () => new PrayerBeads(), () => new Sandals(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new PrayerBook(), () => new VotiveCandle(), () => new Incense(), () => new HolyWater(),
    };

    public override string SelfIntroduction => "the priest of this temple - its door is open, and so am I";
    public override string Workplace        => "the temple";
    public override string Craft            => "the rites";
    public override string DailyLabour      => "saying the offices, hearing confessions, burying the dead, blessing the fields and keeping the lamps lit";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Omens] = "the people want signs. I give them prayers. They are more use",
        [DialogueTopic.Kin] = "everyone in this valley is my family, whether they come to the temple or not",
        [DialogueTopic.Health] = "I sit with the dying more than the doctor does. Someone should",
        [DialogueTopic.Harvest] = "I bless the fields every spring. The fields do not seem to notice, but the people do",
        [DialogueTopic.Rest] = "the holy days are for rest. Nobody keeps them properly any more",
        [DialogueTopic.Stories] = "every saint's day has its story, and I tell them all, every year",
        [DialogueTopic.Trade] = "the temple does not trade. It accepts gifts. There is a difference",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the priest of this temple. You speak with gentle authority, and you are used to being listened to and to being ignored.

You care for your people, though you are weary of their sins. You are well-read, slightly pompous, and kind when it matters.";
}

/// <summary>Monk - brother of a monastery: prayer, labour and the scriptorium.</summary>
public class MonkArchetype : SettledArchetype
{
    public override string ArchetypeId => "monk";
    public override string TradeModusMentisId => "copying";
    public override SocialCategory? Social => SocialCategory.Religious;
    public override string RoleNoun => "monk";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Craftware;
    public override int ModiMentisCount => 8;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "priest" };
    public override string IntroductionRelation => "the abbot";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a hooded figure walks the cloister slowly, hands folded into their sleeves",
        "someone in a coarse habit bends over a desk, a quill moving steadily",
        "a tonsured figure works in the garden, silent",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "hands", "eyes", "ears" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Habit(), () => new Tonsure(), () => new Sandals(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new PrayerBeads(), () => new Quill(), () => new Bread(), () => new Cheese(),
    };

    public override string SelfIntroduction => "a brother of this house. Forgive me - we are not much given to talking";
    public override string Workplace        => "the cloister";
    public override string Craft            => "the quill and the rule";
    public override string DailyLabour      => "the offices at their hours, the garden, the kitchen, the scriptorium, and silence in between";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Rest] = "we rise in the night for prayer. Sleep is a thing we borrow",
        [DialogueTopic.Work] = "prayer is work and work is prayer. The rule says so and I have found it true",
        [DialogueTopic.Food] = "one meal, eaten in silence while a brother reads. You notice the bread more",
        [DialogueTopic.Neighbours] = "the village comes for alms and for physic. We give both",
        [DialogueTopic.Stories] = "the library holds books nobody outside these walls has read in a century",
        [DialogueTopic.Kin] = "I had a family once. Now I have brothers",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a monk of this house. You live by the rule: prayer, labour and silence, and you speak little and thoughtfully when you speak at all.

You are humble, patient, and a little unworldly, and you may know a great deal more than you let on.";
}

/// <summary>Scholar - master of an imperial school, teacher of letters, number and law.</summary>
public class ScholarArchetype : SettledArchetype
{
    public override string ArchetypeId => "scholar";
    public override string TradeModusMentisId => "pedagogy";
    public override SocialCategory? Social => SocialCategory.Bourgeois;
    public override string RoleNoun => "scholar";
    public override ItemTag? SellTag => ItemTag.Craftware;
    public override ItemTag? BuyTag  => ItemTag.Craftware;
    public override int ModiMentisCount => 9;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a black-gowned figure walks fast with an armful of books, muttering",
        "someone stands at a board covered in figures, chalk in hand",
        "an ink-stained figure peers at a page through a lens",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "cerebrum", "tongue", "eyes" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new ScholarGown(), () => new ReadingLenses(), () => new Tome(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Quill(), () => new InkHorn(), () => new Parchment(), () => new Diagram(),
    };

    public override string SelfIntroduction => "a master of this school. I teach, I read, and I am usually late for both";
    public override string Workplace        => "the lecture hall";
    public override string Craft            => "letters and number";
    public override string DailyLabour      => "lecturing, examining, disputing, copying and arguing with colleagues about all four";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work] = "teaching is only explaining the same thing until one of them understands. It is endless and it is good",
        [DialogueTopic.Stories] = "the old histories are wrong about half of everything. I am writing a correction",
        [DialogueTopic.Trade] = "books are the only thing worth spending money on. Food is a distant second",
        [DialogueTopic.Neighbours] = "the other masters are fools. Learned fools, which is the worst kind",
        [DialogueTopic.Omens] = "superstition. Every omen has a cause, if you will only look for it",
        [DialogueTopic.Rest] = "rest? I rest by reading something I do not have to teach",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a master of the imperial school. You are learned, opinionated and easily distracted by an interesting question.

You speak precisely and at length. You are condescending to the ignorant without meaning to be, and delighted by anyone with a genuine question.";
}

/// <summary>Steward - manager of a great household: castle, palace or estate.</summary>
public class StewardArchetype : SettledArchetype
{
    public override string ArchetypeId => "steward";
    public override string TradeModusMentisId => "stewardry";
    public override SocialCategory? Social => SocialCategory.Bourgeois;
    public override string RoleNoun => "steward";
    public override ItemTag? SellTag => ItemTag.Craftware;
    public override ItemTag? BuyTag  => ItemTag.Foodstuff;
    public override int ModiMentisCount => 9;
    public override int AuthorityLevel => 1;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "lord" };
    public override string IntroductionRelation => "my lord";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a figure with a ring of keys at their belt directs servants with small gestures",
        "someone with an account book checks barrels against a list",
        "a neat, watchful figure stands by the hall door, missing nothing",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "cerebrum", "tongue", "eyes" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new KeyRing(), () => new Doublet(), () => new Ledgerbook(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new CoinPurse(), () => new SealingWax(), () => new Letter(), () => new Abacus(),
    };

    public override string SelfIntroduction => "the steward of this house. Whatever you need here, it comes through me";
    public override string Workplace        => "the great hall";
    public override string Craft            => "the household";
    public override string DailyLabour      => "the stores, the servants, the accounts, the guests, and making sure my lord never has to think about any of them";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work] = "a great house is a small town. It needs a mayor. I am it",
        [DialogueTopic.Food] = "I know to the sack what is in the stores, and to the day how long it will last",
        [DialogueTopic.Neighbours] = "every guest is a cost, a risk and an opportunity, in that order",
        [DialogueTopic.Trade] = "the merchants know better than to cheat this house. They tried once",
        [DialogueTopic.Kin] = "my lord's family is my family. My own I see on feast days",
        [DialogueTopic.Rest] = "a steward rests when the house sleeps. The house never quite sleeps",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, steward of this great house. You speak with polished courtesy and you notice everything, especially what is out of place.

You are loyal to the house above all, discreet, efficient and quietly powerful. You can be very helpful or very obstructive, and you choose.";
}

/// <summary>Lord - holder of a castle or palace and the land around it.</summary>
public class LordArchetype : SettledArchetype
{
    public override string ArchetypeId => "lord";
    public override string TradeModusMentisId => "statecraft";
    public override SocialCategory? Social => SocialCategory.Aristocrat;
    public override string RoleNoun => "lord";
    public override ItemTag? SellTag => ItemTag.Ironwork;
    public override ItemTag? BuyTag  => ItemTag.Textile;
    public override int ModiMentisCount => 10;
    public override int AuthorityLevel => 2;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a richly dressed figure sits in the high seat, listening to a petitioner without expression",
        "someone in a fur-trimmed mantle walks the battlements, attended at a distance",
        "a figure with a gold chain glances up from a letter, then away",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "cerebrum", "heart", "visage" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Mantle(), () => new Chain(), () => new Longsword(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Signet(), () => new NobleUndertunic(), () => new SilkStockings(), () => new Wine(),
    };

    public override string SelfIntroduction => "this is my house, and my land around it. You are in it at my pleasure";
    public override string Workplace        => "the high seat";
    public override string Craft            => "the holding";
    public override string DailyLabour      => "hearing petitions, judging disputes, receiving guests, hunting, and keeping the lords on either side of me in check";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Neighbours] = "my neighbours are my rivals. My rivals are my kin. It is all the same thing, in the end",
        [DialogueTopic.Trade] = "I do not trade. My steward trades. I spend",
        [DialogueTopic.Work] = "ruling is work, whatever the peasants think. They should try it",
        [DialogueTopic.Kin] = "the line must go on. Everything I do is for the line",
        [DialogueTopic.Stories] = "my ancestor held this castle against a siege of forty days. We tell it every feast",
        [DialogueTopic.Roads] = "every road through my land pays my toll. That is what roads are for",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, lord of this holding. You speak with the ease of someone who has never had to ask twice for anything, and you expect deference.

You can be gracious, generous and witty, or cold and dangerous, depending on how you are approached. You value loyalty, lineage and courage, and despise flattery that is too obvious.";
}

/// <summary>Merchant - trader of a town or port, buyer and seller of goods from far away.</summary>
public class MerchantArchetype : SettledArchetype
{
    public override string ArchetypeId => "merchant";
    public override string TradeModusMentisId => "brokerage";
    public override SocialCategory? Social => SocialCategory.Urban;
    public override string RoleNoun => "merchant";
    public override ItemTag? SellTag => ItemTag.Textile;
    public override ItemTag? BuyTag  => ItemTag.Crop;
    public override int ModiMentisCount => 9;
    public override int AuthorityLevel => 1;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a well-dressed figure counts bales with a clerk at their elbow",
        "someone in a fine doublet haggles over a bolt of cloth, smiling and immovable",
        "a figure with a signet ring weighs coins on a small brass balance",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "tongue", "cerebrum", "eyes" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Doublet(), () => new Scales(), () => new CoinPurse(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Signet(), () => new PromissoryNote(), () => new DyedCloth(), () => new SpiceBundle(),
    };

    public override string SelfIntroduction => "a merchant of this town - if it is bought or sold within these walls, I have probably had a hand in it";
    public override string Workplace        => "the counting house";
    public override string Craft            => "the deal";
    public override string DailyLabour      => "buying cheap, selling dear, writing letters, chasing debts and watching the ships come in";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Trade] = "buy where it is common, sell where it is rare. Everything else is detail",
        [DialogueTopic.Roads] = "every bandit on the road is a tax I did not vote for",
        [DialogueTopic.Neighbours] = "the other merchants are my rivals and my creditors. Usually both",
        [DialogueTopic.Weather] = "a storm at sea and a ship lost. It has happened to me twice. I have not forgotten either",
        [DialogueTopic.Work] = "my work is knowing things before other people do",
        [DialogueTopic.Kin] = "my son will take over the house. If he ever learns to count",
        [DialogueTopic.Stories] = "I have been to the far coast. The stories are true, mostly. The prices are not",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a merchant. You are shrewd, sociable and always calculating, and you treat every conversation as a possible deal.

You are polite to everyone because everyone is a possible customer, and you are honest when it is profitable, which is more often than people think.";
}

/// <summary>Sailor - deckhand of the ports, between voyages or on shore leave.</summary>
public class SailorArchetype : SettledArchetype
{
    public override string ArchetypeId => "sailor";
    public override string TradeModusMentisId => "seamanship";
    public override SocialCategory? Social => SocialCategory.Urban;
    public override string RoleNoun => "sailor";
    public override ItemTag? SellTag => ItemTag.Fish;
    public override ItemTag? BuyTag  => ItemTag.Foodstuff;
    public override int ModiMentisCount => 7;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "merchant" };
    public override string IntroductionRelation => "the shipowner";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a tar-stained figure splices a rope on the quay, fingers working without looking",
        "someone with a rolling walk and a sun-darkened face sits on a bollard, watching the ships",
        "a figure in canvas slops hauls a line, singing under their breath",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "arms", "hands", "legs" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Slops(), () => new Marlinspike(), () => new LinenTunic(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new BelayingPin(), () => new SaltFish(), () => new Rope(), () => new LuckCharm(),
    };

    public override string SelfIntroduction => "just off a ship. Or about to be on one. One of the two";
    public override string Workplace        => "the deck";
    public override string Craft            => "the ship";
    public override string DailyLabour      => "hauling, splicing, standing watches, going aloft in weather, and drinking it all away in port";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Water] = "the sea does not care about you. Remember that and it might let you live",
        [DialogueTopic.Weather] = "I can smell a storm coming a day out. Most landsmen cannot smell rain when it is falling",
        [DialogueTopic.Rest] = "in port. All of it, in a week, every copper",
        [DialogueTopic.Food] = "salt fish and hard bread for a month at sea. You stop tasting it",
        [DialogueTopic.Omens] = "never whistle on deck. Never sail on the last day of the month. Never let a priest aboard",
        [DialogueTopic.Kin] = "a sweetheart in every port is the saying. One in two ports is closer to the truth",
        [DialogueTopic.Stories] = "I have seen a fish as big as a church. You do not believe me. Nobody ever does",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a sailor. You are loud, superstitious, generous and a little reckless, with a rolling walk and a vocabulary that would shame a priest.

You are at ease anywhere near water and uneasy far from it. You tell tall tales and believe half of them.";
}

/// <summary>Innkeeper - keeper of a tavern or inn, its drink, its beds and its talk.</summary>
public class InnkeeperArchetype : SettledArchetype
{
    public override string ArchetypeId => "innkeeper";
    public override string TradeModusMentisId => "tapstering";
    public override SocialCategory? Social => SocialCategory.Urban;
    public override string RoleNoun => "innkeeper";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Crop;
    public override int ModiMentisCount => 8;
    public override int AuthorityLevel => 1;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a broad figure in an apron wipes down the counter, watching the room",
        "someone draws a pot of ale, listening to two drinkers argue",
        "a figure with a ring of keys greets a traveller at the door",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "tongue", "ears", "hands" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Apron(), () => new Tankard(), () => new KeyRing(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new CoinPurse(), () => new Beer(), () => new Mead(), () => new Dice(),
    };

    public override string SelfIntroduction => "this is my house. Ale, a bed, a hot meal - whatever you want, if you can pay for it";
    public override string Workplace        => "the taproom";
    public override string Craft            => "the house";
    public override string DailyLabour      => "brewing, drawing, cooking, cleaning, keeping the peace and keeping the accounts, from before dawn to after midnight";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Food] = "a hot meal and an honest pot. That is all anyone wants, and you would be amazed how few places give it",
        [DialogueTopic.Trade] = "I sell drink and I sell beds, but what I really sell is somewhere to be",
        [DialogueTopic.Neighbours] = "they all come here. I know everything about all of them. I say nothing",
        [DialogueTopic.Roads] = "travellers bring news. I give it to the next traveller. That is half my trade",
        [DialogueTopic.Rest] = "an innkeeper sleeps when the last drunk goes home. So, not often",
        [DialogueTopic.Stories] = "I have heard every story there is, at least twice, from men who swore it happened to them",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, keeper of this inn. You are hospitable, shrewd and unshockable, and you have seen every kind of guest.

You keep the peace in your house firmly. You know everyone's business and gossip carefully, and you will help a stranger who pays and is polite.";
}

/// <summary>Clerk - writer of the documents of a commandery, chancery or counting house.</summary>
public class ClerkArchetype : SettledArchetype
{
    public override string ArchetypeId => "clerk";
    public override string TradeModusMentisId => "clerkship";
    public override SocialCategory? Social => SocialCategory.Urban;
    public override string RoleNoun => "clerk";
    public override ItemTag? SellTag => ItemTag.Craftware;
    public override ItemTag? BuyTag  => ItemTag.Craftware;
    public override int ModiMentisCount => 8;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "steward", "captain" };
    public override string IntroductionRelation => "my superior";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a thin figure in oversleeves writes steadily at a high desk",
        "someone with ink on their fingers sorts a pile of documents, frowning",
        "a figure peers over a ledger at the visitor, quill still raised",
    };

    public override IReadOnlyList<string> OrganEmphasis =>
        new[] { "hands", "eyes", "cerebrum" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Sleeves(), () => new Quill(), () => new InkHorn(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Parchment(), () => new SealingWax(), () => new ReadingLenses(), () => new Writ(),
    };

    public override string SelfIntroduction => "a clerk of this office. If it is not written down, it did not happen - and I write it down";
    public override string Workplace        => "the clerks' room";
    public override string Craft            => "the quill and the seal";
    public override string DailyLabour      => "copying, recording, sealing, filing, and finding things other people have filed wrongly";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work] = "every document has a form. Get the form wrong and the document is worthless",
        [DialogueTopic.Trade] = "a copper a page, and the page is worth more than the copper. Nobody sees that",
        [DialogueTopic.Neighbours] = "they come to me when they need something written. Otherwise they do not see me",
        [DialogueTopic.Rest] = "the office closes at sunset. My work does not",
        [DialogueTopic.Stories] = "the archive has records going back three hundred years. Nobody reads them but me",
        [DialogueTopic.Kin] = "my father was a clerk. His father was a clerk. It is in the hand",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a clerk. You are precise, pedantic and underappreciated, and you know that the empire runs on documents nobody else bothers to read.

You are cautious, rule-bound and quietly proud. You can be helpful to someone who respects procedure and impossible to someone who does not.";
}
