using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;

namespace Cathedral.Game.Scene.Building;

/// <summary>Medieval building material that shapes descriptions.</summary>
public enum BuildingMaterial
{
    Wood,
    Stone,
    WattleAndDaub,
    Timber,
}

/// <summary>
/// Result of <see cref="HouseBuilder.Build"/>.
/// All returned sections and their areas are fully populated but NOT yet registered in the scene.
/// The caller (SceneFactory) must call RegisterAll on each section and add them to scene.Sections.
/// </summary>
public record HouseResult(
    List<Section>  Sections,
    Area           EntryRoom,
    List<Area>     AllRooms,
    List<Area>     Bedrooms,
    BuildingMaterial Material
);

/// <summary>
/// Procedural building generator. Creates a farmhouse-style structure:
///   • Ground floor: Hall + Kitchen + optional Pantry
///   • Upper floor (optional): Upper Landing + 1-3 Bedrooms
///
/// Inter-room connections use <see cref="DoorPointOfInterest"/> (both areas get the point of interest).
/// Vertical connections use <see cref="StairPointOfInterest"/> (both floor areas get the point of interest).
/// The main entrance door from outside → Hall is NOT created here; the calling factory
/// creates it so it can reference the outdoor area.
///
/// Minimum one bed is always guaranteed: bedrooms on the upper floor if floors ≥ 2,
/// or a bedroom appended to the ground floor if the building is single-storey.
/// </summary>
public class HouseBuilder
{
    public int MinBedrooms { get; init; } = 1;
    public int MaxBedrooms { get; init; } = 3;
    public int MaxFloors   { get; init; } = 2;

    /// <summary>
    /// Build a complete house structure.
    /// The returned <see cref="HouseResult"/> contains all sections and a list of bedroom areas
    /// for NPC spawning. Does not register anything into the scene.
    /// </summary>
    public HouseResult Build(Random rng)
    {
        var material    = (BuildingMaterial)rng.Next(Enum.GetValues<BuildingMaterial>().Length);
        var matWord     = MaterialWord(material);
        var floors      = MaxFloors >= 2 ? rng.Next(1, MaxFloors + 1) : 1;
        var bedroomCount = Math.Clamp(rng.Next(MinBedrooms, MaxBedrooms + 1), MinBedrooms, MaxBedrooms);

        var allRooms  = new List<Area>();
        var sections  = new List<Section>();
        var bedrooms  = new List<Area>();

        // ── Ground floor ──────────────────────────────────────────────────────

        var hall = BuildHall(matWord);
        var kitchen = BuildKitchen(matWord);
        allRooms.Add(hall);
        allRooms.Add(kitchen);

        // Hall ↔ Kitchen door (unlocked — inner door)
        var kitchenDoor = BuildInternalDoor("Kitchen Door", matWord, hall, kitchen, DoorState.Unlocked);
        hall.PointsOfInterest.Add(kitchenDoor);
        kitchen.PointsOfInterest.Add(kitchenDoor);

        // Optional pantry (60 % chance)
        Area? pantry = null;
        if (rng.NextDouble() < 0.60)
        {
            pantry = BuildPantry(matWord);
            allRooms.Add(pantry);
            var pantryDoor = BuildInternalDoor("Pantry Door", matWord, hall, pantry, DoorState.Unlocked);
            hall.PointsOfInterest.Add(pantryDoor);
            pantry.PointsOfInterest.Add(pantryDoor);
        }

        // Single-storey: put bedrooms on ground floor
        if (floors == 1)
        {
            for (int i = 0; i < bedroomCount; i++)
            {
                var bedroom = BuildBedroom(matWord, i);
                allRooms.Add(bedroom);
                bedrooms.Add(bedroom);
                var bedroomDoor = BuildInternalDoor(BedroomDoorName(i), matWord, hall, bedroom, DoorState.Unlocked);
                hall.PointsOfInterest.Add(bedroomDoor);
                bedroom.PointsOfInterest.Add(bedroomDoor);
            }
        }

        var groundSection = new Section(
            $"{MaterialSectionName(material)} Farmhouse",
            new() { $"The ground floor of a {matWord} farmhouse, low-ceilinged and smoky" }
        );
        groundSection.Areas.Add(hall);
        groundSection.Areas.Add(kitchen);
        if (pantry != null) groundSection.Areas.Add(pantry);
        if (floors == 1)    groundSection.Areas.AddRange(bedrooms);

        sections.Add(groundSection);

        // ── Upper floor ───────────────────────────────────────────────────────

        if (floors >= 2)
        {
            var landing = BuildUpperLanding(matWord);
            allRooms.Add(landing);

            // Stairs: Hall ↔ Landing
            var stair = BuildStaircase(hall, landing);
            hall.PointsOfInterest.Add(stair);
            landing.PointsOfInterest.Add(stair);

            for (int i = 0; i < bedroomCount; i++)
            {
                var bedroom = BuildBedroom(matWord, i);
                allRooms.Add(bedroom);
                bedrooms.Add(bedroom);
                var bedroomDoor = BuildInternalDoor(BedroomDoorName(i), matWord, landing, bedroom, DoorState.Unlocked);
                landing.PointsOfInterest.Add(bedroomDoor);
                bedroom.PointsOfInterest.Add(bedroomDoor);
            }

            var upperSection = new Section(
                $"{MaterialSectionName(material)} Farmhouse Upper Floor",
                new() { $"The upper storey of the farmhouse, reached by a narrow wooden staircase" }
            );
            upperSection.Areas.Add(landing);
            upperSection.Areas.AddRange(bedrooms);
            sections.Add(upperSection);
        }

        return new HouseResult(sections, hall, allRooms, bedrooms, material);
    }

