using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// Builds and furnishes the individual rooms of a building.
///
/// <para>Every entry point dispatches on <see cref="RoomRole"/>, never on a display name. Room names
/// are qualified per building ("Forge Kitchen", "Longhouse Kitchen") so that the narration graph can
/// tell two buildings' rooms apart, which means the old name-matching furniture switch would have
/// stopped matching anything the moment a second building existed.</para>
/// </summary>
public static class BuildingRooms
{
    // ── Room construction ─────────────────────────────────────────────────────

    /// <summary>
    /// Builds an empty room for <paramref name="role"/>, named with the building's prefix.
    /// <paramref name="ordinal"/> distinguishes repeated rooms of one role (second bedroom, third
    /// dormitory) and must keep names unique inside the building.
    /// </summary>
    public static Area Build(RoomRole role, string prefix, BuildingKind kind, BuildingMaterial mat, int ordinal)
    {
        var matWord = BuildingDescriptions.MaterialWord(mat);
        var word    = RoleWord(role);
        var name    = ordinal <= 1 ? $"{prefix} {word}" : $"{prefix} {OrdinalWord(ordinal)} {word}";
        // The building-qualified name, not the bare role word: "in the forge bedroom" tells the LLM
        // which building it is in, where "in the bedroom" would read the same in every house.
        var lower   = name.ToLowerInvariant();

        return NewRoom(role,
            displayName:           name,
            contextDescription:    $"in the {lower}",
            transitionDescription: $"step into the {lower}",
            descriptions:          new() { RoleDescription(role, kind, matWord) },
            moods:                 RoleMoods(role)
        );
    }

    private static string RoleWord(RoomRole role) => role switch
    {
        RoomRole.PublicHall => "Hall",
        RoomRole.Bedroom    => "Bedroom",
        RoomRole.Dormitory  => "Dormitory",
        RoomRole.Kitchen    => "Kitchen",
        RoomRole.Pantry     => "Pantry",
        RoomRole.Store      => "Store",
        _                   => "Landing",
    };

    /// <summary>
    /// The typed room for a role. Replaced a <c>RoleLemma</c> switch that returned a bare string:
    /// the role enum was already a closed vocabulary and the lemma threw that away, so a lesson
    /// asking about a kitchen had to match the word rather than the room.
    /// </summary>
    private static Area NewRoom(RoomRole role, string displayName, string contextDescription,
                                string transitionDescription, List<string> descriptions,
                                string[]? moods = null, bool isPrivate = false) => role switch
    {
        RoomRole.PublicHall => new HallArea(displayName, contextDescription, transitionDescription, descriptions, moods, isPrivate),
        RoomRole.Bedroom    => new BedroomArea(displayName, contextDescription, transitionDescription, descriptions, moods, isPrivate),
        RoomRole.Dormitory  => new DormitoryArea(displayName, contextDescription, transitionDescription, descriptions, moods, isPrivate),
        RoomRole.Kitchen    => new KitchenArea(displayName, contextDescription, transitionDescription, descriptions, moods, isPrivate),
        RoomRole.Pantry     => new PantryArea(displayName, contextDescription, transitionDescription, descriptions, moods, isPrivate),
        RoomRole.Store      => new StoreArea(displayName, contextDescription, transitionDescription, descriptions, moods, isPrivate),
        _                   => new LandingArea(displayName, contextDescription, transitionDescription, descriptions, moods, isPrivate),
    };

    private static string RoleDescription(RoomRole role, BuildingKind kind, string mat) => role switch
    {
        RoomRole.PublicHall when kind == BuildingKind.Longhouse
            => $"A long {mat} hall with a hearth down the middle and benches to either side, where the household eats and business is done",
        RoomRole.PublicHall
            => $"The front room of a {mat} building, kept for callers — a hearth, a bench, and space to stand and talk",
        RoomRole.Bedroom
            => "A sparse sleeping room with a straw pallet and a wooden chest",
        RoomRole.Dormitory
            => "A low room laid out with pallets along the wall, each with a chest at its foot",
        RoomRole.Kitchen
            => $"A cramped kitchen with a clay hearth, a roughhewn table, and shelves of crockery and hanging herbs",
        RoomRole.Pantry
            => "A cool storage room with shelves and barrels, smelling of grain, salt, and dried meat",
        RoomRole.Store
            => "A cluttered store room of stacked crates, tools and spare stock",
        _
            => "A narrow landing at the top of the stairs, planked floor creaking with every step",
    };

