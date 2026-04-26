namespace Cathedral.Audio;

/// <summary>
/// Medieval modal scales expressed as MIDI note arrays spanning 3 octaves,
/// rooted at A3 (MIDI 57). Each entry is an absolute MIDI note number.
/// </summary>
public static class ModalScale
{
    // Root = A3 = MIDI 57
    private const int Root = 57;

    // Interval sets (semitones from root, one octave)
    private static readonly int[] IonianIntervals       = { 0, 2, 4, 5, 7, 9, 11, 12 }; // A B C# D E F# G# — major scale, bright/tavern
    private static readonly int[] MixolydianIntervals   = { 0, 2, 4, 5, 7, 9, 10, 12 }; // A B C# D E F# G  — bright with flat 7th
    private static readonly int[] DorianIntervals       = { 0, 2, 3, 5, 7, 9, 10, 12 }; // A B C D E F# G  — neutral medieval
    private static readonly int[] AeolianIntervals      = { 0, 2, 3, 5, 7, 8, 10, 12 }; // A B C D E F G   — natural minor, melancholic
    private static readonly int[] PhrygianIntervals     = { 0, 1, 3, 5, 7, 8, 10, 12 }; // A Bb C D E F G  — dark, haunting
    private static readonly int[] LocrianIntervals      = { 0, 1, 3, 5, 6, 8, 10, 12 }; // A Bb C D Eb F G — dissonant, scary: b5 makes it unstable
    private static readonly int[] PentatonicMinorIntervals = { 0, 3, 5, 7, 10, 12 };    // A C D E G       — sparse, otherworldly
    private static readonly int[] WholeToneIntervals    = { 0, 2, 4, 6, 8, 10, 12 };    // A B C# Eb F G   — symmetrical, no tonal centre, uncanny

    public static readonly int[] Ionian          = BuildScale(IonianIntervals,         octaves: 3);
    public static readonly int[] Mixolydian      = BuildScale(MixolydianIntervals,     octaves: 3);
    public static readonly int[] Dorian          = BuildScale(DorianIntervals,         octaves: 3);
    public static readonly int[] Aeolian         = BuildScale(AeolianIntervals,        octaves: 3);
    public static readonly int[] Phrygian        = BuildScale(PhrygianIntervals,       octaves: 3);
    public static readonly int[] Locrian         = BuildScale(LocrianIntervals,        octaves: 3);
    public static readonly int[] PentatonicMinor = BuildScale(PentatonicMinorIntervals, octaves: 3);
    public static readonly int[] WholeTone       = BuildScale(WholeToneIntervals,      octaves: 3);

    public static readonly string[] ScaleNames = { "Ionian", "Mixolydian", "Dorian", "Aeolian", "Phrygian", "PentatonicMinor" };

    /// <summary>
    /// Selects the appropriate scale based on sadness, mystery, and fear.
    /// Sadness spans the full gamut from Ionian (major/dance) to Phrygian (dark haunting).
    /// Fear pushes toward Locrian (dissonant b5 — scary, unstable).
    /// Mystery pushes toward WholeTone (no tonal centre — uncanny) or PentatonicMinor (sparse).
    /// </summary>
    public static int[] GetScaleForMood(float sadness, float mystery, float fear, Random rng)
    {
        // High fear: Locrian — half-step from tonic creates maximum dissonance
        if (fear > 0.65f && rng.NextDouble() < (fear - 0.65f) * 2.5)
            return Locrian;

        // High mystery: WholeTone — perfectly symmetrical, no resolution possible, deeply uncanny
        if (mystery > 0.65f && rng.NextDouble() < (mystery - 0.65f) * 1.4)
            return WholeTone;

        // Moderate mystery: PentatonicMinor (sparse, otherworldly)
        if (mystery > 0.45f && rng.NextDouble() < (mystery - 0.45f) * 0.85)
            return PentatonicMinor;

        return sadness switch
        {
            < 0.15f => Ionian,
            < 0.38f => Mixolydian,
            < 0.60f => Dorian,
            < 0.82f => Aeolian,
            _       => Phrygian,
        };
    }

    /// <summary>Returns a human-readable name for the given scale array.</summary>
    public static string GetScaleName(int[] scale)
    {
        if (scale == Ionian)          return "Ionian (Major)";
        if (scale == Mixolydian)      return "Mixolydian";
        if (scale == Dorian)          return "Dorian";
        if (scale == Aeolian)         return "Aeolian";
        if (scale == Phrygian)        return "Phrygian";
        if (scale == Locrian)         return "Locrian (scary)";
        if (scale == PentatonicMinor) return "PentatonicMinor";
        if (scale == WholeTone)       return "WholeTone (uncanny)";
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
