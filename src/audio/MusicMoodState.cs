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

    /// <summary>
    /// 0 = no one is celebrating, 1 = a crowded tavern on a feast night. Pulls the music towards the
    /// festive palette: Celtic dance tunes (jigs and reels) on fiddle, whistle and pipes over a strummed
    /// lute and a bodhrán. Set by the place, not the moment: taverns, markets, ports, towns.
    /// <para>
    /// It does not lift the ban on a happy key. Dance tunes stay in Dorian and Aeolian, which is where
    /// a great many of the old session tunes live anyway: festive is a lively room, not a bright one.
    /// </para>
    /// </summary>
    public float Festivity;

    /// <summary>
    /// How holy a place is: 0 (most) to 1 (a cathedral). Where it leads (temples, churches,
    /// monasteries, shrines, now and then a castle's chapel) the piece is chant: a choir or an organ
    /// on a slow stepwise line, a second voice in fourths and fifths, a drone, bells, a long hall.
    /// Lower (a peak, a snowfield, a grove, old ruins) it only lets a distant wordless choir drift
    /// through some sections. Set by the place, not the moment; a fight ignores it, night keeps it.
    /// </summary>
    public float Sacred;

    /// <summary>
    /// How far a place is from the empire's ear: 0 (home) to 1 (a jungle, a deep desert). Where it
    /// leads, the piece is strange: modes with a raised fourth or a lowered fifth, a third between
    /// minor and major, notes that slide into place, beating metal, a buzzing string, a reed, bowed
    /// glass, a drum that bends, an uneven 3+3+2 pulse. Lower (a port, a market) one strange voice
    /// sits in for a section, bending its notes. Strange, not borrowed: nothing here imitates a
    /// real people's music. Set by the place; a fight ignores it, night keeps it.
    /// </summary>
    public float Far;

    public MusicMoodState(float coldness, float fear, float mystery, float intensity = 1.0f, float festivity = 0f, float sacred = 0f, float far = 0f)
    {
        Far       = Math.Clamp(far,       0f, 1f);
        Sacred    = Math.Clamp(sacred,    0f, 1f);
        Coldness   = Math.Clamp(coldness,   0f, 1f);
        Fear      = Math.Clamp(fear,      0f, 1f);
        Mystery   = Math.Clamp(mystery,   0f, 1f);
        Intensity = Math.Clamp(intensity, 0f, 1f);
        Festivity = Math.Clamp(festivity, 0f, 1f);
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

    /// <summary>Getting up after the reminiscence: spent and alone under one tree, waking from the dream.</summary>
    public static readonly MusicMoodState GetUp = new(0.60f, 0.15f, 0.15f, 0.6f);

    /// <summary>World exploration: alert, exposed.</summary>
    public static readonly MusicMoodState WorldView = new(0.65f, 0.60f, 0.0f, 0.25f);

    // ── Archetype presets (for PoC demonstration) ─────────────────────────────
    /// <summary>Tavern: the warmest place in the world, and the one where they dance — jigs and reels.</summary>
    public static readonly MusicMoodState Tavern = new(0.32f, 0.28f, 0.05f, 1.0f, 0.85f);

    /// <summary>The same tavern on a quiet night: the old preset, before the dancing came in.</summary>
    public static readonly MusicMoodState QuietTavern = new(0.32f, 0.28f, 0.05f);

    /// <summary>Tense chase/battle: fast BPM, staccato, urgent feel.</summary>
    public static readonly MusicMoodState Battle = new(0.35f, 0.92f, 0.10f);

    /// <summary>Dark dungeon: mournful, fearful, highly mysterious with vast silences.</summary>
    public static readonly MusicMoodState DarkDungeon = new(0.90f, 0.45f, 0.92f);

    /// <summary>Cathedral lament: deeply mournful and calm, moderately mysterious.</summary>
    public static readonly MusicMoodState Lament = new(0.88f, 0.02f, 0.38f);

    /// <summary>Returns a copy with Coldness changed by delta, clamped.</summary>
    public MusicMoodState WithColdness(float delta) =>
        new(Coldness + delta, Fear, Mystery, Intensity, Festivity, Sacred, Far);

    /// <summary>Returns a copy with Fear changed by delta, clamped.</summary>
    public MusicMoodState WithFear(float delta) =>
        new(Coldness, Fear + delta, Mystery, Intensity, Festivity, Sacred, Far);

    /// <summary>Returns a copy with Mystery changed by delta, clamped.</summary>
    public MusicMoodState WithMystery(float delta) =>
        new(Coldness, Fear, Mystery + delta, Intensity, Festivity, Sacred, Far);

    /// <summary>Returns a copy with Festivity changed by delta, clamped.</summary>
    public MusicMoodState WithFestivity(float delta) =>
        new(Coldness, Fear, Mystery, Intensity, Festivity + delta, Sacred, Far);

    /// <summary>Returns a copy with Sacred changed by delta, clamped.</summary>
    public MusicMoodState WithSacred(float delta) =>
        new(Coldness, Fear, Mystery, Intensity, Festivity, Sacred + delta, Far);

    /// <summary>Returns a copy with Far changed by delta, clamped.</summary>
    public MusicMoodState WithFar(float delta) =>
        new(Coldness, Fear, Mystery, Intensity, Festivity, Sacred, Far + delta);

    /// <summary>
    /// The same place at night: colder, a little stranger and more uneasy, nobody dancing, and
    /// sparser (lower Intensity thins the ostinato and the percussion first). Each place keeps its
    /// own character, shifted, so every location has its own night music.
    /// </summary>
    public MusicMoodState AtNight() =>
        new(Coldness + 0.18f, Fear + 0.06f, Mystery + 0.14f, Intensity * 0.82f, 0f, Sacred, Far);

    /// <summary>Returns a copy with Intensity changed by delta, clamped.</summary>
    public MusicMoodState WithIntensity(float delta) =>
        new(Coldness, Fear, Mystery, Intensity + delta, Festivity, Sacred, Far);

    public override string ToString() =>
        $"Coldness={Coldness:F2}  Fear={Fear:F2}  Mystery={Mystery:F2}  Intensity={Intensity:F2}  Festivity={Festivity:F2}  Sacred={Sacred:F2}  Far={Far:F2}";
}
