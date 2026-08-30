namespace Cathedral.Audio;

/// <summary>
/// Medieval modal scales expressed as MIDI note arrays spanning 3 octaves,
/// rooted at A3 (MIDI 57). Each entry is an absolute MIDI note number.
/// <para>
/// <b>Every scale in this palette has a minor or ambiguous third.</b> The major third is the
/// single interval that makes a line read as cheerful, so the major-third modes (Ionian, the
/// major scale; Mixolydian; Lydian) are deliberately absent — there is no value of any mood
/// axis that can produce a happy key. WholeTone is the one entry that does contain a major
/// third, and it is the "ambiguous" case rather than an oversight: with no perfect fifth it
/// cannot form a major triad, which is why it sounds weightless instead of bright.
/// The <see cref="GetScaleForMood"/> ladder therefore runs
/// from *cool* to *grieving* rather than from *bright* to *dark*: at Coldness 0 the music is
/// hollow and detached, never sunny.
/// </para>
/// </summary>
public static class ModalScale
{
    // Root = A3 = MIDI 57
    private const int Root = 57;

    // Interval sets (semitones from root, one octave)
    private static readonly int[] DorianIntervals       = { 0, 2, 3, 5, 7, 9, 10, 12 }; // A B C D E F# G  — minor 3rd, major 6th: cool and hollow, the least bleak mode available
    private static readonly int[] AeolianIntervals      = { 0, 2, 3, 5, 7, 8, 10, 12 }; // A B C D E F G   — natural minor, melancholic
    private static readonly int[] PhrygianIntervals     = { 0, 1, 3, 5, 7, 8, 10, 12 }; // A Bb C D E F G  — b2: dark, haunting
    private static readonly int[] LocrianIntervals      = { 0, 1, 3, 5, 6, 8, 10, 12 }; // A Bb C D Eb F G — dissonant, scary: b5 makes it unstable
    private static readonly int[] PentatonicMinorIntervals = { 0, 3, 5, 7, 10, 12 };    // A C D E G       — sparse, otherworldly, no leading tone
    private static readonly int[] WholeToneIntervals    = { 0, 2, 4, 6, 8, 10, 12 };    // A B C# Eb F G   — symmetrical, no tonal centre, uncanny
    private static readonly int[] HarmonicMinorIntervals = { 0, 2, 3, 5, 7, 8, 11, 12 }; // A B C D E F G#  — raised 7th over a minor triad: severe, grieving

    public static readonly int[] Dorian          = BuildScale(DorianIntervals,          octaves: 3);
    public static readonly int[] Aeolian         = BuildScale(AeolianIntervals,         octaves: 3);
    public static readonly int[] Phrygian        = BuildScale(PhrygianIntervals,        octaves: 3);
    public static readonly int[] Locrian         = BuildScale(LocrianIntervals,         octaves: 3);
    public static readonly int[] PentatonicMinor = BuildScale(PentatonicMinorIntervals, octaves: 3);
    public static readonly int[] WholeTone       = BuildScale(WholeToneIntervals,       octaves: 3);
    public static readonly int[] HarmonicMinor   = BuildScale(HarmonicMinorIntervals,   octaves: 3);

