using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Glyph.Microworld;

namespace Cathedral.Game.Scene.Shared;

/// <summary>The outdoor plan of a town or city: its squares, streets, alleys and gate.</summary>
public sealed class CityPlan
{
    public List<Area> Squares { get; } = new();
    public List<Area> Streets { get; } = new();
    public List<Area> Alleys { get; } = new();
    public Area? Gateway { get; set; }

    /// <summary>The town's one outdoor section. Added to the scene; the caller registers it.</summary>
    public Section Section { get; set; } = null!;

    /// <summary>Every outdoor area, squares first.</summary>
    public List<Area> All => Squares.Concat(Streets).Concat(Alleys).Concat(Gateway != null ? new[] { Gateway } : Array.Empty<Area>()).ToList();

    /// <summary>Where a building's door may open: streets and squares mostly, alleys for the meaner ones.</summary>
    public List<Area> Frontages => Streets.Concat(Squares).ToList();
}

/// <summary>
/// Lays out the streets of a town or city: squares as its hubs, streets running between and off them,
/// alleys behind the streets, and a gateway where the road comes in.
///
/// <para><b>Two densities.</b> <see cref="Build"/> is the small town that stands round a great building
/// (a citadel, a port, a palace, a school) - one or two squares and a handful of streets, so the great
/// building stays the thing a visitor came for. <see cref="BuildDense"/> is a city cell proper: about
/// twice as many ways, every one of them lined with doors (<c>CitySceneFactory</c>). The small town
/// keeps the counts it was designed at - the name pools grew for the dense city, and
/// <see cref="ClassicStreetCap"/> and <see cref="ClassicAlleyCap"/> are what stop that growth leaking
/// into it.</para>
///
/// <para><b>A graph, not a grid.</b> A square is joined to the streets around it; consecutive
/// squares are joined through a street, so there is always a way across town that is not only one
/// square; an alley hangs off a single street, which is what makes it an alley. Every join is a
/// path with its graph edge, through <see cref="OutdoorLayout.Link"/>, so <c>--building-audit</c>
/// reads a city exactly as it reads a village.</para>
///
/// <para><b>The climate names and dresses it.</b> A plain town has a market square and a high street;
/// a mountain one climbs on stairs and terraces; a hot-steppe one has a bazaar, a well court and
/// shaded lanes; a cold-steppe one a fair ground and a sledge way. The buildings standing on the
/// streets are the caller's, built in <see cref="Building.BuildingDescriptions.MaterialsOf"/>.</para>
/// </summary>
public static class CityLayout
{
    private sealed record Way(string Name, string Description, string[] Moods);

    /// <summary>
    /// How many streets and alleys the small town round a great building may have, per style: the
    /// size of each pool before the dense city enlarged it. Plain had five streets, the others four;
    /// every style had two alleys.
    /// </summary>
    private static int ClassicStreetCap(string style) => style == "plain" ? 5 : 4;
    private const int ClassicAlleyCap = 2;