    // ── Area builders ─────────────────────────────────────────────────────────

    private static Area BuildHall(string mat) => new(
        displayName: "Hall",
        contextDescription: "standing in the farmhouse hall",
        transitionDescription: "enter the hall",
        descriptions: new() { $"The main room of the farmhouse — {mat} beams overhead, a hearthstone at one end, the smell of woodsmoke and tallow" },
        keywords: new()
        {
            KeywordInContext.Parse($"the rough {mat} <beam>s crossing the low ceiling"),
            KeywordInContext.Parse("the cold <hearthstone> blackened by years of fire"),
            KeywordInContext.Parse("a long <trestle> pushed against the wall"),
            KeywordInContext.Parse("the faint <smell> of tallow and old smoke"),
        },
        moods: new[] { "smoky", "low-ceilinged", "worn", "quiet", "dim", "heavy", "dusty" }
    );

    private static Area BuildKitchen(string mat) => new(
        displayName: "Kitchen",
        contextDescription: "in the farmhouse kitchen",
        transitionDescription: "step into the kitchen",
        descriptions: new() { $"A cramped kitchen with a clay hearth, a roughhewn table, and shelves of crockery and hanging herbs" },
        keywords: new()
        {
            KeywordInContext.Parse("the clay <hearth> ringed with soot and ash"),
            KeywordInContext.Parse("a bundle of dried <herb>s hanging from a rafter"),
            KeywordInContext.Parse("the rough-hewn <table> scored by years of knife work"),
            KeywordInContext.Parse("some clay <pot>s stacked beside the fire"),
        },
        moods: new[] { "warm", "cramped", "smoky", "fragrant", "cluttered", "low", "close" }
    );

    private static Area BuildPantry(string mat) => new(
        displayName: "Pantry",
        contextDescription: "in the farmhouse pantry",
        transitionDescription: "step into the pantry",
        descriptions: new() { "A cool storage room with shelves and barrels, smelling of grain, salt, and dried meat" },
        keywords: new()
        {
            KeywordInContext.Parse("the heavy <barrel>s lined along the wall"),
            KeywordInContext.Parse("some cloth <sack>s of grain piled in a corner"),
            KeywordInContext.Parse("a <shelf> of earthen jars sealed with wax"),
            KeywordInContext.Parse("the cool dry <smell> of cured provisions"),
        },
        moods: new[] { "cool", "quiet", "dim", "dry", "musty", "orderly", "provisioned" }
    );

    private static Area BuildUpperLanding(string mat) => new(
        displayName: "Upper Landing",
        contextDescription: "on the upper landing of the farmhouse",
        transitionDescription: "reach the upper landing",
        descriptions: new() { "A narrow landing at the top of the stairs, planked floor creaking with every step" },
        keywords: new()
        {
            KeywordInContext.Parse("the narrow <landing> between the bedroom doors"),
            KeywordInContext.Parse("the creaking <floor> of rough-sawn planks"),
            KeywordInContext.Parse("a faint <draught> pushing through the eaves"),
            KeywordInContext.Parse("the low <slope> of the roof pressing close overhead"),
        },
        moods: new[] { "narrow", "creaking", "dim", "low", "quiet", "close", "dusky" }
    );

