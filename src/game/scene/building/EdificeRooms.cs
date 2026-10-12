using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// Names, types and furnishes the rooms of a great building (<see cref="EdificeFactory"/>).
///
/// <para>The default furniture of every role lives here, so the forty rooms of a castle need no
/// authoring beyond what is particular to a castle: its factory furnishes the armoury and the throne
/// room through <see cref="EdificeSpec.Furnish"/> and leaves the kitchens, dormitories, bathhouse and
/// passages to these. Every piece carries items, senses and lessons, as everything observable must.</para>
/// </summary>
public static class EdificeRooms
{
    private static ItemElement I(Item item) => new(item);

    private static Dictionary<string, string> L(params (string Verb, string Mm)[] pairs)
        => pairs.ToDictionary(p => p.Verb, p => p.Mm);

    /// <summary>"" for the first of a role, then "Second", "Third"... — the room-name qualifier.</summary>
    public static string Ordinal(int n) => n switch
    {
        1 => "", 2 => "Second", 3 => "Third", 4 => "Fourth", 5 => "Fifth", 6 => "Sixth",
        7 => "Seventh", 8 => "Eighth", 9 => "Ninth", 10 => "Tenth", _ => $"{n}th",
    };

    public static string Word(EdificeRoom role) => role switch
    {
        EdificeRoom.Entrance      => "Entrance Hall",
        EdificeRoom.Corridor      => "Passage",
        EdificeRoom.GreatHall     => "Great Hall",
        EdificeRoom.Refectory     => "Refectory",
        EdificeRoom.Kitchen       => "Kitchen",
        EdificeRoom.Larder        => "Larder",
        EdificeRoom.Scullery      => "Scullery",
        EdificeRoom.Bathhouse     => "Bathhouse",
        EdificeRoom.Latrine       => "Garderobe",
        EdificeRoom.Laundry       => "Laundry",
        EdificeRoom.Dormitory     => "Dormitory",
        EdificeRoom.Cell          => "Cell",
        EdificeRoom.Chamber       => "Chamber",
        EdificeRoom.Solar         => "Solar",
        EdificeRoom.Chapel        => "Chapel",
        EdificeRoom.Crypt         => "Crypt",
        EdificeRoom.Library       => "Library",
        EdificeRoom.Scriptorium   => "Scriptorium",
        EdificeRoom.Archive       => "Archive",
        EdificeRoom.Classroom     => "Schoolroom",
        EdificeRoom.Lecture       => "Lecture Hall",
        EdificeRoom.Armoury       => "Armoury",
        EdificeRoom.Guardroom     => "Guardroom",
        EdificeRoom.Dungeon       => "Dungeon",
        EdificeRoom.Treasury      => "Treasury",
        EdificeRoom.Countinghouse => "Counting Room",
        EdificeRoom.Chancery      => "Chancery",
        EdificeRoom.Infirmary     => "Infirmary",
        EdificeRoom.Cellar        => "Cellar",
        EdificeRoom.Store         => "Storeroom",
        EdificeRoom.Brewhouse     => "Brewhouse",
        EdificeRoom.Wardrobe      => "Wardrobe",
        EdificeRoom.Workshop      => "Workroom",
        EdificeRoom.Observatory   => "Observatory",
        _                         => "Battlements",
    };

