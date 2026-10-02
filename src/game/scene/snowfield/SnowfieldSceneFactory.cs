using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Shared;

namespace Cathedral.Game.Scene.Snowfield;

/// <summary>
/// Builds a snowfield — what a mountain becomes in cold country (see <c>ClimateRule</c>).
///
/// Sections: Snowfield (the open snow, its drifts, an avalanche chute and the cornice above it) and
/// Sheltered Ground (when rolled, a stand of wind-crippled trees and a frozen tarn).
/// Climb: up the chute's side onto the cornice ridge, which sees the whole place.
/// Uninhabited: no settlement stands in cold country yet.
/// </summary>
public class SnowfieldSceneFactory : ClimateSceneFactory
{
    public SnowfieldSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private Area? _chute, _cornice;
    private readonly List<Area> _sheltered = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        bool hasKrummholz = rng.NextDouble() < 0.55;
        bool hasTarn      = rng.NextDouble() < 0.40;

        _chute = BuildChute();
        var open = new List<Area> { BuildSnowpack(), BuildDrifts(), _chute };
        if (hasKrummholz) _sheltered.Add(BuildKrummholz());
        if (hasTarn) _sheltered.Add(BuildTarn());
        // Somewhere out of the wind there always is, if only a hollow among the drifts.
        if (_sheltered.Count == 0) _sheltered.Add(BuildKrummholz());
        _cornice = BuildCornice();

        foreach (var area in open.Concat(_sheltered).Append(_cornice)) Populate(area, rng);

        AddSection(scene, "Snowfield", "Snow to the sky in every direction, the light coming from everywhere at once",
            seed => new NoisyGenerator { Seed = seed, Density = 0.90f }, open.Append(_cornice));
        AddSection(scene, "Sheltered Ground", "A dip out of the wind where something manages to live",
            seed => new NoisyGenerator { Seed = seed, Density = 0.68f }, _sheltered);

        ConnectChain(scene, open.Take(2).Concat(_sheltered).Append(_chute).ToList(),
            (a, b) => a is ChuteArea || b is ChuteArea ? "Traverse"
                    : a is DriftArea || b is DriftArea ? "Drift Line"
                    : "Snow Track",
            "across the snow", new[] { "deep", "crusted", "blue-shadowed" });

        AddViewpoint(scene, _chute, _cornice, "Chute Wall",
            "The steep side of the avalanche chute, snow over rock, kicked steps going up to where the cornice overhangs",
            new[] { "steep", "kicked", "overhung", "loaded" }, icy: true);

        Furnish(rng, scene, FurnitureSubfactory.Setting.Frozen, _cornice);

        Console.WriteLine($"SnowfieldSceneFactory: krummholz={hasKrummholz}, tarn={hasTarn}, {_allAreas.Count} areas");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildSnowpack() => new SnowpackArea(
        displayName: "Open Snowfield",
        contextDescription: "out on the snowfield",
        transitionDescription: "wade out into the snow",
        descriptions: new() { "A sweep of untrodden snow, thigh-deep and crusted, without a mark on it from edge to edge" },
        moods: new[] { "untrodden", "blinding", "silent", "deep" });

    private static Area BuildDrifts() => new DriftArea(
        displayName: "Wind Drifts",
        contextDescription: "among the wind drifts",
        transitionDescription: "push in among the drifts",
        descriptions: new() { "Snow heaped by the wind into dunes and wave-crests, hard as board on top, bottomless underneath" },
        moods: new[] { "heaped", "wave-crested", "hard-crusted", "hissing" });

    private static Area BuildChute() => new ChuteArea(
        displayName: "Avalanche Chute",
        contextDescription: "in the avalanche chute",
        transitionDescription: "cross into the chute",
        descriptions: new() { "A trough swept clean down the mountainside, trees snapped off along its edges, the snow in it loaded and quiet" },
        moods: new[] { "swept", "loaded", "quiet", "dangerous" });

    private static Area BuildKrummholz() => new KrummholzArea(
        displayName: "Krummholz",
        contextDescription: "among the krummholz",
        transitionDescription: "crouch into the krummholz",
        descriptions: new() { "Trees crippled by the wind into a waist-high mat, so dense the snow lies on it like a roof" },
        moods: new[] { "crippled", "dense", "sheltered", "dark" });

    private static Area BuildTarn() => new TarnArea(
        displayName: "Frozen Tarn",
        contextDescription: "on the frozen tarn",
        transitionDescription: "step out onto the frozen tarn",
        descriptions: new() { "A small lake in a hollow, frozen and snowed over, its shape only a flatness in the snow" },
        moods: new[] { "flat", "hidden", "booming", "cold" });

