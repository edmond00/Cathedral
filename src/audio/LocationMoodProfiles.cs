using System;
using System.Collections.Generic;

namespace Cathedral.Audio;

/// <summary>
/// Defines deterministic mood ranges for every biome and location type on the sphere.
/// Call <see cref="SampleMood"/> to get a repeatable per-location mood seeded by the
/// vertex index so each visit to the same location always produces the same music.
/// </summary>
public static class LocationMoodProfiles
{
    private readonly record struct MoodRange(MusicMoodState Min, MusicMoodState Max);

    // ── Biome profiles ────────────────────────────────────────────────────────
    // Each range covers the expressive space appropriate to that biome.
    // Higher sadness/mystery = more melancholic/modal music.
    // Higher fear = more dissonant/unstable music.
    private static readonly Dictionary<string, MoodRange> Profiles = new()
    {
        // --- biomes ---
        ["plain"]    = new(new(0.10f, 0.05f, 0.15f), new(0.35f, 0.25f, 0.40f)),
        ["forest"]   = new(new(0.30f, 0.10f, 0.45f), new(0.55f, 0.30f, 0.70f)),
        ["mountain"] = new(new(0.25f, 0.15f, 0.45f), new(0.50f, 0.40f, 0.70f)),
        ["peak"]     = new(new(0.40f, 0.25f, 0.65f), new(0.70f, 0.50f, 0.90f)),
        ["coast"]    = new(new(0.15f, 0.10f, 0.30f), new(0.40f, 0.35f, 0.55f)),
        ["city"]     = new(new(0.10f, 0.15f, 0.10f), new(0.35f, 0.40f, 0.35f)),
        ["sea"]      = new(new(0.30f, 0.20f, 0.40f), new(0.60f, 0.45f, 0.65f)),
        ["ocean"]    = new(new(0.40f, 0.30f, 0.55f), new(0.75f, 0.55f, 0.85f)),
        ["field"]    = new(new(0.05f, 0.05f, 0.10f), new(0.25f, 0.20f, 0.35f)),

        // --- location types ---
        ["church"]         = new(new(0.55f, 0.05f, 0.45f), new(0.80f, 0.20f, 0.65f)),
        ["dungeon"]        = new(new(0.65f, 0.45f, 0.65f), new(0.90f, 0.80f, 0.90f)),
        ["castle"]         = new(new(0.30f, 0.35f, 0.30f), new(0.55f, 0.60f, 0.55f)),
        ["village"]        = new(new(0.15f, 0.05f, 0.10f), new(0.35f, 0.20f, 0.30f)),
        ["observatory"]    = new(new(0.35f, 0.10f, 0.70f), new(0.60f, 0.30f, 0.90f)),
        ["stable"]         = new(new(0.10f, 0.05f, 0.05f), new(0.30f, 0.20f, 0.25f)),
        ["farm"]           = new(new(0.10f, 0.05f, 0.05f), new(0.25f, 0.15f, 0.20f)),
        ["grove"]          = new(new(0.35f, 0.10f, 0.55f), new(0.60f, 0.30f, 0.75f)),
        ["amphitheater"]   = new(new(0.10f, 0.20f, 0.10f), new(0.30f, 0.40f, 0.30f)),
        ["monastery"]      = new(new(0.55f, 0.05f, 0.50f), new(0.80f, 0.20f, 0.70f)),
        ["mine"]           = new(new(0.45f, 0.40f, 0.35f), new(0.70f, 0.65f, 0.60f)),
        ["cave"]           = new(new(0.50f, 0.35f, 0.60f), new(0.80f, 0.65f, 0.85f)),
        ["shrine"]         = new(new(0.45f, 0.10f, 0.65f), new(0.70f, 0.30f, 0.85f)),
        ["tavern"]         = new(new(0.05f, 0.20f, 0.05f), new(0.20f, 0.40f, 0.20f)),
        ["catacombs"]      = new(new(0.70f, 0.50f, 0.65f), new(0.90f, 0.80f, 0.88f)),
        ["market"]         = new(new(0.05f, 0.15f, 0.05f), new(0.20f, 0.35f, 0.20f)),
        ["stadium"]        = new(new(0.05f, 0.25f, 0.05f), new(0.20f, 0.50f, 0.20f)),
        ["forum"]          = new(new(0.15f, 0.20f, 0.15f), new(0.35f, 0.40f, 0.35f)),
        ["academy"]        = new(new(0.20f, 0.10f, 0.40f), new(0.40f, 0.25f, 0.60f)),
        ["sanatorium"]     = new(new(0.60f, 0.30f, 0.40f), new(0.85f, 0.55f, 0.65f)),
        ["cathedral"]      = new(new(0.65f, 0.05f, 0.40f), new(0.88f, 0.20f, 0.65f)),
        ["institute"]      = new(new(0.25f, 0.15f, 0.35f), new(0.45f, 0.30f, 0.55f)),
        ["library"]        = new(new(0.35f, 0.05f, 0.50f), new(0.55f, 0.20f, 0.70f)),
        ["bank"]           = new(new(0.20f, 0.25f, 0.10f), new(0.40f, 0.45f, 0.30f)),
        ["port"]           = new(new(0.15f, 0.25f, 0.25f), new(0.35f, 0.45f, 0.50f)),
        ["forge"]          = new(new(0.25f, 0.30f, 0.15f), new(0.45f, 0.55f, 0.35f)),
        ["workshop"]       = new(new(0.15f, 0.15f, 0.15f), new(0.35f, 0.35f, 0.35f)),
        ["reef"]           = new(new(0.35f, 0.25f, 0.55f), new(0.60f, 0.50f, 0.80f)),
        ["mist"]           = new(new(0.45f, 0.30f, 0.65f), new(0.70f, 0.55f, 0.85f)),
        ["haze"]           = new(new(0.45f, 0.30f, 0.65f), new(0.70f, 0.55f, 0.85f)),
        ["snowfield"]      = new(new(0.50f, 0.15f, 0.55f), new(0.75f, 0.40f, 0.80f)),
        ["ice_lake"]       = new(new(0.50f, 0.20f, 0.60f), new(0.75f, 0.45f, 0.85f)),
        ["lake"]           = new(new(0.30f, 0.10f, 0.40f), new(0.55f, 0.30f, 0.65f)),
        ["ruins"]          = new(new(0.55f, 0.25f, 0.60f), new(0.80f, 0.55f, 0.85f)),
        ["sunken_city"]    = new(new(0.65f, 0.40f, 0.75f), new(0.88f, 0.65f, 0.92f)),
        ["ancient_city"]   = new(new(0.55f, 0.20f, 0.65f), new(0.80f, 0.45f, 0.85f)),
        ["swamp"]          = new(new(0.55f, 0.35f, 0.55f), new(0.80f, 0.65f, 0.80f)),
        ["circus"]         = new(new(0.05f, 0.25f, 0.05f), new(0.20f, 0.45f, 0.25f)),
        ["ice_cave"]       = new(new(0.55f, 0.30f, 0.65f), new(0.80f, 0.55f, 0.85f)),
        ["oasis"]          = new(new(0.20f, 0.05f, 0.30f), new(0.40f, 0.20f, 0.55f)),
        ["assassin_guild"] = new(new(0.40f, 0.55f, 0.35f), new(0.65f, 0.80f, 0.60f)),
        ["villa"]          = new(new(0.20f, 0.15f, 0.15f), new(0.40f, 0.35f, 0.35f)),
    };

    private static readonly MoodRange Fallback =
        new(new(0.25f, 0.15f, 0.30f), new(0.50f, 0.35f, 0.55f));

    /// <summary>
    /// Returns a deterministic mood for a given location type and vertex/location id.
    /// The mood is sampled uniformly from the predefined min–max range so each
    /// location type has a consistent character but individual locations feel distinct.
    /// Intensity is always sampled in the upper half (0.70–1.0) so all locations play
    /// with reasonably full layering.
    /// </summary>
    public static MusicMoodState SampleMood(string locationType, int locationId)
    {
        var rng = new Random(locationId ^ 0x3F7A2C1);   // XOR salt to avoid seed 0 collapse
        var p = Profiles.TryGetValue(locationType.ToLowerInvariant(), out var profile)
              ? profile
              : Fallback;

        float Lerp(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        return new MusicMoodState(
            Lerp(p.Min.Sadness,   p.Max.Sadness),
            Lerp(p.Min.Fear,      p.Max.Fear),
            Lerp(p.Min.Mystery,   p.Max.Mystery),
            0.70f + (float)rng.NextDouble() * 0.30f   // intensity 0.70–1.0
        );
    }
}
