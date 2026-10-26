using System;
using System.Collections.Generic;

namespace Cathedral.Audio.Music;

/// <summary>
/// A mode: the scale a piece lives in. The palette is <see cref="ModalScale"/>'s, deliberately kept:
/// no mode with a major third over its tonic, so no value of any mood axis produces a happy key.
/// Dorian is the warmest the music gets, which is why the "comfy" pole of the genre (Fief's Lydian
/// and Mixolydian pastorals) is played here in Dorian.
/// </summary>
internal sealed class Mode
{
    public readonly string Name;
    public readonly int[] Steps;    // semitones above the tonic, ascending, within one octave

    private Mode(string name, params int[] steps) { Name = name; Steps = steps; }

    public int Size => Steps.Length;
    public bool Heptatonic => Steps.Length == 7;

    public static readonly Mode Dorian = new("Dorian", 0, 2, 3, 5, 7, 9, 10);
    public static readonly Mode Aeolian = new("Aeolian", 0, 2, 3, 5, 7, 8, 10);
    public static readonly Mode Phrygian = new("Phrygian", 0, 1, 3, 5, 7, 8, 10);
    public static readonly Mode HarmonicMinor = new("HarmonicMinor", 0, 2, 3, 5, 7, 8, 11);
    public static readonly Mode Locrian = new("Locrian", 0, 1, 3, 5, 6, 8, 10);
    public static readonly Mode PentatonicMinor = new("PentatonicMinor", 0, 3, 5, 7, 10);

    /// <summary>The name <see cref="AmbianceEngine.CurrentScaleName"/> has always shown.</summary>
    public string DisplayName => Name switch
    {
        "Dorian" => "Dorian (cool)",
        "Locrian" => "Locrian (scary)",
        "HarmonicMinor" => "HarmonicMinor (grieving)",
        _ => Name,
    };

    /// <summary>
    /// The same ladder as <see cref="ModalScale.GetScaleForMood"/>: Dorian at the cool end, down
    /// through Aeolian and Phrygian to harmonic minor; fear pulls to Locrian, mystery to the
    /// pentatonic. Whole-tone is left out: with no fifth there is nothing for a drone, a chord or an
    /// organum to stand on, and this composer is built on all three.
    /// </summary>
    public static Mode ForMood(MusicMoodState m, Random rng)
    {
        if (m.Fear > 0.7f && rng.NextDouble() < (m.Fear - 0.7f) * 2.0) return Locrian;
        if (m.Mystery > 0.55f && rng.NextDouble() < (m.Mystery - 0.55f) * 0.9) return PentatonicMinor;
        if (m.Coldness > 0.6f && m.Fear > 0.25f && rng.NextDouble() < (m.Coldness - 0.6f) * 1.4) return HarmonicMinor;
        // A little randomness at the boundaries, so neighbouring places do not always share a mode.
        float c = m.Coldness + (float)(rng.NextDouble() - 0.5) * 0.16f + m.Fear * 0.1f;
        return c switch
        {
            < 0.42f => Dorian,
            < 0.62f => Aeolian,
            < 0.80f => Phrygian,
            _ => rng.NextDouble() < 0.5 ? HarmonicMinor : Aeolian,
        };
    }
}

/// <summary>A tonic and a mode: turns scale degrees (any integer, octaves included) into MIDI pitches.</summary>
internal readonly struct Key
{
    public readonly int Tonic;     // MIDI pitch of degree 0 (the tonic in the reference octave)
    public readonly Mode Mode;

    public Key(int tonic, Mode mode) { Tonic = tonic; Mode = mode; }

    public int Pitch(int degree)
    {
        int n = Mode.Size;
        int oct = (int)Math.Floor(degree / (double)n);
        int d = degree - oct * n;
        return Tonic + oct * 12 + Mode.Steps[d];
    }

    /// <summary>The degree nearest to (at or below) a MIDI pitch.</summary>
    public int DegreeAtOrBelow(int midi)
    {
        int d = (int)Math.Floor((midi - Tonic) / 12.0) * Mode.Size;
        while (Pitch(d + 1) <= midi) d++;
        while (Pitch(d) > midi) d--;
        return d;
    }

    public bool Contains(int midi)
    {
        int pc = ((midi - Tonic) % 12 + 12) % 12;
        return Array.IndexOf(Mode.Steps, pc) >= 0;
    }

    public static int Mod(int a, int n) => ((a % n) + n) % n;
}