    private static Area BuildCornice() => new CorniceArea(
        displayName: "Cornice Ridge",
        contextDescription: "on the cornice ridge",
        transitionDescription: "climb out onto the ridge",
        descriptions: new() { "A ridge-line whose snow curls out over the drop in a frozen wave, the whole snowfield below" },
        moods: new[] { "curling", "overhanging", "high", "precarious" });

    // ── Spot population ──────────────────────────────────────────────────────

    private void Populate(Area area, Random rng)
    {
        switch (area)
        {
            case SnowpackArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildRockOutcrop());
                break;
            case DriftArea:
                area.PointsOfInterest.Add(BuildSastrugi());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildSnowDrift());
                break;
            case ChuteArea:
                area.PointsOfInterest.Add(BuildAvalancheDebris());
                area.PointsOfInterest.Add(BuildSnappedTrees());
                break;
            case KrummholzArea:
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildStuntedFir());
                area.PointsOfInterest.Add(ClimateTerrainSubfactory.BuildCrowberryMat());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(TerrainSubfactory.BuildShelteredHollow());
                break;
            case TarnArea:
                area.PointsOfInterest.Add(BuildTarnIce());
                break;
            case CorniceArea:
                area.PointsOfInterest.Add(BuildCorniceLip());
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                break;
        }
    }

    private static PointOfInterest BuildSastrugi() => new DriftPointOfInterest(
        displayName: "Sastrugi",
        descriptions: new() { "Ridges carved in the hard snow by the wind, sharp-prowed, all pointing the way the gales blow" },
        items: new() { new ItemElement(new IceShard()) },
        moods: new[] { "carved", "prowed", "aligned", "hard" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "wind_reading", ["contemplate"] = "aesthetic" } };

    private static PointOfInterest BuildAvalancheDebris() => new RubblePointOfInterest(
        displayName: "Avalanche Debris",
        descriptions: new() { "A tumbled fan of snow blocks set hard as stone, rocks and broken branches frozen into it" },
        items: new() { new ItemElement(new Rock()), new ItemElement(new IceShard()), new ItemElement(new Branch()) },
        moods: new[] { "tumbled", "set-hard", "chaotic", "recent" },
        isNatural: true
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "survivalism", ["contemplate"] = "dread" } };

    private static PointOfInterest BuildSnappedTrees() => new DeadfallPointOfInterest(
        displayName: "Snapped Trees",
        descriptions: new() { "Firs broken off at head height along the chute's edge, all fallen the same way" },
        items: new() { new ItemElement(new Branch()), new ItemElement(new Twig()), new ItemElement(new PineNeedle()) },
        moods: new[] { "snapped", "aligned", "splintered", "resinous" },
        isNatural: true
    ) { Senses = SensoryProfile.Odorous, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "woodcraft", ["smell"] = "petrichor" } };

    private static PointOfInterest BuildTarnIce() => new IcePointOfInterest(
        displayName: "Tarn Ice",
        descriptions: new() { "Black ice where the wind has swept the snow off, and under it, very slow, the shapes of fish" },
        items: new() { new ItemElement(new IceShard()), new ItemElement(new ArcticChar()) },
        moods: new[] { "black", "booming", "clear", "deep" },
        isNatural: true
    ) { Senses = new SensoryProfile(Examine: true, Contemplate: true, Listen: true),
        VerbModiMentis = new Dictionary<string, string> { ["examine"] = "anglery", ["listen"] = "hollow_ear", ["contemplate"] = "meditation" } };

    private static PointOfInterest BuildCorniceLip() => new DriftPointOfInterest(
        displayName: "Cornice Lip",
        descriptions: new() { "The lip of the cornice curling out over nothing, icicled underneath, its edge impossible to see from above" },
        items: new() { new ItemElement(new IceShard()) },
        moods: new[] { "curling", "icicled", "deceptive", "vertiginous" }
    ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "survivalism", ["contemplate"] = "awe" } };

    // ── NPC construction ─────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        TrySpawnBeast(rng, scene, new SnowLeopardArchetype(), 0.30);
        TrySpawnBeast(rng, scene, new WhiteWolfArchetype(),   0.15);

        TrySpawnCreature(rng, scene, new MountainGoatArchetype(), 0.40);
        TrySpawnCreature(rng, scene, new SnowHareArchetype(),     0.45);
        TrySpawnCreature(rng, scene, new PtarmiganArchetype(),    0.45);
        TrySpawnCreature(rng, scene, new ErmineArchetype(),       0.35);
        TrySpawnCreature(rng, scene, new PikaArchetype(),         0.45);
        TrySpawnCreature(rng, scene, new RavenArchetype(),        0.40);
        TrySpawnCreature(rng, scene, new SnowyOwlArchetype(),     0.20);

        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Frozen, 1, 3);
    }
}
