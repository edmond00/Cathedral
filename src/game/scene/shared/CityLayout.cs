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
/// Lays out the streets of a town or city: one or two squares as its hubs, streets running between
/// and off them, alleys behind the streets, and a gateway where the road comes in.
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

    private static readonly Dictionary<string, (Way[] Squares, Way[] Streets, Way[] Alleys)> Styles = new()
    {
        ["plain"] = (
            new[]
            {
                new Way("Market Square", "A wide cobbled square ringed with gabled fronts, stalls set out under its arcades", new[] { "busy", "loud", "wide" }),
                new Way("Guildhall Square", "A square before a tall guildhall, its steps crowded with men in good cloth", new[] { "proud", "civic", "bustling" }),
            },
            new[]
            {
                new Way("High Street", "The town's main street, broad enough for two carts, shopfronts down both sides", new[] { "broad", "busy", "rutted" }),
                new Way("Tanners' Row", "A street of tanneries and their pits, the stink of it hanging in the air", new[] { "rank", "wet", "working" }),
                new Way("Bridge Street", "A street running down to the river crossing, inns along it for travellers", new[] { "trafficked", "noisy", "damp" }),
                new Way("Chandlers' Lane", "A lane of candle-makers and grocers, smelling of tallow and spice", new[] { "close", "fragrant", "narrow" }),
                new Way("Shambles", "The butchers' street, its gutter red and its stalls hung with carcasses", new[] { "bloody", "loud", "rank" }),
            },
            new[]
            {
                new Way("Cutpurse Alley", "A dark alley behind the high street, barely wider than a man", new[] { "dark", "narrow", "watchful" }),
                new Way("Midden Wynd", "A wynd behind the houses where the night soil is thrown", new[] { "foul", "narrow", "dim" }),
            }),
        ["mountain"] = (
            new[]
            {
                new Way("Upper Terrace", "A terrace cut into the mountainside, a parapet on its open side and the valley below", new[] { "high", "windy", "proud" }),
                new Way("Fountain Court", "A small walled court around a spring led down from the snows", new[] { "cold", "splashing", "enclosed" }),
            },
            new[]
            {
                new Way("Stair Street", "A street that is mostly stairs, climbing between tall stone houses", new[] { "steep", "stepped", "echoing" }),
                new Way("Masons' Climb", "A steep way of masons' yards, chisels ringing on every side", new[] { "steep", "dusty", "ringing" }),
                new Way("Lower Road", "The road along the foot of the town, where the mule-trains unload", new[] { "dusty", "busy", "level" }),
                new Way("Bellfounders' Way", "A street of foundries, their chimneys smoking into the thin air", new[] { "smoky", "hot", "loud" }),
            },
            new[]
            {
                new Way("Goat Steps", "A crooked flight of steps between walls, too narrow for anything but goats and boys", new[] { "narrow", "crooked", "dim" }),
                new Way("Drain Passage", "A passage beside the town's open drain, slick and shadowed", new[] { "slick", "dark", "cold" }),
            }),
        [BiomeDatabase.HotSteppe] = (
            new[]
            {
                new Way("Bazaar", "A covered bazaar of awnings and stalls, shouts and smells in every direction", new[] { "crowded", "shaded", "loud" }),
                new Way("Well Court", "A courtyard round a deep well, women queuing with jars in the shade of a wall", new[] { "shaded", "patient", "cool" }),
            },
            new[]
            {
                new Way("Spice Lane", "A lane of spice-sellers, their sacks open and heaped in red and yellow", new[] { "fragrant", "colourful", "narrow" }),
                new Way("Caravan Road", "The broad road the caravans come in by, camels kneeling along it", new[] { "dusty", "broad", "busy" }),
                new Way("Potters' Lane", "A lane of kilns and drying racks, the walls cracked with heat", new[] { "hot", "dusty", "working" }),
                new Way("Dyers' Street", "A street of dye vats, the gutters running blue and red", new[] { "stained", "rank", "colourful" }),
            },
            new[]
            {
                new Way("Covered Passage", "A vaulted passage between houses, cool and dark after the glare", new[] { "cool", "dark", "vaulted" }),
                new Way("Blind Lane", "A lane that turns twice and ends at a blank wall", new[] { "narrow", "blank", "quiet" }),
            }),
        [BiomeDatabase.ColdSteppe] = (
            new[]
            {
                new Way("Fair Ground", "A trampled open ground where the furs are sold at the fairs, empty between them", new[] { "open", "muddy", "cold" }),
                new Way("Kremlin Yard", "The yard before the town's log stockade, a bell on a frame at its centre", new[] { "stockaded", "cold", "watchful" }),
            },
            new[]
            {
                new Way("Sledge Way", "The broad way the sledges come in by, rutted deep and frozen hard", new[] { "rutted", "frozen", "broad" }),
                new Way("Fur Row", "A row of fur-traders' houses, pelts hung out on poles", new[] { "smelly", "rich", "cold" }),
                new Way("Plank Street", "A street paved with split logs laid crosswise over the mud", new[] { "planked", "muddy", "creaking" }),
                new Way("Smithy Lane", "A lane of smiths, the only warm street in the town", new[] { "warm", "loud", "smoky" }),
            },
            new[]
            {
                new Way("Woodpile Gap", "A gap between stacked firewood walls taller than a man", new[] { "narrow", "resinous", "dim" }),
                new Way("Frozen Ditch", "A ditch-side way along the town's frozen sewer", new[] { "frozen", "foul", "slippery" }),
            }),
    };

    /// <summary>The style key for a biome: its own when it has one, plain otherwise.</summary>
    private static string StyleOf(string biome) => Styles.ContainsKey(biome) ? biome : "plain";

    /// <summary>
    /// Builds the outdoor plan of a town of the given size (1 small .. 3 large) in the style of
    /// <paramref name="biome"/>, as one section named <paramref name="sectionName"/>, adds it to the
    /// scene and joins its ways up. The caller registers <see cref="CityPlan.Section"/> (factories hold
    /// the registration door).
    /// </summary>
    public static CityPlan Build(Scene scene, Random rng, string biome, int size, string sectionName)
    {
        var style = Styles[StyleOf(biome)];
        var plan = new CityPlan();

        int squares = size >= 3 ? 2 : 1;
        int streets = Math.Clamp(2 + size + rng.Next(0, 2), 2, style.Streets.Length);
        int alleys = Math.Clamp(size - 1 + rng.Next(0, 2), 1, style.Alleys.Length);

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
        if (plan.Squares.Count > 1)
            OutdoorLayout.Link(scene, plan.Streets[0], plan.Squares[1], "Way");
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
