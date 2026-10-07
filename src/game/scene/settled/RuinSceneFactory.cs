using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Fight.Generators;
using Cathedral.Game.History;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Items;
using Cathedral.Game.Narrative.World.Items;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Shared;

namespace Cathedral.Game.Scene.Settled;

/// <summary>
/// Every ruined place of history, whatever it was: one factory, as agreed — a ruin does not sprawl and
/// is not rebuilt in its old kind's image. What it was survives only in its <b>name</b> ("the ruins of
/// the castle of Varsk", from <see cref="WorldSites"/>) and in what the rubble gives up: a castle's
/// ruins yield a corroded coin and a broken helm, a temple's a broken idol and an inscription.
///
/// <para>Nobody keeps a ruin. Now and then somebody has moved into one — a hermit, one time in five —
/// and the jackals, owls and snakes always have.</para>
/// </summary>
public sealed class RuinSceneFactory : SettledSceneFactory
{
    private readonly List<Area> _areas = new();
    private PlaceKind? _was;

    public RuinSceneFactory(string? biome = null, string? sessionPath = null) : base(biome, sessionPath) { }

    protected override string DefaultBiome => "plain";

    private string Title => Site?.Place is { } p ? $"Ruins of {p.Name}" : "Ruins";

    private string WhatItWas => _was switch
    {
        PlaceKind.Castle or PlaceKind.Fortress or PlaceKind.Citadel or PlaceKind.Commandery => "fortress",
        PlaceKind.Temple or PlaceKind.ImperialTemple or PlaceKind.Sanctuary or PlaceKind.Monastery => "temple",
        PlaceKind.Palace or PlaceKind.ImperialSchool                                       => "palace",
        PlaceKind.Port                                                                       => "harbour",
        PlaceKind.Mine                                                                       => "mine",
        PlaceKind.Pyramid or PlaceKind.BurialField                                           => "tomb",
        null                                                                                 => "building",
        _                                                                                    => "settlement",
    };