    /// <summary>
    /// An empty room of <paramref name="role"/>, typed and named. A passage takes its storey's name
    /// (<paramref name="qualifier"/>) and the spec's passage word; any other room takes the spec's
    /// prefix and an ordinal for its second and later instances.
    /// </summary>
    public static Area Room(EdificeRoom role, EdificeSpec spec, string qualifier, BuildingMaterial mat)
    {
        string name = role switch
        {
            EdificeRoom.Entrance    => $"{spec.Prefix} Entrance Hall",
            EdificeRoom.Corridor    => $"{spec.Prefix} {qualifier} {spec.PassageWord}",
            EdificeRoom.Battlements => $"{spec.Prefix} Battlements",
            _ => qualifier.Length == 0 ? $"{spec.Prefix} {Word(role)}" : $"{spec.Prefix} {qualifier} {Word(role)}",
        };
        string lower = name.ToLowerInvariant();
        var desc = new List<string> { Description(role, BuildingDescriptions.MaterialWord(mat), spec.FunctionNoun) };
        string context = $"in the {lower}", transition = $"go into the {lower}";
        var moods = Moods(role);

        return role switch
        {
            EdificeRoom.Entrance      => new GatehouseArea(name, context, transition, desc, moods),
            EdificeRoom.Corridor      => new CorridorArea(name, context, transition, desc, moods),
            EdificeRoom.GreatHall     => new HallArea(name, context, transition, desc, moods),
            EdificeRoom.Refectory     => new RefectoryArea(name, context, transition, desc, moods),
            EdificeRoom.Kitchen       => new KitchenArea(name, context, transition, desc, moods),
            EdificeRoom.Larder        => new PantryArea(name, context, transition, desc, moods),
            EdificeRoom.Scullery      => new SculleryArea(name, context, transition, desc, moods),
            EdificeRoom.Bathhouse     => new BathhouseArea(name, context, transition, desc, moods),
            EdificeRoom.Latrine       => new LatrineArea(name, context, transition, desc, moods),
            EdificeRoom.Laundry       => new LaundryArea(name, context, transition, desc, moods),
            EdificeRoom.Dormitory     => new DormitoryArea(name, context, transition, desc, moods),
            EdificeRoom.Cell          => new CellArea(name, context, transition, desc, moods),
            EdificeRoom.Chamber       => new ChamberArea(name, context, transition, desc, moods),
            EdificeRoom.Solar         => new SolarArea(name, context, transition, desc, moods),
            EdificeRoom.Chapel        => new ChapelArea(name, context, transition, desc, moods),
            EdificeRoom.Crypt         => new CryptArea(name, context, transition, desc, moods),
            EdificeRoom.Library       => new LibraryArea(name, context, transition, desc, moods),
            EdificeRoom.Scriptorium   => new ScriptoriumArea(name, context, transition, desc, moods),
            EdificeRoom.Archive       => new ArchiveArea(name, context, transition, desc, moods),
            EdificeRoom.Classroom     => new ClassroomArea(name, context, transition, desc, moods),
            EdificeRoom.Lecture       => new LectureArea(name, context, transition, desc, moods),
            EdificeRoom.Armoury       => new ArmouryArea(name, context, transition, desc, moods),
            EdificeRoom.Guardroom     => new GuardroomArea(name, context, transition, desc, moods),
            EdificeRoom.Dungeon       => new DungeonArea(name, context, transition, desc, moods),
            EdificeRoom.Treasury      => new TreasuryArea(name, context, transition, desc, moods),
            EdificeRoom.Countinghouse => new CountinghouseArea(name, context, transition, desc, moods),
            EdificeRoom.Chancery      => new ChanceryArea(name, context, transition, desc, moods),
            EdificeRoom.Infirmary     => new InfirmaryArea(name, context, transition, desc, moods),
            EdificeRoom.Cellar        => new CellarArea(name, context, transition, desc, moods),
            EdificeRoom.Store         => new StoreArea(name, context, transition, desc, moods),
            EdificeRoom.Brewhouse     => new BrewhouseArea(name, context, transition, desc, moods),
            EdificeRoom.Wardrobe      => new WardrobeArea(name, context, transition, desc, moods),
            EdificeRoom.Workshop      => new WorkshopArea(name, context, transition, desc, moods),
            EdificeRoom.Observatory   => new ObservatoryArea(name, context, transition, desc, moods),
            _                         => new BattlementsArea(name, $"up on the {lower}", "climb out onto the battlements", desc, moods),
        };
    }

