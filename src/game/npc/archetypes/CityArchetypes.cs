using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Npc.Archetypes;

// ─────────────────────────────────────────────────────────────────────────────
//  The people of a dense city: eight shop trades, each a master with an apprentice and a shop of
//  their own (CityTradeSubfactory), and four callings of the street who live in its houses - the
//  porter, the laundress, the water-carrier and the beggar.
// ─────────────────────────────────────────────────────────────────────────────

// ── The shop trades ─────────────────────────────────────────────────────────

/// <summary>Cobbler - makes and mends the town's shoes and boots.</summary>
public class CobblerArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "cobbler";
    public override string TradeModusMentisId => "cobbling";
    public override ItemTag? SellTag => ItemTag.Clothing;
    public override ItemTag? BuyTag  => ItemTag.Pelt;
    public override int ModiMentisCount => 8;
    public override int AuthorityLevel => 1;
    public override string RoleNoun => "cobbler";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a hunched figure on a low bench drives an awl through a sole, thread in their teeth",
        "someone with a boot over one knee hammers in hobnails without looking up",
        "a figure squints at a customer's feet as if reading a letter",
    };

    public override IReadOnlyList<string> OrganEmphasis => new[] { "hands", "eyes", "backbone" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new LeatherApron(), () => new Awl(), () => new LinenTunic(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new ShoeLast(), () => new Hobnails(), () => new LeatherBoots(), () => new CoinPurse(),
    };

    public override string SelfIntroduction => "the cobbler. Half the feet in this town walk on my work, and the other half should";
    public override string Workplace        => "the bench";
    public override string Craft            => "shoemaking";
    public override string DailyLabour      => "cutting uppers, lasting, stitching welts and putting new heels on old boots until the light goes";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work]       = "a shoe is made for one foot and it lies to every other. Remember that when you buy second-hand",
        [DialogueTopic.Roads]      = "every bad road in the province ends up on my bench, one heel at a time",
        [DialogueTopic.Neighbours] = "I know who limps, who drinks and who walks to the widow's at night. Heels do not lie",
        [DialogueTopic.Trade]      = "the tanner robs me, I rob the gentry, and the poor get their boots mended for bread",
        [DialogueTopic.Weather]    = "rain is good for trade. Mud is better",
        [DialogueTopic.Health]     = "my back is bent like a hook and my eyes are going. Thirty years of looking down does that",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the cobbler. You spend your days bent over other people's feet and you have opinions about all of them.

You are dry, observant and a little sour, and you judge everyone first by their shoes. You talk while you work and never stop working while you talk.";
}

/// <summary>Tailor - cuts and sews the town's clothes.</summary>
public class TailorArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "tailor";
    public override string TradeModusMentisId => "tailoring";
    public override ItemTag? SellTag => ItemTag.Clothing;
    public override ItemTag? BuyTag  => ItemTag.Textile;
    public override int ModiMentisCount => 8;
    public override int AuthorityLevel => 1;
    public override string RoleNoun => "tailor";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a neat figure with pins in their mouth kneels to chalk a hem",
        "someone with a tape round their neck runs a critical eye over a passer-by's coat",
        "a figure sits cross-legged on a table, sewing with tiny quick stitches",
    };

    public override IReadOnlyList<string> OrganEmphasis => new[] { "eyes", "hands", "tongue" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Waistcoat(), () => new Thimble(), () => new SewingNeedles(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Shears(), () => new TailorsChalk(), () => new DyedCloth(), () => new CoinPurse(),
    };

    public override string SelfIntroduction => "a tailor. Whatever you are wearing, I could do better - forgive me, I cannot help seeing it";
    public override string Workplace        => "the cutting table";
    public override string Craft            => "the needle";
    public override string DailyLabour      => "measuring, cutting, fitting and sewing, and listening to customers lie about their waists";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work]       = "anyone can sew. The art is in the cutting, and nobody sees it unless it is wrong",
        [DialogueTopic.Trade]      = "the merchants dress like lords now. The lords dress like merchants. It is all very good for me",
        [DialogueTopic.Neighbours] = "I have measured half the town. I know who has grown fat and who is wasting, and I say nothing",
        [DialogueTopic.Kin]        = "my mother sewed, and hers. The needle passes down like the nose",
        [DialogueTopic.Stories]    = "I once made a coat for a man who was hanged in it the next week. It fitted beautifully",
        [DialogueTopic.Rest]       = "rest? There is always a hem waiting",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a tailor. You are precise, a little vain and very observant, and you cannot look at anyone without measuring them.

