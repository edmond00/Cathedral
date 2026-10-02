using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Shared;

/// <summary>
/// The things a place has that are not resources, not people and not the way out: somewhere to sit,
/// somewhere to hide, an obstacle worth going through rather than round, something to climb.
///
/// <para>These are what make two villages different from each other. The areas a factory rolls are
/// much the same each time; what varies is the bench in one square and the horse trough in the next,
/// the bramble shortcut that exists here and not there. Everything in here is rolled, and everything
/// is optional.</para>
///
/// <para><b>Crossings and water are added as extra routes, never as replacements.</b> A crossing
/// between two areas that already have a path is pointless, and a crossing that replaces the only
/// path can leave a location whose far half is behind a difficulty-5 check. Both connect areas that
/// are <i>not</i> otherwise adjacent, so they are pure shortcuts: worth the risk, never required.</para>
/// </summary>
public static class FurnitureSubfactory
{
    /// <summary>Which flavour of furniture a location gets. Drives every pool below.</summary>
    /// <summary>
    /// Which flavour of furniture a location gets. Drives every pool below. The last three are the
    /// hot and cold country's (see <c>ClimateRule</c>): <see cref="Arid"/> for desert, hot steppe and
    /// canyon, <see cref="Jungle"/>, and <see cref="Frozen"/> for everything the cold makes.
    /// </summary>
    public enum Setting { Settlement, Farmland, Woodland, Water, Highland, Underground, Arid, Jungle, Frozen }

    /// <summary>Wild ground: its furniture is the land's own, not anything somebody put there.</summary>
    private static bool IsWild(Setting setting)
        => setting is Setting.Woodland or Setting.Highland or Setting.Arid or Setting.Jungle or Setting.Frozen;

