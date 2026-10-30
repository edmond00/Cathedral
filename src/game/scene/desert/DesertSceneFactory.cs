using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Shared;

namespace Cathedral.Game.Scene.Desert;

/// <summary>
/// Builds a desert — what plain and field become in hot country (see <c>ClimateRule</c>).
///
/// Identity: erg (a sea of sand), reg (stony desert) or pan (a dry salt lake) — gates the open areas.
/// Sections: Open Desert (dunes, gravel, salt) and Low Ground (the wadi, and sometimes an oasis).
/// Climb: the slip face of a great dune, the only way to its crest, which sees the whole place.
/// Uninhabited: no settlement stands in hot country yet.
/// </summary>
public class DesertSceneFactory : WildSceneFactory
{
    public DesertSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Erg, Reg, Pan }

    private Identity _identity;
    private Area? _oasis, _wadi, _duneCrest, _summit, _windCave;
    private Area? _field, _trough, _crest, _truffles;

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // Radial: in a desert every track leads to the water, and the low ground is the hub.
        Profile = LayoutProfile.Radial;
        _identity = rng.NextDouble() switch
        {
            < 0.45 => Identity.Erg,
            < 0.80 => Identity.Reg,
            _      => Identity.Pan,
        };
        double W(double erg, double reg, double pan) => _identity switch { Identity.Erg => erg, Identity.Reg => reg, _ => pan };

        // ── The hub: the low ground ──────────────────────────────────────────
        _wadi = BuildWadi();
        var low = new List<Area> { _wadi };
        if (rng.NextDouble() < (_identity == Identity.Pan ? 0.30 : 0.50))
        {
            _oasis = BuildOasis();
            low.Add(_oasis);
            if (rng.NextDouble() < 0.5) low.Add(BuildPalmGrove());
        }
        if (rng.NextDouble() < 0.4) low.Add(BuildAcaciaScrub());

        // ── The sand sea, sized so the whole place comes to six to nine ──────
        bool hasRock = rng.NextDouble() < W(0.45, 0.75, 0.55);
        int rockCount = hasRock ? Between(rng, 1, 2) : 0;
        int openCount = Math.Clamp(Between(rng, 6, 8) - low.Count - rockCount, 2, 5);
        var open = Draw(rng, new Option[]
        {
            new(BuildDuneField,   W(3.0, 0.6, 0.8)),
            new(BuildDuneTrough,  W(2.0, 1.0, 0.5)),
            new(BuildStonyDesert, W(0.6, 3.0, 1.2)),
            new(BuildSaltPan,     W(0.3, 0.8, 3.0)),
            new(BuildTruffleGround, 0.8),
            new(BuildBuriedRuin, 0.7),
        }, openCount);

        // ── Rock country ─────────────────────────────────────────────────────
        var rock = !hasRock ? new List<Area>() : Draw(rng, new Option[]
        {
            new(BuildInselberg, 2.0),
            new(BuildYardangs,  W(1.2, 2.0, 1.5)),
        }, rockCount);

        // A big desert is two deserts: the sand sea, and a gravel plain beside it.
        var gravel = new List<Area>();
        if (open.Count >= 3 && rng.NextDouble() < 0.45)
        {
            gravel = open.Where(a => a is not DuneArea).ToList();
            if (gravel.Count == 0 || gravel.Count == open.Count) gravel.Clear();
            else open = open.Where(a => a is DuneArea).ToList();
        }

        // The crest and the inselberg's top are climbs' tops; the wind cave a squeeze.
        _duneCrest = open.Any(a => a == _field || a == _trough) || rng.NextDouble() < 0.5 ? BuildDuneCrest() : null;
        var inselberg = rock.OfType<InselbergArea>().FirstOrDefault();
        _summit = inselberg != null && rng.NextDouble() < 0.6 ? BuildInselbergTop() : null;
        var carved = rock.FirstOrDefault();
        _windCave = carved != null && rng.NextDouble() < 0.5 && low.Count + open.Count + gravel.Count + rock.Count < 8 ? BuildWindCave() : null;

        foreach (var area in low.Concat(open).Concat(gravel).Concat(rock)) Populate(area, rng);
        foreach (var area in new[] { _duneCrest, _summit, _windCave }) if (area != null) Populate(area, rng);

        AddSection(scene, _oasis != null ? "The Oasis" : "Low Ground",
            "A dry watercourse and what grows along it; the only shade for a day in any direction",
            seed => new NoisyGenerator { Seed = seed, Density = 0.80f }, low, tier: 0);
        var openSection = AddSection(scene, _identity == Identity.Erg || gravel.Count > 0 ? "Sand Sea" : "Open Desert",
            "Sand, gravel and salt under an empty sky; no shade, and the horizon trembling",
            seed => new RadiantGenerator { Seed = seed, CentreDensity = 0.95f, EdgeDensity = 0.72f }, open, tier: 1);
        if (gravel.Count > 0)
            AddSection(scene, "Gravel Plain", "Black wind-polished stone and white salt, flat to the edge of sight",
                seed => new RadiantGenerator { Seed = seed, CentreDensity = 0.95f, EdgeDensity = 0.8f }, gravel, tier: 1);
        Section? rockSection = rock.Count == 0 ? null : AddSection(scene, "Rock Country",
            "Bare rock standing out of the sand, carved by the wind into ribs, hulls and hollows",
            seed => new WaveGenerator { Seed = seed }, rock, tier: 1);

        string[] moods = { "faint", "sand-blown", "sun-hard" };
        string Way(Area a, Area b) =>
            a == _oasis || b == _oasis ? "Palm Track"
            : a is DuneArea || b is DuneArea ? "Dune Track"
            : "Caravan Trace";

        WeaveSection(rng, scene, low, Weave.Line, Way, "along the watercourse", moods);
        WeaveSection(rng, scene, open, RollWeave(rng), Way, "across sand and gravel", moods);
        WeaveSection(rng, scene, gravel, RollWeave(rng), Way, "across sand and gravel", moods);
        WeaveSection(rng, scene, rock, Weave.Line, Way, "across the rock", moods);
        if (gravel.Count > 0)
            JoinSections(rng, scene, low, gravel, 1, Way, "across sand and gravel", moods);
        JoinSections(rng, scene, low, open, rng.NextDouble() < 0.4 ? 2 : 1, Way, "across sand and gravel", moods);
        if (rock.Count > 0)
            JoinSections(rng, scene, low, rock, 1, Way, "across sand and gravel", moods);

        // A dune's slip face is sand at the angle sand stops holding: a climb, not a walk.
        if (_duneCrest != null)
        {
            AddToSection(scene, openSection, _duneCrest);
            AddViewpoint(scene, open.FirstOrDefault(a => a is DuneArea) ?? open[0], _duneCrest, "Slip Face",
                "The steep lee face of a great dune, sand pouring down it at every step taken up it",
                new[] { "steep", "pouring", "blinding", "hot" });
        }
        if (_summit != null)
        {
            AddToSection(scene, rockSection!, _summit);
            AddViewpoint(scene, inselberg!, _summit, "Inselberg Face",
                "A dome of bare rock going up out of the sand, smooth as a skull, cracked in a few places deep enough for hands",
                new[] { "domed", "smooth", "sun-hot" }, lesson: "summit_fever");
        }
        if (_windCave != null)
        {
            AddToSection(scene, rockSection!, _windCave);
            SlipInto(scene, carved!, _windCave, SlipKind.Crevice, "Wind Hole",
                "A hole the wind has bored into the rock, the size of a body, sand drifted in its mouth and cool air inside",
                new[] { "bored", "drifted", "cool" }, lesson: "potholing");
        }

        Furnish(rng, scene, FurnitureSubfactory.Setting.Arid, _duneCrest, _summit, _windCave);

        Console.WriteLine($"DesertSceneFactory: {_identity}, oasis={_oasis != null}, {low.Count}/{open.Count}/{rock.Count} areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private Area BuildDuneField() => _field = new DuneArea(
        displayName: "Dune Field",
        contextDescription: "among the dunes",
        transitionDescription: "climb into the dunes",
        descriptions: new() { "Dunes in ranks to the horizon, their crests smoking with blown sand, every trough the same" },
        moods: new[] { "endless", "shifting", "blinding", "silent" });

    private Area BuildDuneTrough() => _trough = new DuneArea(
        displayName: "Dune Trough",
        contextDescription: "in a trough between dunes",
        transitionDescription: "slide down into the trough",
        descriptions: new() { "A hard-floored corridor between two dunes, the gravel showing, the wind cut off" },
        moods: new[] { "enclosed", "still", "hot", "hushed" });

    private static Area BuildStonyDesert() => new RegArea(
        displayName: "Stony Desert",
        contextDescription: "on the stony desert",
        transitionDescription: "step out onto the gravel plain",
        descriptions: new() { "A plain of black wind-polished pebbles packed like paving, flat to the edge of sight" },
        moods: new[] { "flat", "black", "glittering", "relentless" });

    private static Area BuildSaltPan() => new PanArea(
        displayName: "Salt Pan",
        contextDescription: "out on the salt pan",
        transitionDescription: "walk out onto the salt",
        descriptions: new() { "The white floor of a lake dead for longer than anyone has been counting, cracked into plates" },
        moods: new[] { "white", "cracked", "glaring", "dead" });

    private Area BuildTruffleGround() => _truffles = new RegArea(
        displayName: "Truffle Ground",
        contextDescription: "on the truffle ground",
        transitionDescription: "walk out onto the cracked ground",
        descriptions: new() { "A stretch of sandy ground after rain, little cracked mounds raised all over it where something swells underneath" },
        moods: new[] { "cracked", "mounded", "faintly green", "promising" });

    private static Area BuildWadi() => new WadiArea(
        displayName: "Wadi",
        contextDescription: "in the wadi",
        transitionDescription: "drop down into the wadi",
        descriptions: new() { "A dry riverbed between low banks, its sand still rippled by the last flood, whenever that was" },
        moods: new[] { "dry", "winding", "sheltered", "expectant" });

    private static Area BuildOasis() => new OasisArea(
        displayName: "Oasis",
        contextDescription: "at the oasis",
        transitionDescription: "come down into the oasis",
        descriptions: new() { "Palms crowded round a pool of green water, birds in them, the air suddenly wet" },
        moods: new[] { "green", "shaded", "loud with birds", "unreal" });

    private static Area BuildAcaciaScrub() => new ThornbrushArea(
        displayName: "Acacia Scrub",
        contextDescription: "in the acacia scrub",
        transitionDescription: "push into the acacia scrub",
        descriptions: new() { "Flat-topped acacias and thorn along the watercourse, the only shade for leagues, grey with dust" },
        moods: new[] { "thorny", "dusty", "flat-topped", "shaded" });

    private static Area BuildBuriedRuin() => new RuinArea(
        displayName: "Buried Ruin",
        contextDescription: "among the buried ruin",
        transitionDescription: "walk in among the buried walls",
        descriptions: new() { "The tops of walls and a broken arch standing out of the sand, a town or a fort the dunes have been swallowing for a thousand rounds" },
        moods: new[] { "swallowed", "broken-arched", "silent", "sand-drifted" });

    private static Area BuildPalmGrove() => new GroveArea(
        displayName: "Palm Grove",
        contextDescription: "in the palm grove",
        transitionDescription: "walk in under the palms",
        descriptions: new() { "Date palms in rows past the pool, old irrigation runnels choked with sand between them, the shade striped and deep" },
        moods: new[] { "striped", "shaded", "rustling", "abandoned" });

    private Area BuildDuneCrest() => _crest = new DuneArea(
        displayName: "Great Dune Crest",
        contextDescription: "on the crest of the great dune",
        transitionDescription: "haul yourself onto the crest",
        descriptions: new() { "A knife-edge of sand high above everything, the wind tearing a plume off it" },
        moods: new[] { "high", "knife-edged", "windswept", "vast" });

    private static Area BuildInselberg() => new InselbergArea(
        displayName: "Inselberg",
        contextDescription: "at the foot of the inselberg",
        transitionDescription: "walk in under the inselberg",
        descriptions: new() { "A lone hill of bare red rock rising sheer out of the flat, the only shadow for a day's walk round it" },
        moods: new[] { "lone", "red", "sheer", "shadow-casting" });

    private static Area BuildInselbergTop() => new CragArea(
        displayName: "Inselberg Top",
        contextDescription: "on top of the inselberg",
        transitionDescription: "pull yourself onto the top of the inselberg",
        descriptions: new() { "The rounded top of the rock, pitted with hollows that hold rainwater, the desert laid out flat to every horizon" },
        moods: new[] { "rounded", "pitted", "commanding", "baking" });

    private static Area BuildYardangs() => new YardangArea(
        displayName: "Yardangs",
        contextDescription: "among the yardangs",
        transitionDescription: "walk in among the yardangs",
        descriptions: new() { "Long ridges of rock carved by the wind into the shapes of upturned hulls, all lying the same way, the sand scoured from between them" },
        moods: new[] { "hulled", "scoured", "aligned", "moaning" });

    private static Area BuildWindCave() => new OverhangArea(
        displayName: "Wind Cave",
        contextDescription: "in the wind cave",
        transitionDescription: "squeeze into the wind cave",
        descriptions: new() { "A smooth hollow bored into the rock by the wind, cool and dim, its floor of fine drifted sand" },
        moods: new[] { "smooth", "cool", "dim", "humming" });

    // ── Spot population ──────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        var p = area.PointsOfInterest;
        if (area == _crest)
        {
            p.Add(BuildMirage());
            p.Add(BuildSandPlume());
            return;
        }
        if (area == _field)
        {
            if (rng.NextDouble() < 0.5) p.Add(BuildMirage());
            if (rng.NextDouble() < 0.4) p.Add(ClimateTerrainSubfactory.BuildBleachedBones());
            p.Add(BuildBurrow());
            return;
        }
        if (area == _trough)
        {
            p.Add(BuildDesertRoses());
            if (rng.NextDouble() < 0.5) p.Add(ClimateTerrainSubfactory.BuildCactus());
            return;
        }
        if (area == _truffles)
        {
            p.Add(BuildTruffleMounds());
            if (rng.NextDouble() < 0.5) p.Add(ClimateTerrainSubfactory.BuildThornBush());
            return;
        }
        switch (area)
        {
            case RegArea:
                p.Add(BuildVentifact());
                if (rng.NextDouble() < 0.6) p.Add(ClimateTerrainSubfactory.BuildMyrrhBush());
                if (rng.NextDouble() < 0.3) p.Add(ClimateTerrainSubfactory.BuildStele("half-buried in the gravel and leaning"));
                if (rng.NextDouble() < 0.4) p.Add(ClimateTerrainSubfactory.BuildBleachedBones());
                break;
            case PanArea:
                p.Add(BuildSaltCrust());
                p.Add(BuildMirage());
                break;
            case WadiArea:
                p.Add(ClimateTerrainSubfactory.BuildAcacia());
                p.Add(ClimateTerrainSubfactory.BuildThornBush());
                if (rng.NextDouble() < 0.5) p.Add(ClimateTerrainSubfactory.BuildAloe());
                if (rng.NextDouble() < 0.5) p.Add(BuildBurrow());
                break;
            case OasisArea:
                p.Add(ClimateTerrainSubfactory.BuildDatePalm());
                p.Add(BuildSpring());
                p.Add(TerrainSubfactory.BuildReedBed());
                if (rng.NextDouble() < 0.5) p.Add(ClimateTerrainSubfactory.BuildAloe());
                break;
            case ThornbrushArea:
                p.Add(ClimateTerrainSubfactory.BuildAcacia());
                p.Add(ClimateTerrainSubfactory.BuildThornBush());
                break;
            case RuinArea:
                p.Add(ClimateTerrainSubfactory.BuildStele("standing out of the drift, its face scoured smooth"));
                p.Add(BuildBuriedWall());
                break;
            case GroveArea:
                p.Add(ClimateTerrainSubfactory.BuildDatePalm());
                p.Add(ClimateTerrainSubfactory.BuildDatePalm());
                break;
            case InselbergArea:
                p.Add(ClimateTerrainSubfactory.BuildPetroglyphs());
                p.Add(BuildGnamma());
                break;
            case CragArea:
                p.Add(BuildRockHollows());
                break;
            case YardangArea:
                p.Add(BuildCarvedRidges());
                if (rng.NextDouble() < 0.4) p.Add(ClimateTerrainSubfactory.BuildBleachedBones());
                break;
            case OverhangArea:
                p.Add(BuildDriftedSand());
                break;
        }
    }

    private static PointOfInterest BuildTruffleMounds() => new MoundPointOfInterest(
        displayName: "Cracked Mounds",
        descriptions: new() { "Little mounds of sand split across the top, something pale showing in the cracks underneath" },
        items: new() { new ItemElement(new DesertTruffle()), new ItemElement(new DesertTruffle()) },
        moods: new[] { "split", "pale-showing", "small", "promising" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "truffling", ["smell"] = "mycology" } };

    private static PointOfInterest BuildBuriedWall() => new RubblePointOfInterest(
        displayName: "Buried Wall",
        descriptions: new() { "A course of dressed stone running into the dune, a doorway's lintel just showing, sherds of glazed pottery in the sand at its foot" },
        items: new() { new ItemElement(new Potsherd()), new ItemElement(new Sandstone()) },
        moods: new[] { "dressed", "swallowed", "lintelled", "patient" },
        isNatural: false
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "archeology", ["contemplate"] = "ruin_sense" } };

    private static PointOfInterest BuildGnamma() => new PoolPointOfInterest(
        displayName: "Rock Pool",
        descriptions: new() { "A hole in the rock at the inselberg's foot holding a skin of green rainwater, a trail of tracks leading to it" },
        items: new() { new ItemElement(new Rock()) },
        moods: new[] { "green", "hidden", "tracked", "precious" },
        isNatural: true
    ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "dowsing" } };

    private static PointOfInterest BuildRockHollows() => new PoolPointOfInterest(
        displayName: "Weathering Pits",
        descriptions: new() { "Round pits worn into the rock of the top, a few holding rainwater with tiny shrimp darting in it" },
        items: new() { new ItemElement(new Rock()) },
        moods: new[] { "round", "rain-holding", "pitted", "alive" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "dowsing", ["contemplate"] = "awe" } };

    private static PointOfInterest BuildCarvedRidges() => new RockPointOfInterest(
        displayName: "Wind-Carved Ridges",
        descriptions: new() { "The ridges up close: fluted, undercut, polished on the windward end, moaning faintly when the wind blows" },
        items: new() { new ItemElement(new Sandstone()), new ItemElement(new Flint()) },
        moods: new[] { "fluted", "polished", "moaning", "undercut" },
        isNatural: true
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "sandcraft", ["listen"] = "wind_reading" } };

    private static PointOfInterest BuildDriftedSand() => new HollowPointOfInterest(
        displayName: "Drifted Floor",
        descriptions: new() { "A floor of fine sand drifted into ripples by the wind, the bones of some small animal half-buried in it" },
        items: new() { new ItemElement(new Bone()), new ItemElement(new Sandstone()) },
        moods: new[] { "rippled", "fine", "cool", "hushed" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "sandcraft", ["contemplate"] = "meditation" } };

    private static PointOfInterest BuildMirage() => new MiragePointOfInterest(
        displayName: "Mirage",
        descriptions: new() { "A sheet of bright water lying across the horizon, a line of trees standing in it upside down" },
        items: new(),
        moods: new[] { "trembling", "bright", "false", "beckoning" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "thermodynamics", ["contemplate"] = "dreamlore" } };

    private static PointOfInterest BuildSandPlume() => new CrustPointOfInterest(
        displayName: "Wind-Carved Crest",
        descriptions: new() { "The very crest of the dune, honed by the wind to an edge, sand streaming off it in a thin smoke" },
        items: new() { new ItemElement(new Sand()), new ItemElement(new Sand()) },
        moods: new[] { "honed", "streaming", "hissing", "high" }
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "wind_reading", ["listen"] = "weather_ear", ["contemplate"] = "awe" } };

    private static PointOfInterest BuildBurrow() => new BurrowPointOfInterest(
        displayName: "Burrow Mouths",
        descriptions: new() { "A cluster of small burrows in the firmer sand, tracks like stitching running between them" },
        items: new() { new ItemElement(new WildMillet()) },
        moods: new[] { "small", "busy", "stitched with tracks", "hidden" }
    ) { Senses = SensoryProfile.Audible, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "spoor_reading", ["listen"] = "keen_ear" } };

    private static PointOfInterest BuildDesertRoses() => new CrystalPointOfInterest(
        displayName: "Desert Roses",
        descriptions: new() { "Clusters of sand-coloured crystal blades weathering out of the trough floor, each grown into a flower" },
        items: new() { new ItemElement(new DesertRose()), new ItemElement(new DesertRose()), new ItemElement(new Sand()) },
        moods: new[] { "crystalline", "petalled", "brittle", "strange" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "veinsight", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildVentifact() => new RockPointOfInterest(
        displayName: "Wind-Scoured Rock",
        descriptions: new() { "A rock the wind has sanded into facets and flutes, polished on its windward side like glass" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new Flint()), new ItemElement(new Jasper()) },
        moods: new[] { "faceted", "polished", "fluted", "hot" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildSaltCrust() => new CrustPointOfInterest(
        displayName: "Salt Crust",
        descriptions: new() { "Plates of white salt crust curled up at their edges like old paint, soft grey mud beneath" },
        items: new() { new ItemElement(new Natron()), new ItemElement(new Natron()), new ItemElement(new Salt()) },
        moods: new[] { "white", "curling", "brittle", "bitter" }
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "thermodynamics", ["smell"] = "brine_sense" } };

    private static PointOfInterest BuildSpring() => new SpringPointOfInterest(
        displayName: "Spring Pool",
        descriptions: new() { "Clear water welling up through white sand at the pool's head, the sand dancing in it" },
        items: new() { new ItemElement(new Clay()), new ItemElement(new Reed()) },
        moods: new[] { "clear", "welling", "cool", "precious" }
    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string>
        { ["examine"] = "drainage", ["listen"] = "water_voice", ["smell"] = "taint_sense", ["contemplate"] = "piety" } };

    // ── NPC construction ─────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        TrySpawnBeast(rng, scene, new HyenaArchetype(), 0.40);
        TrySpawnBeast(rng, scene, new LionArchetype(),  0.10);

        TrySpawnCreature(rng, scene, new DromedaryArchetype(), 0.35);
        TrySpawnCreature(rng, scene, new FennecArchetype(),    0.50);
        TrySpawnCreature(rng, scene, new JerboaArchetype(),    0.50);
        TrySpawnCreature(rng, scene, new SandViperArchetype(), 0.40);
        TrySpawnCreature(rng, scene, new VultureArchetype(),   0.55);

        // The birds that need water keep to it.
        var wet = _oasis != null ? new List<Area> { _oasis } : new List<Area> { _wadi! };
        TrySpawnCreature(rng, scene, new SandgrouseArchetype(), 0.45, wet);
        if (_oasis != null) TrySpawnCreature(rng, scene, new DragonflyArchetype(), 0.6, wet);
        TrySpawnCreatureIn<InselbergArea>(rng, scene, new DesertMonitorArchetype(), 0.55);
        TrySpawnCreatureIn<YardangArea>(rng, scene, new DesertMonitorArchetype(), 0.35);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Arid, 2, 4);
    }
}
