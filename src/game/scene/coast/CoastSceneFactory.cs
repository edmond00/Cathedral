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

namespace Cathedral.Game.Scene.Coast;

/// <summary>
/// Builds a procedural coast scene per the v1 world-content spec (coast.md).
///
/// Coast identity: sandy / rocky / clifftop — gates section pool & areas.
/// Variant: estuary (~20%) replaces Shore with mud-flat / willow-bank ecology.
/// CliffPoI between Cliff Base and Cliff Top (clifftop coast).
/// Coast camp (CampSubfactory.BuildCoastCamp) added to Sandy Beach when fisherman present (~30%).
/// </summary>
public class CoastSceneFactory : SceneFactory
{
    public CoastSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    private enum CoastIdentity { Sandy, Rocky, Clifftop }
    private enum FishKind { Herring, Cod, Mackerel }

    private CoastIdentity _identity;
    private FishKind _fish;
    private bool _isEstuary;
    private bool _hasFisherman;
    private Area? _sandyBeach, _rockyShore, _cliffBase, _cliffTop, _tidePoolZone, _estuaryFlat;
    private readonly List<Area> _allAreas = new();

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        var roll = rng.NextDouble();
        _identity = roll switch
        {
            < 0.33 => CoastIdentity.Sandy,
            < 0.66 => CoastIdentity.Rocky,
            _      => CoastIdentity.Clifftop,
        };
        _fish        = (FishKind)rng.Next(3);
        _isEstuary   = rng.NextDouble() < 0.20;
        _hasFisherman = rng.NextDouble() < 0.30;

        // ── Build areas ──────────────────────────────────────────────────────

        if (_isEstuary)
        {
            _estuaryFlat = BuildEstuaryFlat();
            _sandyBeach  = BuildSandyBeach();
        }
        else if (_identity == CoastIdentity.Sandy)
        {
            _sandyBeach   = BuildSandyBeach();
            _rockyShore   = BuildRockyShore();
            _tidePoolZone = BuildTidePoolZone();
        }
        else if (_identity == CoastIdentity.Rocky)
        {
            _rockyShore   = BuildRockyShore();
            _tidePoolZone = BuildTidePoolZone();
            _sandyBeach   = BuildSandyBeach();
        }
        else // Clifftop
        {
            _cliffBase    = BuildCliffBase();
            _cliffTop     = BuildCliffTop();
            _rockyShore   = BuildRockyShore();
        }

        var allAreas = new List<Area>();
        if (_sandyBeach   != null) allAreas.Add(_sandyBeach);
        if (_rockyShore   != null) allAreas.Add(_rockyShore);
        if (_tidePoolZone != null) allAreas.Add(_tidePoolZone);
        if (_cliffBase    != null) allAreas.Add(_cliffBase);
        if (_cliffTop     != null) allAreas.Add(_cliffTop);
        if (_estuaryFlat  != null) allAreas.Add(_estuaryFlat);

        foreach (var area in allAreas)
            PopulateArea(area, rng);

        // Coast camp on Sandy Beach if fisherman present
        if (_hasFisherman && _sandyBeach != null)
            foreach (var poi in CampSubfactory.BuildCoastCamp())
                _sandyBeach.PointsOfInterest.Add(poi);

        // ── Sections ─────────────────────────────────────────────────────────

        var shoreAreas = allAreas.Where(a => a != _cliffTop).ToList();
        var shoreSection = new Section(
            _isEstuary ? "Estuary" : "Shore",
            new() { _isEstuary ? "Where river meets sea, brackish and muddy" : "Where land meets the sea — beach, rocks, tide-line" }
        );
        shoreSection.Areas.AddRange(shoreAreas);
        scene.Sections.Add(shoreSection);
        RegisterAll(scene, shoreSection);

        if (_cliffTop != null)
        {
            var cliftopSection = new Section(
                "Clifftop",
                new() { "Elevated edge above the water, exposed to wind" }
            );
            cliftopSection.Areas.Add(_cliffTop);
            scene.Sections.Add(cliftopSection);
            RegisterAll(scene, cliftopSection);
        }

        _allAreas.AddRange(allAreas);

        // ── Connections ──────────────────────────────────────────────────────

        // Linear shore connections via PathPoIs
        for (int i = 0; i < shoreAreas.Count - 1; i++)
        {
            var a = shoreAreas[i];
            var b = shoreAreas[i + 1];
            scene.ConnectAreasBidirectional(a, b);
            string name = (a.DisplayName == "Estuary Flat" || b.DisplayName == "Estuary Flat") ? "Estuary Track" : "Shore Path";
            var path = new PathPointOfInterest(
                a, b, name,
                new() { $"A wet path running between {a.DisplayName.ToLowerInvariant()} and {b.DisplayName.ToLowerInvariant()}" },
                new[] { "wet", "salt-stained", "winding" }
            );
            a.PointsOfInterest.Add(path);
            b.PointsOfInterest.Add(path);
            path.Register(scene);
        }