You are courteous to customers and merciless about cloth. You gossip about who wore what, and you notice a badly cut coat across a square.";
}

/// <summary>Chandler - renders tallow and makes and sells candles.</summary>
public class ChandlerArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "chandler";
    public override string TradeModusMentisId => "chandlery";
    public override ItemTag? SellTag => ItemTag.Craftware;
    public override ItemTag? BuyTag  => ItemTag.Foodstuff;
    public override int ModiMentisCount => 8;
    public override int AuthorityLevel => 1;
    public override string RoleNoun => "chandler";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a greasy-aproned figure lowers a frame of wicks into a steaming vat",
        "someone smelling strongly of mutton fat bundles candles by the dozen",
        "a figure skims scum off a pot of rendering tallow, eyes watering",
    };

    public override IReadOnlyList<string> OrganEmphasis => new[] { "hands", "nose", "arms" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new LeatherApron(), () => new Candle(), () => new Wick(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Tallow(), () => new Beeswax(), () => new CandleMould(), () => new Soap(),
    };

    public override string SelfIntroduction => "the chandler. Every light in this town after sunset is one of mine, or stolen from a church";
    public override string Workplace        => "the vat";
    public override string Craft            => "candles";
    public override string DailyLabour      => "rendering suet, dipping wicks, casting tapers, and making soap from what is left";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work]    = "dip and wait, dip and wait. A good candle takes thirty dips and no hurrying",
        [DialogueTopic.Seasons] = "winter is my harvest. Long nights, everyone buying light",
        [DialogueTopic.Trade]   = "beeswax for the priests and the rich, tallow for everyone else. The tallow pays better",
        [DialogueTopic.Omens]   = "a candle that gutters for no reason means someone in the house is going to die. I sell a lot of candles",
        [DialogueTopic.Food]    = "the butcher's leavings are my living. I eat well enough, but I smell of it",
        [DialogueTopic.Health]  = "the fumes get into your chest. Every chandler coughs",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the chandler. You render fat and dip candles all day in a reeking shop and you have long since stopped noticing the smell.

You are patient, practical and a little gloomy, with a dark line in jokes about light and death.";
}

/// <summary>Butcher - slaughters, cuts and sells meat.</summary>
public class ButcherArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "butcher";
    public override string TradeModusMentisId => "butchery";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Pelt;
    public override int ModiMentisCount => 8;
    public override int AuthorityLevel => 1;
    public override string RoleNoun => "butcher";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a big figure in a bloody apron brings a cleaver down through a joint with one blow",
        "someone wipes their hands on a rag and weighs a cut of mutton for a waiting woman",
        "a figure hoists a side of pork onto a hook, whistling",
    };

    public override IReadOnlyList<string> OrganEmphasis => new[] { "arms", "hands", "backbone" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new LeatherApron(), () => new Cleaver(), () => new Knife(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Sausage(), () => new MeatHook(), () => new Tripe(), () => new CoinPurse(),
    };

    public override string SelfIntroduction => "the butcher. If you eat meat in this quarter, I cut it - and I knew it when it was walking";
    public override string Workplace        => "the block";
    public override string Craft            => "the cleaver";
    public override string DailyLabour      => "slaughtering at dawn, jointing all morning, selling till the meat turns, and selling cheaper after that";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Food]    = "people who turn their noses up at tripe have never been hungry",
        [DialogueTopic.Beasts]  = "I do it quick and I do it clean. That is more mercy than the wolves show",
        [DialogueTopic.Work]    = "find the joint and the knife does the work. Miss it and you are hacking like a soldier",
        [DialogueTopic.Weather] = "a hot week is a ruinous week. The flies come before the customers",
        [DialogueTopic.Trade]   = "the drovers bring the beasts, the tanner takes the hides, the chandler the fat. Nothing is wasted but the squeal",
        [DialogueTopic.Health]  = "I have all ten fingers. Ask any other butcher how rare that is",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the butcher. You are big, blunt, cheerful and entirely unsentimental about meat.

You speak loudly and plainly, you are generous with scraps to the poor and ruthless with anyone who haggles too hard.";
}