    private static string Description(EdificeRoom role, string mat, string what) => role switch
    {
        EdificeRoom.Entrance      => $"A high {mat} hall inside the great door of the {what}, where visitors wait and are looked over",
        EdificeRoom.Corridor      => $"A long {mat} passage lit by narrow windows, doors opening off it at intervals",
        EdificeRoom.GreatHall     => $"A vast {mat} hall under a roof of blackened beams, a dais at one end and long tables below",
        EdificeRoom.Refectory     => "A long refectory of scrubbed tables and benches, a reading desk raised at one end",
        EdificeRoom.Kitchen       => "A great kitchen with two hearths, its walls hung with pots and the air thick with steam",
        EdificeRoom.Larder        => "A cold larder of hooks and slate shelves, smelling of salt and onions",
        EdificeRoom.Scullery      => "A wet scullery of stone sinks and draining boards, the floor always awash",
        EdificeRoom.Bathhouse     => "A steamy bathhouse of tiled tubs and a copper boiler, benches along the walls",
        EdificeRoom.Latrine       => "A narrow garderobe built out over the wall, a draught always coming up through the seat",
        EdificeRoom.Laundry       => "A laundry of tubs and wringers and lines of linen hung to dry overhead",
        EdificeRoom.Dormitory     => "A long dormitory of beds in two rows, a chest at the foot of each",
        EdificeRoom.Cell          => "A bare cell barely longer than the bed in it, a crucifix-nail over the door",
        EdificeRoom.Chamber       => $"A private chamber with a curtained bed, a window seat and a {mat} hearth",
        EdificeRoom.Solar         => "A bright upper room of glazed windows where the household's betters sit and talk",
        EdificeRoom.Chapel        => "A small vaulted chapel, an altar at the east end under a painted window",
        EdificeRoom.Crypt         => "A low vaulted crypt of squat pillars, the dead laid in niches along the walls",
        EdificeRoom.Library       => "A library of tall presses crammed with books, chained to their shelves",
        EdificeRoom.Scriptorium   => "A scriptorium of sloped desks under the windows, the smell of ink and size in the air",
        EdificeRoom.Archive       => "A muniment room of chests and pigeonholes stuffed with rolled charters",
        EdificeRoom.Classroom     => "A schoolroom of benches facing a master's chair and a board scrawled in chalk",
        EdificeRoom.Lecture       => "A tiered lecture hall, benches climbing steeply away from a lectern",
        EdificeRoom.Armoury       => "An armoury of racked spears and hung mail, a grindstone in the corner",
        EdificeRoom.Guardroom     => "A guardroom of a table, stools and a brazier, where the watch waits out its hours",
        EdificeRoom.Dungeon       => "A dungeon of iron-gated cells below ground, the straw rotten and the walls wet",
        EdificeRoom.Treasury      => "A windowless treasury behind an iron-bound door, strongboxes lined against the walls",
        EdificeRoom.Countinghouse => "A counting room of a long table chequered for reckoning, ledgers stacked at its end",
        EdificeRoom.Chancery      => "A chancery where letters are drafted and sealed, the clerks' desks crowded together",
        EdificeRoom.Infirmary     => "An infirmary of beds and herb-smelling cupboards, kept clean and quiet",
        EdificeRoom.Cellar        => "A barrel-vaulted cellar of casks and racks, the air cool and sour",
        EdificeRoom.Store         => "A storeroom of crates, sacks and spare everything, stacked to the ceiling",
        EdificeRoom.Brewhouse     => "A brewhouse of mash tuns and cooling trays, the floor sticky with spilt wort",
        EdificeRoom.Wardrobe      => "A wardrobe room of presses and pegs, the household's linen and livery kept in order",
        EdificeRoom.Workshop      => "A workroom of benches and tools where the building's own repairs are done",
        EdificeRoom.Observatory   => "A round room at the top of a tower, its roof open to the sky through a shuttered slot",
        _                         => $"The wall-walk at the top of the {what}, crenels on every side and the country spread below",
    };

