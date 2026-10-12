using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc.Archetypes;
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
public class DesertSceneFactory : ClimateSceneFactory
{
    public DesertSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum Identity { Erg, Reg, Pan }

    private Identity _identity;
    private Area? _oasis, _wadi, _duneCrest;

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        _identity = rng.NextDouble() switch
        {
            < 0.45 => Identity.Erg,
            < 0.80 => Identity.Reg,
            _      => Identity.Pan,
        };
        bool hasOasis = rng.NextDouble() < (_identity == Identity.Pan ? 0.30 : 0.45);

        // ── Areas ────────────────────────────────────────────────────────────

        var open = _identity switch
        {
            Identity.Erg => new List<Area> { BuildDuneField(), BuildDuneTrough() },
            Identity.Reg => new List<Area> { BuildStonyDesert(), BuildDuneTrough() },
            _            => new List<Area> { BuildSaltPan(), BuildStonyDesert() },
        };
        if (rng.NextDouble() < 0.5)
            open.Add(_identity == Identity.Pan ? BuildDuneField() : rng.NextDouble() < 0.5 ? BuildSaltPan() : BuildStonyDesert());
        open = open.GroupBy(a => a.DisplayName).Select(g => g.First()).ToList();

        _wadi = BuildWadi();
        var low = new List<Area> { _wadi };
        if (hasOasis) { _oasis = BuildOasis(); low.Add(_oasis); }

        // The crest is the climb's top and nothing else reaches it, so it stays out of the chain.
        _duneCrest = BuildDuneCrest();

        foreach (var area in open.Concat(low).Append(_duneCrest))
            Populate(area, rng);

        // ── Sections ─────────────────────────────────────────────────────────

        AddSection(scene, "Open Desert", "Sand, gravel and salt under an empty sky; no shade, and the horizon trembling",
            seed => new RadiantGenerator { Seed = seed, CentreDensity = 0.95f, EdgeDensity = 0.72f },
            open.Append(_duneCrest));
        AddSection(scene, "Low Ground", "A dry watercourse and what grows along it; the only shade for a day in any direction",
            seed => new NoisyGenerator { Seed = seed, Density = 0.80f },
            low);

        var chain = open.Concat(low).ToList();
        ConnectChain(scene, chain,
            (a, b) => a == _oasis || b == _oasis ? "Palm Track"
                    : a.DisplayName.Contains("Dune") || b.DisplayName.Contains("Dune") ? "Dune Track"
                    : "Caravan Trace",
            "across sand and gravel", new[] { "faint", "sand-blown", "sun-hard" });

        // A dune's slip face is sand at the angle sand stops holding: a climb, not a walk.
        AddViewpoint(scene, open[0], _duneCrest, "Slip Face",
            "The steep lee face of a great dune, sand pouring down it at every step taken up it",
            new[] { "steep", "pouring", "blinding", "hot" });

        Furnish(rng, scene, FurnitureSubfactory.Setting.Arid, _duneCrest);

        Console.WriteLine($"DesertSceneFactory: {_identity}, oasis={hasOasis}, {_allAreas.Count} areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildDuneField() => new DuneArea(
        displayName: "Dune Field",
        contextDescription: "among the dunes",
        transitionDescription: "climb into the dunes",
        descriptions: new() { "Dunes in ranks to the horizon, their crests smoking with blown sand, every trough the same" },
        moods: new[] { "endless", "shifting", "blinding", "silent" });

    private static Area BuildDuneTrough() => new DuneArea(
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

    private static Area BuildDuneCrest() => new DuneArea(
        displayName: "Great Dune Crest",
        contextDescription: "on the crest of the great dune",
        transitionDescription: "haul yourself onto the crest",
        descriptions: new() { "A knife-edge of sand high above everything, the wind tearing a plume off it" },
        moods: new[] { "high", "knife-edged", "windswept", "vast" });

    // ── Spot population ──────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        switch (area.DisplayName)
        {
            case "Dune Field":
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(BuildMirage());
                if (rng.NextDouble() < 0.4) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildBleachedBones());
                area.PointsOfInterest.Add(BuildBurrow());
                break;
            case "Dune Trough":
                area.PointsOfInterest.Add(BuildDesertRoses());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildCactus());
                break;
            case "Stony Desert":
                area.PointsOfInterest.Add(BuildVentifact());
                if (rng.NextDouble() < 0.6) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildMyrrhBush());
                if (rng.NextDouble() < 0.3) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildStele("half-buried in the gravel and leaning"));
                if (rng.NextDouble() < 0.4) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildBleachedBones());
                break;
            case "Salt Pan":
                area.PointsOfInterest.Add(BuildSaltCrust());
                area.PointsOfInterest.Add(BuildMirage());
                break;
            case "Wadi":
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildAcacia());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildThornBush());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildAloe());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(BuildBurrow());
                break;
            case "Oasis":
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildDatePalm());
                area.PointsOfInterest.Add(BuildSpring());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildReedBed());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildAloe());
                break;
            case "Great Dune Crest":
                area.PointsOfInterest.Add(BuildMirage());
                area.PointsOfInterest.Add(BuildSandPlume());
                break;
        }
    }

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

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Arid, 2, 4);
    }
}