    private static Area BuildBedroom(string mat, int index)
    {
        var names = new[] { "Bedroom", "Second Bedroom", "Third Bedroom" };
        var name  = index < names.Length ? names[index] : $"Bedroom {index + 1}";
        return new(
            displayName: name,
            contextDescription: $"inside the {name.ToLowerInvariant()}",
            transitionDescription: $"enter the {name.ToLowerInvariant()}",
            descriptions: new() { "A sparse sleeping room with a straw pallet and a wooden chest" },
            keywords: new()
            {
                KeywordInContext.Parse("a straw-filled <pallet> low on the floor"),
                KeywordInContext.Parse("a worn <chest> at the foot of the bed"),
                KeywordInContext.Parse("the rough <wall> showing gaps where the wind seeps in"),
                KeywordInContext.Parse("a single <tallow> candle stub on a nail"),
            },
            moods: new[] { "sparse", "cold", "quiet", "small", "close", "still", "dark" }
        );
    }

    // ── Door / stair builders ─────────────────────────────────────────────────

    private static DoorPointOfInterest BuildInternalDoor(
        string name, string mat, Area front, Area back, DoorState initialState)
        => new(
            frontArea: front,
            backArea:  back,
            displayName: name,
            descriptions: new() { $"A low {mat} door set in the wall, iron-hinged and rough-fitted" },
            keywords: new()
            {
                KeywordInContext.Parse($"a low {mat} <door> set into the dividing wall"),
                KeywordInContext.Parse("the iron <hinge>s dark with old rust"),
            },
            initialState: initialState
        );

    private static StairPointOfInterest BuildStaircase(Area bottom, Area top)
        => new(
            bottomArea: bottom,
            topArea:    top,
            displayName: "Wooden Staircase",
            descriptions: new() { "A steep narrow staircase of rough-cut timber, worn smooth in the middle" },
            keywords: new()
            {
                KeywordInContext.Parse("the steep <staircase> rising into the upper floor"),
                KeywordInContext.Parse("the worn <tread>s groaning under weight"),
            }
        );

    // ── Furniture spots ───────────────────────────────────────────────────────

    /// <summary>
    /// Adds appropriate furniture spots to each area, with procedural variety via rng.
    /// Called by the factory after receiving HouseResult, before registration.
    /// </summary>
    public static void PopulateFurniture(HouseResult house, Random rng)
    {
        foreach (var area in house.AllRooms)
        {
            var name = area.DisplayName;
            if      (name == "Hall")           PopulateHall(area, rng);
            else if (name == "Kitchen")        PopulateKitchen(area, rng);
            else if (name == "Pantry")         PopulatePantry(area, rng);
            else if (name == "Upper Landing")  { /* corridor — no furniture */ }
            else if (name.Contains("Bedroom")) PopulateBedroom(area, rng);
        }
    }

    private static void PopulateHall(Area area, Random rng)
    {
        // Required
        area.PointsOfInterest.Add(BuildHearthPointOfInterest());
        area.PointsOfInterest.Add(BuildTrestleTablePointOfInterest());
        // Optional: pick 1 from pool
        var pool = new List<Func<PointOfInterest>>
        {
            () => BuildCandleStandPointOfInterest(),
            () => BuildSpinningWheelPointOfInterest(),
            () => BuildRushMatPointOfInterest(),
        };
        foreach (var poi in SampleOptional(rng, pool, 1))
            area.PointsOfInterest.Add(poi);
    }

    private static void PopulateKitchen(Area area, Random rng)
    {
        // Required
        area.PointsOfInterest.Add(BuildCookingHearthPointOfInterest());
        area.PointsOfInterest.Add(BuildKitchenShelfPointOfInterest(rng));
        // Optional: pick 1-2 from pool
        var pool = new List<Func<PointOfInterest>>
        {
            () => BuildHangingHerbsPointOfInterest(rng),
            () => BuildSaltingBarrelPointOfInterest(rng),
            () => BuildMortarAndPestlePointOfInterest(),
            () => BuildButcherBlockPointOfInterest(rng),
        };
        foreach (var poi in SampleOptional(rng, pool, rng.Next(1, 3)))
            area.PointsOfInterest.Add(poi);
    }

