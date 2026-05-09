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

namespace Cathedral.Game.Scene.Village;

/// <summary>
/// Builds a procedural village scene per the v1 world-content spec (village.md).
///
/// Mandatory areas: Square (hub), Forge, Mill.
/// Optional pool (roll 2–5): Carpenter's Workshop, Cooper's Workshop, Weaver's Workshop,
/// Bakery, Alehouse, Craftsmen Hall, Sleeping Quarters.
/// Total: 5–8 areas.
///
/// Sections: Village Square, Craft Row, Market End.
/// Connections: <see cref="PathPointOfInterest"/> village roads & lanes;
/// <see cref="DoorPointOfInterest"/> from Craftsmen Hall → Sleeping Quarters.
///
/// NPCs: Master craftsman per workshop + 0–2 apprentices/journeymen, plus Miller, Baker,
/// optional Brewer / Dairymaid. Day-cycle schedules follow the spec.
/// </summary>
public class VillageSceneFactory : SceneFactory
{
    public VillageSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private Area? _square, _forge, _mill, _carpenter, _cooper, _weaver, _bakery, _alehouse, _hall, _sleeping;
    private readonly List<Area> _allAreas = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // ── 1. Mandatory areas ───────────────────────────────────────────────

        _square = BuildSquare(rng);
        _forge  = WorkshopSubfactory.BuildForge();
        _mill   = WorkshopSubfactory.BuildMill();

        // ── 2. Optional pool — roll 2–5 ──────────────────────────────────────

        var optionalBuilders = new List<(string id, Action build)>
        {
            ("carpenter", () => _carpenter = WorkshopSubfactory.BuildCarpenterWorkshop()),
            ("cooper",    () => _cooper    = WorkshopSubfactory.BuildCooperWorkshop()),
            ("weaver",    () => _weaver    = WorkshopSubfactory.BuildWeaverWorkshop()),
            ("bakery",    () => _bakery    = WorkshopSubfactory.BuildBakery()),
            ("alehouse",  () => _alehouse  = WorkshopSubfactory.BuildAlehouse()),
            ("hall",      () => _hall      = WorkshopSubfactory.BuildCraftsmenHall()),
            ("sleeping",  () => _sleeping  = WorkshopSubfactory.BuildSleepingQuarters()),
        };

        int optCount = rng.Next(2, 6); // 2–5
        var picks = SampleUniqueIndices(rng, optionalBuilders.Count, optCount);
        // Always prefer hall + sleeping if any worker spawned (spec note)
        var pickSet = new HashSet<int>(picks);
        // Force-include hall + sleeping (so at least one is present)
        for (int i = 0; i < optionalBuilders.Count; i++)
        {
            if ((optionalBuilders[i].id == "hall" || optionalBuilders[i].id == "sleeping") && !pickSet.Contains(i))
            {
                if (pickSet.Count < 5) pickSet.Add(i);
            }
        }
        foreach (var idx in pickSet)
            optionalBuilders[idx].build();

        // ── 3. Distribute areas across sections ──────────────────────────────

        var squareSection = new Section(
            "Village Square",
            new() { "An open central space where village life converges" }
        );
        squareSection.Areas.Add(_square);
        scene.Sections.Add(squareSection);
        RegisterAll(scene, squareSection);

        var craftRow = new Section(
            "Craft Row",
            new() { "Workshops clustered along a lane, living quarters above or behind" }
        );
        if (_carpenter != null) craftRow.Areas.Add(_carpenter);
        if (_cooper    != null) craftRow.Areas.Add(_cooper);
        if (_weaver    != null) craftRow.Areas.Add(_weaver);
        if (_hall      != null) craftRow.Areas.Add(_hall);
        if (_sleeping  != null) craftRow.Areas.Add(_sleeping);
        if (_forge     != null) craftRow.Areas.Add(_forge);
        scene.Sections.Add(craftRow);
        RegisterAll(scene, craftRow);

        var marketEnd = new Section(
            "Market End",
            new() { "Miller, baker, and brewer occupying the working end of the village" }
        );
        marketEnd.Areas.Add(_mill);
        if (_bakery   != null) marketEnd.Areas.Add(_bakery);
        if (_alehouse != null) marketEnd.Areas.Add(_alehouse);
        scene.Sections.Add(marketEnd);
        RegisterAll(scene, marketEnd);

        _allAreas.AddRange(squareSection.Areas);
        _allAreas.AddRange(craftRow.Areas);
        _allAreas.AddRange(marketEnd.Areas);