    private static string[] RoleMoods(RoomRole role) => role switch
    {
        RoomRole.PublicHall => new[] { "smoky", "low-ceilinged", "worn", "quiet", "dim", "heavy", "dusty" },
        RoomRole.Bedroom    => new[] { "sparse", "cold", "quiet", "small", "close", "still", "dark" },
        RoomRole.Dormitory  => new[] { "low", "shared", "dim", "close", "quiet", "stale" },
        RoomRole.Kitchen    => new[] { "warm", "cramped", "smoky", "fragrant", "cluttered", "low", "close" },
        RoomRole.Pantry     => new[] { "cool", "quiet", "dim", "dry", "musty", "orderly", "provisioned" },
        RoomRole.Store      => new[] { "cluttered", "dim", "dusty", "crowded", "cold" },
        _                   => new[] { "narrow", "creaking", "dim", "low", "quiet", "close", "dusky" },
    };

    private static string OrdinalWord(int n) => n switch
    {
        2 => "Second", 3 => "Third", 4 => "Fourth", 5 => "Fifth",
        6 => "Sixth", 7 => "Seventh", 8 => "Eighth", _ => $"{n}th",
    };

    // ── Furniture ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Fills a room with the furniture its role calls for: a required core plus a sample from an
    /// optional pool, so two kitchens in one village differ. <paramref name="bedCount"/> only applies
    /// to sleeping rooms and decides how many pallets go in.
    /// </summary>
    public static void Populate(Area room, RoomRole role, BuildingMaterial mat, Random rng, int bedCount = 1)
    {
        switch (role)
        {
            case RoomRole.PublicHall: PopulateHall(room, rng);                 break;
            case RoomRole.Kitchen:    PopulateKitchen(room, rng);              break;
            case RoomRole.Pantry:     PopulatePantry(room, rng);               break;
            case RoomRole.Store:      PopulateStore(room, rng);                break;
            case RoomRole.Bedroom:    PopulateSleeping(room, rng, 1);          break;
            case RoomRole.Dormitory:  PopulateSleeping(room, rng, bedCount);   break;
            case RoomRole.Landing:    /* corridor — deliberately bare */       break;
        }
    }

    /// <summary>
    /// The bed PoIs in a room, in insertion order. The caller pairs these with its NPC roster, so a
    /// dormitory with three pallets sleeps exactly three people.
    /// </summary>
    public static IEnumerable<PointOfInterest> BedsIn(Area room)
        => room.PointsOfInterest.OfType<PalletPointOfInterest>();

    private static void PopulateHall(Area area, Random rng)
    {
        area.PointsOfInterest.Add(BuildHearth());
        area.PointsOfInterest.Add(BuildTrestleTable());
        foreach (var poi in SampleOptional(rng, new List<Func<PointOfInterest>>
        {
            BuildCandleStand,
            BuildSpinningWheel,
            BuildRushMat,
            BuildBenchRow,
        }, 1))
            area.PointsOfInterest.Add(poi);
    }

    private static void PopulateKitchen(Area area, Random rng)
    {
        area.PointsOfInterest.Add(BuildCookingHearth());
        area.PointsOfInterest.Add(BuildKitchenShelf(rng));
        foreach (var poi in SampleOptional(rng, new List<Func<PointOfInterest>>
        {
            () => BuildHangingHerbs(rng),
            () => BuildSaltingBarrel(rng),
            BuildMortarAndPestle,
            () => BuildButcherBlock(rng),
        }, rng.Next(1, 3)))
            area.PointsOfInterest.Add(poi);
    }

    private static void PopulatePantry(Area area, Random rng)
    {
        area.PointsOfInterest.Add(BuildStorageBarrel(PickFromPool(rng, BarrelPool(), 1, 2)));
        area.PointsOfInterest.Add(BuildStorageShelf(PickFromPool(rng, ShelfPool(), 1, 2)));
        if (rng.NextDouble() < 0.40)
            area.PointsOfInterest.Add(BuildColdShelf(rng));
    }