    private static void PopulatePantry(Area area, Random rng)
    {
        // Both storage spots have randomised contents
        area.PointsOfInterest.Add(BuildBarrelPointOfInterest(PickFromPool(rng, BarrelPool(), 1, 2)));
        area.PointsOfInterest.Add(BuildShelfPointOfInterest(PickFromPool(rng, ShelfPool(), 1, 2)));
        // 40% chance of an extra cold shelf
        if (rng.NextDouble() < 0.40)
            area.PointsOfInterest.Add(BuildColdShelfPointOfInterest(rng));
    }

    private static void PopulateBedroom(Area area, Random rng)
    {
        // Required
        area.PointsOfInterest.Add(BuildBedPointOfInterest(rng));
        area.PointsOfInterest.Add(BuildChestPointOfInterest(rng));
        // Optional: pick 1-2 from pool
        var pool = new List<Func<PointOfInterest>>
        {
            () => BuildWashstandPointOfInterest(),
            () => BuildChamberPotPointOfInterest(),
            () => BuildPrayerStoolPointOfInterest(),
            () => BuildClothesPegPointOfInterest(rng),
            () => BuildRushLightPointOfInterest(),
        };
        foreach (var poi in SampleOptional(rng, pool, rng.Next(1, 3)))
            area.PointsOfInterest.Add(poi);
    }

    // ── Item pools for randomised storage ─────────────────────────────────────

    private static List<Func<Item>> BarrelPool() => new()
    {
        () => new Grain(),
        () => new Grain(),
        () => new Turnip(),
        () => new Cabbage(),
        () => new DriedMeat(),
        () => new DriedPeas(),
        () => new Salt(),
    };

    private static List<Func<Item>> ShelfPool() => new()
    {
        () => new Hay(),
        () => new Bread(),
        () => new Cheese(),
        () => new Onion(),
        () => new Herb(),
        () => new Lard(),
        () => new ClayPot(),
    };

    private static List<Func<Item>> ChestPool() => new()
    {
        () => new LinenTunic(),
        () => new WoolCloak(),
        () => new LeatherBelt(),
        () => new WoolCap(),
        () => new Knife(),
        () => new Bread(),
        () => new LeatherBoots(),
        () => new WoolStockings(),
        () => new LeatherGloves(),
        () => new Flint(),
    };

    // ── Randomisation helpers ─────────────────────────────────────────────────

    /// <summary>Picks count distinct points of interest from a builder pool using rng.</summary>
    private static List<PointOfInterest> SampleOptional(Random rng, List<Func<PointOfInterest>> pool, int count)
    {
        count = Math.Min(count, pool.Count);
        var indices = new List<int>(System.Linq.Enumerable.Range(0, pool.Count));
        var result  = new List<PointOfInterest>();
        for (int i = 0; i < count; i++)
        {
            int pick = rng.Next(indices.Count);
            result.Add(pool[indices[pick]]());
            indices.RemoveAt(pick);
        }
        return result;
    }

    /// <summary>Picks min–max items from an item factory pool, no duplicates.</summary>
    private static ItemElement[] PickFromPool(Random rng, List<Func<Item>> pool, int min, int max)
    {
        int count   = rng.Next(min, max + 1);
        count       = Math.Min(count, pool.Count);
        var indices = new List<int>(System.Linq.Enumerable.Range(0, pool.Count));
        var result  = new List<ItemElement>();
        for (int i = 0; i < count; i++)
        {
            int pick = rng.Next(indices.Count);
            result.Add(new ItemElement(pool[indices[pick]]()));
            indices.RemoveAt(pick);
        }
        return result.ToArray();
    }

    private static PointOfInterest BuildHearthPointOfInterest() => new(
        displayName: "Stone Hearth",
        descriptions: new() { "A wide stone hearth, ash-grey and cold between meals" },
        keywords: new()
        {
            KeywordInContext.Parse("the cold <hearth> stacked with grey ash"),
            KeywordInContext.Parse("the blackened <stone> of the fireplace lintel"),
        },
        moods: new[] { "cold", "grey", "wide", "sooty", "still" }
    );