        // Cliff Base ↔ Cliff Top via CliffPoI
        if (_cliffBase != null && _cliffTop != null)
        {
            scene.ConnectAreasBidirectional(_cliffBase, _cliffTop);
            var cliff = new CliffPointOfInterest(
                bottomArea: _cliffBase,
                topArea:    _cliffTop,
                displayName: "Cliff Face",
                descriptions: new() { "A sheer cliff face with hand- and foot-holds, salt-stained from the spray" },
                moods: new[] { "sheer", "wet", "salt-bitten", "vertiginous" }
            );
            _cliffBase.PointsOfInterest.Add(cliff);
            _cliffTop.PointsOfInterest.Add(cliff);
            cliff.Register(scene);
        }

        Console.WriteLine($"CoastSceneFactory: {_identity}{(_isEstuary ? "/estuary" : "")}, fish={_fish}, fisherman={_hasFisherman}");
    }

    // ── Area builders ────────────────────────────────────────────────────────

    private static Area BuildSandyBeach() => new(
        displayName: "Sandy Beach",
        contextDescription: "on the sandy beach",
        transitionDescription: "step onto the sandy beach",
        descriptions: new() { "A wide sweep of pale sand running down to the surf-line" },
        moods: new[] { "open", "sun-warmed", "salt-stained", "wide" }
    );

    private static Area BuildRockyShore() => new(
        displayName: "Rocky Shore",
        contextDescription: "on the rocky shore",
        transitionDescription: "pick a way along the rocky shore",
        descriptions: new() { "A foreshore of black stones and barnacled boulders, slick with weed" },
        moods: new[] { "slick", "barnacled", "uneven", "salt-bitten" }
    );

    private static Area BuildCliffBase() => new(
        displayName: "Cliff Base",
        contextDescription: "at the cliff base",
        transitionDescription: "approach the cliff base",
        descriptions: new() { "The sea breaks against towering cliffs, spray reaching where you stand" },
        moods: new[] { "loud", "wet", "looming", "echoing" }
    );

    private static Area BuildCliffTop() => new(
        displayName: "Cliff Top",
        contextDescription: "on the cliff top",
        transitionDescription: "step onto the cliff top",
        descriptions: new() { "An exposed grassy edge above a long fall, the sea wrinkling far below" },
        moods: new[] { "exposed", "high", "windy", "vast" }
    );

    private static Area BuildTidePoolZone() => new(
        displayName: "Tide Pool Zone",
        contextDescription: "among the tide pools",
        transitionDescription: "step among the tide pools",
        descriptions: new() { "A scatter of tide pools cupped in the rocks, full of small life" },
        moods: new[] { "still", "salty", "shimmering", "small" }
    );

    private static Area BuildEstuaryFlat() => new(
        displayName: "Estuary Flat",
        contextDescription: "on the estuary flat",
        transitionDescription: "wade onto the estuary flat",
        descriptions: new() { "A wide muddy flat where the river spreads into the sea, wading birds at work" },
        moods: new[] { "muddy", "wide", "wading-bird-haunted", "tidal" }
    );

    // ── Spot population ──────────────────────────────────────────────────────

    private void PopulateArea(Area area, Random rng)
    {
        switch (area.DisplayName)
        {
            case "Sandy Beach":
                area.PointsOfInterest.Add(BuildDriftwoodPile());
                area.PointsOfInterest.Add(BuildKelpBed());
                if (rng.NextDouble() < 0.5) area.PointsOfInterest.Add(BuildStrandedNet());
                break;
            case "Rocky Shore":
                area.PointsOfInterest.Add(BuildKelpBed());
                area.PointsOfInterest.Add(BuildTidePool());
                area.PointsOfInterest.Add(new PointOfInterest(
                    displayName: "Rock Crevice",
                    descriptions: new() { "A narrow crevice between rocks, weed-fringed and damp" },
                    items: new()
                    {
                        new ItemElement(new Shell()),
                        new ItemElement(new Flint()),
                    },
                    moods: new[] { "narrow", "wet", "barnacled" }
                ));
                break;
            case "Cliff Base":
                area.PointsOfInterest.Add(TerrainSubfactory.BuildRockFace());
                area.PointsOfInterest.Add(BuildKelpBed());
                break;
            case "Cliff Top":
                area.PointsOfInterest.Add(new PointOfInterest(
                    displayName: "Seabird Nest",
                    descriptions: new() { "A jumble of stick and weed lodged on a ledge, eggs glinting in the cup" },
                    items: new()
                    {
                        new ItemElement(new Egg()),
                        new ItemElement(new Feather()),
                        new ItemElement(new Feather()),
                    },
                    moods: new[] { "high", "salt-stained", "noisy" }
                ));
                area.PointsOfInterest.Add(TerrainSubfactory.BuildLichenCrust());
                break;
            case "Tide Pool Zone":
                area.PointsOfInterest.Add(BuildTidePool());
                area.PointsOfInterest.Add(BuildTidePool());
                break;
            case "Estuary Flat":
                area.PointsOfInterest.Add(new PointOfInterest(
                    displayName: "Mud Flat",
                    descriptions: new() { "A glistening mud flat marked with wading-bird tracks" },
                    items: new()
                    {
                        new ItemElement(new Clay()),
                        new ItemElement(new Clay()),
                        new ItemElement(new Reed()),
                    },
                    moods: new[] { "glistening", "soft", "tidal" }
                ));
                area.PointsOfInterest.Add(new PointOfInterest(
                    displayName: "Willow Bank",
                    descriptions: new() { "A willow leans over the muddy bank, its branches trailing the water" },
                    items: new()
                    {
                        new ItemElement(new Branch()),
                        new ItemElement(new Bark()),
                    },
                    moods: new[] { "weeping", "trailing", "wet" }
                ));
                break;
        }
    }

    private PointOfInterest BuildDriftwoodPile() => new(
        displayName: "Driftwood Pile",
        descriptions: new() { "A heap of bleached driftwood pushed up by storm and tide" },
        items: new()
        {
            new ItemElement(new Driftwood()),
            new ItemElement(new Driftwood()),
            new ItemElement(new RopeFragment()),
        },
        moods: new[] { "bleached", "salt-stained", "tangled" }
    );

    private PointOfInterest BuildKelpBed() => new(
        displayName: "Kelp Bed",
        descriptions: new() { "A heap of dark kelp washed up on the stones" },
        items: new()
        {
            new ItemElement(new Seaweed()),
            new ItemElement(new Seaweed()),
        },
        moods: new[] { "slick", "salt-fragrant", "dark" }
    );

    private PointOfInterest BuildTidePool() => new(
        displayName: "Tide Pool",
        descriptions: new() { "A small still pool cupped in the rock, anemones at the bottom" },
        items: new()
        {
            new ItemElement(new Crab()),
            new ItemElement(new Mussel()),
            new ItemElement(new Shell()),
            new ItemElement(new Rock()),
        },
        moods: new[] { "still", "salt-bright", "small" }
    );

    private PointOfInterest BuildStrandedNet()
    {
        // Fish dropped depends on coast's dominant fish
        var fishItem = _fish switch
        {
            FishKind.Herring  => (ItemElement)new ItemElement(new Herring()),
            FishKind.Cod      => new ItemElement(new Cod()),
            _                 => new ItemElement(new Mackerel()),
        };
        return new PointOfInterest(
            displayName: "Stranded Net",
            descriptions: new() { "A torn fishing net half-buried in sand, a dead fish still tangled in its mesh" },
            items: new()
            {
                new ItemElement(new Net()),
                fishItem,
            },
            moods: new[] { "tangled", "salt-stained", "abandoned" }
        );
    }

    // ── NPC construction ────────────────────────────────────────────────────

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        if (_allAreas.Count == 0) return;

        if (_hasFisherman && _sandyBeach != null)
        {
            AffinityTable? saved = null;
            var archetype = new FishermanArchetype();
            if (_locationState?.NpcAffinityData.TryGetValue(archetype.ArchetypeId, out var dict) == true)
                saved = new AffinityTable(dict);
            else if (_locationState != null)
            {
                var newDict = new Dictionary<string, AffinityLevel>();
                _locationState.NpcAffinityData[archetype.ArchetypeId] = newDict;
                saved = new AffinityTable(newDict);
            }
            var entity = archetype.Spawn(rng, _sandyBeach.ContextDescription, saved);
            var sceneNpc = new SceneNpc(entity);
            sceneNpc.Register(scene);
            scene.Npcs.Add(sceneNpc);
            var beachId = _sandyBeach.DisplayName.ToLowerInvariant();
            var rockyId = (_rockyShore ?? _sandyBeach).DisplayName.ToLowerInvariant();
            scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Roaming(new()
            {
                [TimePeriod.Dawn]      = beachId,
                [TimePeriod.Morning]   = null,
                [TimePeriod.Noon]      = null,
                [TimePeriod.Afternoon] = rockyId,
                [TimePeriod.Evening]   = beachId,
                [TimePeriod.Night]     = beachId,
            });
        }

        TrySpawnShallow(rng, scene, new SealArchetype(),    0.20);

        SpawnShallow(rng, scene, new SeagullArchetype());
        TrySpawnShallow(rng, scene, new HeronArchetype(),   0.40);
        TrySpawnShallow(rng, scene, new CrabArchetype(),    0.50);

        if (_isEstuary)
            TrySpawnShallow(rng, scene, new SandpiperArchetype(), 0.60);
    }

    private void SpawnShallow(Random rng, Scene scene, ShallowNpcArchetype archetype)
    {
        var area = _allAreas[rng.Next(_allAreas.Count)];
        var entity = archetype.Spawn(rng, area.DisplayName.ToLowerInvariant());
        var sceneNpc = new SceneNpc(entity);
        sceneNpc.Register(scene);
        scene.Npcs.Add(sceneNpc);
        scene.NpcSchedules[sceneNpc.Id] = NpcSchedule.Always(area.DisplayName.ToLowerInvariant());
    }

    private void TrySpawnShallow(Random rng, Scene scene, ShallowNpcArchetype archetype, double chance)
    {
        if (rng.NextDouble() > chance) return;
        SpawnShallow(rng, scene, archetype);
    }
}