    private static readonly Dictionary<string, (Way[] Squares, Way[] Streets, Way[] Alleys)> Styles = new()
    {
        ["plain"] = (
            new[]
            {
                new Way("Market Square", "A wide cobbled square ringed with gabled fronts, stalls set out under its arcades", new[] { "busy", "loud", "wide" }),
                new Way("Guildhall Square", "A square before a tall guildhall, its steps crowded with men in good cloth", new[] { "proud", "civic", "bustling" }),
                new Way("Haymarket", "A rough square where the carts of hay and straw stand unhitched, horses dozing", new[] { "dusty", "trampled", "rustic" }),
                new Way("Cross Square", "A small square round a stone market cross, steps worn hollow by sitters", new[] { "old", "worn", "gathering" }),
            },
            new[]
            {
                new Way("High Street", "The town's main street, broad enough for two carts, shopfronts down both sides", new[] { "broad", "busy", "rutted" }),
                new Way("Tanners' Row", "A street of tanneries and their pits, the stink of it hanging in the air", new[] { "rank", "wet", "working" }),
                new Way("Bridge Street", "A street running down to the river crossing, inns along it for travellers", new[] { "trafficked", "noisy", "damp" }),
                new Way("Chandlers' Lane", "A lane of candle-makers and grocers, smelling of tallow and spice", new[] { "close", "fragrant", "narrow" }),
                new Way("Shambles", "The butchers' street, its gutter red and its stalls hung with carcasses", new[] { "bloody", "loud", "rank" }),
                new Way("Cordwainers' Street", "A street of shoemakers, boots hung by the dozen outside every door", new[] { "leathery", "busy", "hammering" }),
                new Way("Church Street", "A quieter street of tall houses running up to a parish church", new[] { "quiet", "respectable", "shaded" }),
                new Way("Wool Street", "A street of wool merchants' warehouses, hoists swinging bales up to the lofts", new[] { "creaking", "busy", "dusty" }),
                new Way("Pottergate", "A sloping street of potters' yards, kiln smoke drifting over the roofs", new[] { "smoky", "sloping", "dusty" }),
                new Way("Fish Street", "A narrow street running down to the fish stalls, wet underfoot and loud with gulls", new[] { "wet", "briny", "loud" }),
                new Way("Mercers' Row", "A neat row of cloth and silk shops with glazed windows and bowing shopmen", new[] { "neat", "genteel", "glazed" }),
                new Way("Sheep Street", "A street the flocks are driven down on market day, its cobbles fouled and slick", new[] { "slick", "noisy", "trampled" }),
            },
            new[]
            {
                new Way("Cutpurse Alley", "A dark alley behind the high street, barely wider than a man", new[] { "dark", "narrow", "watchful" }),
                new Way("Midden Wynd", "A wynd behind the houses where the night soil is thrown", new[] { "foul", "narrow", "dim" }),
                new Way("Ropers' Court", "A cramped court of rope-walks and washing lines, overlooked on every side", new[] { "cramped", "overlooked", "damp" }),
                new Way("Dark Entry", "A covered passage under a house, black at midday and dripping", new[] { "black", "dripping", "covered" }),
                new Way("Gropers' Lane", "A crooked lane of doorways where nobody walks alone after dark", new[] { "crooked", "furtive", "shadowed" }),
                new Way("Dog Leg", "A passage that turns twice between blank walls, a dog barking somewhere in it", new[] { "turning", "blank", "barking" }),
            }),
        ["mountain"] = (
            new[]
            {
                new Way("Upper Terrace", "A terrace cut into the mountainside, a parapet on its open side and the valley below", new[] { "high", "windy", "proud" }),
                new Way("Fountain Court", "A small walled court around a spring led down from the snows", new[] { "cold", "splashing", "enclosed" }),
                new Way("Mule Yard", "A walled yard where the mule-trains are unloaded, packs stacked against the wall", new[] { "dusty", "crowded", "braying" }),
                new Way("Bell Terrace", "A narrow terrace under a bell tower, the whole town spread below it", new[] { "high", "airy", "echoing" }),
            },
            new[]
            {
                new Way("Stair Street", "A street that is mostly stairs, climbing between tall stone houses", new[] { "steep", "stepped", "echoing" }),
                new Way("Masons' Climb", "A steep way of masons' yards, chisels ringing on every side", new[] { "steep", "dusty", "ringing" }),
                new Way("Lower Road", "The road along the foot of the town, where the mule-trains unload", new[] { "dusty", "busy", "level" }),
                new Way("Bellfounders' Way", "A street of foundries, their chimneys smoking into the thin air", new[] { "smoky", "hot", "loud" }),
                new Way("Switchback", "A street that zigzags up the slope in hairpin turns, carts groaning on it", new[] { "steep", "winding", "groaning" }),
                new Way("Quarry Road", "A road of pale dust that runs up to the town quarry, stone carts going both ways", new[] { "pale", "dusty", "rumbling" }),
                new Way("Wool Steps", "A broad flight of steps lined with weavers' doors, looms clacking inside", new[] { "stepped", "clacking", "busy" }),
                new Way("Gorge Street", "A street along the lip of the gorge, a low wall the only thing between it and the drop", new[] { "giddy", "windy", "narrow" }),
                new Way("Smiths' Ledge", "A ledge of forges cut into the rock, sparks flying out over the void", new[] { "hot", "ringing", "precarious" }),
                new Way("Cheese Row", "A cool street of cheese cellars dug into the mountain, wheels stacked in the doors", new[] { "cool", "pungent", "dim" }),
                new Way("Saint's Climb", "A pilgrim's way of steps and wayside shrines winding up to a chapel", new[] { "devout", "steep", "candlelit" }),
                new Way("Timber Slide", "A steep street greased for sliding logs down from the forest above", new[] { "slick", "steep", "resinous" }),
            },
            new[]
            {
                new Way("Goat Steps", "A crooked flight of steps between walls, too narrow for anything but goats and boys", new[] { "narrow", "crooked", "dim" }),
                new Way("Drain Passage", "A passage beside the town's open drain, slick and shadowed", new[] { "slick", "dark", "cold" }),
                new Way("Rock Cut", "A cleft cut through a spur of rock, just wide enough to pass sideways", new[] { "tight", "cold", "echoing" }),
                new Way("Cistern Stair", "A dank stair down to an old cistern, moss on every step", new[] { "dank", "mossy", "descending" }),
                new Way("Overhang", "A passage beneath an overhang of rock, houses built right into it", new[] { "sheltered", "dim", "dripping" }),
                new Way("Snow Gully", "A shaded gully between houses where the old snow never quite melts", new[] { "cold", "shaded", "icy" }),
            }),
        [BiomeDatabase.HotSteppe] = (
            new[]
            {
                new Way("Bazaar", "A covered bazaar of awnings and stalls, shouts and smells in every direction", new[] { "crowded", "shaded", "loud" }),
                new Way("Well Court", "A courtyard round a deep well, women queuing with jars in the shade of a wall", new[] { "shaded", "patient", "cool" }),
                new Way("Camel Square", "A dusty square where the caravans kneel their camels, drovers shouting prices", new[] { "dusty", "loud", "pungent" }),
                new Way("Date Palm Court", "A court shaded by tall date palms, a fountain trickling in the middle", new[] { "shaded", "tranquil", "green" }),
            },
            new[]
            {
                new Way("Spice Lane", "A lane of spice-sellers, their sacks open and heaped in red and yellow", new[] { "fragrant", "colourful", "narrow" }),
                new Way("Caravan Road", "The broad road the caravans come in by, camels kneeling along it", new[] { "dusty", "broad", "busy" }),
                new Way("Potters' Lane", "A lane of kilns and drying racks, the walls cracked with heat", new[] { "hot", "dusty", "working" }),
                new Way("Dyers' Street", "A street of dye vats, the gutters running blue and red", new[] { "stained", "rank", "colourful" }),
                new Way("Coppersmiths' Souk", "A covered street of coppersmiths, the hammering never stopping", new[] { "deafening", "shaded", "gleaming" }),
                new Way("Carpet Street", "A street where carpets hang from every balcony to be seen and sold", new[] { "colourful", "shaded", "haggling" }),
                new Way("Saddlers' Lane", "A lane of saddlers and harness-makers, leather smell thick in the heat", new[] { "leathery", "hot", "busy" }),
                new Way("Perfumers' Street", "A narrow street of perfume-sellers, their stoppered bottles glinting", new[] { "fragrant", "dim", "genteel" }),
                new Way("Water Street", "A street along the town's open channel, water-sellers filling skins at the steps", new[] { "wet", "busy", "cooler" }),
                new Way("Goldsmiths' Row", "A guarded row of goldsmiths' booths behind iron grilles", new[] { "guarded", "glittering", "hushed" }),
                new Way("Slipper Street", "A street of slipper-makers, embroidered shoes heaped on every step", new[] { "colourful", "crowded", "soft" }),
                new Way("Mud Brick Row", "A street of mud-brick houses with blank walls and small high windows", new[] { "blank", "hot", "quiet" }),
            },
            new[]
            {
                new Way("Covered Passage", "A vaulted passage between houses, cool and dark after the glare", new[] { "cool", "dark", "vaulted" }),
                new Way("Blind Lane", "A lane that turns twice and ends at a blank wall", new[] { "narrow", "blank", "quiet" }),
                new Way("Shadow Lane", "A lane so narrow the roofs nearly meet overhead, always in shade", new[] { "shaded", "narrow", "close" }),
                new Way("Dust Court", "A small forgotten court half full of drifted dust and broken jars", new[] { "dusty", "forgotten", "broken" }),
                new Way("Scorpion Alley", "A rubbish-strewn alley where nobody sits down without looking first", new[] { "strewn", "wary", "hot" }),
                new Way("Lattice Lane", "A lane overlooked by latticed windows, from behind which someone is always watching", new[] { "watched", "narrow", "latticed" }),
            }),
        [BiomeDatabase.ColdSteppe] = (
            new[]
            {
                new Way("Fair Ground", "A trampled open ground where the furs are sold at the fairs, empty between them", new[] { "open", "muddy", "cold" }),
                new Way("Kremlin Yard", "The yard before the town's log stockade, a bell on a frame at its centre", new[] { "stockaded", "cold", "watchful" }),
                new Way("Horse Market", "A frozen square of horse-traders, steaming beasts tethered in rows", new[] { "frozen", "steaming", "loud" }),
                new Way("Church Green", "A snowy green round an onion-domed wooden church", new[] { "snowy", "domed", "quiet" }),
            },
            new[]
            {
                new Way("Sledge Way", "The broad way the sledges come in by, rutted deep and frozen hard", new[] { "rutted", "frozen", "broad" }),
                new Way("Fur Row", "A row of fur-traders' houses, pelts hung out on poles", new[] { "smelly", "rich", "cold" }),
                new Way("Plank Street", "A street paved with split logs laid crosswise over the mud", new[] { "planked", "muddy", "creaking" }),
                new Way("Smithy Lane", "A lane of smiths, the only warm street in the town", new[] { "warm", "loud", "smoky" }),
                new Way("Bathhouse Street", "A street of steaming bathhouses, birch switches piled at the doors", new[] { "steaming", "birch-scented", "wet" }),
                new Way("Salt Row", "A row of salt-fish and salt-meat sellers, barrels stacked against every wall", new[] { "salty", "briny", "busy" }),
                new Way("Carvers' Street", "A street of woodcarvers, every gable and shutter cut into lace", new[] { "carved", "fragrant", "quiet" }),
                new Way("Tar Street", "A street of tar-boilers, black smoke rolling over the snow", new[] { "smoky", "black", "acrid" }),
                new Way("Candle Row", "A row of chandlers' shops lit warm against the early dark", new[] { "warm", "greasy", "lit" }),
                new Way("Ice Road", "A street running down to the frozen river, sledges sliding out onto the ice", new[] { "icy", "broad", "glittering" }),
                new Way("Bell Street", "A street of bellmakers and tinkers, small bells hung jangling at every door", new[] { "jangling", "busy", "cold" }),
                new Way("Bark Street", "A street of birch-bark weavers and basket-makers, shavings in the slush", new[] { "slushy", "busy", "fragrant" }),
            },
            new[]
            {
                new Way("Woodpile Gap", "A gap between stacked firewood walls taller than a man", new[] { "narrow", "resinous", "dim" }),
                new Way("Frozen Ditch", "A ditch-side way along the town's frozen sewer", new[] { "frozen", "foul", "slippery" }),
                new Way("Drift Lane", "A lane half buried in a snowdrift, a trench dug through it", new[] { "buried", "cold", "narrow" }),
                new Way("Soot Passage", "A passage behind the smithies, black with soot and warm underfoot", new[] { "sooty", "warm", "dark" }),
                new Way("Icicle Row", "A narrow way under eaves hung with icicles as long as spears", new[] { "dripping", "glittering", "dangerous" }),
                new Way("Dog Kennel Lane", "A lane of sledge-dog kennels, the howling never quite stopping", new[] { "howling", "rank", "narrow" }),
            }),
    };