    private static PointOfInterest BuildTrestleTablePointOfInterest() => new(
        displayName: "Trestle Table",
        descriptions: new() { "A long trestle table of rough wood, benches tucked beneath" },
        keywords: new()
        {
            KeywordInContext.Parse("the long <trestle> table scarred by years of use"),
            KeywordInContext.Parse("the worn <bench>es tucked beneath the table"),
        },
        moods: new[] { "worn", "scarred", "long", "simple", "communal" }
    );

    private static PointOfInterest BuildCookingHearthPointOfInterest() => new(
        displayName: "Cooking Hearth",
        descriptions: new() { "A clay-rimmed cooking hearth with an iron hook and suspended pot" },
        keywords: new()
        {
            KeywordInContext.Parse("the clay-rimmed <hearth> glowing faintly with coals"),
            KeywordInContext.Parse("the iron <hook> from which a pot hangs"),
        },
        moods: new[] { "warm", "sooty", "smoky", "active", "dim" }
    );

    private static PointOfInterest BuildKitchenShelfPointOfInterest(Random rng)
    {
        var items = new List<ItemElement> { new(new Herb()) };
        if (rng.NextDouble() < 0.60) items.Add(new(new WoodenBowl()));
        return new PointOfInterest(
            displayName: "Kitchen Shelf",
            descriptions: new() { "Rough wooden shelves holding crockery, a salt block, and hanging herbs" },
            keywords: new()
            {
                KeywordInContext.Parse("the wooden <shelf> sagging with crockery"),
                KeywordInContext.Parse("a dried <herb> bundle swinging in the draught"),
            },
            items: items,
            moods: new[] { "cluttered", "fragrant", "dim", "crammed" }
        );
    }

    private static PointOfInterest BuildBarrelPointOfInterest(params ItemElement[] items)
    {
        var poi = new PointOfInterest(
            displayName: "Storage Barrel",
            descriptions: new() { "A wide oak barrel, banded in iron, sealed with a waxed stopper" },
            keywords: new()
            {
                KeywordInContext.Parse("a wide oak <barrel> standing in the corner"),
                KeywordInContext.Parse("the iron <band> of the storage barrel"),
            },
            moods: new[] { "heavy", "solid", "dim", "full", "old" }
        );
        poi.Items.AddRange(items);
        return poi;
    }

    private static PointOfInterest BuildShelfPointOfInterest(params ItemElement[] items)
    {
        var poi = new PointOfInterest(
            displayName: "Storage Shelf",
            descriptions: new() { "Sagging wooden shelves stacked with sacks and provisions" },
            keywords: new()
            {
                KeywordInContext.Parse("the sagging <shelf> lined with cloth sacks"),
                KeywordInContext.Parse("a <sack> of dried provisions tied at the neck"),
            },
            moods: new[] { "cluttered", "low", "dim", "heavy" }
        );
        poi.Items.AddRange(items);
        return poi;
    }

    private static PointOfInterest BuildColdShelfPointOfInterest(Random rng)
    {
        var pool  = new Func<Item>[] { () => new Cheese(), () => new Egg(), () => new Bread() };
        var items = new List<ItemElement> { new(pool[rng.Next(pool.Length)]()) };
        return new PointOfInterest(
            displayName: "Cold Shelf",
            descriptions: new() { "A low stone shelf in the coolest corner, used for perishables" },
            keywords: new()
            {
                KeywordInContext.Parse("the cold <shelf> in the corner where food stays fresh"),
                KeywordInContext.Parse("a cloth-wrapped <wedge> of something on the stone"),
            },
            items: items,
            moods: new[] { "cool", "dim", "quiet", "still" }
        );
    }

    private static PointOfInterest BuildBedPointOfInterest(Random rng)
    {
        var items = new List<ItemElement> { new(new Straw()) };
        if (rng.NextDouble() < 0.40) items.Add(new(new WoolCap()));
        return new PointOfInterest(
            displayName: "Straw Pallet",
            descriptions: new() { "A straw-stuffed pallet on a low wooden frame — the sleeping place" },
            keywords: new()
            {
                KeywordInContext.Parse("the lumpen <pallet> smelling of straw and sleep"),
                KeywordInContext.Parse("a rough <blanket> folded at the foot of the bed"),
            },
            items: items,
            moods: new[] { "sparse", "low", "quiet", "lumpy", "still" }
        );
    }