        // Mark sleeping quarters as private (they're behind a door)
        if (_sleeping != null) _sleeping.IsPrivate = true;

        // ── 4. Connect village ──────────────────────────────────────────────

        // Village Roads from Square out to key workshops
        ConnectViaPath(scene, _square, _forge!, "Village Road");
        ConnectViaPath(scene, _square, _mill!,  "Village Road");
        if (_bakery   != null) ConnectViaPath(scene, _square, _bakery,   "Village Road");
        if (_hall     != null) ConnectViaPath(scene, _square, _hall,     "Village Road");
        if (_carpenter != null) ConnectViaPath(scene, _square, _carpenter, "Village Road");

        // Lanes between workshops
        if (_carpenter != null && _cooper != null)
            ConnectViaPath(scene, _carpenter, _cooper, "Lane");
        if (_hall != null && _weaver != null)
            ConnectViaPath(scene, _hall, _weaver, "Lane");
        if (_hall != null && _alehouse != null)
            ConnectViaPath(scene, _hall, _alehouse, "Lane");

        // Mill → Bakery internal shortcut
        if (_bakery != null)
            ConnectViaPath(scene, _mill!, _bakery, "Mill Lane");

        // Door: Craftsmen Hall → Sleeping Quarters
        if (_hall != null && _sleeping != null)
        {
            scene.ConnectAreasBidirectional(_hall, _sleeping);
            var door = new DoorPointOfInterest(
                frontArea: _hall,
                backArea:  _sleeping,
                displayName: "Sleeping Quarters Door",
                descriptions: new() { "A timber door at the back of the hall, leading to the workers' sleeping quarters" },
                initialState: DoorState.Unlocked
            );
            _hall.PointsOfInterest.Add(door);
            _sleeping.PointsOfInterest.Add(door);
            door.Register(scene);
        }