    /// <summary>The style key for a biome: its own when it has one, plain otherwise.</summary>
    private static string StyleOf(string biome) => Styles.ContainsKey(biome) ? biome : "plain";

    /// <summary>
    /// The small town round a great building, of the given size (1 small .. 3 large), in the style of
    /// <paramref name="biome"/>: one or two squares, two to six streets, one or two alleys. Built as
    /// one section named <paramref name="sectionName"/>, added to the scene and joined up. The caller
    /// registers <see cref="CityPlan.Section"/> (factories hold the registration door).
    /// </summary>
    public static CityPlan Build(Scene scene, Random rng, string biome, int size, string sectionName)
    {
        string style = StyleOf(biome);
        int squares = size >= 3 ? 2 : 1;
        int streets = Math.Clamp(2 + size + rng.Next(0, 2), 2, ClassicStreetCap(style));
        int alleys  = Math.Clamp(size - 1 + rng.Next(0, 2), 1, ClassicAlleyCap);
        return Lay(scene, rng, biome, style, squares, streets, alleys, sectionName);
    }

    /// <summary>
    /// A city cell proper, of the given size (1 .. 3): two or three squares, seven to twelve streets,
    /// two to five alleys and the gate - about thirteen, seventeen or twenty ways. Otherwise as
    /// <see cref="Build"/>.
    /// </summary>
    public static CityPlan BuildDense(Scene scene, Random rng, string biome, int size, string sectionName)
    {
        string style = StyleOf(biome);
        var pools = Styles[style];
        int squares = Math.Min(size >= 2 ? 3 : 2, pools.Squares.Length);
        int streets = Math.Min(5 + 2 * size + rng.Next(0, 2), pools.Streets.Length);
        int alleys  = Math.Min(1 + size + rng.Next(0, 2), pools.Alleys.Length);
        return Lay(scene, rng, biome, style, squares, streets, alleys, sectionName);
    }

