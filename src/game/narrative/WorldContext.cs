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
    // The world-register sentence that used to live here (EpochContext) was never injected into any
    // prompt despite its comment saying so — which is part of why modern references kept getting
    // through. That job now belongs to SceneSetting: SceneSetting.Rule closes every system prompt and
    // SceneSetting.Reminder() closes every request, the latter naming the place this class describes.

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
            "plain" => new PlainBiomeContext(),
            _       => new FallbackWorldContext(biome.Name)
        };
    }
}

/// <summary>
/// Open plains — wide skies, rolling grass, scattered trees and gentle hills.
/// </summary>
public sealed class PlainBiomeContext : WorldContext
{
    private static readonly string[] Flavors =
    {
        "open", "windswept", "sun-bleached", "rolling", "wide", "quiet",
        "grassy", "vast", "golden", "still",
    };

    public override string DisplayName => "Plain";

    public override string GenerateContextDescription(int locationId)
    {
        var rng = new Random(locationId);
        return $"{Flavors[rng.Next(Flavors.Length)]} plain";
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