/// <summary>Tanner - makes leather from hides in the stinking yard at the edge of town.</summary>
public class TannerArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "tanner";
    public override string TradeModusMentisId => "tanning";
    public override ItemTag? SellTag => ItemTag.Pelt;
    public override ItemTag? BuyTag  => ItemTag.Pelt;
    public override int ModiMentisCount => 8;
    public override int AuthorityLevel => 1;
    public override string RoleNoun => "tanner";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a figure stained brown to the elbows drags a dripping hide out of a pit",
        "someone scrapes a hide over a sloping beam with a long two-handled knife",
        "a figure with a scarf over their face stirs a pit of brown liquor with a pole",
    };

    public override IReadOnlyList<string> OrganEmphasis => new[] { "arms", "hands", "trunk" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new LeatherApron(), () => new FleshingKnife(), () => new LeatherBoots(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new TannedLeather(), () => new TanBark(), () => new LeatherGloves(), () => new CoinPurse(),
    };

    public override string SelfIntroduction => "the tanner. Yes, it is me you can smell. You get used to it. I did";
    public override string Workplace        => "the pits";
    public override string Craft            => "leather";
    public override string DailyLabour      => "fleshing, liming, laying down hides in the bark pits and turning them, month after month";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work]       = "a hide takes a year to become good leather. People who want it in a month get what they deserve",
        [DialogueTopic.Neighbours] = "they built the town upwind of me on purpose. I do not blame them",
        [DialogueTopic.Trade]      = "every saddle, boot and belt in this town started in my pits. They hold their noses and pay",
        [DialogueTopic.Water]      = "the stream below the yard runs brown. The water-carriers know not to draw from it",
        [DialogueTopic.Kin]        = "nobody marries a tanner's child but another tanner's child. So we are all cousins",
        [DialogueTopic.Health]     = "my hands are like boot-leather themselves. The lime eats everything else",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the tanner. You do the filthiest work in the town and you are proud of it, and rich from it, and lonely because of it.

You are blunt, hard to offend and slow to trust people who wrinkle their noses at you.";
}

/// <summary>Potter - throws and fires the town's pots, jugs and bowls.</summary>
public class PotterArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "potter";
    public override string TradeModusMentisId => "potcraft";
    public override ItemTag? SellTag => ItemTag.Craftware;
    public override ItemTag? BuyTag  => ItemTag.Mineral;
    public override int ModiMentisCount => 8;
    public override int AuthorityLevel => 1;
    public override string RoleNoun => "potter";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a clay-spattered figure kicks a wheel round, a pot rising between their hands",
        "someone taps a jug with a fingernail and listens to it ring",
        "a figure stacks green pots in a kiln with great care",
    };

    public override IReadOnlyList<string> OrganEmphasis => new[] { "hands", "arms", "ears" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new LeatherApron(), () => new PottersRib(), () => new Clay(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new EarthenJug(), () => new GlazedBowl(), () => new ClayPot(), () => new CoinPurse(),
    };

    public override string SelfIntroduction => "the potter. Everything you drink from and cook in, I made, and I will make it again when you break it";
    public override string Workplace        => "the wheel";
    public override string Craft            => "the clay";
    public override string DailyLabour      => "wedging, throwing, trimming, glazing and firing, and losing a third of it in the kiln";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work]    = "centre the clay first. If it is not centred, nothing you do after will save it",
        [DialogueTopic.Weather] = "damp weather and the pots will not dry. Dry weather and they crack. There is no good weather",
        [DialogueTopic.Trade]   = "pots break. That is the whole secret of my trade",
        [DialogueTopic.Seasons] = "I fire the big kiln four times a year. The whole street comes out to watch",
        [DialogueTopic.Wilds]   = "the best clay is in the river bank two miles north. I will not tell you where exactly",
        [DialogueTopic.Rest]    = "I sit at the wheel and it is rest and work both. Strange trade",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, the potter. You are calm, earthy and patient, with clay drying on your forearms and a habit of turning things over in your hands.

You speak slowly and plainly and you enjoy explaining your craft to anyone who asks.";
}