    private static PointOfInterest BuildChestPointOfInterest(Random rng)
    {
        var items = PickFromPool(rng, ChestPool(), 0, 2);
        var poi   = new PointOfInterest(
            displayName: "Wooden Chest",
            descriptions: new() { "A sturdy chest with a hasp lock, sitting at the foot of the bed" },
            keywords: new()
            {
                KeywordInContext.Parse("the battered <chest> at the foot of the bed"),
                KeywordInContext.Parse("the iron <hasp> of the chest, worn bright with handling"),
            },
            moods: new[] { "battered", "solid", "quiet", "closed" }
        );
        poi.Items.AddRange(items);
        return poi;
    }

    // ── New optional furniture spots ──────────────────────────────────────────

    private static PointOfInterest BuildCandleStandPointOfInterest() => new(
        displayName: "Candle Stand",
        descriptions: new() { "A tall wooden post with an iron spike for a candle, black with old wax" },
        keywords: new()
        {
            KeywordInContext.Parse("a tall <stand> topped with a melted iron spike"),
            KeywordInContext.Parse("the old wax <drip> running down the wooden post"),
        },
        items: new() { new ItemElement(new Candle()) },
        moods: new[] { "dim", "waxy", "quiet", "old" }
    );

    private static PointOfInterest BuildSpinningWheelPointOfInterest() => new(
        displayName: "Spinning Wheel",
        descriptions: new() { "A worn wooden spinning wheel in the corner, the spindle dusty from disuse" },
        keywords: new()
        {
            KeywordInContext.Parse("the old wooden <wheel> of the spinning frame"),
            KeywordInContext.Parse("the dusty <spindle> waiting for thread"),
        },
        moods: new[] { "quiet", "worn", "still", "dusty", "old" }
    );

    private static PointOfInterest BuildRushMatPointOfInterest() => new(
        displayName: "Rush Mat",
        descriptions: new() { "A woven mat of dried rushes by the door, muddy at the edges" },
        keywords: new()
        {
            KeywordInContext.Parse("a woven rush <mat> laid across the threshold"),
            KeywordInContext.Parse("the dry <crackle> of compressed rushes underfoot"),
        },
        moods: new[] { "flat", "earthy", "dry", "worn" }
    );

    private static PointOfInterest BuildHangingHerbsPointOfInterest(Random rng)
    {
        var items = new List<ItemElement> { new(new Herb()) };
        if (rng.NextDouble() < 0.50) items.Add(new(new Onion()));
        return new PointOfInterest(
            displayName: "Hanging Herbs",
            descriptions: new() { "Bundles of dried herbs and roots strung from a rafter, rustling in the draught" },
            keywords: new()
            {
                KeywordInContext.Parse("the hanging <bundle>s of dried herbs tied to the rafter"),
                KeywordInContext.Parse("the sweet dry <smell> of culinary herbs overhead"),
            },
            items: items,
            moods: new[] { "fragrant", "dim", "rustic", "dry", "dangling" }
        );
    }

    private static PointOfInterest BuildSaltingBarrelPointOfInterest(Random rng)
    {
        var items = new List<ItemElement> { new(new DriedMeat()) };
        if (rng.NextDouble() < 0.50) items.Add(new(new Tallow()));
        return new PointOfInterest(
            displayName: "Salting Barrel",
            descriptions: new() { "A wide barrel of dark brine in which cuts of meat are preserved" },
            keywords: new()
            {
                KeywordInContext.Parse("the dark brine <barrel> smelling of salt and fat"),
                KeywordInContext.Parse("the floating dark <meat> in the salting barrel"),
            },
            items: items,
            moods: new[] { "pungent", "dim", "dark", "heavy", "close" }
        );
    }

    private static PointOfInterest BuildMortarAndPestlePointOfInterest() => new(
        displayName: "Mortar and Pestle",
        descriptions: new() { "A heavy stone mortar and pestle, stained dark with ground herbs and spices" },
        keywords: new()
        {
            KeywordInContext.Parse("the heavy stone <mortar> dark with ground herbs"),
            KeywordInContext.Parse("the smooth worn <pestle> resting in the bowl"),
        },
        moods: new[] { "heavy", "old", "stained", "solid" }
    );