    protected override void BuildPlace(Random rng, int locationId, Scene scene)
    {
        _was = Site?.Place?.Kind;
        string what = WhatItWas;

        _areas.Add(Ruin("Fallen Gate", "by the fallen gate", $"The tumbled gateway of a {what}, its lintel broken across the way",
            new[] { "broken", "overgrown", "silent" },
            new RubblePointOfInterest("Gate Rubble", new() { "Dressed blocks fallen in a heap, tool-marks still on them" },
                Items(() => new Rock(), () => new MosaicTile()), new[] { "tumbled", "mossy" })
            { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "archeology")) }));

        _areas.Add(Ruin("Roofless Hall", "in the roofless hall", $"The shell of the {what}'s great room, open to the sky, a tree growing in it",
            new[] { "open", "echoing", "green" },
            new StatuePointOfInterest("Broken Figure", new() { "A carved figure toppled from its niche, face down in the grass" },
                Items(() => new Idol()), new[] { "toppled", "weathered" })
            { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "iconography"), ("contemplate", "ruin_sense")) }));

        if (rng.NextDouble() < 0.7)
            _areas.Add(Ruin("Undercroft", "in the undercroft", "A vault still standing under the rubble, dark and dripping",
                new[] { "dark", "dripping", "cold" },
                new CratePointOfInterest("Rotted Chests", new() { "Chests fallen apart, their contents spilled in the silt" },
                    Items(Find(rng), Find(rng), () => new Potsherd()), new[] { "rotted", "silted" })
                { Senses = SensoryProfile.Examinable, VerbModiMentis = Teach(("examine", "treasure_hunting")) }));

        _areas.Add(Ruin("Old Wall", "along the old wall", "A run of wall still standing to head height, ivy over it",
            new[] { "ivied", "crumbling", "sheltered" },
            new RubblePointOfInterest("Inscribed Stone", new() { "A stone set in the wall, cut with lines of a script nobody reads now" },
                Items(() => new Inscription(), () => new OldCoin()), new[] { "cut", "worn" })
            { Senses = SensoryProfile.Beautiful, VerbModiMentis = Teach(("examine", "decipher"), ("contemplate", "elegy")) }));

        var section = new Section(Title, new() { $"What is left of a {what}, given back to the weather" },
            seed => new NoisyGenerator { Seed = seed, Density = 0.80f });
        foreach (var a in _areas) section.Areas.Add(a);
        scene.Sections.Add(section);
        RegisterAll(scene, section);
        OutdoorLayout.Connect(scene, _areas, OutdoorLayout.RollShape(rng), "Way", rng);

        FurnitureSubfactory.AddSitSpots(rng, _areas, FurnitureSubfactory.Setting.Highland);
        FurnitureSubfactory.AddHidingPlaces(rng, _areas, FurnitureSubfactory.Setting.Highland);
        FurnitureSubfactory.AddShortcuts(rng, scene, _areas, FurnitureSubfactory.Setting.Highland);
        FurnitureSubfactory.AddExtractionPoints(rng, _areas, FurnitureSubfactory.Setting.Highland);
        Console.WriteLine($"RuinSceneFactory: Built {Title} (was {what}) — biome={Biome}, {_areas.Count} areas");
    }

    private static Area Ruin(string name, string ctx, string text, string[] moods, PointOfInterest poi)
    {
        var a = new RuinArea(name, ctx, $"pick your way to the {name.ToLowerInvariant()}", new() { text }, moods);
        a.PointsOfInterest.Add(poi);
        return a;
    }

    /// <summary>What the rubble of this kind of place gives up.</summary>
    private Func<Item> Find(Random rng)
    {
        Func<Item>[] pool = WhatItWas switch
        {
            "fortress"  => new Func<Item>[] { () => new OldCoin(), () => new Horseshoe(), () => new KeyRing(), () => new Manacles() },
            "temple"    => new Func<Item>[] { () => new Idol(), () => new Relic(), () => new Censer(), () => new Ampulla() },
            "palace"    => new Func<Item>[] { () => new Signet(), () => new GraveRing(), () => new Tome(), () => new Astrolabe() },
            "harbour"   => new Func<Item>[] { () => new Compass(), () => new Amphora(), () => new OldCoin(), () => new Chart() },
            "mine"      => new Func<Item>[] { () => new IronOre(), () => new MinersLamp(), () => new CopperOre(), () => new Pick() },
            "tomb"      => new Func<Item>[] { () => new DeathMask(), () => new Scarab(), () => new BurialUrn(), () => new Shroud() },
            _           => new Func<Item>[] { () => new OldCoin(), () => new Potsherd(), () => new ClayPot(), () => new Knife() },
        };
        return pool[rng.Next(pool.Length)];
    }

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        // A hermit, now and then, sleeping on a pallet in whatever corner keeps the rain off.
        if (rng.NextDouble() < 0.2)
        {
            var corner = _areas[rng.Next(_areas.Count)];
            var bedroll = new PalletPointOfInterest("Hermit's Pallet", new() { "A pallet of old sacking and bracken in the lee of a wall" },
                Items(() => new Straw()), new[] { "makeshift", "musty" })
                { Senses = SensoryProfile.Examinable };
            corner.PointsOfInterest.Add(bedroll);
            bedroll.Register(scene);
            var day = BuildingSchedule.ForHand(corner, _areas, rng);
            SpawnResident(rng, scene, new HermitArchetype(), "among the ruins", day);
        }

        TrySpawnShallow(rng, scene, new OwlArchetype(), _areas, 0.6);
        TrySpawnShallow(rng, scene, new BatArchetype(), _areas, 0.5);
        TrySpawnShallow(rng, scene, Biome == Glyph.Microworld.BiomeDatabase.HotSteppe || Biome == Glyph.Microworld.BiomeDatabase.Desert
            ? new JackalArchetype() : new CrowArchetype(), _areas, 0.5);
        TrySpawnShallow(rng, scene, Biome == Glyph.Microworld.BiomeDatabase.Jungle ? new CobraArchetype() : new AdderArchetype(), _areas, 0.4);
        SprinkleSmallLife(rng, scene, _areas, SmallLife.Tomb, 2, 4);
    }
}