    private static string[] Moods(EdificeRoom role) => role switch
    {
        EdificeRoom.Entrance or EdificeRoom.GreatHall => new[] { "high", "echoing", "cold", "imposing", "draughty" },
        EdificeRoom.Corridor     => new[] { "long", "dim", "echoing", "draughty", "quiet" },
        EdificeRoom.Kitchen or EdificeRoom.Brewhouse or EdificeRoom.Bathhouse => new[] { "steamy", "hot", "loud", "busy", "close" },
        EdificeRoom.Chapel or EdificeRoom.Crypt => new[] { "hushed", "cold", "candlelit", "solemn", "still" },
        EdificeRoom.Library or EdificeRoom.Scriptorium or EdificeRoom.Archive => new[] { "hushed", "dusty", "studious", "dim" },
        EdificeRoom.Dungeon      => new[] { "wet", "foul", "dark", "despairing", "cold" },
        EdificeRoom.Battlements  => new[] { "windy", "high", "exposed", "vast" },
        _                        => new[] { "plain", "quiet", "orderly", "cool", "dim" },
    };

    // ── Default furniture ─────────────────────────────────────────────────────

    /// <summary>The default furniture of a room of <paramref name="role"/>: two or three pieces, rolled.</summary>
    public static void Populate(EdificeRoom role, Area room, BuildingMaterial mat, Random rng)
    {
        var pois = room.PointsOfInterest;
        switch (role)
        {
            case EdificeRoom.Entrance:
                pois.Add(Banner(rng));
                pois.Add(new BenchPointOfInterest("Waiting Bench", new() { "A long bench against the wall for those kept waiting" },
                    moods: new[] { "worn", "hard", "long" }) { Senses = SensoryProfile.Examinable, VerbModiMentis = L(("examine", "whittlecraft")) });
                break;
            case EdificeRoom.Corridor:
                if (rng.NextDouble() < 0.6) pois.Add(Brazier());
                if (rng.NextDouble() < 0.5) pois.Add(Tapestry(rng));
                break;
            case EdificeRoom.GreatHall:
            case EdificeRoom.Refectory:
                pois.Add(new TablePointOfInterest("Long Table", new() { "A table long enough to seat forty, scored by generations of knives" },
                    new() { I(new Bread()), I(new WoodenBowl()), I(new Mug()) }, new[] { "long", "scored", "communal" })
                    { Senses = SensoryProfile.Fragrant, VerbModiMentis = L(("examine", "hospitality"), ("smell", "bouquet"), ("contemplate", "gregariousness")) });
                pois.Add(new LecternPointOfInterest("Reading Desk", new() { "A raised desk from which something is read aloud during meals" },
                    new() { I(new Tome()) }, new[] { "raised", "solemn", "worn" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = L(("examine", "decipher"), ("contemplate", "piety")) });
                break;
            case EdificeRoom.Kitchen:
                pois.Add(new HearthPointOfInterest("Great Hearth", new() { "A hearth big enough to roast an ox, its spit-dogs blackened" },
                    new() { I(new Meat()), I(new Lard()) }, new[] { "roaring", "black", "huge" })
                    { Senses = SensoryProfile.FullyAlive, VerbModiMentis = L(("examine", "firecraft"), ("smell", "bouquet"), ("listen", "forge_ear")) });
                pois.Add(new TablePointOfInterest("Kitchen Board", new() { "A thick board scattered with peelings, knives and a half-plucked bird" },
                    new() { I(new Onion()), I(new Knife()), I(new Herb()) }, new[] { "busy", "scarred", "wet" })
                    { Senses = SensoryProfile.Odorous, VerbModiMentis = L(("examine", "butchery"), ("smell", "taint_sense")) });
                break;
            case EdificeRoom.Larder:
            case EdificeRoom.Cellar:
                pois.Add(new BarrelPointOfInterest(role == EdificeRoom.Cellar ? "Wine Casks" : "Salt Barrels",
                    new() { role == EdificeRoom.Cellar ? "Casks racked three high, chalked with dates and vintages" : "Barrels of salted meat and fish, lids weighted with stones" },
                    role == EdificeRoom.Cellar ? new() { I(new Wine()), I(new Ale()) } : new() { I(new DriedMeat()), I(new Salt()), I(new Herring()) },
                    new[] { "racked", "cool", "sour" })
                    { Senses = SensoryProfile.Odorous, VerbModiMentis = L(("examine", "cellarcraft"), ("smell", "bouquet")) });
                pois.Add(new ShelfPointOfInterest("Slate Shelves", new() { "Slate shelves of cheeses, crocks and sealed jars" },
                    new() { I(new Cheese()), I(new Butter()), I(new ClayPot()) }, new[] { "cold", "ordered", "laden" })
                    { Senses = SensoryProfile.Odorous, VerbModiMentis = L(("examine", "thrift"), ("smell", "taint_sense")) });
                break;
            case EdificeRoom.Scullery:
            case EdificeRoom.Laundry:
                pois.Add(new BasinPointOfInterest(role == EdificeRoom.Laundry ? "Wash Tubs" : "Stone Sinks",
                    new() { "Grey water standing in stone basins, a scum of fat and soap on it" },
                    new() { I(new Soap()), I(new Linen()) }, new[] { "wet", "grey", "steaming" })
                    { Senses = SensoryProfile.Odorous, VerbModiMentis = L(("examine", "dirty_labor"), ("smell", "taint_sense")) });
                break;
            case EdificeRoom.Bathhouse:
                pois.Add(new BathtubPointOfInterest("Bathing Tubs", new() { "Wooden tubs lined with linen, a copper of hot water steaming beside them" },
                    new() { I(new Soap()), I(new Linen()) }, new[] { "steaming", "warm", "tiled" })
                    { Senses = SensoryProfile.FullyAlive, VerbModiMentis = L(("examine", "grooming"), ("listen", "water_voice"), ("smell", "perfumery")) });
                break;
            case EdificeRoom.Latrine:
                pois.Add(new PotPointOfInterest("Garderobe Seat", new() { "A plank seat over a shaft that drops straight down the outer wall" },
                    moods: new[] { "draughty", "foul", "plain" }) { Senses = SensoryProfile.Odorous, VerbModiMentis = L(("examine", "drainage"), ("smell", "taint_sense")) });
                break;
            case EdificeRoom.Chamber:
            case EdificeRoom.Solar:
                pois.Add(new ChestPointOfInterest("Linen Press", new() { "A carved press of folded linen and good clothes, lavender between the sheets" },
                    new() { I(new LinenTunic()), I(new WoolCloak()) }, new[] { "carved", "fragrant", "full" })
                    { Senses = SensoryProfile.Fragrant, VerbModiMentis = L(("examine", "threadwork"), ("smell", "perfumery"), ("contemplate", "vanitas")) });
                pois.Add(Tapestry(rng));
                break;
            case EdificeRoom.Chapel:
                pois.Add(new AltarPointOfInterest("Altar", new() { "A stone altar under a fair cloth, candles burning down at either end" },
                    new() { I(new Candle()), I(new Incense()) }, new[] { "hushed", "candlelit", "holy" })
                    { Senses = SensoryProfile.FullyAlive, VerbModiMentis = L(("examine", "iconography"), ("listen", "hearkening"), ("smell", "perfumery"), ("contemplate", "piety")) });
                break;
            case EdificeRoom.Crypt:
                pois.Add(new TombPointOfInterest("Effigy Tomb", new() { "A stone tomb with a knight lying on its lid, hands folded and nose broken" },
                    new() { I(new Bone()) }, new[] { "cold", "solemn", "worn" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = L(("examine", "lineage_lore"), ("contemplate", "elegy")) });
                pois.Add(new OssuaryPointOfInterest("Bone Niches", new() { "Skulls and long bones stacked neatly in niches cut into the wall" },
                    new() { I(new Skull()), I(new Bone()) }, new[] { "stacked", "dry", "patient" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = L(("examine", "gravesight"), ("contemplate", "vanitas")) });
                break;
            case EdificeRoom.Library:
            case EdificeRoom.Archive:
                pois.Add(new BookcasePointOfInterest(role == EdificeRoom.Library ? "Book Presses" : "Charter Chests",
                    new() { role == EdificeRoom.Library ? "Tall presses of chained books, their spines lettered in faded red" : "Iron-bound chests of rolled charters, each tagged with a seal" },
                    new() { I(new Tome()), I(new Parchment()) }, new[] { "dusty", "crammed", "quiet" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = L(("examine", "scholarship"), ("contemplate", "philosophy")) });
                pois.Add(new DeskPointOfInterest("Reading Desk", new() { "A sloped desk with a book open on it and a candle-stub at its edge" },
                    new() { I(new Candle()), I(new WaxTablet()) }, new[] { "sloped", "quiet", "worn" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = L(("examine", "decipher"), ("contemplate", "introspection")) });
                break;
            case EdificeRoom.Scriptorium:
            case EdificeRoom.Chancery:
            case EdificeRoom.Countinghouse:
                pois.Add(new DeskPointOfInterest(role == EdificeRoom.Countinghouse ? "Reckoning Table" : "Copying Desks",
                    new() { role == EdificeRoom.Countinghouse ? "A table chequered black and white for counting, jetons stacked at its edge" : "Sloped desks in a row, each with its inkhorn and its half-finished page" },
                    new() { I(new Quill()), I(new InkHorn()), I(new Parchment()) }, new[] { "orderly", "ink-stained", "quiet" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = L(("examine", role == EdificeRoom.Countinghouse ? "tallycraft" : "prosaic_grammar"), ("contemplate", "diligence")) });
                pois.Add(new LedgerPointOfInterest("Ledgers", new() { "A stack of bound ledgers, the topmost open on columns of figures" },
                    new() { I(new Tome()) }, new[] { "stacked", "dense", "precise" })
                    { Senses = SensoryProfile.Examinable, VerbModiMentis = L(("examine", "arithmetic_logic")) });
                break;
            case EdificeRoom.Classroom:
            case EdificeRoom.Lecture:
                pois.Add(new ChalkboardPointOfInterest("Master's Board", new() { "A board of blackened slate scrawled with figures and half-wiped declensions" },
                    new() { I(new Chalk()) }, new[] { "scrawled", "dusty", "patient" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = L(("examine", "algebraic_analysis"), ("contemplate", "scholarship")) });
                pois.Add(new BenchPointOfInterest("Scholars' Benches", new() { "Benches carved with the initials of every pupil who ever sat on them" },
                    new() { I(new WaxTablet()) }, new[] { "carved", "worn", "rowed" })
                    { Senses = SensoryProfile.Examinable, VerbModiMentis = L(("examine", "recollection")) });
                break;
            case EdificeRoom.Armoury:
                pois.Add(new WeaponrackPointOfInterest("Spear Rack", new() { "Spears racked upright, their heads greased against rust" },
                    new() { I(new WarSpear()), I(new HuntingSpear()) }, new[] { "bristling", "greased", "ordered" })
                    { Senses = SensoryProfile.Examinable, VerbModiMentis = L(("examine", "battlecraft")) });
                pois.Add(new ArmourstandPointOfInterest("Mail Pegs", new() { "Shirts of mail and padded coats hung on pegs like a row of headless men" },
                    new() { I(new PaddedGambeson()), I(new IronKettleHelm()), I(new RoundShield()) }, new[] { "hung", "heavy", "silent" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = L(("examine", "metalcraft"), ("contemplate", "dread")) });
                break;
            case EdificeRoom.Guardroom:
                pois.Add(Brazier());
                pois.Add(new TablePointOfInterest("Dicing Table", new() { "A table ringed with mug-stains, dice and a few coins left on it" },
                    new() { I(new BoneDice()), I(new Mug()) }, new[] { "stained", "rowdy", "idle" })
                    { Senses = SensoryProfile.Audible, VerbModiMentis = L(("examine", "gambling"), ("listen", "banter")) });
                break;
            case EdificeRoom.Dungeon:
                pois.Add(new ChainPointOfInterest("Wall Chains", new() { "Chains and cuffs bolted into the wall at the height of a sitting man" },
                    new() { I(new Manacles()) }, new[] { "rusted", "cold", "cruel" })
                    { Senses = SensoryProfile.Audible, VerbModiMentis = L(("examine", "severity"), ("listen", "dread")) });
                pois.Add(new CagePointOfInterest("Iron Grille", new() { "An iron grille closing off a cell, its bars worn bright at shoulder height" },
                    moods: new[] { "barred", "dark", "foul" }) { Senses = SensoryProfile.Odorous, VerbModiMentis = L(("examine", "lockpicking"), ("smell", "carrion_sense")) });
                break;
            case EdificeRoom.Treasury:
                pois.Add(new StrongboxPointOfInterest("Strongboxes", new() { "Iron-banded strongboxes chained to rings in the floor, each with three locks" },
                    new() { I(new CoinPurse()), I(new Parchment()) }, new[] { "heavy", "locked", "guarded" })
                    { Senses = SensoryProfile.Examinable, VerbModiMentis = L(("examine", "coin_eye")) });
                break;
            case EdificeRoom.Infirmary:
                pois.Add(new CotPointOfInterest("Sick Beds", new() { "Narrow beds in a row, the linen boiled and the blankets rough" },
                    new() { I(new Bandage()), I(new Linen()) }, new[] { "clean", "quiet", "sad" })
                    { Senses = SensoryProfile.Odorous, VerbModiMentis = L(("examine", "midwifery"), ("smell", "apothecary_nose")) });
                pois.Add(new ShelfPointOfInterest("Simples Cupboard", new() { "A cupboard of labelled jars: dried herbs, salves, and a poultice wrapped ready" },
                    new() { I(new Poultice()), I(new Chamomile()), I(new Valerian()) }, new[] { "labelled", "fragrant", "orderly" })
                    { Senses = SensoryProfile.Fragrant, VerbModiMentis = L(("examine", "herblore"), ("smell", "apothecary_nose"), ("contemplate", "mercy")) });
                break;
            case EdificeRoom.Brewhouse:
                pois.Add(new VatPointOfInterest("Mash Tun", new() { "A great tun of steaming mash, a paddle across its rim" },
                    new() { I(new Grain()), I(new Ale()) }, new[] { "steaming", "sweet", "huge" })
                    { Senses = SensoryProfile.Odorous, VerbModiMentis = L(("examine", "brewcraft"), ("smell", "bouquet")) });
                break;
            case EdificeRoom.Wardrobe:
                pois.Add(new PegPointOfInterest("Livery Pegs", new() { "Rows of the household's livery hung on pegs, brushed and mended" },
                    new() { I(new TownsmanCloak()), I(new TownsmanTunic()), I(new Narrative.World.Items.Thread()) }, new[] { "ordered", "brushed", "colourful" })
                    { Senses = SensoryProfile.Examinable, VerbModiMentis = L(("examine", "threadwork")) });
                break;
            case EdificeRoom.Workshop:
                pois.Add(new WorkbenchPointOfInterest("Repair Bench", new() { "A bench of tools for mending everything from hinges to harness" },
                    new() { I(new Hammer()), I(new Nail()), I(new Rope()) }, new[] { "cluttered", "useful", "worn" })
                    { Senses = SensoryProfile.Examinable, VerbModiMentis = L(("examine", "whittlecraft")) });
                break;
            case EdificeRoom.Observatory:
                pois.Add(new TelescopePointOfInterest("Sighting Tube", new() { "A long tube of hammered brass on a pivot, aimed at the slot in the roof" },
                    moods: new[] { "brass", "precise", "strange" }) { Senses = SensoryProfile.Beautiful, VerbModiMentis = L(("examine", "sky_reading"), ("contemplate", "awe")) });
                pois.Add(new OrreryPointOfInterest("Orrery", new() { "A clockwork of brass rings and spheres, the world's moons riding wires around it" },
                    moods: new[] { "brass", "intricate", "still" }) { Senses = SensoryProfile.Beautiful, VerbModiMentis = L(("examine", "geometric_scheme"), ("contemplate", "philosophy")) });
                break;
            case EdificeRoom.Battlements:
                pois.Add(new BattlementPointOfInterest("Crenellations", new() { "Merlons and embrasures along the wall-walk, each embrasure a frame of distant country" },
                    new() { I(new Rock()) }, new[] { "windy", "notched", "high" })
                    { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true), VerbModiMentis = L(("examine", "tactics"), ("listen", "wind_reading"), ("contemplate", "vantage")) });
                break;
            default: // Store, and anything else
                pois.Add(new CratePointOfInterest("Stacked Crates", new() { "Crates and sacks stacked to the ceiling, chalked with what is in them" },
                    new() { I(new Rope()), I(new Sack()), I(new Candle()) }, new[] { "stacked", "dusty", "full" })
                    { Senses = SensoryProfile.Examinable, VerbModiMentis = L(("examine", "thrift")) });
                break;
        }
    }

    /// <summary>Beds and chests for a sleeping room: one of each per sleeper, so the roster pairs exactly.</summary>
    public static void PopulateSleeping(Area room, int beds, Random rng)
    {
        for (int i = 0; i < Math.Max(1, beds); i++)
        {
            string where = beds > 1 ? $" ({Position(i)})" : "";
            room.PointsOfInterest.Add(new PalletPointOfInterest($"Bed{where}",
                new() { "A plank bed with a straw mattress and a coarse blanket — a sleeping place" },
                new() { new ItemElement(new Straw()) }, new[] { "plain", "narrow", "quiet" })
                { Senses = SensoryProfile.Fragrant, VerbModiMentis = L(("examine", "peasantry"), ("smell", "taint_sense"), ("contemplate", "introspection")) });
            room.PointsOfInterest.Add(new ChestPointOfInterest($"Foot Chest{where}",
                new() { "A small chest at the bed's foot, holding everything its sleeper owns" },
                rng.NextDouble() < 0.5 ? new() { new ItemElement(new LinenTunic()) } : new() { new ItemElement(new Candle()) },
                new[] { "small", "battered", "closed" })
                { Senses = SensoryProfile.Examinable, VerbModiMentis = L(("examine", "thrift")) });
        }
    }

    private static string Position(int i) => i switch
    {
        0 => "by the door", 1 => "by the window", 2 => "against the wall", 3 => "in the corner",
        4 => "under the beam", 5 => "at the far end", _ => $"no. {i + 1}",
    };

    private static PointOfInterest Brazier() => new BrazierPointOfInterest("Brazier",
        new() { "An iron brazier on legs, coals glowing in it and a smell of hot metal" },
        new() { new ItemElement(new Coal()) }, new[] { "glowing", "warm", "smoky" })
        { Senses = SensoryProfile.FullyAlive, VerbModiMentis = L(("examine", "firecraft"), ("listen", "forge_ear"), ("smell", "smoke_reading"), ("contemplate", "hearthlonging")) };

    private static PointOfInterest Banner(Random rng) => new BannerPointOfInterest("Hanging Banner",
        new() { rng.NextDouble() < 0.5 ? "A long banner hung from the beams, its device faded by smoke" : "A tattered banner taken in some old war, hung high as a trophy" },
        moods: new[] { "faded", "proud", "dusty" })
        { Senses = SensoryProfile.Beautiful, VerbModiMentis = L(("examine", "heraldry"), ("contemplate", "pride")) };

    private static PointOfInterest Tapestry(Random rng) => new TapestryPointOfInterest("Tapestry",
        new() { rng.NextDouble() < 0.5 ? "A tapestry of a hunt in a blue wood, moth-eaten at the hem" : "A tapestry of a siege, the colours gone soft with age" },
        moods: new[] { "faded", "woven", "storied" })
        { Senses = SensoryProfile.Beautiful, VerbModiMentis = L(("examine", "threadwork"), ("contemplate", "aesthetic")) };
}
