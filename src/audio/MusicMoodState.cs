namespace Cathedral.Audio;

/// <summary>
/// High-level mood parameters that drive procedural music generation.
/// All values are clamped to [0, 1].
/// </summary>
public struct MusicMoodState
{
    /// <summary>0 = bright/hopeful, 1 = deeply melancholic. Drives scale choice and tempo.</summary>
    public float Sadness;

    /// <summary>0 = calm, 1 = terrifying. Drives dissonance, broken rhythms, and velocity spikes.</summary>
    public float Fear;

    /// <summary>0 = grounded, 1 = otherworldly. Drives use of sparse/modal intervals.</summary>
    public float Mystery;

    /// <summary>
    /// 0 = silence, 1 = full layering. Controls per-track volume thresholds:
    /// 0.00–0.10 Drone fades in, 0.25–0.35 Melody, 0.50–0.60 Counter, 0.75–0.85 Texture.
    /// </summary>
    public float Intensity;

    public MusicMoodState(float sadness, float fear, float mystery, float intensity = 1.0f)
    {
        Sadness   = Math.Clamp(sadness,   0f, 1f);
        Fear      = Math.Clamp(fear,      0f, 1f);
        Mystery   = Math.Clamp(mystery,   0f, 1f);
        Intensity = Math.Clamp(intensity, 0f, 1f);
    }

    /// <summary>Neutral starting mood: slightly contemplative.</summary>
    public static readonly MusicMoodState Neutral = new(0.2f, 0.1f, 0.2f);

    // ── Game-state presets ────────────────────────────────────────────────────
    /// <summary>Protagonist creation: self-reflective, slightly melancholic.</summary>
    public static readonly MusicMoodState Creation = new(0.35f, 0.05f, 0.3f);

    /// <summary>Childhood reminiscence: sad, dreamlike, mysterious.</summary>
    public static readonly MusicMoodState Childhood = new(0.65f, 0.05f, 0.6f);

    /// <summary>World exploration: alert, mysterious.</summary>
    public static readonly MusicMoodState WorldView = new(0.3f, 0.25f, 0.45f);

    // ── Archetype presets (for PoC demonstration) ─────────────────────────────
    /// <summary>Lively tavern: major scale, lively rhythms, bright instruments.</summary>
    public static readonly MusicMoodState Tavern = new(0.0f, 0.28f, 0.02f);

    /// <summary>Tense chase/battle: fast BPM, staccato, urgent feel.</summary>
    public static readonly MusicMoodState Battle = new(0.08f, 0.92f, 0.08f);

    /// <summary>Dark dungeon: very sad, fearful, highly mysterious with vast silences.</summary>
    public static readonly MusicMoodState DarkDungeon = new(0.88f, 0.45f, 0.92f);

    /// <summary>Cathedral lament: deeply sad and calm, moderately mysterious.</summary>
    public static readonly MusicMoodState Lament = new(0.85f, 0.02f, 0.38f);

    /// <summary>Returns a copy with Sadness changed by delta, clamped.</summary>
    public MusicMoodState WithSadness(float delta) =>
        new(Sadness + delta, Fear, Mystery, Intensity);

    /// <summary>Returns a copy with Fear changed by delta, clamped.</summary>
    public MusicMoodState WithFear(float delta) =>
        new(Sadness, Fear + delta, Mystery, Intensity);

    /// <summary>Returns a copy with Mystery changed by delta, clamped.</summary>
    public MusicMoodState WithMystery(float delta) =>
        new(Sadness, Fear, Mystery + delta, Intensity);

    /// <summary>Returns a copy with Intensity changed by delta, clamped.</summary>
    public MusicMoodState WithIntensity(float delta) =>
        new(Sadness, Fear, Mystery, Intensity + delta);

    public override string ToString() =>
        $"Sadness={Sadness:F2}  Fear={Fear:F2}  Mystery={Mystery:F2}  Intensity={Intensity:F2}";
}