    private static void PopulateStore(Area area, Random rng)
    {
        area.PointsOfInterest.Add(BuildToolCorner(rng));
        if (rng.NextDouble() < 0.55)
            area.PointsOfInterest.Add(BuildStorageBarrel(PickFromPool(rng, BarrelPool(), 1, 2)));
        if (rng.NextDouble() < 0.45)
            area.PointsOfInterest.Add(BuildFirewoodStack(rng));
        if (rng.NextDouble() < 0.60)
            area.PointsOfInterest.Add(BuildBreakableFurniture(rng));
    }

    /// <summary>
    /// A piece of furniture solid enough to be worth wrecking. Yields nothing to a thief and a
    /// handful of salvage to a hammer, which is the point: <c>BreakVerb</c> is vandalism, not theft,
    /// and what it leaves behind stays in the room for whoever walks in next.
    /// </summary>
    private static PointOfInterest BuildBreakableFurniture(Random rng)
    {
        var (name, lemma, whole, wrecked, salvage) = rng.Next(4) switch
        {
            0 => ("Storage Chest", "chest",
                  "A banded chest of thick boards, the lid warped just enough not to sit flat",
                  "The wreck of a chest, boards sprung off their bands and the lid in two pieces",
                  2),
            1 => ("Work Table", "table",
                  "A heavy table scarred all over its top by whatever has been done on it",
                  "A table with its legs broken out from under it and its top split along the grain",
                  3),
            2 => ("Shelving Rack", "rack",
                  "A rack of pegged shelves running the height of the wall, bowed under its own load",
                  "Shelving pulled off the wall in a heap, pegs snapped and boards across the floor",
                  2),
            _ => ("Grain Bin", "bin",
                  "A bin of close-fitted boards, tight enough to hold grain and keep the mice out",
                  "A staved-in bin with its contents run out across the floor",
                  1),
        };

        var broken = new PointOfInterest(
            $"Broken {name}", lemma, new List<string> { wrecked },
            items: Enumerable.Range(0, salvage)
                             .Select(_ => new ItemElement(new SplinteredTimber()))
                             .ToList(),
            moods: new[] { "splintered", "staved-in", "wrecked" })
        {
            Senses = SensoryProfile.Examinable,
        };

        return new BreakablePointOfInterest(
            name, lemma, new List<string> { whole }, broken,
            new[] { "solid", "scarred", "heavy", "close-fitted" },
            new Dictionary<string, string> { ["examine"] = "whittlecraft" });
    }

