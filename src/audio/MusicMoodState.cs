namespace Cathedral.Audio;

/// <summary>
/// High-level mood parameters that drive procedural music generation.
/// All values are clamped to [0, 1].
/// </summary>
public struct MusicMoodState
{
    /// <summary>
    /// 0 = cool and detached, 1 = deeply mournful. Drives scale choice, contour and tempo.
    /// <para>
    /// Named for what its <b>low</b> end means. This axis has no happy end: 0 is not "bright" or
    /// "hopeful", it is merely the least grieving the music ever gets — hollow, still, unhurried.
    /// The scale palette contains no major-third mode at any value (see <see cref="ModalScale"/>),
    /// the melodic walk leans downward at every value, and the rhythm sets range from measured to
    /// dragging. Turning this down makes the music emptier, not cheerier.
    /// </para>
    /// </summary>
    public float Coldness;

    /// <summary>0 = calm, 1 = terrifying. Drives dissonance, broken rhythms, and velocity spikes.</summary>
    public float Fear;

    /// <summary>0 = grounded, 1 = otherworldly. Drives use of sparse/modal intervals.</summary>
    public float Mystery;

    /// <summary>
    /// 0 = silence, 1 = full layering. Controls per-track volume thresholds:
    /// 0.00–0.10 Drone fades in, 0.25–0.35 Melody, 0.50–0.60 Counter, 0.75–0.85 Texture.
    /// </summary>
    public float Intensity;

    public MusicMoodState(float coldness, float fear, float mystery, float intensity = 1.0f)
    {
        Coldness   = Math.Clamp(coldness,   0f, 1f);
        Fear      = Math.Clamp(fear,      0f, 1f);
        Mystery   = Math.Clamp(mystery,   0f, 1f);
        Intensity = Math.Clamp(intensity, 0f, 1f);
    }

    // Presets sit noticeably higher on Coldness than they used to. The old floor (Tavern at 0.00,
    // Neutral at 0.20) was written when 0 meant "bright", and it put the common cases squarely in
    // major-scale territory. Nothing now goes below ~0.30: the warmest room in the world is still
    // a cold one.

    /// <summary>Neutral starting mood: still and contemplative, not cheerful.</summary>
    public static readonly MusicMoodState Neutral = new(0.38f, 0.10f, 0.25f);

    // ── Game-state presets ────────────────────────────────────────────────────
    /// <summary>Protagonist creation: self-reflective, inward.</summary>
    public static readonly MusicMoodState Creation = new(0.50f, 0.05f, 0.35f);

    /// <summary>Childhood reminiscence: mournful, dreamlike, mysterious.</summary>
    public static readonly MusicMoodState Childhood = new(0.72f, 0.05f, 0.62f);

    /// <summary>World exploration: alert, exposed.</summary>
    public static readonly MusicMoodState WorldView = new(0.65f, 0.60f, 0.0f, 0.25f);

    // ── Archetype presets (for PoC demonstration) ─────────────────────────────
    /// <summary>Tavern: the warmest place in the world — Dorian, measured pulse, still not a dance.</summary>
    public static readonly MusicMoodState Tavern = new(0.32f, 0.28f, 0.05f);

    /// <summary>Tense chase/battle: fast BPM, staccato, urgent feel.</summary>
    public static readonly MusicMoodState Battle = new(0.35f, 0.92f, 0.10f);

    /// <summary>Dark dungeon: mournful, fearful, highly mysterious with vast silences.</summary>
    public static readonly MusicMoodState DarkDungeon = new(0.90f, 0.45f, 0.92f);

    /// <summary>Cathedral lament: deeply mournful and calm, moderately mysterious.</summary>
    public static readonly MusicMoodState Lament = new(0.88f, 0.02f, 0.38f);

    /// <summary>Returns a copy with Coldness changed by delta, clamped.</summary>
    public MusicMoodState WithColdness(float delta) =>
        new(Coldness + delta, Fear, Mystery, Intensity);

    /// <summary>Returns a copy with Fear changed by delta, clamped.</summary>
    public MusicMoodState WithFear(float delta) =>
        new(Coldness, Fear + delta, Mystery, Intensity);

    /// <summary>Returns a copy with Mystery changed by delta, clamped.</summary>
    public MusicMoodState WithMystery(float delta) =>
        new(Coldness, Fear, Mystery + delta, Intensity);

    /// <summary>Returns a copy with Intensity changed by delta, clamped.</summary>
    public MusicMoodState WithIntensity(float delta) =>
        new(Coldness, Fear, Mystery, Intensity + delta);

    public override string ToString() =>
        $"Coldness={Coldness:F2}  Fear={Fear:F2}  Mystery={Mystery:F2}  Intensity={Intensity:F2}";
}