    private static PointOfInterest BuildButcherBlockPointOfInterest(Random rng)
    {
        var items = new List<ItemElement>();
        if (rng.NextDouble() < 0.40) items.Add(new(new Knife()));
        return new PointOfInterest(
            displayName: "Butcher Block",
            descriptions: new() { "A thick scarred chopping block of end-grain wood, stained dark" },
            keywords: new()
            {
                KeywordInContext.Parse("the thick scarred <block> of end-grain wood"),
                KeywordInContext.Parse("the dark <stain> soaked deep into the chopping surface"),
            },
            items: items,
            moods: new[] { "scarred", "heavy", "dark", "old", "solid" }
        );
    }

    private static PointOfInterest BuildWashstandPointOfInterest() => new(
        displayName: "Washstand",
        descriptions: new() { "A low wooden stand holding a clay basin and ewer for washing" },
        keywords: new()
        {
            KeywordInContext.Parse("the clay <basin> sitting on the low washing stand"),
            KeywordInContext.Parse("the rough wooden <stand> beside the bed"),
        },
        moods: new[] { "low", "plain", "cold", "damp", "sparse" }
    );

    private static PointOfInterest BuildChamberPotPointOfInterest() => new(
        displayName: "Chamber Pot",
        descriptions: new() { "A glazed clay chamber pot tucked under the bed" },
        keywords: new()
        {
            KeywordInContext.Parse("a glazed clay <pot> tucked under the bed frame"),
            KeywordInContext.Parse("the plain <glaze> of a chamber pot catching the dim light"),
        },
        moods: new[] { "plain", "utilitarian", "dim", "quiet" }
    );

    private static PointOfInterest BuildPrayerStoolPointOfInterest() => new(
        displayName: "Prayer Stool",
        descriptions: new() { "A simple kneeling stool worn smooth in the middle from long use" },
        keywords: new()
        {
            KeywordInContext.Parse("the simple <stool> worn smooth by years of kneeling"),
            KeywordInContext.Parse("the worn <wood> of the prayer stool, pale and smooth"),
        },
        moods: new[] { "quiet", "worn", "plain", "still", "humble" }
    );

    private static PointOfInterest BuildClothesPegPointOfInterest(Random rng)
    {
        var items = new List<ItemElement>();
        if (rng.NextDouble() < 0.60) items.Add(new(new WoolCloak()));
        if (rng.NextDouble() < 0.50) items.Add(new(new LinenTunic()));
        return new PointOfInterest(
            displayName: "Clothes Pegs",
            descriptions: new() { "A row of wooden pegs hammered into the wall for hanging clothes" },
            keywords: new()
            {
                KeywordInContext.Parse("the row of wooden <peg>s hammered into the wall"),
                KeywordInContext.Parse("a hanging <garment> draped from the wall pegs"),
            },
            items: items,
            moods: new[] { "plain", "bare", "utilitarian", "dim" }
        );
    }

    private static PointOfInterest BuildRushLightPointOfInterest() => new(
        displayName: "Rush Light",
        descriptions: new() { "A tallow rush-light on an iron spike, the wick pinched and black" },
        keywords: new()
        {
            KeywordInContext.Parse("the iron <spike> of the rush-light holder on the wall"),
            KeywordInContext.Parse("the black pinched <wick> of a burned-down rush light"),
        },
        items: new() { new ItemElement(new Candle()) },
        moods: new[] { "dim", "sooty", "cold", "plain" }
    );

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string MaterialWord(BuildingMaterial m) => m switch
    {
        BuildingMaterial.Stone        => "stone",
        BuildingMaterial.WattleAndDaub => "wattle-and-daub",
        BuildingMaterial.Timber       => "timber-framed",
        _                             => "wooden",
    };

    private static string MaterialSectionName(BuildingMaterial m) => m switch
    {
        BuildingMaterial.Stone        => "Stone",
        BuildingMaterial.WattleAndDaub => "Wattle",
        BuildingMaterial.Timber       => "Timber",
        _                             => "Wooden",
    };

    private static string BedroomDoorName(int index) => index switch
    {
        0 => "Bedroom Door",
        1 => "Second Bedroom Door",
        _ => $"Bedroom {index + 1} Door",
    };
}
