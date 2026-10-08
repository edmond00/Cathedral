using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Scene.Shared;

/// <summary>
/// The shop floors of a dense city's trades, the counterpart of <see cref="WorkshopSubfactory"/>: each
/// builder returns the public hall of its building, furnished and stocked, for
/// <c>BuildingSpec.PublicHallBuilder</c>. Every room keeps one fixed name, which is unique in a
/// location because a city builds each trade at most once.
/// </summary>
public static class CityTradeSubfactory
{
    private static ItemElement I(Item item) => new(item);

    private static Dictionary<string, string> Teach(params (string verb, string mm)[] lessons)
    {
        var d = new Dictionary<string, string>();
        foreach (var (verb, mm) in lessons) d[verb] = mm;
        return d;
    }

    // ── Cobbler ──────────────────────────────────────────────────────────────

    public static Area BuildCobblery()
    {
        var shop = new CobbleryArea("Cobbler's Shop", "in the cobbler's shop", "step into the cobbler's shop",
            new() { "A low shop smelling of leather and wax, boots hung by their laces from the beams" },
            new[] { "cramped", "leathery", "waxy", "quiet" });

        shop.PointsOfInterest.Add(new LastPointOfInterest("Rack of Lasts",
            new() { "Wooden feet of every size hung on pegs, each chalked with a customer's name" },
            new() { I(new ShoeLast()), I(new ShoeLast()) }, new[] { "crowded", "dark-handled", "labelled" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "cobbling"), ("contemplate", "wear_reading")) });