    // ── Sitting ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Scatters somewhere to sit through the given areas. A sit spot is the only way in the game to
    /// move the clock on deliberately, so a location with none is a location where the time of day
    /// is whatever it was when you arrived, for as long as you stay.
    /// </summary>
    public static void AddSitSpots(Random rng, IReadOnlyList<Area> areas, Setting setting, int min = 1, int max = 3)
    {
        if (areas.Count == 0) return;

        (string Name, string Lemma, string Desc, string[] Moods)[] pool = setting switch
        {
            Setting.Settlement => new[]
            {
                ("Stone Bench",    "bench",  "A worn stone bench set against the wall, dished in the middle by long use", new[] { "worn", "cold", "sun-warmed", "smooth" }),
                ("Mounting Block", "block",  "Three cut steps of stone for getting onto a horse, and for sitting on when there is no horse", new[] { "squat", "chipped", "solid" }),
                ("Cart Tail",      "cart",   "A handcart tipped back on its shafts, the tailboard at just the right height", new[] { "weathered", "splintered", "tipped" }),
            },
            Setting.Farmland => new[]
            {
                ("Feed Trough",  "trough", "An upturned feed trough, dry inside and steady enough to sit on", new[] { "upturned", "cracked", "sun-bleached" }),
                ("Gate Bar",     "gate",   "A five-bar gate with its top rail rubbed smooth by leaning", new[] { "grey", "smooth-railed", "sagging" }),
                ("Straw Bale",   "bale",   "A bale of straw pushed against the wall and sat on until it lost its corners", new[] { "flattened", "dry", "sweet" }),
            },
            Setting.Woodland => new[]
            {
                ("Tree Stump",   "stump",  "A broad stump cut level, rings showing through a skin of moss", new[] { "mossy", "broad", "damp", "level" }),
                ("Fallen Bough", "bough",  "A thick bough down across the ground, dry-barked and steady", new[] { "dry", "solid", "lichened" }),
            },
            Setting.Water => new[]
            {
                ("Flat Rock",    "rock",   "A slab of rock above the tideline, dry and warm on its landward side", new[] { "sun-warmed", "salt-crusted", "flat" }),
                ("Boat Thwart",  "thwart", "An upturned boat with its thwart bench still in it, hauled clear of the water", new[] { "tarred", "upturned", "weed-fringed" }),
            },
            Setting.Highland => new[]
            {
                ("Sheltered Ledge", "ledge", "A shelf of rock out of the wind, worn hollow by whoever sat here before", new[] { "sheltered", "wind-scoured", "cold" }),
                ("Boulder Seat",    "boulder", "A boulder with one side split away flat, at the height of a chair", new[] { "split", "grey", "bare" }),
            },
            Setting.Arid => new[]
            {
                ("Shade Rock",    "rock",  "A rock leaning far enough over to throw a seat's worth of shade at noon", new[] { "shaded", "hot-sided", "wind-polished" }),
                ("Bleached Log",  "log",   "A log of some long-dead tree, silver-grey and hard as horn, half in the sand", new[] { "bleached", "cracked", "sun-hot" }),
            },
            Setting.Jungle => new[]
            {
                ("Buttress Root", "root",  "The great flange of a buttress root, a seat between two of its walls", new[] { "mossed", "damp", "sheltered" }),
                ("Fallen Giant",  "trunk", "A fallen trunk so wide it is sat on like a wall, furred with moss and fern", new[] { "rotting", "green", "broad" }),
            },
            Setting.Frozen => new[]
            {
                ("Snow Bench",    "bench", "A step of wind-packed snow hard enough to sit on and cut square by the wind", new[] { "packed", "wind-cut", "cold" }),
                ("Lee Stone",     "stone", "A stone standing out of the snow, its lee side bare and dry", new[] { "sheltered", "lichened", "cold" }),
            },
            _ => new[]
            {
                ("Rock Shelf",  "shelf", "A ledge of rock at sitting height, dry where the seep does not reach", new[] { "dry", "cold", "smooth" }),
                ("Timber Prop", "prop",  "A cut prop laid on its side against the wall, out of the way of the roof", new[] { "notched", "damp", "heavy" }),
            },
        };

        int count = rng.Next(min, max + 1);
        var used  = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            var spec = pool[rng.Next(pool.Length)];
            if (!used.Add(spec.Name)) continue;   // one of each kind per location; names must stay unique

            var area = areas[rng.Next(areas.Count)];
            if (area.PointsOfInterest.Any(p => p.DisplayName == spec.Name)) continue;

            area.PointsOfInterest.Add(new SitSpotPointOfInterest(
                spec.Name, spec.Lemma, new List<string> { spec.Desc }, spec.Moods,
                isNatural: IsWild(setting),
                verbModiMentis: new Dictionary<string, string>
                {
                    ["contemplate"] = "meditation",
                    ["listen"]      = "keen_ear",
                })
            {
                // Worth sitting on, worth looking out from, worth listening from. Three verbs on one
                // small object, which is the shape every observable is meant to have.
                Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
            });
        }
    }

    // ── Hiding ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds somewhere to get out of sight and watch. What hiding buys is knowledge of who comes and
    /// goes, which is otherwise only learnable by following people around.
    /// </summary>
    public static void AddHidingPlaces(Random rng, IReadOnlyList<Area> areas, Setting setting, int min = 1, int max = 2)
    {
        if (areas.Count == 0) return;

        (string Name, string Lemma, string Desc, string[] Moods)[] pool = setting switch
        {
            Setting.Settlement => new[]
            {
                ("Alley Recess", "recess", "A gap between two walls too narrow to be a way anywhere, and deep enough to stand in", new[] { "narrow", "dank", "shadowed" }),
                ("Wood Stack",   "stack",  "A stack of split wood built out from the wall, with a hollow behind it", new[] { "stacked", "resinous", "shadowed" }),
            },
            Setting.Farmland => new[]
            {
                ("Hay Bale Stack", "hay",  "Bales stacked two high with a gap left where one was pulled out", new[] { "dusty", "sweet", "close" }),
                ("Upturned Cart",  "cart", "A cart tipped onto its side, the bed of it making a low dark space", new[] { "tipped", "muddy", "dark" }),
            },
            Setting.Woodland => new[]
            {
                ("Root Hollow",   "hollow",  "A washed-out hollow beneath the roots of a great tree, dry and just big enough", new[] { "earthy", "dry", "close", "hidden" }),
                ("Bracken Stand", "bracken", "A stand of bracken shoulder-high and thick enough to close behind you", new[] { "rustling", "green", "dense" }),
            },
            Setting.Water => new[]
            {
                ("Overhang", "overhang", "A shelf of rock leaning out far enough to stand under and not be seen", new[] { "dripping", "shadowed", "salt-bitten" }),
            },
            Setting.Highland => new[]
            {
                ("Rock Cleft", "cleft", "A split in the rock face wide enough to back into and be gone from view", new[] { "narrow", "cold", "wind-quiet" }),
            },
            Setting.Arid => new[]
            {
                ("Sand Scoop",  "scoop",  "A hollow scooped by the wind in the lee of a dune, deep enough to lie in unseen", new[] { "hot", "shifting", "low" }),
                ("Rock Shadow", "shadow", "A slot of shadow under a tilted slab, black against the glare", new[] { "black", "cool", "narrow" }),
            },
            Setting.Jungle => new[]
            {
                ("Fern Screen", "fern",   "A wall of tree ferns, fronds overlapping, that closes behind whoever pushes in", new[] { "green", "dripping", "dense" }),
                ("Root Cave",   "cave",   "A cave of roots under a strangler fig, its old host long since rotted away", new[] { "hollow", "dark", "close" }),
            },
            Setting.Frozen => new[]
            {
                ("Snow Hollow", "hollow", "A hollow dug by the wind under a drift's lip, blue-dark inside", new[] { "blue", "muffled", "close" }),
                ("Ice Block",   "block",  "A tilted block of ice the size of a cart, with a dark gap behind it", new[] { "tilted", "glassy", "cold" }),
            },
            _ => new[]
            {
                ("Side Niche", "niche", "A worked-out niche in the tunnel wall, black past the first foot of it", new[] { "black", "damp", "close" }),
            },
        };

        int count = rng.Next(min, max + 1);
        var used  = new HashSet<string>();

        for (int i = 0; i < count; i++)
        {
            var spec = pool[rng.Next(pool.Length)];
            if (!used.Add(spec.Name)) continue;

            var area = areas[rng.Next(areas.Count)];
            if (area.PointsOfInterest.Any(p => p.DisplayName == spec.Name)) continue;

            area.PointsOfInterest.Add(new HidingPointOfInterest(
                spec.Name, spec.Lemma, new List<string> { spec.Desc }, spec.Moods,
                isNatural: IsWild(setting),
                verbModiMentis: new Dictionary<string, string> { ["listen"] = "keen_ear" })
            {
                Senses = new SensoryProfile(Examine: true, Listen: true),
            });
        }
    }

    // ── Shortcuts ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a hard shortcut between two areas that are not already adjacent — a bramble thicket, a
    /// mud wallow, a fallen trunk, a river. Nothing is ever <i>only</i> reachable this way, so a
    /// failed crossing costs a roll and some skin and never strands anybody.
    /// </summary>
    public static void AddShortcuts(Random rng, Scene scene, IReadOnlyList<Area> areas, Setting setting,
                                    double chance = 0.55, int max = 2)
    {
        if (areas.Count < 3) return;

        // Candidate pairs: not adjacent, not already joined by a connector. Sorted for determinism —
        // the pair list must not depend on hash order or a location re-rolls between builds.
        var pairs = new List<(Area A, Area B)>();
        for (int i = 0; i < areas.Count; i++)
        for (int j = i + 1; j < areas.Count; j++)
        {
            var (a, b) = (areas[i], areas[j]);
            bool adjacent = scene.AreaGraph.TryGetValue(a.Id, out var reach) && reach.Contains(b.Id);
            bool joined   = a.PointsOfInterest.OfType<ConnectorPointOfInterest>().Any(c => c.Touches(b));
            if (!adjacent && !joined) pairs.Add((a, b));
        }

        if (pairs.Count == 0) return;

        int added = 0;
        for (int attempt = 0; attempt < max && pairs.Count > 0; attempt++)
        {
            if (rng.NextDouble() > chance) continue;

            int index = rng.Next(pairs.Count);
            var (a, b) = pairs[index];
            pairs.RemoveAt(index);

            if (setting is Setting.Water && rng.NextDouble() < 0.6)
                AddWaterCrossing(rng, scene, a, b, setting);
            else
                AddDryCrossing(rng, scene, a, b, setting);

            added++;
        }

        _ = added;
    }

    private static void AddDryCrossing(Random rng, Scene scene, Area a, Area b, Setting setting)
    {
        CrossingKind kind = setting switch
        {
            Setting.Settlement  => rng.NextDouble() < 0.5 ? CrossingKind.MudPuddle : CrossingKind.Hedgerow,
            Setting.Farmland    => rng.NextDouble() < 0.5 ? CrossingKind.Hedgerow  : CrossingKind.MudPuddle,
            Setting.Woodland    => rng.NextDouble() < 0.5 ? CrossingKind.Brambles  : CrossingKind.FallenTrunk,
            Setting.Highland    => CrossingKind.Scree,
            Setting.Underground => CrossingKind.Scree,
            Setting.Arid        => rng.NextDouble() < 0.5 ? CrossingKind.Quicksand : CrossingKind.BoulderChoke,
            Setting.Jungle      => rng.NextDouble() < 0.6 ? CrossingKind.Lianas    : CrossingKind.FallenTrunk,
            Setting.Frozen      => rng.NextDouble() < 0.5 ? CrossingKind.SnowBridge : CrossingKind.ThinIce,
            _                   => rng.NextDouble() < 0.5 ? CrossingKind.Nettles   : CrossingKind.Brambles,
        };

        (string Name, string Desc, string[] Moods) spec = kind switch
        {
            CrossingKind.Brambles    => ("Bramble Thicket", "A wall of bramble grown across the gap, thorned the whole length of every cane", new[] { "thorned", "dense", "shoulder-high" }),
            CrossingKind.MudPuddle   => ("Mud Wallow",      "Ground churned to a wallow deep enough to take a boot and keep it",              new[] { "churned", "sucking", "foul" }),
            CrossingKind.FallenTrunk => ("Fallen Trunk",    "A great trunk down across the gap, barkless and rounded and a long way up",       new[] { "barkless", "rounded", "slick" }),
            CrossingKind.Scree       => ("Scree Slide",     "A tongue of loose stone that shifts underfoot at the first weight put on it",     new[] { "loose", "shifting", "grey" }),
            CrossingKind.Nettles     => ("Nettle Bed",      "A bed of nettles grown waist-high where nothing has trodden them",                new[] { "waist-high", "rank", "stinging" }),
            CrossingKind.Quicksand   => ("Quicksand",       "A flat of sand darker than the rest, with a skin on it that trembles when trodden near", new[] { "dark", "trembling", "smooth" }),
            CrossingKind.BoulderChoke => ("Boulder Choke",  "A gap choked with fallen blocks the size of carts, wedged one on another",       new[] { "wedged", "sun-hot", "jagged" }),
            CrossingKind.Lianas      => ("Liana Curtain",   "A curtain of lianas and creeper hung across the way, thick as a man's wrist",     new[] { "hanging", "tangled", "dripping" }),
            CrossingKind.ThinIce     => ("Thin Ice",        "A stretch of new grey ice, dark where the water shows through it",                new[] { "grey", "creaking", "thin" }),
            CrossingKind.SnowBridge  => ("Snow Bridge",     "A bridge of old snow sagging over a crevasse, its edges blue with depth",         new[] { "sagging", "blue-edged", "silent" }),
            _                        => ("Thorn Hedge",     "A laid hedge of thorn, grown thick and pleached to turn stock",                   new[] { "laid", "thick", "thorn-set" }),
        };

        new CrossingPointOfInterest(
            a, b, kind,
            $"{spec.Name} ({a.DisplayName}–{b.DisplayName})",
            new List<string> { spec.Desc },
            spec.Moods,
            verbModiMentis: new Dictionary<string, string> { ["examine"] = CrossingLore(kind) })
        {
            Senses = SensoryProfile.Examinable,
        }.AttachTo(scene);
    }

    /// <summary>What looking hard at an obstacle teaches: hedge lore for growth, the ground for the rest.</summary>
    private static string CrossingLore(CrossingKind kind) => kind switch
    {
        CrossingKind.Quicksand or CrossingKind.ThinIce or CrossingKind.SnowBridge => "survivalism",
        CrossingKind.BoulderChoke or CrossingKind.Scree                          => "stonework",
        _                                                                        => "hedgecraft",
    };


    private static void AddWaterCrossing(Random rng, Scene scene, Area a, Area b, Setting setting)
    {
        WaterKind kind = setting switch
        {
            Setting.Water      => rng.NextDouble() < 0.5 ? WaterKind.Cove : WaterKind.River,
            Setting.Settlement => WaterKind.MillLeat,
            Setting.Highland   => WaterKind.Creek,
            Setting.Arid       => WaterKind.Pond,
            Setting.Jungle     => rng.NextDouble() < 0.6 ? WaterKind.River : WaterKind.Pond,
            Setting.Frozen     => rng.NextDouble() < 0.5 ? WaterKind.Lead  : WaterKind.Meltwater,
            _                  => rng.NextDouble() < 0.5 ? WaterKind.Creek : WaterKind.Pond,
        };

        (string Name, string Desc, string[] Moods) spec = kind switch
        {
            WaterKind.River    => ("River Reach", "The water runs wide and steady here, with a pull to it that shows in the weed",  new[] { "brown", "steady", "deep-running" }),
            WaterKind.Creek    => ("Creek",       "A fast narrow watercourse cut down into its own bed, cold the whole year",       new[] { "fast", "cold", "narrow" }),
            WaterKind.Pond     => ("Pond",        "Still water gone green at the edges, deeper in the middle than it looks",        new[] { "still", "green", "flat" }),
            WaterKind.Cove     => ("Cove",        "A bite of sea between two arms of rock, swelling and dropping against them",     new[] { "swelling", "cold", "green-black" }),
            WaterKind.Lead     => ("Open Lead",   "A lane of black sea open between two floes, steaming in the cold",              new[] { "black", "steaming", "still" }),
            WaterKind.Meltwater => ("Meltwater Channel", "A channel cut into the ice by its own melt, glass-sided and racing",   new[] { "racing", "blue", "glass-sided" }),
            _                  => ("Mill Leat",   "A cut channel running fast and straight, walled in stone and deeper than a man", new[] { "fast", "straight", "walled" }),
        };

        // What is in the water. Rolled per stretch, and sometimes nothing: a pond that always gives
        // a fish up is worse than one that sometimes does not.
        bool holdsFish = rng.NextDouble() < 0.75;
        var catchable  = new List<ItemElement>();
        if (holdsFish)
        {
            Func<Narrative.Item>[] stock = (kind, setting) switch
            {
                (WaterKind.Cove or WaterKind.Lead, _) => new Func<Narrative.Item>[]
                {
                    () => new Narrative.World.Items.Herring(),
                    () => new Narrative.World.Items.Cod(),
                    () => (kind == WaterKind.Lead ? new Narrative.World.Items.ArcticChar() : new Narrative.World.Items.Mackerel()),
                },
                (WaterKind.Meltwater, _) => new Func<Narrative.Item>[] { () => new Narrative.World.Items.ArcticChar() },
                (_, Setting.Jungle or Setting.Arid) => new Func<Narrative.Item>[]
                {
                    () => new Narrative.World.Items.Catfish(),
                    () => new Narrative.World.Items.Eel(),
                },
                _ => new Func<Narrative.Item>[]
                {
                    () => new Narrative.World.Items.Trout(),
                    () => new Narrative.World.Items.Perch(),
                    () => new Narrative.World.Items.Eel(),
                    () => new Narrative.World.Items.Pike(),
                },
            };

            for (int n = rng.Next(2, 5); n > 0; n--)
                catchable.Add(new ItemElement(stock[rng.Next(stock.Length)]()));
        }

        new WaterCrossingPointOfInterest(
            a, b, kind,
            $"{spec.Name} ({a.DisplayName}–{b.DisplayName})",
            new List<string> { spec.Desc },
            spec.Moods,
            items: catchable)
        {
            HoldsFish = holdsFish,
            // Water is the archetypal multi-verb object: swim it, fish it, look at it, listen to it.
            Senses = SensoryProfile.Audible,
            VerbModiMentis = new Dictionary<string, string>
            {
                ["examine"] = "drainage",
                ["listen"]  = "keen_ear",
            },
        }.AttachTo(scene);
    }

    // ── Extraction points ─────────────────────────────────────────────────────

    /// <summary>
    /// Adds the things a tool is for: ore seams underground and in the hills, ground worth digging
    /// wherever there is ground worth digging.
    ///
    /// <para>Trees are deliberately not handled here. <c>CutWoodVerb</c> accepts anything anchored on
    /// the lemmas the terrain builders already use — "tree", "log", "stump", "deadfall" — so every
    /// wood in the game became cuttable the day the verb was written, without a single placement.</para>
    /// </summary>
    public static void AddExtractionPoints(Random rng, IReadOnlyList<Area> areas, Setting setting)
    {
        if (areas.Count == 0) return;

        if (setting is Setting.Underground or Setting.Highland)
        {
            int veins = rng.Next(1, setting == Setting.Underground ? 4 : 3);
            var kinds = new (string Name, string Desc, Func<Narrative.Item>[] Yield)[]
            {
                ("Copper Seam", "A seam of green-stained rock running out of sight into the wall",
                    new Func<Narrative.Item>[] { () => new Narrative.World.Items.CopperOre() }),
                ("Tin Seam",    "A narrow dark seam picked at before and abandoned",
                    new Func<Narrative.Item>[] { () => new Narrative.World.Items.TinOre() }),
                ("Lead Seam",   "Grey crystal breaking out of the rock in cubes",
                    new Func<Narrative.Item>[] { () => new Narrative.World.Items.LeadOre() }),
                ("Iron Seam",   "A rust-coloured band of ore running through the stone",
                    new Func<Narrative.Item>[] { () => new Narrative.World.Items.IronOre() }),
            };

            var used = new HashSet<string>();
            for (int i = 0; i < veins; i++)
            {
                var kind = kinds[rng.Next(kinds.Length)];
                if (!used.Add(kind.Name)) continue;

                var area = areas[rng.Next(areas.Count)];
                if (area.PointsOfInterest.Any(p => p.DisplayName == kind.Name)) continue;

                var items = new List<ItemElement>();
                for (int n = rng.Next(2, 5); n > 0; n--) items.Add(new ItemElement(kind.Yield[0]()));

                area.PointsOfInterest.Add(new OreVeinPointOfInterest(
                    kind.Name, "seam", new List<string> { kind.Desc }, items,
                    new[] { "banded", "picked-at", "glittering", "hard" }));
            }
        }

        // Diggable ground. Everywhere has some; what comes out of it differs.
        if (rng.NextDouble() < 0.7)
        {
            (string Name, string Lemma, string Desc, Func<Narrative.Item>[] Yield) spec = setting switch
            {
                Setting.Water => ("Sand Flat", "sand", "A wide flat of coarse sand left bare by the tide",
                    new Func<Narrative.Item>[] { () => new Narrative.World.Items.Sand() }),
                Setting.Woodland => ("Leaf Mould", "mould", "Years of leaf fall gone down to black crumbling earth",
                    new Func<Narrative.Item>[] { () => new Narrative.World.Items.Loam() }),
                Setting.Highland or Setting.Underground => ("Clay Bank", "clay", "A cut bank of grey clay, slick where water has run down it",
                    new Func<Narrative.Item>[] { () => new Narrative.World.Items.Clay() }),
                Setting.Arid => ("Ochre Bank", "ochre", "A bank of soft red earth, the colour coming off on anything that brushes it",
                    new Func<Narrative.Item>[] { () => new Narrative.World.Items.Ochre() }),
                Setting.Jungle => ("Black Mud", "mud", "Rich black mud under the leaf litter, warm and stinking and full of roots",
                    new Func<Narrative.Item>[] { () => new Narrative.World.Items.Loam() }),
                Setting.Frozen => ("Silt Bed", "silt", "A bed of grey silt dropped by old meltwater, frozen hard a hand's depth down",
                    new Func<Narrative.Item>[] { () => new Narrative.World.Items.RockFlour() }),
                _ => ("Peat Cut", "peat", "A worked peat cut with the turves stacked to dry along its edge",
                    new Func<Narrative.Item>[] { () => new Narrative.World.Items.Peat() }),
            };

            var area = areas[rng.Next(areas.Count)];
            if (!area.PointsOfInterest.Any(p => p.DisplayName == spec.Name))
            {
                var items = new List<ItemElement>();
                for (int n = rng.Next(2, 5); n > 0; n--) items.Add(new ItemElement(spec.Yield[0]()));

                area.PointsOfInterest.Add(new DiggableGroundPointOfInterest(
                    spec.Name, spec.Lemma, new List<string> { spec.Desc }, items,
                    new[] { "worked", "dark", "damp", "crumbling" }));
            }
        }
    }

    // ── Climbing ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds a climb from <paramref name="bottom"/> up to a freshly built top area, and returns the
    /// top so the caller can put it in a section (sections must partition the areas, so only the
    /// factory can decide where it belongs).
    ///
    /// <para>Returns null when the roll says no. The top area comes back bare, and the caller must
    /// put something on it — a climb that costs a roll and arrives nowhere is what the building
    /// audit's empty-top warning exists to catch.</para>
    /// </summary>
    public static Area? AddClimb(Random rng, Scene scene, Area bottom, Setting setting, double chance = 0.5)
    {
        if (rng.NextDouble() > chance) return null;

        // The climbed-to place carries its own constructor rather than a lemma string, so the kind of
        // vantage a lesson asks about is checked by the compiler.
        (ScaleKind Kind, string TopName, string TopLemma,
         System.Func<string, string, string, List<string>, string[], Area> Build,
         string TopDesc, string ClimbName, string ClimbDesc) spec =
            setting switch
            {
                Setting.Settlement => (ScaleKind.Wall, "Rooftop", "roof",
                    (n, c, t, d, m) => new RoofArea(n, c, t, d, m),
                    "A pitch of thatch and battens with the whole street laid out below it",
                    "House Wall", "A wall of coursed stone with gaps enough for fingers and boot-toes, going up to the eaves"),
                Setting.Farmland => (ScaleKind.Stack, "Barn Loft", "loft",
                    (n, c, t, d, m) => new LoftArea(n, c, t, d, m),
                    "The open floor above the threshing bay, half-filled with last year's straw",
                    "Loft Ladder", "A ladder of pegged rungs going up into the dark under the roof"),
                Setting.Woodland => (ScaleKind.Tree, "Canopy", "canopy",
                    (n, c, t, d, m) => new CanopyArea(n, c, t, d, m),
                    "A fork high in a great tree, wide enough to sit in, with the wood spread out beneath",
                    "Great Oak", "An oak thick enough that the lowest limbs are already above head height"),
                Setting.Water => (ScaleKind.Wall, "Headland", "headland",
                    (n, c, t, d, m) => new HeadlandArea(n, c, t, d, m),
                    "A shelf of turf at the top of the rock, with the whole bay opened out below",
                    "Rock Stair", "A break in the rock where the strata step up, wet most of the way"),
                Setting.Jungle => (ScaleKind.Tree, "Canopy", "canopy",
                    (n, c, t, d, m) => new CanopyArea(n, c, t, d, m),
                    "A crotch high in a kapok, among bromeliads and ants, with the green roof of the jungle all round",
                    "Kapok Trunk", "A kapok whose spines have worn to knobs low down, and whose lianas make a ladder above"),
                Setting.Underground => (ScaleKind.Ladder, "Upper Gallery", "gallery",
                    (n, c, t, d, m) => new GalleryArea(n, c, t, d, m),
                    "A worked-out gallery above the main level, its floor the roof of the workings below",
                    "Timber Stage", "A stage of props and planks going up the shaft wall, creaking under nothing at all"),
                _ => (ScaleKind.Wall, "Crag Head", "crag",
                    (n, c, t, d, m) => new CragArea(n, c, t, d, m),
                    "A bare shoulder of rock standing clear of everything around it",
                    "Rock Step", "A run of blocky rock, holds good but the whole of it leaning outward"),
            };

        var top = spec.Build(
            spec.TopName,
            $"up on the {spec.TopLemma}",
            $"pull yourself onto the {spec.TopLemma}",
            new List<string> { spec.TopDesc },
            new[] { "airy", "exposed", "quiet", "high" });

        new ScalePointOfInterest(
            bottom, top, spec.Kind, spec.ClimbName,
            new List<string> { spec.ClimbDesc },
            new[] { "sheer", "weathered", "hand-worn" })
        {
            Senses = SensoryProfile.Examinable,
            VerbModiMentis = new Dictionary<string, string> { ["examine"] = "architecture" },
        }.AttachTo(scene);

        return top;
    }

    /// <summary>
    /// Adds a giant tree to <paramref name="area"/> — a trunk big enough to climb, and a crown to
    /// climb to. Returns the crown, or null when the roll says no.
    ///
    /// <para>The crown is the forest's viewpoint: from up there the rest of the wood is laid out, and
    /// the factory hangs a landscape per area on it. On the ground a forest is the least legible
    /// place in the game — everything looks like more forest — so the tree is worth the climb in a
    /// way a barn loft is not.</para>
    /// </summary>
    public static Area? AddGiantTree(Random rng, Scene scene, Area area, double chance = 0.5)
    {
        if (rng.NextDouble() > chance) return null;

        var crown = new CrownArea(
            displayName:           "Giant Tree Crown",
            contextDescription:    "up in the crown of the giant tree",
            transitionDescription: "haul yourself into the crown",
            descriptions:          new List<string>
            {
                "A platform of limbs near the top of a tree far older than the wood around it, "
                + "with the whole forest laid out below the leaves",
            },
            moods: new[] { "airy", "swaying", "green-lit", "high" });

        new ScalePointOfInterest(
            area, crown, ScaleKind.Tree, "Giant Tree",
            new List<string>
            {
                "A trunk too wide for three people to reach round, its bark broken into holds the "
                + "whole way up to where the limbs begin",
            },
            new[] { "vast", "mossed", "ancient" })
        {
            Senses = SensoryProfile.Examinable,
            VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft" },
        }.AttachTo(scene);

        return crown;
    }

}
