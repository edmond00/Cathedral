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
            new() { "Entrance zone where daylight reaches; relatively safe" }
        );
        mouth.Areas.AddRange(mouthAreas);
        scene.Sections.Add(mouth);
        RegisterAll(scene, mouth);

        var tunnels = new Section(
            "Tunnel Network",
            new() { "Deeper passages, dark and uneven; only the lantern's light reaches" }
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
        scene.ConnectAreasBidirectional(_entrance, _mainShaft);
        _entrance.PointsOfInterest.Add(ladder);
        _mainShaft.PointsOfInterest.Add(ladder);
        ladder.Register(scene);

        // ── Connect deeper rooms to Main Shaft via PathPoIs ──────────────────

        for (int i = 1; i < deepAreas.Count; i++)
        {
            var b = deepAreas[i];
            scene.ConnectAreasBidirectional(_mainShaft, b);
            string passName = b.DisplayName == "Underground Pool" ? "Flooded Passage" : "Passage";
            var path = new PathPointOfInterest(
                _mainShaft, b, passName,
                new() { $"A rough passage leading to the {b.DisplayName.ToLowerInvariant()}" },
                new[] { "narrow", "rough-hewn", "echoing" }
            );
            _mainShaft.PointsOfInterest.Add(path);
            b.PointsOfInterest.Add(path);
            path.Register(scene);
        }

        Console.WriteLine($"CaveSceneFactory: {_type} cave, {_allAreas.Count} areas, miner={_hasMiner}");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildEntranceHall() => new(
        displayName: "Entrance Hall",
        contextDescription: "in the cave's entrance hall",
        transitionDescription: "step into the cave entrance",
        descriptions: new() { "The cave mouth opens into a wide low chamber lit by daylight from outside" },
        moods: new[] { "dim", "echoing", "cool", "damp", "wide" }
    );

    private static Area BuildMainShaft() => new(
        displayName: "Main Shaft",
        contextDescription: "in the main shaft",
        transitionDescription: "descend into the main shaft",
        descriptions: new() { "A long passage cut deep into the rock, the air close and damp" },
        moods: new[] { "narrow", "dark", "damp", "low-roofed", "echoing" }
    );

    private static Area BuildOreChamber() => new(
        displayName: "Ore Chamber",
        contextDescription: "in the ore chamber",
        transitionDescription: "step into the ore chamber",
        descriptions: new() { "A wider chamber where a vein of iron ore breaks through the rock" },
        moods: new[] { "iron-stained", "rough-walled", "cool", "lantern-lit" }
    );

    private static Area BuildCoalSeam() => new(
        displayName: "Coal Seam",
        contextDescription: "at the coal seam",
        transitionDescription: "step to the coal seam",
        descriptions: new() { "A glittering black seam of coal cuts across the chamber wall" },
        moods: new[] { "black", "glittering", "soot-covered", "close" }
    );

    private static Area BuildUndergroundPool() => new(
        displayName: "Underground Pool",
        contextDescription: "by the underground pool",
        transitionDescription: "approach the underground pool",
        descriptions: new() { "A still dark pool fed by water seeping through the rock" },
        moods: new[] { "still", "dark", "wet", "cold", "echoing" }
    );

    private static Area BuildCollapsedTunnel() => new(
        displayName: "Collapsed Tunnel",
        contextDescription: "at the collapsed tunnel",
        transitionDescription: "approach the collapsed tunnel",
        descriptions: new() { "A dead-end of fallen rock and rubble, the way blocked" },
        moods: new[] { "dead-end", "rubble", "still", "warning" }
    );

    private static Area BuildSideAlcove() => new(
        displayName: "Side Alcove",
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
                    descriptions: new() { "A cache of mining tools propped in the rock" },
                    items: new()
                    {
                        new ItemElement(new Pick()),
                        new ItemElement(new Shovel()),
                        new ItemElement(new Rope()),
                    },
                    moods: new[] { "ordered", "iron-grey", "soot-marked" }
                ));
                break;

            case "Main Shaft":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildRockFace());
                if (rng.NextDouble() < 0.5)
                    area.PointsOfInterest.Add(new PointOfInterest(
                        displayName: "Bat Roost",
                        descriptions: new() { "A high hollow in the rock alive with the wing-flutter of bats" },
                        moods: new[] { "high", "rustling", "fetid" }
                    ));
                break;

            case "Ore Chamber":
                area.PointsOfInterest.Add(new PointOfInterest(
                    displayName: "Ore Vein",
                    descriptions: new() { "A bright streak of iron ore exposed by recent picking" },
                    items: new()
                    {
                        new ItemElement(new IronOre()),
                        new ItemElement(new IronOre()),
                        new ItemElement(new IronOre()),
                    },
                    moods: new[] { "bright", "iron-red", "fresh-picked" }
                ));
                break;

            case "Coal Seam":
                area.PointsOfInterest.Add(new PointOfInterest(
                    displayName: "Coal Seam Deposit",
                    descriptions: new() { "A dense seam of coal, freshly worked at one end" },
                    items: new()
                    {
                        new ItemElement(new Coal()),
                        new ItemElement(new Coal()),
                    },
                    moods: new[] { "black", "glittering", "soot-stained" }
                ));
                break;

            case "Underground Pool":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildGorgePool());
                break;

            case "Collapsed Tunnel":
                area.PointsOfInterest.Add(new PointOfInterest(
                    displayName: "Rubble Pile",
                    descriptions: new() { "A heap of broken stone where the tunnel collapsed" },
                    items: new()
                    {
                        new ItemElement(new Rock()),
                        new ItemElement(new Flint()),
                    },
                    moods: new[] { "loose", "treacherous", "dead-end" }
                ));
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
            AffinityTable? saved = null;
            var archetype = new MinerArchetype();
            if (_locationState?.NpcAffinityData.TryGetValue(archetype.ArchetypeId, out var dict) == true)
                saved = new AffinityTable(dict);
            else if (_locationState != null)
            {
                var newDict = new Dictionary<string, AffinityLevel>();
                _locationState.NpcAffinityData[archetype.ArchetypeId] = newDict;
                saved = new AffinityTable(newDict);
            }
            var entity = archetype.Spawn(rng, _entrance.ContextDescription, saved);
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
    }

    private NpcSchedule BuildMinerSchedule()
    {
        var entrance = _entrance!.DisplayName.ToLowerInvariant();
        var ore      = (_oreChamber ?? _mainShaft!).DisplayName.ToLowerInvariant();
        var shaft    = _mainShaft!.DisplayName.ToLowerInvariant();

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
        scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(area.DisplayName.ToLowerInvariant());
    }
}
