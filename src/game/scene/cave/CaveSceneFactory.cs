using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Shared;
using Cathedral.Fight.Generators;

namespace Cathedral.Game.Scene.Cave;

/// <summary>
/// Builds a procedural cave scene per the v1 world-content spec (cave.md).
///
/// Cave type: iron-rich (Ore Chamber present), stone-quarry (no ore vein),
/// or coal-bearing (Coal Seam added).
/// Sections: Cave Mouth, Tunnel Network.
/// Areas (3–5): Entrance Hall (always), Main Shaft, Ore Chamber, Coal Seam,
/// Underground Pool, Collapsed Tunnel, Side Alcove.
/// CliffPoI ladder between Entrance Hall and deeper Main Shaft.
/// Mine camp added to Entrance Hall when miner is present (~25%).
/// </summary>
public class CaveSceneFactory : SceneFactory
{
    public CaveSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum CaveType { IronRich, StoneQuarry, CoalBearing }

    private CaveType _type;
    private bool _hasMiner;
    private Area? _entrance, _mainShaft, _oreChamber, _coalSeam;
    private readonly List<Area> _allAreas = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        var typeRoll = rng.NextDouble();
        _type = typeRoll switch
        {
            < 0.20 => CaveType.StoneQuarry,
            < 0.40 => CaveType.CoalBearing,
            _      => CaveType.IronRich,
        };
        _hasMiner = rng.NextDouble() < 0.25;

        // ── Build areas ──────────────────────────────────────────────────────

        _entrance  = BuildEntranceHall();
        _mainShaft = BuildMainShaft();

        var mouthAreas = new List<Area> { _entrance };
        var deepAreas  = new List<Area> { _mainShaft };

        if (_type == CaveType.IronRich || _type == CaveType.CoalBearing)
        {
            _oreChamber = BuildOreChamber();
            deepAreas.Add(_oreChamber);
        }

        if (_type == CaveType.CoalBearing || (_type == CaveType.IronRich && rng.NextDouble() < 0.20))
        {
            _coalSeam = BuildCoalSeam();
            deepAreas.Add(_coalSeam);
        }

        if (rng.NextDouble() < 0.40) deepAreas.Add(BuildUndergroundPool());
        if (rng.NextDouble() < 0.30) deepAreas.Add(BuildCollapsedTunnel());
        if (rng.NextDouble() < 0.50) deepAreas.Add(BuildSideAlcove());

        foreach (var area in mouthAreas.Concat(deepAreas))
            PopulateArea(area, rng);

        // Add mine camp to entrance if miner present
        if (_hasMiner)
            foreach (var poi in CampSubfactory.BuildMineCamp())
                _entrance!.PointsOfInterest.Add(poi);

        // ── Build sections ───────────────────────────────────────────────────

        var mouth = new Section(
            "Cave Mouth",
            new() { "Entrance zone where daylight reaches; relatively safe" },
            seed => new RoomsGenerator { Seed = seed }
        );
        mouth.Areas.AddRange(mouthAreas);
        scene.Sections.Add(mouth);
        RegisterAll(scene, mouth);

        var tunnels = new Section(
            "Tunnel Network",
            new() { "Deeper passages, dark and uneven; only the lantern's light reaches" },
            seed => new CorridorGenerator { Seed = seed }
        );
        tunnels.Areas.AddRange(deepAreas);
        scene.Sections.Add(tunnels);
        RegisterAll(scene, tunnels);

        _allAreas.AddRange(mouthAreas);
        _allAreas.AddRange(deepAreas);

        // ── Connect: Entrance ↔ Main Shaft via CliffPoI ladder ───────────────

        var ladder = new CliffPointOfInterest(
            bottomArea: _mainShaft, // "down" → main shaft is deeper
            topArea:    _entrance,
            displayName: "Mineshaft Ladder",
            descriptions: new() { "A long timber ladder fixed against the rock, descending into the darker tunnels below" },
            moods: new[] { "long", "rope-bound", "creaking", "narrow" }
        );
        // No area-graph edge: the ladder IS the way down. An edge here handed MoveToAreaVerb
        // (difficulty 1, never fails) a free bypass around the difficulty-6 climb, so the ladder was
        // decorative and the shaft cost nothing to enter.
        ladder.AttachTo(scene);

        // ── Connect deeper rooms to Main Shaft via PathPoIs ──────────────────

        for (int i = 1; i < deepAreas.Count; i++)
        {
            var b = deepAreas[i];
            scene.ConnectAreasBidirectional(_mainShaft, b);
            string passName = b.DisplayName == "Underground Pool" ? "Flooded Passage" : "Passage";
            var path = new PathPointOfInterest(
                _mainShaft, b, PathPointOfInterest.NameFor(_mainShaft, b, passName),
                new() { $"A rough passage leading to the {b.DisplayName.ToLowerInvariant()}" },
                new[] { "narrow", "rough-hewn", "echoing" }
            );
            _mainShaft.PointsOfInterest.Add(path);
            b.PointsOfInterest.Add(path);
            path.Register(scene);
        }

