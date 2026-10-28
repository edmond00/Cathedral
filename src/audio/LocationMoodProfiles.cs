using System;
using System.Collections.Generic;
using System.Linq;

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
    // Higher coldness/mystery = more melancholic/modal music.
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

        // --- climate biomes --- the hot country warmer and stranger, the cold country colder.
        ["desert"]      = new(new(0.10f, 0.15f, 0.40f), new(0.35f, 0.40f, 0.70f)),
        ["hot steppe"]  = new(new(0.05f, 0.15f, 0.25f), new(0.30f, 0.40f, 0.50f)),
        ["jungle"]      = new(new(0.20f, 0.25f, 0.50f), new(0.45f, 0.50f, 0.80f)),
        ["canyon"]      = new(new(0.20f, 0.15f, 0.50f), new(0.45f, 0.40f, 0.75f)),
        ["sea ice"]     = new(new(0.60f, 0.25f, 0.55f), new(0.85f, 0.50f, 0.80f)),
        ["glacier"]     = new(new(0.70f, 0.30f, 0.65f), new(0.95f, 0.55f, 0.90f)),
        ["snowfield"]   = new(new(0.60f, 0.20f, 0.55f), new(0.85f, 0.45f, 0.80f)),
        ["cold steppe"] = new(new(0.50f, 0.15f, 0.40f), new(0.75f, 0.35f, 0.65f)),

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

        // --- the settled country --- (farmland is by family, below)
        ["burg"]            = new(new(0.40f, 0.10f, 0.30f), new(0.65f, 0.30f, 0.50f)),
        ["hamlet"]          = new(new(0.10f, 0.10f, 0.25f), new(0.30f, 0.25f, 0.45f)),
        ["townlet"]         = new(new(0.05f, 0.15f, 0.20f), new(0.25f, 0.35f, 0.40f)),
        ["fort"]            = new(new(0.30f, 0.40f, 0.40f), new(0.55f, 0.65f, 0.65f)),
        ["sheepfold"]       = new(new(0.25f, 0.05f, 0.30f), new(0.45f, 0.20f, 0.50f)),
        ["ranch"]           = new(new(0.10f, 0.15f, 0.25f), new(0.30f, 0.35f, 0.45f)),
        ["pasture"]         = new(new(0.40f, 0.10f, 0.35f), new(0.65f, 0.25f, 0.55f)),
        ["citadel"]         = new(new(0.30f, 0.35f, 0.30f), new(0.55f, 0.60f, 0.55f)),
        ["palace"]          = new(new(0.25f, 0.30f, 0.40f), new(0.50f, 0.55f, 0.60f)),
        ["imperial school"] = new(new(0.20f, 0.10f, 0.40f), new(0.40f, 0.25f, 0.60f)),
        ["fortress"]        = new(new(0.40f, 0.45f, 0.35f), new(0.65f, 0.70f, 0.60f)),
        ["temple"]          = new(new(0.55f, 0.05f, 0.45f), new(0.80f, 0.20f, 0.65f)),
        ["imperial temple"] = new(new(0.65f, 0.15f, 0.45f), new(0.88f, 0.35f, 0.70f)),
        ["commandery"]      = new(new(0.40f, 0.35f, 0.55f), new(0.65f, 0.60f, 0.80f)),
        ["sanctuary"]       = new(new(0.45f, 0.10f, 0.65f), new(0.70f, 0.30f, 0.85f)),
        ["burial field"]    = new(new(0.65f, 0.30f, 0.60f), new(0.88f, 0.55f, 0.85f)),
        ["pyramid"]         = new(new(0.60f, 0.45f, 0.75f), new(0.85f, 0.70f, 0.92f)),
        ["wreck"]           = new(new(0.55f, 0.35f, 0.60f), new(0.80f, 0.60f, 0.85f)),
        ["ruin"]            = new(new(0.55f, 0.25f, 0.60f), new(0.80f, 0.55f, 0.85f)),
    };

    /// <summary>The farmland, by family: an apple orchard plays like a cherry orchard.</summary>
    private static readonly Dictionary<Cathedral.Glyph.Microworld.SettlementTable.Family, MoodRange> FarmlandProfiles = new()
    {
        [Cathedral.Glyph.Microworld.SettlementTable.Family.Field]      = new(new(0.05f, 0.05f, 0.10f), new(0.25f, 0.20f, 0.35f)),
        [Cathedral.Glyph.Microworld.SettlementTable.Family.Orchard]    = new(new(0.10f, 0.05f, 0.25f), new(0.30f, 0.20f, 0.45f)),
        [Cathedral.Glyph.Microworld.SettlementTable.Family.Grove]      = new(new(0.20f, 0.05f, 0.35f), new(0.40f, 0.20f, 0.55f)),
        [Cathedral.Glyph.Microworld.SettlementTable.Family.Plantation] = new(new(0.25f, 0.30f, 0.30f), new(0.50f, 0.55f, 0.55f)),
        [Cathedral.Glyph.Microworld.SettlementTable.Family.Cellar]     = new(new(0.45f, 0.25f, 0.55f), new(0.70f, 0.50f, 0.80f)),
        [Cathedral.Glyph.Microworld.SettlementTable.Family.Garden]     = new(new(0.20f, 0.05f, 0.50f), new(0.40f, 0.20f, 0.70f)),
        [Cathedral.Glyph.Microworld.SettlementTable.Family.Paddy]      = new(new(0.15f, 0.05f, 0.40f), new(0.35f, 0.20f, 0.60f)),
        [Cathedral.Glyph.Microworld.SettlementTable.Family.Vineyard]   = new(new(0.10f, 0.10f, 0.20f), new(0.30f, 0.25f, 0.40f)),
    };

    /// <summary>
    /// How festive each kind of place is, as a range sampled like the others. Absent means 0: the
    /// wilds, the holy places and the dead ones never dance. Above about 0.5 the festive palette
    /// leads (jigs and reels on fiddle, whistle and pipes); below it, it only colours the others.
    /// A town or a village rolls anywhere in its range, so some of them are lively and some are not.
    /// </summary>
    private static readonly Dictionary<string, (float Min, float Max)> Festivity = new()
    {
        ["tavern"]      = (0.80f, 1.00f),
        ["market"]      = (0.60f, 0.90f),
        ["port"]        = (0.55f, 0.90f),
        ["circus"]      = (0.70f, 1.00f),
        ["stadium"]     = (0.50f, 0.80f),
        ["amphitheater"] = (0.35f, 0.70f),
        ["city"]        = (0.35f, 0.75f),
        ["village"]     = (0.30f, 0.70f),
        ["townlet"]     = (0.35f, 0.75f),
        ["hamlet"]      = (0.15f, 0.50f),
        ["forum"]       = (0.20f, 0.55f),
        ["burg"]        = (0.15f, 0.45f),
        ["villa"]       = (0.15f, 0.45f),
        ["farm"]        = (0.10f, 0.40f),
        ["ranch"]       = (0.10f, 0.40f),
        ["stable"]      = (0.05f, 0.30f),
        ["workshop"]    = (0.05f, 0.30f),
        ["oasis"]       = (0.10f, 0.40f),
    };

    /// <summary>
    /// How holy each kind of place is. Absent means 0. From about 0.5 the sacred palette leads and the
    /// piece is chant; a castle, a palace or a citadel reaches it only now and then (its chapel), and
    /// the wild high or ancient places stay under it, where it only lets a distant choir drift in.
    /// </summary>
    private static readonly Dictionary<string, (float Min, float Max)> Sacred = new()
    {
        ["cathedral"]   = (0.80f, 1.00f),
        ["temple"]      = (0.75f, 1.00f),
        ["imperial temple"] = (0.75f, 1.00f),
        ["monastery"]   = (0.70f, 0.95f),
        ["church"]      = (0.65f, 0.95f),
        ["sanctuary"]   = (0.60f, 0.90f),
        ["shrine"]      = (0.55f, 0.85f),
        ["commandery"]  = (0.25f, 0.55f),
        ["palace"]      = (0.15f, 0.50f),
        ["castle"]      = (0.15f, 0.48f),
        ["citadel"]     = (0.15f, 0.45f),
        ["pyramid"]     = (0.25f, 0.45f),
        ["catacombs"]   = (0.25f, 0.45f),
        ["burial field"] = (0.25f, 0.45f),
        ["peak"]        = (0.25f, 0.45f),
        ["grove"]       = (0.20f, 0.40f),
        ["snowfield"]   = (0.15f, 0.35f),
        ["glacier"]     = (0.15f, 0.35f),
        ["ruins"]       = (0.15f, 0.35f),
        ["ruin"]        = (0.15f, 0.35f),
        ["ancient_city"] = (0.15f, 0.35f),
        ["sunken_city"] = (0.10f, 0.30f),
        ["ice_cave"]    = (0.10f, 0.30f),
    };

    /// <summary>
    /// How far each kind of place is from the empire's ear. Absent means 0. From about 0.5 the far
    /// palette leads (jungles, deserts, oases, the hot steppe, the oldest ruins of other peoples);
    /// below it a strange voice sits in now and then (ports and markets, where strangers pass).
    /// </summary>
    private static readonly Dictionary<string, (float Min, float Max)> Far = new()
    {
        ["jungle"]      = (0.75f, 1.00f),
        ["desert"]      = (0.70f, 0.95f),
        ["oasis"]       = (0.60f, 0.90f),
        ["hot steppe"]  = (0.55f, 0.85f),
        ["pyramid"]     = (0.55f, 0.85f),
        ["sunken_city"] = (0.45f, 0.75f),
        ["reef"]        = (0.30f, 0.55f),
        ["swamp"]       = (0.25f, 0.45f),
        ["canyon"]      = (0.25f, 0.45f),
        ["ancient_city"] = (0.20f, 0.40f),
        ["port"]        = (0.20f, 0.45f),
        ["market"]      = (0.15f, 0.40f),
        ["forum"]       = (0.10f, 0.30f),
    };

    private static readonly MoodRange Fallback =
        new(new(0.25f, 0.15f, 0.30f), new(0.50f, 0.35f, 0.55f));

    /// <summary>
    /// Lowest Coldness any location may play at, however warm its profile says it is.
    /// <para>
    /// The table above was authored when the axis was called Sadness and 0 meant "bright", so its
    /// warm end (tavern/market/field/circus at 0.05) asked for outright major-key music. Rather
    /// than rewrite fifty hand-tuned rows and lose their *relative* character, the sampled value
    /// is compressed into [<see cref="ColdnessFloor"/>, 1] on the way out: a tavern is still the
    /// warmest thing on the sphere, it is just no longer a cheerful one. Tune the whole world's
    /// warmth from this one constant.
    /// </para>
    /// </summary>
    private const float ColdnessFloor = 0.32f;

    /// <summary>
    /// Returns a deterministic mood for a given location type and vertex/location id.
    /// The mood is sampled uniformly from the predefined min–max range so each
    /// location type has a consistent character but individual locations feel distinct,
    /// then Coldness is lifted into [<see cref="ColdnessFloor"/>, 1] — see that constant.
    /// Intensity is always sampled in the upper half (0.70–1.0) so all locations play
    /// with reasonably full layering.
    /// </summary>
    /// <summary>Every kind of place that has a profile, for the census.</summary>
    public static IEnumerable<string> Kinds => Profiles.Keys;

    public static MusicMoodState SampleMood(string locationType, int locationId)
    {
        var rng = new Random(locationId ^ 0x3F7A2C1);   // XOR salt to avoid seed 0 collapse
        string key = locationType.ToLowerInvariant();
        var p = Profiles.TryGetValue(key, out var profile) ? profile
              : Cathedral.Glyph.Microworld.SettlementTable.AllAgriculture.Contains(key)
                    ? FarmlandProfiles[Cathedral.Glyph.Microworld.SettlementTable.FamilyOf(key)]
                    : Fallback;

        float Lerp(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        float coldness = Lerp(p.Min.Coldness, p.Max.Coldness);
        coldness = ColdnessFloor + coldness * (1f - ColdnessFloor);

        float fear = Lerp(p.Min.Fear, p.Max.Fear);
        float mystery = Lerp(p.Min.Mystery, p.Max.Mystery);
        float intensity = 0.70f + (float)rng.NextDouble() * 0.30f;   // 0.70–1.0
        // Drawn last, so every place keeps the coldness, fear and mystery it had before festivity existed.
        float festivity = Festivity.TryGetValue(key, out var f) ? Lerp(f.Min, f.Max) : 0f;
        // Last again, for the same reason.
        float sacred = Sacred.TryGetValue(key, out var h) ? Lerp(h.Min, h.Max) : 0f;
        float far = Far.TryGetValue(key, out var r) ? Lerp(r.Min, r.Max) : 0f;
        return new MusicMoodState(coldness, fear, mystery, intensity, festivity, sacred, far);
    }
}