        shop.PointsOfInterest.Add(new BenchPointOfInterest("Cobbler's Bench",
            new() { "A low bench with a seat worn hollow, its tray full of awls, wax and hobnails" },
            new() { I(new Awl()), I(new Hobnails()), I(new Narrative.World.Items.Thread()) }, new[] { "worn", "cluttered", "low" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "cobbling"), ("smell", "keen_nose")) });

        shop.PointsOfInterest.Add(new ShelfPointOfInterest("Shoe Shelf",
            new() { "A shelf of finished shoes and boots waiting to be fetched, mended heels uppermost" },
            new() { I(new LeatherBoots()), I(new BespokeShoes()) }, new[] { "polished", "waiting", "ordered" })
            { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "hallmark"), ("contemplate", "journeyman_eye")) });

        return shop;
    }

    // ── Tailor ───────────────────────────────────────────────────────────────

    public static Area BuildTailory()
    {
        var shop = new TailoryArea("Tailor's Shop", "in the tailor's shop", "step into the tailor's shop",
            new() { "A bright shop with a broad cutting table under the window and cloth heaped on every surface" },
            new[] { "bright", "cluttered", "hushed", "fussy" });

        shop.PointsOfInterest.Add(new DummyPointOfInterest("Tailor's Dummy",
            new() { "A padded wooden torso on a stand, a half-made coat pinned to it" },
            new() { I(new Waistcoat()) }, new[] { "headless", "pinned", "elegant" })
            { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "tailoring"), ("contemplate", "vanitas")) });

        shop.PointsOfInterest.Add(new TablePointOfInterest("Cutting Table",
            new() { "A long table chalked with patterns, shears and a thimble lying where they were put down" },
            new() { I(new Shears()), I(new Thimble()), I(new TailorsChalk()) }, new[] { "long", "chalked", "scarred" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "tailoring")) });

        shop.PointsOfInterest.Add(new ClothPointOfInterest("Bolts of Cloth",
            new() { "Bolts of wool, linen and dyed cloth stacked on their ends against the wall" },
            new() { I(new DyedCloth()), I(new SewingNeedles()) }, new[] { "colourful", "soft", "stacked" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "appraisal")) });

        return shop;
    }

    // ── Chandler ─────────────────────────────────────────────────────────────

    public static Area BuildChandlery()
    {
        var shop = new ChandleryArea("Chandlery", "in the chandlery", "step into the chandlery",
            new() { "A greasy, stuffy shop with a tallow vat steaming at the back and candles hanging in bunches" },
            new[] { "greasy", "stuffy", "rank", "warm" });

        shop.PointsOfInterest.Add(new VatPointOfInterest("Tallow Vat",
            new() { "A copper vat of melted tallow over a low fire, a skin forming on its surface" },
            new() { I(new Tallow()), I(new Tallow()) }, new[] { "steaming", "greasy", "rank" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "chandlery"), ("smell", "taint_sense")) });

        shop.PointsOfInterest.Add(new RackPointOfInterest("Dipping Rack",
            new() { "A wheel-shaped rack hung with rows of half-dipped candles cooling on their wicks" },
            new() { I(new Candle()), I(new Wick()), I(new CandleMould()) }, new[] { "dripping", "pale", "turning" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "chandlery"), ("contemplate", "patience")) });

        shop.PointsOfInterest.Add(new CounterPointOfInterest("Candle Counter",
            new() { "A counter laid with tallow dips for the poor and beeswax tapers for the church" },
            new() { I(new Beeswax()), I(new Candle()) }, new[] { "waxy", "ordered", "honeyed" })
            { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "bargaining"), ("smell", "bouquet"), ("contemplate", "piety")) });

        return shop;
    }

    // ── Butcher ──────────────────────────────────────────────────────────────

    public static Area BuildButchery()
    {
        var shop = new ButcheryArea("Butcher's Shop", "in the butcher's shop", "step into the butcher's shop",
            new() { "An open-fronted shop with sawdust on the floor, joints hanging from hooks along the beam" },
            new[] { "bloody", "cold", "loud", "fly-buzzed" });

        shop.PointsOfInterest.Add(new CarcassPointOfInterest("Hanging Carcasses",
            new() { "Split sides of pig and mutton hung from hooks, dripping into the sawdust" },
            new() { I(new MeatHook()), I(new Lard()) }, new[] { "dripping", "heavy", "raw" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "anatomy_lore"), ("smell", "carrion_sense"), ("contemplate", "vanitas")) });

        shop.PointsOfInterest.Add(new BlockPointOfInterest("Chopping Block",
            new() { "A great round of elm worn into a hollow by years of the cleaver" },
            new() { I(new Cleaver()) }, new[] { "hollowed", "scarred", "stained" })
            { Senses = SensoryProfile.Audible, VerbModiMentis = Teach(("examine", "butchery"), ("listen", "keen_ear")) });

        shop.PointsOfInterest.Add(new CounterPointOfInterest("Meat Counter",
            new() { "A slab counter laid with tripe, puddings and sausage for those who cannot afford a joint" },
            new() { I(new Tripe()), I(new BloodPudding()), I(new Sausage()) }, new[] { "slabbed", "crowded", "cheap" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "butchery"), ("smell", "keen_nose")) });

        return shop;
    }

    // ── Tanner ───────────────────────────────────────────────────────────────

    public static Area BuildTannery()
    {
        var yard = new TanneryArea("Tannery", "in the tannery yard", "step into the tannery yard",
            new() { "A walled yard of stinking pits and hides stretched on frames, the gutters running brown" },
            new[] { "stinking", "wet", "brown", "flyblown" });

        yard.PointsOfInterest.Add(new PitPointOfInterest("Bark Pits",
            new() { "Rows of square pits brimming with brown liquor, hides sunk in them under planks" },
            new() { I(new TanBark()) }, new[] { "brown", "deep", "reeking" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "tanning"), ("smell", "taint_sense")) });

        yard.PointsOfInterest.Add(new FramePointOfInterest("Stretching Frames",
            new() { "Hides laced tight on wooden frames to dry, pale side out" },
            new() { I(new TannedLeather()), I(new Hide()) }, new[] { "taut", "pale", "drying" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "tanning"), ("contemplate", "dirty_labor")) });

        yard.PointsOfInterest.Add(new RackPointOfInterest("Beam and Knives",
            new() { "A sloping log beam for fleshing, with the knives racked beside it" },
            new() { I(new FleshingKnife()), I(new LeatherApron()) }, new[] { "slick", "sloping", "sharp" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "hard_labor")) });

        return yard;
    }

    // ── Potter ───────────────────────────────────────────────────────────────

    public static Area BuildPottery()
    {
        var shop = new PotteryArea("Pottery", "in the pottery", "step into the pottery",
            new() { "A dusty workshop of shelves of drying pots, a wheel by the door and a kiln's heat at the back" },
            new[] { "dusty", "warm", "earthy", "quiet" });

        shop.PointsOfInterest.Add(new WheelPointOfInterest("Potter's Wheel",
            new() { "A kick-wheel with a wet lump of clay still centred on its head" },
            new() { I(new Clay()), I(new PottersRib()) }, new[] { "wet", "spattered", "still" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "potcraft"), ("contemplate", "meditation")) });

        shop.PointsOfInterest.Add(new KilnPointOfInterest("Kiln",
            new() { "A beehive kiln of blackened brick, ticking as it cools" },
            new() { I(new Potsherd()) }, new[] { "hot", "ticking", "blackened" })
            { Senses = SensoryProfile.Audible, VerbModiMentis = Teach(("examine", "firecraft"), ("listen", "potcraft")) });

        shop.PointsOfInterest.Add(new ShelfPointOfInterest("Ware Shelves",
            new() { "Shelves of jugs, bowls and pots, the fired ones glazed and the green ones grey and dull" },
            new() { I(new EarthenJug()), I(new GlazedBowl()), I(new ClayPot()) }, new[] { "crowded", "glazed", "fragile" })
            { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "potcraft"), ("contemplate", "aesthetic")) });

        return shop;
    }

    // ── Apothecary ───────────────────────────────────────────────────────────

    public static Area BuildDispensary()
    {
        var shop = new DispensaryArea("Apothecary's Shop", "in the apothecary's shop", "step into the apothecary's shop",
            new() { "A dim shop lined with painted jars, bunches of dried herbs hanging from the ceiling" },
            new[] { "dim", "fragrant", "hushed", "dusty" });

        shop.PointsOfInterest.Add(new JarPointOfInterest("Painted Jars",
            new() { "Rows of waisted majolica jars, each labelled in a careful Latin hand" },
            new() { I(new Albarello()), I(new Theriac()) }, new[] { "painted", "labelled", "ordered" })
            { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "physic"), ("smell", "apothecary_nose")) });

        shop.PointsOfInterest.Add(new MortarPointOfInterest("Bronze Mortar",
            new() { "A heavy bronze mortar on the counter, green with old powders" },
            new() { I(new Pestle()), I(new Herb()) }, new[] { "heavy", "powdered", "green" })
            { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "alchemy"), ("smell", "herblore")) });

        shop.PointsOfInterest.Add(new HerbPointOfInterest("Drying Herbs",
            new() { "Bunches of herbs and roots hung upside down from the beams to dry" },
            new() { I(new Herb()), I(new Herb()) }, new[] { "dry", "rustling", "bitter" })
            { Senses = SensoryProfile.Fragrant, VerbModiMentis = Teach(("examine", "herblore"), ("smell", "apothecary_nose")) });

        return shop;
    }

    // ── Barber-surgeon ───────────────────────────────────────────────────────

    public static Area BuildBarbershop()
    {
        var shop = new BarbershopArea("Barber's Shop", "in the barber's shop", "step into the barber's shop",
            new() { "A bright shop with a striped pole outside, a chair in the middle and a basin of grey water" },
            new[] { "talkative", "bright", "soapy", "crowded" });

        shop.PointsOfInterest.Add(new ChairPointOfInterest("Barber's Chair",
            new() { "A high wooden chair with a headrest, its arms worn pale by gripping hands" },
            new() { I(new Razor()) }, new[] { "high", "worn", "ominous" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "tooth_drawing"), ("contemplate", "dread")) });

        shop.PointsOfInterest.Add(new BasinPointOfInterest("Bleeding Basin",
            new() { "A pewter basin and a bowl notched for the arm, a lancet laid ready beside them" },
            new() { I(new Lancet()), I(new BleedingBowl()) }, new[] { "pewter", "stained", "ready" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "surgery")) });

        shop.PointsOfInterest.Add(new ShelfPointOfInterest("Shaving Shelf",
            new() { "A shelf of soaps, strops and a jar of drawn teeth kept as proof of the trade" },
            new() { I(new Soap()), I(new Bone()) }, new[] { "soapy", "grisly", "proud" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "diagnosis"), ("smell", "keen_nose")) });

        return shop;
    }

    // ── Wash house ───────────────────────────────────────────────────────────

    public static Area BuildWashHouse()
    {
        var wash = new LaundryArea("Wash House", "in the wash house", "step into the wash house",
            new() { "A steaming stone hall of tubs and coppers, linen hung on lines overhead to dry" },
            new[] { "steaming", "wet", "loud", "lye-sharp" });

        wash.PointsOfInterest.Add(new TubPointOfInterest("Wash Tubs",
            new() { "A row of wooden tubs of grey soapy water, linen soaking in each" },
            new() { I(new Lye()), I(new Soap()) }, new[] { "steaming", "soapy", "grey" })
            { Senses = SensoryProfile.Odorous, VerbModiMentis = Teach(("examine", "laundering"), ("smell", "taint_sense")) });

        wash.PointsOfInterest.Add(new BlockPointOfInterest("Beating Stone",
            new() { "A flat stone slab by the drain where linen is beaten clean with wooden bats" },
            new() { I(new WashBeetle()) }, new[] { "wet", "slapping", "worn" })
            { Senses = SensoryProfile.Audible, VerbModiMentis = Teach(("examine", "laundering"), ("listen", "gossip")) });

        wash.PointsOfInterest.Add(new RackPointOfInterest("Drying Lines",
            new() { "Lines of sheets and shirts strung under the roof, dripping onto the flags" },
            new() { I(new LinenTunic()) }, new[] { "dripping", "white", "crowded" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "wear_reading"), ("contemplate", "laundering")) });

        return wash;
    }
}