    /// <summary>
    /// Selects the appropriate scale based on coldness, mystery, and fear.
    /// Coldness runs from Dorian (cool, hollow — the *least* bleak mode, not a bright one) down
    /// through Aeolian and Phrygian to HarmonicMinor (severe, grieving).
    /// Fear pushes toward Locrian (dissonant b5 — scary, unstable).
    /// Mystery pushes toward WholeTone (no tonal centre — uncanny) or PentatonicMinor (sparse).
    /// </summary>
    public static int[] GetScaleForMood(float coldness, float mystery, float fear, Random rng, float sessionTension = 0f)
    {
        // Session tension darkens mood gradually — long sessions drift darker
        float effectiveColdness = Math.Clamp(coldness + sessionTension * 0.20f, 0f, 1f);
        float effectiveFear    = Math.Clamp(fear    + sessionTension * 0.12f, 0f, 1f);

        // High fear: Locrian — half-step from tonic creates maximum dissonance
        if (effectiveFear > 0.65f && rng.NextDouble() < (effectiveFear - 0.65f) * 2.5)
            return Locrian;

        // High mystery: WholeTone — perfectly symmetrical, no resolution possible, deeply uncanny
        if (mystery > 0.65f && rng.NextDouble() < (mystery - 0.65f) * 1.4)
            return WholeTone;

        // Moderate mystery: PentatonicMinor (sparse, otherworldly)
        if (mystery > 0.45f && rng.NextDouble() < (mystery - 0.45f) * 0.85)
            return PentatonicMinor;

        // Low coldness + curiosity used to be Lydian — bright wonder, the raised 4th of discovery.
        // PentatonicMinor takes that slot instead: the same open, unresolved, gap-toothed quality,
        // but hollow rather than radiant. Wonder in this world is a cold feeling.
        if (effectiveColdness < 0.30f && mystery > 0.35f && rng.NextDouble() < (mystery - 0.35f) * 0.80)
            return PentatonicMinor;

        // Harmonic Minor: severe and grieving; moderate-high coldness + some fear
        if (effectiveColdness > 0.55f && effectiveFear > 0.25f && rng.NextDouble() < (effectiveColdness - 0.55f) * 1.2)
            return HarmonicMinor;

        return effectiveColdness switch
        {
            < 0.25f => Dorian,        // cool, hollow, self-possessed — the floor of the palette
            < 0.50f => Aeolian,       // natural minor
            < 0.75f => Phrygian,      // b2 — dark, haunting
            _       => HarmonicMinor, // severe, grieving
        };
    }

    /// <summary>Returns a human-readable name for the given scale array.</summary>
    public static string GetScaleName(int[] scale)
    {
        if (scale == Dorian)          return "Dorian (cool)";
        if (scale == Aeolian)         return "Aeolian";
        if (scale == Phrygian)        return "Phrygian";
        if (scale == Locrian)         return "Locrian (scary)";
        if (scale == PentatonicMinor) return "PentatonicMinor";
        if (scale == WholeTone)       return "WholeTone (uncanny)";
        if (scale == HarmonicMinor)   return "HarmonicMinor (grieving)";
        return "Custom";
    }

    /// <summary>
    /// Returns the inclusive [minIndex, maxIndex] into the scale array for a given track role.
    /// Roles occupy distinct pitch zones — Drone=bass, Counter=tenor, Melody=soprano, Texture=high.
    /// Slight overlaps at boundaries allow natural voice leading between adjacent roles.
    /// </summary>
    public static (int min, int max) GetNoteRange(int scaleLen, TrackRole role)
    {
        int q = scaleLen / 4;
        return role switch
        {
            TrackRole.Drone   => (0,         q),           // lowest quarter — bass pedal
            TrackRole.Counter => (q / 2,     q + q / 2),  // lower-mid — tenor, below melody
            TrackRole.Melody  => (q + q / 2, q * 3),      // middle-upper — soprano cantus
            TrackRole.Texture => (q * 2 + 1, scaleLen - 1), // high — ornamental decoration
            TrackRole.Noise   => (0,         q * 2),      // lower half — deep background wash
            _                 => (0, scaleLen - 1),
        };
    }

    // ── private helpers ──────────────────────────────────────────────────────

    private static int[] BuildScale(int[] intervals, int octaves)
    {
        var notes = new List<int>();
        for (int oct = 0; oct < octaves; oct++)
        {
            foreach (int interval in intervals)
            {
                // Skip duplicate root at top of each octave (already added as bottom of next)
                if (oct > 0 && interval == 0) continue;
                int note = Root + oct * 12 + interval;
                if (note is >= 0 and <= 127)
                    notes.Add(note);
            }
        }
        return notes.ToArray();
    }
}