    /// <summary>
    /// Lays out <paramref name="beds"/> pallets, each with its own chest, then a shared extra or two.
    /// One pallet per sleeper is the rule the whole roster/bed pairing rests on — a room with fewer
    /// pallets than occupants is exactly the anonymous-dormitory problem this replaces.
    /// </summary>
    /// <summary>
    /// A cradle. Households of this period had them and this one did not, which left midwifery — a
    /// modus mentis the game has always shipped — keyed to furniture that was never built.
    /// </summary>
    private static PointOfInterest BuildCradle() => new CradlePointOfInterest(
        displayName: "Cradle",
        descriptions: new() { "A hollowed wooden cradle on rockers, a folded blanket inside it" },
        moods: new[] { "small", "worn", "quiet", "rocked-smooth" }
    ) { Senses = SensoryProfile.Beautiful,
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "midwifery" } };

    /// <summary>
    /// A psalter — the one book a household of this station plausibly owns, and the only written
    /// thing in the game. Philosophy and the lettered lessons had nothing to be read off.
    /// </summary>
    private static PointOfInterest BuildPsalter() => new PsalterPointOfInterest(
        displayName: "Psalter",
        descriptions: new() { "A small psalter on the chest, its boards scuffed and its clasp long gone" },
        moods: new[] { "worn", "thumbed", "precious", "dim" }
    ) { Senses = SensoryProfile.Beautiful,
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "decipher", ["contemplate"] = "piety" } };

    private static void PopulateSleeping(Area area, Random rng, int beds)
    {
        for (int i = 0; i < Math.Max(1, beds); i++)
        {
            area.PointsOfInterest.Add(BuildPallet(rng, i, beds));
            area.PointsOfInterest.Add(BuildChest(rng, i, beds));
        }

        foreach (var poi in SampleOptional(rng, new List<Func<PointOfInterest>>
        {
            BuildWashstand,
            BuildChamberPot,
            BuildPrayerStool,
            () => BuildClothesPegs(rng),
            BuildRushLight,
            BuildCradle,
            BuildPsalter,
        }, rng.Next(1, 3)))
            area.PointsOfInterest.Add(poi);
    }

    // ── Item pools ────────────────────────────────────────────────────────────

    private static List<Func<Item>> BarrelPool() => new()
    {
        () => new Grain(), () => new Grain(), () => new Turnip(), () => new Cabbage(),
        () => new DriedMeat(), () => new DriedPeas(), () => new Salt(),
    };

    private static List<Func<Item>> ShelfPool() => new()
    {
        () => new Hay(), () => new Bread(), () => new Cheese(), () => new Onion(),
        () => new Herb(), () => new Lard(), () => new ClayPot(),
    };

    private static List<Func<Item>> ChestPool() => new()
    {
        () => new LinenTunic(), () => new WoolCloak(), () => new LeatherBelt(), () => new WoolCap(),
        () => new Knife(), () => new Bread(), () => new LeatherBoots(), () => new WoolStockings(),
        () => new LeatherGloves(), () => new Flint(),
    };

    // ── Randomisation helpers ─────────────────────────────────────────────────

    private static List<PointOfInterest> SampleOptional(Random rng, List<Func<PointOfInterest>> pool, int count)
    {
        count = Math.Min(count, pool.Count);
        var indices = new List<int>(Enumerable.Range(0, pool.Count));
        var result  = new List<PointOfInterest>();
        for (int i = 0; i < count; i++)
        {
            int pick = rng.Next(indices.Count);
            result.Add(pool[indices[pick]]());
            indices.RemoveAt(pick);
        }
        return result;
    }

    private static ItemElement[] PickFromPool(Random rng, List<Func<Item>> pool, int min, int max)
    {
        int count   = Math.Min(rng.Next(min, max + 1), pool.Count);
        var indices = new List<int>(Enumerable.Range(0, pool.Count));
        var result  = new List<ItemElement>();
        for (int i = 0; i < count; i++)
        {
            int pick = rng.Next(indices.Count);
            result.Add(new ItemElement(pool[indices[pick]]()));
            indices.RemoveAt(pick);
        }
        return result.ToArray();
    }

    // ── Furniture builders ────────────────────────────────────────────────────

    private static PointOfInterest BuildHearth() => new HearthPointOfInterest(
        displayName: "Stone Hearth",
        descriptions: new() { "A wide stone hearth, ash-grey and cold between meals" },
        moods: new[] { "cold", "grey", "wide", "sooty", "still" }) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "firecraft", ["listen"] = "forge_ear", ["smell"] = "smoke_reading", ["contemplate"] = "journeyman_eye" } };

    private static PointOfInterest BuildTrestleTable() => new TablePointOfInterest(
        displayName: "Trestle Table",
        descriptions: new() { "A long trestle table of rough wood, benches tucked beneath" },
        moods: new[] { "worn", "scarred", "long", "simple", "communal" }) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "whittlecraft" } };

    private static PointOfInterest BuildBenchRow() => new BenchPointOfInterest(
        displayName: "Bench Row",
        descriptions: new() { "A row of low benches along the wall, polished by use" },
        moods: new[] { "low", "worn", "smooth", "waiting" }) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "whittlecraft" } };

    private static PointOfInterest BuildCookingHearth() => new HearthPointOfInterest(
        displayName: "Cooking Hearth",
        descriptions: new() { "A clay-rimmed cooking hearth with an iron hook and suspended pot" },
        moods: new[] { "warm", "sooty", "smoky", "active", "dim" }) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "firecraft", ["listen"] = "forge_ear", ["smell"] = "smoke_reading", ["contemplate"] = "journeyman_eye" } };

    private static PointOfInterest BuildKitchenShelf(Random rng)
    {
        var items = new List<ItemElement> { new(new Herb()) };
        if (rng.NextDouble() < 0.60) items.Add(new(new WoodenBowl()));
        return new ShelfPointOfInterest("Kitchen Shelf",
            new() { "Rough wooden shelves holding crockery, a salt block, and hanging herbs" },
            items, new[] { "cluttered", "fragrant", "dim", "crammed" }) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "peasantry", ["smell"] = "taint_sense" } };
    }

    private static PointOfInterest BuildStorageBarrel(params ItemElement[] items)
    {
        var poi = new BarrelPointOfInterest("Storage Barrel",
            new() { "A wide oak barrel, banded in iron, sealed with a waxed stopper" },
            moods: new[] { "heavy", "solid", "dim", "full", "old" }) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "cellarcraft", ["smell"] = "bouquet" } };
        poi.Items.AddRange(items);
        return poi;
    }

    private static PointOfInterest BuildStorageShelf(params ItemElement[] items)
    {
        var poi = new ShelfPointOfInterest("Storage Shelf",
            new() { "Sagging wooden shelves stacked with sacks and provisions" },
            moods: new[] { "cluttered", "low", "dim", "heavy" }) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "peasantry", ["smell"] = "taint_sense" } };
        poi.Items.AddRange(items);
        return poi;
    }

    private static PointOfInterest BuildColdShelf(Random rng)
    {
        var pool  = new Func<Item>[] { () => new Cheese(), () => new Egg(), () => new Bread() };
        var items = new List<ItemElement> { new(pool[rng.Next(pool.Length)]()) };
        return new ShelfPointOfInterest("Cold Shelf",
            new() { "A low stone shelf in the coolest corner, used for perishables" },
            items, new[] { "cool", "dim", "quiet", "still" }) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "peasantry", ["smell"] = "taint_sense" } };
    }

    private static PointOfInterest BuildToolCorner(Random rng)
    {
        var items = new List<ItemElement> { new(new Rope()) };
        if (rng.NextDouble() < 0.50) items.Add(new(new Hammer()));
        if (rng.NextDouble() < 0.40) items.Add(new(new Knife()));
        return new ToolPointOfInterest("Tool Corner",
            new() { "Hand tools leaning in a corner, hafts up, blades furred with old rust" },
            items, new[] { "cluttered", "iron-smelling", "dim", "propped" }) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "whittlecraft" } };
    }

    private static PointOfInterest BuildFirewoodStack(Random rng)
    {
        var items = new List<ItemElement> { new(new Log()) };
        if (rng.NextDouble() < 0.50) items.Add(new(new Twig()));
        return new FirewoodPointOfInterest("Firewood Stack",
            new() { "Split logs stacked to the roofline, the bottom course gone soft with damp" },
            items, new[] { "stacked", "dry", "resinous", "heavy" }) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["smell"] = "petrichor" } };
    }

    /// <summary>A pallet. Named per sleeper in a shared room so each is its own observation.</summary>
    private static PointOfInterest BuildPallet(Random rng, int index, int total)
    {
        var items = new List<ItemElement> { new(new Straw()) };
        if (rng.NextDouble() < 0.40) items.Add(new(new WoolCap()));
        return new PalletPointOfInterest(
            total > 1 ? $"Straw Pallet ({PositionWord(index)})" : "Straw Pallet",
            new() { "A straw-stuffed pallet on a low wooden frame — a sleeping place" },
            items, new[] { "sparse", "low", "quiet", "lumpy", "still" }) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "peasantry", ["smell"] = "taint_sense" } };
    }

    private static PointOfInterest BuildChest(Random rng, int index, int total)
    {
        var poi = new ChestPointOfInterest(
            total > 1 ? $"Wooden Chest ({PositionWord(index)})" : "Wooden Chest",
            new() { "A sturdy chest with a hasp lock, sitting at the foot of the bed" },
            moods: new[] { "battered", "solid", "quiet", "closed" }) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "whittlecraft" } };
        poi.Items.AddRange(PickFromPool(rng, ChestPool(), 0, 2));
        return poi;
    }

    /// <summary>
    /// Distinguishes same-role furniture within a room. Observation candidates are de-duplicated by
    /// display name, so two pallets called "Straw Pallet" would leave only one of them observable.
    /// </summary>
    private static string PositionWord(int index) => index switch
    {
        0 => "by the door", 1 => "by the wall",   2 => "under the window",
        3 => "in the corner", 4 => "by the hearth", 5 => "at the far end",
        _ => $"no. {index + 1}",
    };

    private static PointOfInterest BuildCandleStand() => new CandlePointOfInterest(
        displayName: "Candle Stand",
        descriptions: new() { "A tall wooden post with an iron spike for a candle, black with old wax" },
        items: new() { new ItemElement(new Candle()) },
        moods: new[] { "dim", "waxy", "quiet", "old" }) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "firecraft", ["smell"] = "smoke_reading" } };

    private static PointOfInterest BuildSpinningWheel() => new WheelPointOfInterest(
        displayName: "Spinning Wheel",
        descriptions: new() { "A worn wooden spinning wheel in the corner, the spindle dusty from disuse" },
        moods: new[] { "quiet", "worn", "still", "dusty", "old" }) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true), VerbModiMentis = new Dictionary<string, string> { ["examine"] = "threadwork", ["listen"] = "forge_ear", ["contemplate"] = "journeyman_eye" } };

    private static PointOfInterest BuildRushMat() => new MatPointOfInterest(
        displayName: "Rush Mat",
        descriptions: new() { "A woven mat of dried rushes by the door, muddy at the edges" },
        moods: new[] { "flat", "earthy", "dry", "worn" }) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "knotwork" } };

    private static PointOfInterest BuildHangingHerbs(Random rng)
    {
        var items = new List<ItemElement> { new(new Herb()) };
        if (rng.NextDouble() < 0.50) items.Add(new(new Onion()));
        return new HerbPointOfInterest("Hanging Herbs",
            new() { "Bundles of dried herbs and roots strung from a rafter, rustling in the draught" },
            items, new[] { "fragrant", "dim", "rustic", "dry", "dangling" }) { Senses = SensoryProfile.Fragrant, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "apothecary_nose", ["contemplate"] = "aesthetic" } };
    }

    private static PointOfInterest BuildSaltingBarrel(Random rng)
    {
        var items = new List<ItemElement> { new(new DriedMeat()) };
        if (rng.NextDouble() < 0.50) items.Add(new(new Tallow()));
        return new BarrelPointOfInterest("Salting Barrel",
            new() { "A wide barrel of dark brine in which cuts of meat are preserved" },
            items, new[] { "pungent", "dim", "dark", "heavy", "close" }) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "cellarcraft", ["smell"] = "bouquet" } };
    }

    private static PointOfInterest BuildMortarAndPestle() => new MortarPointOfInterest(
        displayName: "Mortar and Pestle",
        descriptions: new() { "A heavy stone mortar and pestle, stained dark with ground herbs and spices" },
        moods: new[] { "heavy", "old", "stained", "solid" }) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "herblore", ["smell"] = "apothecary_nose" } };

    private static PointOfInterest BuildButcherBlock(Random rng)
    {
        var items = new List<ItemElement>();
        if (rng.NextDouble() < 0.40) items.Add(new(new Knife()));
        return new BlockPointOfInterest("Butcher Block",
            new() { "A thick scarred chopping block of end-grain wood, stained dark" },
            items, new[] { "scarred", "heavy", "dark", "old", "solid" }) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "butchery" } };
    }

    private static PointOfInterest BuildWashstand() => new WashstandPointOfInterest(
        displayName: "Washstand",
        descriptions: new() { "A low wooden stand holding a clay basin and ewer for washing" },
        moods: new[] { "low", "plain", "cold", "damp", "sparse" }) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "grooming" } };

    private static PointOfInterest BuildChamberPot() => new PotPointOfInterest(
        displayName: "Chamber Pot",
        descriptions: new() { "A glazed clay chamber pot tucked under the bed" },
        moods: new[] { "plain", "utilitarian", "dim", "quiet" }) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "peasantry", ["smell"] = "taint_sense" } };

    private static PointOfInterest BuildPrayerStool() => new StoolPointOfInterest(
        displayName: "Prayer Stool",
        descriptions: new() { "A simple kneeling stool worn smooth in the middle from long use" },
        moods: new[] { "quiet", "worn", "plain", "still", "humble" }) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "iconography", ["contemplate"] = "meditation" } };

    private static PointOfInterest BuildClothesPegs(Random rng)
    {
        var items = new List<ItemElement>();
        if (rng.NextDouble() < 0.60) items.Add(new(new WoolCloak()));
        if (rng.NextDouble() < 0.50) items.Add(new(new LinenTunic()));
        return new PegPointOfInterest("Clothes Pegs",
            new() { "A row of wooden pegs hammered into the wall for hanging clothes" },
            items, new[] { "plain", "bare", "utilitarian", "dim" }) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "threadwork" } };
    }

    private static PointOfInterest BuildRushLight() => new LightPointOfInterest(
        displayName: "Rush Light",
        descriptions: new() { "A tallow rush-light on an iron spike, the wick pinched and black" },
        items: new() { new ItemElement(new Candle()) },
        moods: new[] { "dim", "sooty", "cold", "plain" }) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "firecraft", ["smell"] = "smoke_reading" } };
}