    private static CityPlan Lay(Scene scene, Random rng, string biome, string styleKey,
                                int squares, int streets, int alleys, string sectionName)
    {
        var style = Styles[styleKey];
        var plan = new CityPlan();

        foreach (int i in Pick(rng, style.Squares.Length, squares))
            plan.Squares.Add(Make(style.Squares[i], (n, c, t, d, m) => new SquareArea(n, c, t, d, m), "in"));
        foreach (int i in Pick(rng, style.Streets.Length, streets))
            plan.Streets.Add(Make(style.Streets[i], (n, c, t, d, m) => new StreetArea(n, c, t, d, m), "on"));
        foreach (int i in Pick(rng, style.Alleys.Length, alleys))
            plan.Alleys.Add(Make(style.Alleys[i], (n, c, t, d, m) => new AlleyArea(n, c, t, d, m), "in"));
        plan.Gateway = Make(new Way("Town Gate", "The gate where the road comes in, a toll-keeper's window beside the arch", new[] { "watched", "busy", "arched" }),
                            (n, c, t, d, m) => new GatewayArea(n, c, t, d, m), "at");

        foreach (var a in plan.All) Dress(a, biome, rng);

        var section = new Section(sectionName, new() { "Streets and squares crowded between walls, the noise of a town on every side" },
                                  seed => new Cathedral.Fight.Generators.GeometricGenerator { Seed = seed });
        section.Areas.AddRange(plan.All);
        scene.Sections.Add(section);
        plan.Section = section;

        // Squares to the streets around them, consecutive squares through a shared street.
        for (int i = 0; i < plan.Streets.Count; i++)
        {
            var square = plan.Squares[i % plan.Squares.Count];
            OutdoorLayout.Link(scene, square, plan.Streets[i], "Way");
        }
        for (int k = 1; k < plan.Squares.Count && k - 1 < plan.Streets.Count; k++)
            if ((k - 1) % plan.Squares.Count != k)
                OutdoorLayout.Link(scene, plan.Streets[k - 1], plan.Squares[k], "Way");
        // Streets end to end, so the town is not only a star round its square.
        for (int i = 0; i + 1 < plan.Streets.Count; i += 2)
            OutdoorLayout.Link(scene, plan.Streets[i], plan.Streets[i + 1], "Way");
        // An alley hangs off one street.
        foreach (var alley in plan.Alleys)
            OutdoorLayout.Link(scene, plan.Streets[rng.Next(plan.Streets.Count)], alley, "Passage");
        // The gate opens onto the first street.
        OutdoorLayout.Link(scene, plan.Streets[0], plan.Gateway!, "Road");

        return plan;
    }