/// <summary>A chord: a root degree and the degrees above it that sound with it.</summary>
internal readonly struct Chord
{
    public readonly int Root;
    public readonly int[] Tones;   // degrees, root first, relative to the key (absolute degrees)

    public Chord(int root, int[] tones) { Root = root; Tones = tones; }

    /// <summary>A triad stacked in thirds inside the mode; on a five-note mode, root, third step and fifth.</summary>
    public static Chord On(Key key, int root)
    {
        if (key.Mode.Heptatonic) return new Chord(root, new[] { root, root + 2, root + 4 });
        // Pentatonic minor (0 3 5 7 10): step 3 above any degree is a fourth or fifth; step 2 a third or fourth.
        return new Chord(root, new[] { root, root + 2, root + 3 });
    }

    public bool Has(Key key, int degree)
    {
        int n = key.Mode.Size;
        foreach (var t in Tones)
            if (Key.Mod(t - degree, n) == 0) return true;
        return false;
    }

    /// <summary>Pitch classes, for the score log and the harness.</summary>
    public int[] PitchClasses(Key key)
    {
        var pcs = new int[Tones.Length];
        for (int i = 0; i < Tones.Length; i++) pcs[i] = Key.Mod(key.Pitch(Tones[i]), 12);
        return pcs;
    }
}

/// <summary>
/// Chord plans: the genre's shuttles, written as root degrees (0 = i). Slow harmonic rhythm is the
/// composer's job; these are only the orders. Few dominant cadences: the V-i pull exists only in the
/// harmonic-minor set, which is the mode that asked for it.
/// </summary>
internal static class Progressions
{
    // A sections: open on the tonic, shuttle, come home.
    private static readonly int[][] AeolianA =
    {
        new[] { 0, 6, 5, 6 }, new[] { 0, 0, 6, 6 }, new[] { 0, 5, 2, 6 }, new[] { 0, 3, 0, 6 },
        new[] { 0, 6, 0, 6 }, new[] { 0, 5, 3, 4 }, new[] { 0, 3, 5, 6 },
    };
    private static readonly int[][] DorianA =
    {
        new[] { 0, 3, 0, 3 }, new[] { 0, 6, 0, 3 }, new[] { 0, 2, 6, 0 }, new[] { 0, 6, 3, 0 },
        new[] { 0, 0, 3, 3 }, new[] { 0, 4, 6, 0 },
    };
    private static readonly int[][] PhrygianA =
    {
        new[] { 0, 1, 0, 1 }, new[] { 0, 1, 0, 6 }, new[] { 0, 6, 5, 1 }, new[] { 0, 0, 1, 1 },
        new[] { 0, 3, 1, 0 },
    };
    private static readonly int[][] HarmonicA =
    {
        new[] { 0, 5, 4, 0 }, new[] { 0, 3, 4, 0 }, new[] { 0, 5, 3, 4 }, new[] { 0, 0, 5, 4 },
    };
    private static readonly int[][] LocrianA =
    {
        new[] { 0, 1, 0, 1 }, new[] { 0, 4, 0, 1 }, new[] { 0, 0, 1, 0 },
    };
    private static readonly int[][] PentaA =
    {
        new[] { 0, 0, 3, 0 }, new[] { 0, 4, 0, 3 }, new[] { 0, 2, 0, 4 }, new[] { 0, 3, 4, 0 },
    };

    // B sections: away from the tonic. Relative major colour (III, VI, VII) is the genre's contrast.
    private static readonly int[][] AeolianB =
    {
        new[] { 5, 6, 0, 0 }, new[] { 2, 6, 5, 6 }, new[] { 5, 2, 6, 6 }, new[] { 3, 5, 6, 6 },
        new[] { 5, 6, 2, 2 }, new[] { 3, 3, 5, 6 },
    };
    private static readonly int[][] DorianB =
    {
        new[] { 3, 3, 6, 6 }, new[] { 2, 6, 3, 0 }, new[] { 6, 3, 6, 0 }, new[] { 3, 6, 2, 6 },
    };
    private static readonly int[][] PhrygianB =
    {
        new[] { 5, 6, 1, 1 }, new[] { 2, 1, 6, 1 }, new[] { 3, 1, 3, 1 },
    };
    private static readonly int[][] HarmonicB =
    {
        new[] { 5, 3, 1, 4 }, new[] { 2, 5, 3, 4 }, new[] { 3, 0, 5, 4 },
    };

    public static int[] For(Mode mode, bool contrast, Random rng)
    {
        int[][] pool = (mode.Name, contrast) switch
        {
            ("Dorian", false) => DorianA,
            ("Dorian", true) => DorianB,
            ("Phrygian", false) => PhrygianA,
            ("Phrygian", true) => PhrygianB,
            ("HarmonicMinor", false) => HarmonicA,
            ("HarmonicMinor", true) => HarmonicB,
            ("Locrian", _) => LocrianA,
            ("PentatonicMinor", _) => PentaA,
            (_, false) => AeolianA,
            (_, true) => AeolianB,
        };
        return pool[rng.Next(pool.Length)];
    }
}
