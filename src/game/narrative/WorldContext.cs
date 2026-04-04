using System;
using Cathedral.Glyph.Microworld;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Describes the world position the protagonist currently occupies — either a raw biome
/// (forest, mountain…) or a named location within one (castle, grove…).
///
/// Subclass for every biome/location that has a narration graph and needs flavored context.
/// All others fall back to <see cref="FallbackWorldContext"/> which emits the plain name.
///
/// The single abstract method <see cref="GenerateContextDescription"/> is the enforcement
/// point: adding a new biome or location class without implementing it is a compile error.
/// </summary>
public abstract class WorldContext
{
    /// <summary>
    /// One-line world-register sentence injected into every LLM prompt, between the persona
    /// line and the location context. Reminds the model it is a dark/medieval fantasy world
    /// and guards against modern language or real-world references.
    /// </summary>
    public static readonly string EpochContext =
        "You stand in a grim age of iron and myth, where shadows linger long and the old ways still hold sway.";

    /// <summary>Short name shown in the UI header (e.g., "Forest", "Castle").</summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Returns a stable, medieval-flavored description fragment embedded as:
    ///   "You are in a {result}."
    /// Use <paramref name="locationId"/> (sphere vertex index) as a Random seed so the
    /// same vertex always produces the same atmosphere.
    /// </summary>
    public abstract string GenerateContextDescription(int locationId);

    /// <summary>
    /// Builds the appropriate WorldContext from raw biome/location data.
    /// A named location takes precedence over the surrounding biome.
    /// Unimplemented types fall back to <see cref="FallbackWorldContext"/>.
    /// </summary>
    public static WorldContext From(BiomeType biome, LocationType? location)
    {
        if (location.HasValue)
        {
            // Location-specific contexts go here as narration graphs are added.
            // For now all named locations fall back to their plain name.
            return new FallbackWorldContext(location.Value.Name);
        }

        return biome.Name.ToLowerInvariant() switch
        {
            "forest"   => new ForestBiomeContext(),
            "mountain" => new MountainBiomeContext(),
            "peak"     => new PeakBiomeContext(),
            _          => new FallbackWorldContext(biome.Name)
        };
    }
}

/// <summary>
/// Dense, lightless woodlands of the dark-fantasy world — ancient trees, deep shade,
/// the smell of rot and moss.
/// </summary>
public sealed class ForestBiomeContext : WorldContext
{
    private static readonly string[] Flavors =
    {
        "gloomy", "fog-drenched", "ancient", "shadowed", "tangled",
        "moss-choked", "sunless", "brooding", "dense", "overgrown",
    };

    public override string DisplayName => "Forest";

    public override string GenerateContextDescription(int locationId)
    {
        var rng = new Random(locationId);
        return $"{Flavors[rng.Next(Flavors.Length)]} forest";
    }
}

/// <summary>
/// The harsh mid-elevation heights — bare rock, cutting wind, treacherous paths.
/// </summary>
public sealed class MountainBiomeContext : WorldContext
{
    private static readonly string[] Flavors =
    {
        "craggy", "harsh", "frost-scarred", "treacherous", "bare",
        "windswept", "forbidding", "steep", "bleak", "boulder-strewn",
    };

    public override string DisplayName => "Mountain";

    public override string GenerateContextDescription(int locationId)
    {
        var rng = new Random(locationId);
        return $"{Flavors[rng.Next(Flavors.Length)]} mountain";
    }
}

/// <summary>
/// The highest, most desolate summits — ice, storm, and silence.
/// </summary>
public sealed class PeakBiomeContext : WorldContext
{
    private static readonly string[] Flavors =
    {
        "frozen", "desolate", "storm-beaten", "ice-scarred", "savage",
        "bitter", "merciless", "barren", "remote", "snow-blasted",
    };

    public override string DisplayName => "Peak";

    public override string GenerateContextDescription(int locationId)
    {
        var rng = new Random(locationId);
        return $"{Flavors[rng.Next(Flavors.Length)]} peak";
    }
}

/// <summary>
/// Passthrough context for biomes and locations that do not yet have a narration graph.
/// Emits the plain name with no flavor. Replace with a typed subclass when a graph is added.
/// </summary>
public sealed class FallbackWorldContext : WorldContext
{
    private readonly string _name;

    public FallbackWorldContext(string name) { _name = name; }

    public override string DisplayName
        => _name.Length > 0 ? char.ToUpper(_name[0]) + _name[1..] : _name;

    public override string GenerateContextDescription(int locationId) => _name;
}
