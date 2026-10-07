using System;
using System.Linq;
using Cathedral.Glyph.Microworld;

namespace Cathedral.Game.Scene.Settled;

/// <summary>
/// A city: the town that sprawls out from an urban place of history — a citadel, a port, a palace or
/// an imperial school — across one to three cells of city ground. One layout generator
/// (<see cref="Shared.CityLayout"/>) for every city; the climate names and dresses its streets and
/// builds its houses (<see cref="SettledSceneFactory.Biome"/>).
///
/// <para>Its size is rolled from its own seed, so neighbouring city cells read as different quarters of
/// one town rather than the same street repeated. Its name, when a world is behind it, is taken from
/// the place it grew around.</para>
/// </summary>
public sealed class CitySceneFactory : SettledSceneFactory
{
    public CitySceneFactory(string? biome = null, string? sessionPath = null) : base(biome, sessionPath) { }

    protected override string DefaultBiome => "plain";

    protected override void BuildPlace(Random rng, int locationId, Scene scene)
    {
        int size = rng.Next(1, 4);
        string name = Site?.Origin is { } origin ? $"Streets of {origin.Name}" : size >= 3 ? "City" : "Town";
        BuildTown(rng, scene, size, name, harbour: Site?.Origin?.Kind == History.PlaceKind.Port);
        FinishTown(rng, scene);
        Console.WriteLine($"CitySceneFactory: Built {name} — biome={Biome}, size={size}, {scene.AllAreas.Count} areas");
    }

    protected override void BuildNpcs(Random rng, int locationId, Scene scene)
    {
        SpawnQueuedCrews(rng, scene);
        TownAnimals(rng, scene, scene.OutdoorAreas);
    }
}