        Console.WriteLine($"CaveSceneFactory: {_type} cave, {_allAreas.Count} areas, miner={_hasMiner}");
    
        // ── Furnishing: somewhere to sit, somewhere to hide, a hard shortcut, a climb ──
        // Rolled, so two places of the same kind are not the same place. Runs after the sections and
        // paths exist: shortcuts need to know what is already adjacent, and the climb needs a section
        // to put its top area in.
        {
            var outdoors = scene.OutdoorAreas;
            FurnitureSubfactory.AddSitSpots(rng, outdoors, FurnitureSubfactory.Setting.Underground);
            FurnitureSubfactory.AddHidingPlaces(rng, outdoors, FurnitureSubfactory.Setting.Underground);
            FurnitureSubfactory.AddShortcuts(rng, scene, outdoors, FurnitureSubfactory.Setting.Underground);
            FurnitureSubfactory.AddExtractionPoints(rng, outdoors, FurnitureSubfactory.Setting.Underground);

            var climbTop = FurnitureSubfactory.AddClimb(
                rng, scene, outdoors[0], FurnitureSubfactory.Setting.Underground);
            if (climbTop != null)
            {
                // Sections must partition the areas, so the new top belongs to the section its foot is
                // in — an area in no section crashes the fight path outright.
                var host = scene.Sections.First(s => s.Areas.Contains(outdoors[0]));
                host.Areas.Add(climbTop);
                RegisterAll(scene, climbTop);
            }
        }