        Console.WriteLine($"VillageSceneFactory: village built — {_allAreas.Count} areas");
    }

    private static void ConnectViaPath(Scene scene, Area a, Area b, string pathName)
    {
        if (a == null || b == null) return;
        scene.ConnectAreasBidirectional(a, b);
        var path = new PathPointOfInterest(
            a, b, pathName,
            new() { $"A {pathName.ToLowerInvariant()} running between {a.DisplayName.ToLowerInvariant()} and {b.DisplayName.ToLowerInvariant()}" },
            new[] { "worn", "muddy", "well-trodden" }
        );
        a.PointsOfInterest.Add(path);
        b.PointsOfInterest.Add(path);
        path.Register(scene);
    }

    // ── Square builder ──────────────────────────────────────────────────────

    private static Area BuildSquare(Random rng)
    {
        var square = new Area(
            displayName: "Square",
            contextDescription: "in the village square",
            transitionDescription: "step into the village square",
            descriptions: new() { "An open dirt square at the heart of the village, foot-traffic crossing every direction" },
            moods: new[] { "open", "noisy", "central", "muddy", "well-trodden" }
        );

        square.PointsOfInterest.Add(new PointOfInterest(
            displayName: "Well",
            descriptions: new() { "A stone-rimmed well at the centre of the square, a wooden bucket on its rope" },
            items: new()
            {
                new ItemElement(new Rope()),
            },
            moods: new[] { "central", "stone-rimmed", "deep", "echoing" }
        ));

        if (rng.NextDouble() < 0.40)
        {
            square.PointsOfInterest.Add(new PointOfInterest(
                displayName: "Market Stall",
                descriptions: new() { "A trestle market-stall set out with goods for sale" },
                items: new()
                {
                    new ItemElement(new Bread()),
                    new ItemElement(new Ale()),
                    new ItemElement(new Cloth()),
                },
                moods: new[] { "bright", "cluttered", "bargain-shouted" }
            ));
        }

        return square;
    }

    // ── NPC construction ────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_square is null || _forge is null || _mill is null) return;

        // Always: blacksmith + miller
        SpawnCraftsman(rng, scene, new BlacksmithArchetype(), _forge);
        if (rng.NextDouble() < 0.5)
            SpawnCraftsman(rng, scene, new ApprenticeArchetype(), _forge);

        SpawnCraftsman(rng, scene, new MillerArchetype(), _mill);

        // Optional masters
        if (_carpenter != null)
        {
            SpawnCraftsman(rng, scene, new CarpenterArchetype(), _carpenter);
        }
        if (_cooper != null)
        {
            SpawnCraftsman(rng, scene, new CooperArchetype(), _cooper);
        }
        if (_weaver != null)
        {
            SpawnCraftsman(rng, scene, new WeaverArchetype(), _weaver);
            if (rng.NextDouble() < 0.5)
                SpawnCraftsman(rng, scene, new ApprenticeArchetype(), _weaver);
        }
        if (_bakery != null)
        {
            SpawnCraftsman(rng, scene, new BakerArchetype(), _bakery);
        }
        if (_alehouse != null)
        {
            SpawnCraftsman(rng, scene, new BrewerArchetype(), _alehouse);
        }
    }

    private void SpawnCraftsman(Random rng, Scene scene, NamedNpcArchetype archetype, Area workArea)
    {
        AffinityTable? saved = null;
        if (_locationState?.NpcAffinityData.TryGetValue(archetype.ArchetypeId, out var dict) == true)
            saved = new AffinityTable(dict);
        else if (_locationState != null)
        {
            var newDict = new Dictionary<string, AffinityLevel>();
            _locationState.NpcAffinityData[archetype.ArchetypeId] = newDict;
            saved = new AffinityTable(newDict);
        }
        var entity = archetype.Spawn(rng, workArea.ContextDescription, saved);

        // Master craftsmen own their workshop section
        if (entity.Archetype is CraftsmanArchetype && entity.Archetype.GetType() != typeof(ApprenticeArchetype))
        {
            // Workshop areas are private to their masters in the sense that stealing is illegal,
            // but we don't currently mark each workshop area private. Owned-section bookkeeping
            // could be added here if desired.
        }

        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = BuildScheduleForRole(archetype.ArchetypeId, workArea);
    }

    private NpcSchedule BuildScheduleForRole(string archetypeId, Area workArea)
    {
        var workId   = workArea.DisplayName.ToLowerInvariant();
        var hallId   = _hall?.DisplayName.ToLowerInvariant()     ?? _square!.DisplayName.ToLowerInvariant();
        var sleepId  = _sleeping?.DisplayName.ToLowerInvariant() ?? hallId;
        var squareId = _square!.DisplayName.ToLowerInvariant();
        var aleId    = _alehouse?.DisplayName.ToLowerInvariant() ?? squareId;
        var bakeId   = _bakery?.DisplayName.ToLowerInvariant()   ?? squareId;

        return archetypeId switch
        {
            "blacksmith" or "carpenter" or "cooper" => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = workId,
                [TimePeriod.Morning]   = workId,
                [TimePeriod.Noon]      = hallId,
                [TimePeriod.Afternoon] = workId,
                [TimePeriod.Evening]   = hallId,
                [TimePeriod.Night]     = sleepId,
            }),

            "weaver" => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = workId,
                [TimePeriod.Morning]   = workId,
                [TimePeriod.Noon]      = hallId,
                [TimePeriod.Afternoon] = workId,
                [TimePeriod.Evening]   = aleId,
                [TimePeriod.Night]     = sleepId,
            }),

            "miller" => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = workId,
                [TimePeriod.Morning]   = workId,
                [TimePeriod.Noon]      = hallId,
                [TimePeriod.Afternoon] = workId,
                [TimePeriod.Evening]   = hallId,
                [TimePeriod.Night]     = sleepId,
            }),

            "baker" => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = workId,
                [TimePeriod.Morning]   = workId,
                [TimePeriod.Noon]      = hallId,
                [TimePeriod.Afternoon] = hallId,
                [TimePeriod.Evening]   = squareId,
                [TimePeriod.Night]     = sleepId,
            }),

            "brewer" => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = workId,
                [TimePeriod.Morning]   = workId,
                [TimePeriod.Noon]      = hallId,
                [TimePeriod.Afternoon] = workId,
                [TimePeriod.Evening]   = squareId,
                [TimePeriod.Night]     = sleepId,
            }),

            "apprentice" => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = workId,
                [TimePeriod.Morning]   = workId,
                [TimePeriod.Noon]      = hallId,
                [TimePeriod.Afternoon] = workId,
                [TimePeriod.Evening]   = hallId,
                [TimePeriod.Night]     = sleepId,
            }),

            _ => NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = workId,
                [TimePeriod.Morning]   = workId,
                [TimePeriod.Noon]      = hallId,
                [TimePeriod.Afternoon] = workId,
                [TimePeriod.Evening]   = hallId,
                [TimePeriod.Night]     = sleepId,
            }),
        };
    }
}