/// <summary>Apothecary - compounds and sells medicines, simples and spices.</summary>
public class ApothecaryArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "apothecary";
    public override string TradeModusMentisId => "physic";
    public override ItemTag? SellTag => ItemTag.Herb;
    public override ItemTag? BuyTag  => ItemTag.Herb;
    public override int ModiMentisCount => 9;
    public override int AuthorityLevel => 1;
    public override string RoleNoun => "apothecary";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a thin figure grinds something in a bronze mortar, sniffing it from time to time",
        "someone in a dark gown reaches down a painted jar and weighs out a pinch on tiny scales",
        "a figure peers at a customer's tongue and shakes their head",
    };

    public override IReadOnlyList<string> OrganEmphasis => new[] { "nose", "cerebrum", "hands" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Doublet(), () => new Pestle(), () => new Theriac(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Albarello(), () => new Herb(), () => new SpiceBundle(), () => new Scales(),
    };

    public override string SelfIntroduction => "an apothecary. A remedy for most things, and for the rest, something that will make you feel you were remedied";
    public override string Workplace        => "the dispensary";
    public override string Craft            => "physic";
    public override string DailyLabour      => "grinding, steeping, distilling and compounding, and listening to the sick describe their bowels";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Health]  = "most illness is the humours out of balance. Most cures are rest and patience. I sell the rest",
        [DialogueTopic.Wilds]   = "the herb-women know more than they let on. I buy from them and say it came from the east",
        [DialogueTopic.Trade]   = "theriac has sixty ingredients and I have perhaps forty of them. Nobody has ever noticed",
        [DialogueTopic.Food]    = "you are what you eat. You, I would guess, eat badly",
        [DialogueTopic.Omens]   = "the stars govern the body. I do not prescribe on an evil day, whatever the patient wants",
        [DialogueTopic.Stories] = "a man came to me certain he was made of glass. I cured him with a hammer. Gently",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, an apothecary. You are learned, a little pompous and genuinely curious about the body and its ills.

You use long words and then explain them. You are kind to the sick, sceptical of physicians and a little ashamed of how much of your trade is reassurance.";
}

/// <summary>Barber-surgeon - shaves, cuts hair, pulls teeth, lets blood and sets bones.</summary>
public class BarberArchetype : CraftsmanArchetype
{
    public override string ArchetypeId => "barber";
    public override string TradeModusMentisId => "tooth_drawing";
    public override ItemTag? SellTag => ItemTag.Tool;
    public override ItemTag? BuyTag  => ItemTag.Craftware;
    public override int ModiMentisCount => 8;
    public override int AuthorityLevel => 1;
    public override string RoleNoun => "barber";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a talkative figure strops a razor, chattering to the man in the chair",
        "someone braces a knee against a chair and pulls, while the patient howls",
        "a figure ties a cord round an arm and lays a basin ready",
    };

    public override IReadOnlyList<string> OrganEmphasis => new[] { "hands", "tongue", "eyes" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Apron(), () => new Razor(), () => new Lancet(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new BleedingBowl(), () => new Soap(), () => new Shears(), () => new CoinPurse(),
    };

    public override string SelfIntroduction => "the barber. A shave, a cut, a tooth, a vein - sit down, I will have you right in no time";
    public override string Workplace        => "the chair";
    public override string Craft            => "the razor";
    public override string DailyLabour      => "shaving, trimming, bleeding, pulling teeth and setting the odd bone, and talking through all of it";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Health]     = "a good bleeding cures most things. What it does not cure, a tooth pulled usually does",
        [DialogueTopic.Neighbours] = "everyone talks in the chair. I hear more than the priest",
        [DialogueTopic.Work]       = "steady hands and a strong arm. And a loud voice, to drown out the screaming",
        [DialogueTopic.Trade]      = "the physicians look down on me. Then their servants come to me because I am cheap and I work",
        [DialogueTopic.Stories]    = "I pulled a tooth from a guard captain once that had three roots. He fainted. I kept the tooth",
        [DialogueTopic.Rest]       = "Sundays I shave for free outside the church. It brings in the week's teeth",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a barber-surgeon. You are chatty, cheerful and utterly unbothered by blood, and you talk without stopping while you work.

You know every rumour in town because everyone talks in the chair, and you pass them on freely.";
}

// ── The street ──────────────────────────────────────────────────────────────