        // Landmarks, and a view from anywhere that has to be climbed to. Must run after the
        // connectors are attached: it finds the high ground by looking for their tops.
        MarkLandmarksAndViews(scene);
}

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildEntranceHall() => new(
        displayName: "Entrance Hall",
        referenceLemma: "entrance",
        contextDescription: "in the cave's entrance hall",
        transitionDescription: "step into the cave entrance",
        descriptions: new() { "The cave mouth opens into a wide low chamber lit by daylight from outside" },
        moods: new[] { "dim", "echoing", "cool", "damp", "wide" }
    );

    private static Area BuildMainShaft() => new(
        displayName: "Main Shaft",
        referenceLemma: "shaft",
        contextDescription: "in the main shaft",
        transitionDescription: "descend into the main shaft",
        descriptions: new() { "A long passage cut deep into the rock, the air close and damp" },
        moods: new[] { "narrow", "dark", "damp", "low-roofed", "echoing" }
    );

    private static Area BuildOreChamber() => new(
        displayName: "Ore Chamber",
        referenceLemma: "chamber",
        contextDescription: "in the ore chamber",
        transitionDescription: "step into the ore chamber",
        descriptions: new() { "A wider chamber where a vein of iron ore breaks through the rock" },
        moods: new[] { "iron-stained", "rough-walled", "cool", "lantern-lit" }
    );

    private static Area BuildCoalSeam() => new(
        displayName: "Coal Seam",
        referenceLemma: "seam",
        contextDescription: "at the coal seam",
        transitionDescription: "step to the coal seam",
        descriptions: new() { "A glittering black seam of coal cuts across the chamber wall" },
        moods: new[] { "black", "glittering", "soot-covered", "close" }
    );

    private static Area BuildUndergroundPool() => new(
        displayName: "Underground Pool",
        referenceLemma: "pool",
        contextDescription: "by the underground pool",
        transitionDescription: "approach the underground pool",
        descriptions: new() { "A still dark pool fed by water seeping through the rock" },
        moods: new[] { "still", "dark", "wet", "cold", "echoing" }
    );

    private static Area BuildCollapsedTunnel() => new(
        displayName: "Collapsed Tunnel",
        referenceLemma: "tunnel",
        contextDescription: "at the collapsed tunnel",
        transitionDescription: "approach the collapsed tunnel",
        descriptions: new() { "A dead-end of fallen rock and rubble, the way blocked" },
        moods: new[] { "dead-end", "rubble", "still", "warning" }
    );

    private static Area BuildSideAlcove() => new(
        displayName: "Side Alcove",
        referenceLemma: "alcove",
        contextDescription: "in the side alcove",
        transitionDescription: "step into the side alcove",
        descriptions: new() { "A small offshoot from the main shaft, the air still and silent" },
        moods: new[] { "small", "still", "hidden" }
    );

    // ── Spot population ──────────────────────────────────────────────────────

    private void PopulateArea(Area area, Random rng)
    {
        switch (area.DisplayName)
        {
            case "Entrance Hall":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildRockFace());
                area.PointsOfInterest.Add(new PointOfInterest(
                    displayName: "Tool Cache",
                    referenceLemma: "tool",
                    descriptions: new() { "A cache of mining tools propped in the rock" },
                    items: new()
                    {
                        new ItemElement(new Pick()),
                        new ItemElement(new Shovel()),
                        new ItemElement(new Rope()),
                    },
                    moods: new[] { "ordered", "iron-grey", "soot-marked" }
                ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework" } });
                break;

            case "Main Shaft":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildRockFace());
                if (rng.NextDouble() < 0.5)
                    area.PointsOfInterest.Add(new PointOfInterest(
                        displayName: "Bat Roost",
                        referenceLemma: "roost",
                        descriptions: new() { "A high hollow in the rock alive with the wing-flutter of bats" },
                        moods: new[] { "high", "rustling", "fetid" }
                    ) { Senses = SensoryProfile.FullyAlive, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "beast_sense", ["listen"] = "keen_ear", ["smell"] = "scenting" } });
                break;

            case "Ore Chamber":
                area.PointsOfInterest.Add(new PointOfInterest(
                    displayName: "Ore Vein",
                    referenceLemma: "ore",
                    descriptions: new() { "A bright streak of iron ore exposed by recent picking" },
                    items: new()
                    {
                        new ItemElement(new IronOre()),
                        new ItemElement(new IronOre()),
                        new ItemElement(new IronOre()),
                    },
                    moods: new[] { "bright", "iron-red", "fresh-picked" }
                ) { Senses = SensoryProfile.Beautiful, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework", ["contemplate"] = "aesthetic" } });
                break;

            case "Coal Seam":
                area.PointsOfInterest.Add(new PointOfInterest(
                    displayName: "Coal Seam Deposit",
                    referenceLemma: "coal",
                    descriptions: new() { "A dense seam of coal, freshly worked at one end" },
                    items: new()
                    {
                        new ItemElement(new Coal()),
                        new ItemElement(new Coal()),
                    },
                    moods: new[] { "black", "glittering", "soot-stained" }
                ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework" } });
                break;

            case "Underground Pool":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildGorgePool());
                break;

            case "Collapsed Tunnel":
                area.PointsOfInterest.Add(new PointOfInterest(
                    displayName: "Rubble Pile",
                    referenceLemma: "rubble",
                    descriptions: new() { "A heap of broken stone where the tunnel collapsed" },
                    items: new()
                    {
                        new ItemElement(new Rock()),
                        new ItemElement(new Flint()),
                    },
                    moods: new[] { "loose", "treacherous", "dead-end" }
                ) { Senses = SensoryProfile.Examinable, VerbModiMentis = new Dictionary<string, string> { ["examine"] = "stonework" } });
                break;

            case "Side Alcove":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildRockFace());
                break;
        }
    }

    // ── NPC construction ────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_entrance is null) return;

        if (_hasMiner)
        {
            var archetype = new MinerArchetype();
            // Affinity persists per NPC: Spawn resolves the table by the NPC's stable id.
            var entity = archetype.Spawn(rng, _entrance.ContextDescription,
                _locationState != null ? _locationState.AffinityFor : null);
            var sceneNpc = new SceneNpc(entity);
            sceneNpc.Register(scene);
            scene.Npcs.Add(sceneNpc);
            scene.NpcSchedules[sceneNpc.Id] = BuildMinerSchedule();
        }

        // Cave Spider (rare)
        TrySpawnShallow(rng, scene, new CaveSpiderArchetype(), 0.15);

        // Common: Rat (entrance/main shaft), Bat (deep)
        TrySpawnShallow(rng, scene, new RatArchetype(), 0.50);
        TrySpawnShallow(rng, scene, new BatArchetype(), 0.45);
    
        // Small life. Every location has some; which and how many is rolled, so two
        // places of the same kind are not the same place.
        SprinkleSmallLife(rng, scene, scene.AllAreas, SmallLife.Subterranean, 2, 4);
}

    private NpcSchedule BuildMinerSchedule()
    {
        var entrance = _entrance!;
        var ore      = _oreChamber ?? _mainShaft!;
        var shaft    = _mainShaft!;

        return NpcSchedule.Roaming(new()
        {
            [TimePeriod.Dawn]      = entrance,
            [TimePeriod.Morning]   = ore,
            [TimePeriod.Noon]      = entrance,
            [TimePeriod.Afternoon] = shaft,
            [TimePeriod.Evening]   = entrance,
            [TimePeriod.Night]     = entrance,
        });
    }

    private void TrySpawnShallow(Random rng, Scene scene, ShallowNpcArchetype archetype, double chance)
    {
        if (rng.NextDouble() > chance) return;
        var area = _allAreas[rng.Next(_allAreas.Count)];
        var entity = archetype.Spawn(rng, area.DisplayName.ToLowerInvariant());
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(area);
    }
}