    private static Area Make(Way way, Func<string, string, string, List<string>, string[], Area> build, string preposition)
    {
        string lower = way.Name.ToLowerInvariant();
        return build(way.Name, $"{preposition} the {lower}", $"walk to the {lower}", new List<string> { way.Description }, way.Moods);
    }

    private static IEnumerable<int> Pick(Random rng, int total, int count)
    {
        var idx = Enumerable.Range(0, total).ToList();
        for (int i = 0; i < Math.Min(count, total); i++)
        {
            int j = rng.Next(idx.Count);
            yield return idx[j];
            idx.RemoveAt(j);
        }
    }

    private static ItemElement I(Item item) => new(item);

    /// <summary>A couple of things to look at in each way of the town.</summary>
    private static void Dress(Area area, string biome, Random rng)
    {
        var pois = area.PointsOfInterest;
        bool hot = biome == BiomeDatabase.HotSteppe;
        switch (area)
        {
            case SquareArea:
                pois.Add(hot
                    ? new CisternPointOfInterest("Public Cistern", new() { "A stone cistern under a domed cover, steps leading down to the cool water" },
                        new() { I(new WaterDraught()) }, new[] { "cool", "domed", "shaded" })
                        { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "drainage", ["listen"] = "water_voice" } }
                    : new FountainPointOfInterest("Fountain", new() { "A stone basin fed by a spout in a carved face, children splashing at its edge" },
                        new() { I(new WaterDraught()) }, new[] { "splashing", "carved", "central" })
                        { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "architecture", ["listen"] = "water_voice" } });
                pois.Add(new StatuePointOfInterest("Statue", new() { rng.NextDouble() < 0.5 ? "A weathered statue of a founder, his name worn off the plinth" : "A statue of a robed figure holding up a book, pigeons on its head" },
                    moods: new[] { "weathered", "proud", "pigeon-spattered" })
                    { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "archeology", ["contemplate"] = "vanitas" } });
                if (rng.NextDouble() < 0.5)
                    pois.Add(new PilloryPointOfInterest("Pillory", new() { "A pillory on a platform, its boards splashed with rotten fruit" },
                        moods: new[] { "grim", "public", "splashed" })
                        { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "wear_reading", ["contemplate"] = "severity" } });
                break;
            case StreetArea:
                pois.Add(new SignboardPointOfInterest("Hanging Signs", new() { "Painted signs creaking on iron brackets over the doors: a boot, a key, a loaf" },
                    moods: new[] { "creaking", "painted", "crowded" })
                    { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "decipher", ["listen"] = "crowd_murmur" } });
                if (rng.NextDouble() < 0.5)
                    pois.Add(new AwningPointOfInterest(hot ? "Shade Awning" : "Street Stall", new() { hot ? "A striped awning stretched across the street on poles, its shade crowded" : "A stall pushed out into the street, its owner shouting prices" },
                        new() { I(new Bread()), I(new Onion()) }, new[] { "crowded", "loud", "colourful" }, isNatural: false)
                        { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "bargaining", ["listen"] = "crowd_murmur", ["smell"] = "keen_nose", ["contemplate"] = "gregariousness" } });
                break;
            case AlleyArea:
                pois.Add(new MiddenPointOfInterest("Refuse Heap", new() { "A heap of broken crockery, bones and worse against the wall, rats in it" },
                    new() { I(new Bone()), I(new SplinteredTimber()) }, new[] { "foul", "rat-run", "dark" })
                    { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "treasure_hunting", ["smell"] = "taint_sense" } });
                break;
            case GatewayArea:
                pois.Add(new GallowsPointOfInterest("Gallows", new() { "A gallows by the gate where the town shows the road what it does to thieves" },
                    moods: new[] { "grim", "creaking", "warning" })
                    { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "severity", ["listen"] = "dread" } });
                break;
        }
    }
}