/// <summary>Porter - carries loads through the town for whoever pays.</summary>
public class PorterArchetype : SettledArchetype
{
    public override string ArchetypeId => "porter";
    public override string TradeModusMentisId => "haulage";
    public override SocialCategory? Social => SocialCategory.Urban;
    public override string RoleNoun => "porter";
    public override ItemTag? SellTag => ItemTag.Crop;
    public override ItemTag? BuyTag  => ItemTag.Foodstuff;
    public override int ModiMentisCount => 7;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "merchant", "innkeeper" };
    public override string IntroductionRelation => "who I carry for";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a broad-backed figure trudges past bent under a bale, a strap across their brow",
        "someone sits on an upturned crate, waiting to be hired, rubbing their shoulders",
        "a figure shoulders through the crowd with a barrel balanced on one shoulder",
    };

    public override IReadOnlyList<string> OrganEmphasis => new[] { "backbone", "legs", "arms" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new Tumpline(), () => new LinenTunic(), () => new Rope(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Bread(), () => new LeatherBoots(), () => new WoolCap(), () => new Knife(),
    };

    public override string SelfIntroduction => "a porter. You have something heavy, I have a strong back. A copper a street";
    public override string Workplace        => "the street";
    public override string Craft            => "carrying";
    public override string DailyLabour      => "waiting at the gate and the quay to be hired, and carrying whatever comes until dark";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Work]       = "lift with the legs, carry with the back, and never let them see it is heavy",
        [DialogueTopic.Health]     = "every porter's back gives out by forty. Then it is begging or the river",
        [DialogueTopic.Trade]      = "the merchants pay by the load and argue over every one",
        [DialogueTopic.Neighbours] = "I know every street and every stair in this town by how much they hurt",
        [DialogueTopic.Roads]      = "the carts take the long road. I take the steps. I am faster",
        [DialogueTopic.Food]       = "bread and an onion at noon and a pot of ale at night. A porter's whole wage goes in at the mouth",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a porter. You are strong, tired, good-humoured and poor, and you carry other people's goods through the town for coppers.

You speak plainly and without self-pity, and you know the town's streets and shortcuts better than anyone.";
}

/// <summary>Laundress - washes the town's linen at the wash house.</summary>
public class LaundressArchetype : SettledArchetype
{
    public override string ArchetypeId => "laundress";
    public override string TradeModusMentisId => "laundering";
    public override SocialCategory? Social => SocialCategory.Urban;
    public override string RoleNoun => "laundress";
    public override ItemTag? SellTag => ItemTag.Textile;
    public override ItemTag? BuyTag  => ItemTag.Craftware;
    public override int ModiMentisCount => 7;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "tailor", "innkeeper" };
    public override string IntroductionRelation => "a customer of mine";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a red-armed figure beats a sheet on a stone, talking all the while",
        "someone wrings a shirt with both fists, water streaming onto the flags",
        "a figure carries a basket of wet linen on one hip, steam rising off it",
    };

    public override IReadOnlyList<string> OrganEmphasis => new[] { "arms", "hands", "tongue" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new WashBeetle(), () => new LinenTunic(), () => new Soap(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Lye(), () => new WickerBasket(), () => new WoolCap(), () => new Bread(),
    };

    public override string SelfIntroduction => "a laundress. Your shirt could do with me, by the look of it";
    public override string Workplace        => "the wash house";
    public override string Craft            => "the wash";
    public override string DailyLabour      => "soaking, beating, rinsing, wringing and spreading linen to whiten, from first light to last";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Neighbours] = "you can tell everything about a household from its washing. Everything",
        [DialogueTopic.Work]       = "cold water for blood, never hot. Hot sets it. Remember that, if you ever need to",
        [DialogueTopic.Weather]    = "a sunny day and the sheets bleach white as bone. A wet week and nothing dries and everyone shouts",
        [DialogueTopic.Health]     = "the lye eats your hands. Look at mine. Forty years and they look sixty",
        [DialogueTopic.Kin]        = "my girls work beside me. They will marry better than I did, if I have any say",
        [DialogueTopic.Stories]    = "the stories you hear at the wash house would curl a priest's hair",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a laundress. You are loud, quick-witted and tireless, and you know every household's secrets by its linen.

You gossip freely, laugh easily and have no patience whatever with people who think themselves above you.";
}

/// <summary>Water-carrier - draws water and carries it to the houses that pay for it.</summary>
public class WaterCarrierArchetype : SettledArchetype
{
    public override string ArchetypeId => "water_carrier";
    public override string TradeModusMentisId => "water_bearing";
    public override SocialCategory? Social => SocialCategory.Urban;
    public override string RoleNoun => "water-carrier";
    public override ItemTag? SellTag => ItemTag.Foodstuff;
    public override ItemTag? BuyTag  => ItemTag.Foodstuff;
    public override int ModiMentisCount => 7;
    public override IReadOnlyList<string> CanIntroduceToArchetypes => new[] { "innkeeper", "laundress" };
    public override string IntroductionRelation => "one of my houses";

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a figure with a yoke across their shoulders walks carefully past, two buckets brimming",
        "someone hauls a dripping bucket up from the well hand over hand",
        "a figure knocks at a door, a bucket in each hand, calling 'water!'",
    };

    public override IReadOnlyList<string> OrganEmphasis => new[] { "arms", "legs", "backbone" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new WaterYoke(), () => new LeatherBucket(), () => new LinenTunic(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new LeatherBucket(), () => new WaterDraught(), () => new WoolCap(), () => new Bread(),
    };

    public override string SelfIntroduction => "the water-carrier. Half the kettles on this street were filled by me this morning";
    public override string Workplace        => "the well";
    public override string Craft            => "the yoke";
    public override string DailyLabour      => "drawing at the well before dawn, then the round of doors, then the well again, all day";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Water]      = "the square well is sweet in spring and foul by autumn. The north well is good all year. Nobody listens",
        [DialogueTopic.Neighbours] = "I go into every kitchen on my round. I know who is sick before the priest does",
        [DialogueTopic.Health]     = "the flux comes from bad water. I have seen it go through a street that drew from the wrong well",
        [DialogueTopic.Work]       = "keep the buckets level and the step soft. Spill it and you carry it twice",
        [DialogueTopic.Weather]    = "drought is good for trade and bad for everything else",
        [DialogueTopic.Roads]      = "I walk every street in this town twice a day. I have never left it",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a water-carrier. You walk the town all day with a yoke on your shoulders and you know every kitchen and every well.

You are patient, steady and footsore, and you worry about the wells the way other people worry about the weather.";
}

/// <summary>Beggar - lives on alms in the squares and at the gate, sleeps in the doss house.</summary>
public class BeggarArchetype : SettledArchetype
{
    public override string ArchetypeId => "beggar";
    public override string TradeModusMentisId => "beggary";
    public override SocialCategory? Social => SocialCategory.Pauper;
    public override string RoleNoun => "beggar";
    public override ItemTag? SellTag => ItemTag.Forage;
    public override ItemTag? BuyTag  => ItemTag.Foodstuff;
    public override int ModiMentisCount => 7;

    protected override string[] ObservationHintVariants(string nodeContext) => new[]
    {
        "a ragged figure holds out a bowl, murmuring a blessing to every passer-by",
        "someone on a crutch sits against the wall, watching the crowd with sharp eyes",
        "a figure in a cloak more patch than cloth counts three coppers with great care",
    };

    public override IReadOnlyList<string> OrganEmphasis => new[] { "tongue", "eyes", "ears" };

    public override IReadOnlyList<Func<Item>> Loadout => new Func<Item>[]
    {
        () => new RaggedCloak(), () => new BeggingBowl(),
    };

    public override IReadOnlyList<Func<Item>> OptionalLoadout => new Func<Item>[]
    {
        () => new Crutch(), () => new Bread(), () => new Potsherd(), () => new LuckCharm(),
    };

    public override string SelfIntroduction => "nobody, friend. Just a poor soul with a bowl. A copper, for the love of the gods?";
    public override string Workplace        => "the church steps";
    public override string Craft            => "the bowl";
    public override string DailyLabour      => "sitting where the crowd is thickest, asking, blessing, and watching everything";

    protected override IReadOnlyDictionary<DialogueTopic, string> TopicOpinions => new Dictionary<DialogueTopic, string>
    {
        [DialogueTopic.Food]       = "I ate yesterday. I think it was yesterday",
        [DialogueTopic.Neighbours] = "nobody sees a beggar. So a beggar sees everybody",
        [DialogueTopic.Rest]       = "the doss house, if I have the copper. The church porch if I do not",
        [DialogueTopic.Kin]        = "I had a trade once, and a wife. Ask me another day",
        [DialogueTopic.Weather]    = "summer you can sleep anywhere. Winter kills one of us every week",
        [DialogueTopic.Omens]      = "give to a beggar and luck follows. Everyone knows that. Not everyone acts on it",
    };

    protected override string GenerateWayToSpeakDescription(string name, Random rng)
        => $@"You are {name}, a beggar. You live on alms in the squares and sleep in the doss house when you can pay for it.

You are humble when asking and sharp when not, and because nobody notices you, you notice everything. You do not like to talk about how you came to this.";
}
