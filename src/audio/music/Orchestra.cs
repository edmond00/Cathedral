using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Audio.Music;

/// <summary>How the counter voice relates to the melody, chosen per section.</summary>
internal enum CounterStyle
{
    /// <summary>The melody doubled a fifth (or a fourth) below, note for note: medieval organum.</summary>
    Organum,
    /// <summary>A slow line of chord tones under the melody, moving against it.</summary>
    Tenor,
    /// <summary>Answers: the counter plays a fragment of the motif while the melody holds a cadence.</summary>
    Echo,
}

internal enum OstinatoStyle { Arpeggio, Broken, Pulse }

/// <summary>Who plays what in one piece. Chosen once per piece from the mood; varied per section.</summary>
internal sealed class Orchestration
{
    public Instrument Melody, MelodyAlt, Counter, Pad, PadAlt, Drone, Ostinato, Bass, Perc;
    public bool Bell, Ambience, Percussion;
    public OstinatoStyle OstinatoStyle;
    public int[] OstinatoPattern = Array.Empty<int>();
    public double OstinatoStep;          // beats per ostinato note
    public bool OpenVoicing;             // pads in open fifths rather than triads
    public float Liveliness, Chant, Fall;
    public string Pole = "";
}

/// <summary>
/// The genre's poles as instrument palettes, and the mood's pull towards each. A mood is a blend,
/// so a piece draws each role from the blend: a tavern is mostly comfy (recorder, harpsichord, hand
/// drum, strings), a dungeon mostly dark (organ, choir, timpani, a sweeping pad, wind), a lament
/// melancholic (harp, strings, choir "ooh", bells), a fight the fear palette (horns, low pulse).
/// </summary>
internal static class Orchestra
{
    private sealed record Pole(
        string Name,
        (Instrument i, double w)[] Melody,
        (Instrument i, double w)[] Ostinato,
        (Instrument i, double w)[] Pad,
        (Instrument i, double w)[] Counter,
        (Instrument i, double w)[] Bass,
        Instrument Perc, double PercChance, double BellChance, double AmbienceChance,
        float Liveliness, float Chant);

    private static readonly Pole Comfy = new("comfy",
        new[] { (Instrument.Recorder, 4.0), (Instrument.OrganFlute, 2.0), (Instrument.Harpsichord, 1.0), (Instrument.Lute, 1.0) },
        new[] { (Instrument.Harpsichord, 3.0), (Instrument.Lute, 3.0), (Instrument.Harp, 1.0) },
        new[] { (Instrument.Strings, 3.0), (Instrument.OrganFlute, 2.0), (Instrument.ChoirOoh, 1.0) },
        new[] { (Instrument.OrganFlute, 2.0), (Instrument.Recorder, 1.0), (Instrument.Strings, 2.0) },
        new[] { (Instrument.Strings, 1.0), (Instrument.OrganFlute, 1.0) },
        Instrument.HandDrum, 0.7, 0.15, 0.0, 0.75f, 0.2f);

    private static readonly Pole Melancholy = new("melancholy",
        new[] { (Instrument.Recorder, 2.0), (Instrument.Strings, 1.5), (Instrument.Horn, 1.0), (Instrument.Choir, 1.0), (Instrument.Harp, 1.0) },
        new[] { (Instrument.Harp, 4.0), (Instrument.Lute, 2.0) },
        new[] { (Instrument.Strings, 3.0), (Instrument.ChoirOoh, 3.0), (Instrument.DarkPad, 1.0) },
        new[] { (Instrument.Strings, 2.0), (Instrument.Horn, 1.0), (Instrument.OrganFlute, 1.0) },
        new[] { (Instrument.Strings, 1.0), (Instrument.Bourdon, 1.0) },
        Instrument.Timpani, 0.2, 0.45, 0.15, 0.4f, 0.35f);

    private static readonly Pole Dark = new("dark",
        new[] { (Instrument.Organ, 3.0), (Instrument.Choir, 3.0), (Instrument.Horn, 2.0), (Instrument.OrganFlute, 1.0) },
        new[] { (Instrument.Harp, 2.0), (Instrument.Harpsichord, 1.0), (Instrument.Organ, 1.0) },
        new[] { (Instrument.DarkPad, 3.0), (Instrument.Choir, 2.0), (Instrument.Organ, 2.0), (Instrument.ChoirOoh, 1.0) },
        new[] { (Instrument.Choir, 2.0), (Instrument.Organ, 2.0), (Instrument.Horn, 1.0) },
        new[] { (Instrument.Organ, 1.0), (Instrument.Bourdon, 1.0) },
        Instrument.Timpani, 0.55, 0.6, 0.5, 0.15f, 0.6f);

    private static readonly Pole Fear = new("fear",
        new[] { (Instrument.Horn, 3.0), (Instrument.Choir, 2.0), (Instrument.Organ, 1.0) },
        new[] { (Instrument.Strings, 2.0), (Instrument.Harp, 1.0), (Instrument.Timpani, 2.0) },
        new[] { (Instrument.DarkPad, 4.0), (Instrument.Choir, 1.0) },
        new[] { (Instrument.Strings, 2.0), (Instrument.Horn, 1.0) },
        new[] { (Instrument.Strings, 1.0), (Instrument.Bourdon, 1.0) },
        Instrument.Timpani, 0.85, 0.3, 0.4, 0.55f, 0.3f);

    /// <summary>The mood's weight on each pole.</summary>
    private static (Pole p, double w)[] Weights(MusicMoodState m)
    {
        double c = m.Coldness, f = m.Fear, y = m.Mystery;
        double comfy = Math.Pow(Math.Max(0, 1 - c * 1.6), 1.5) * (1 - f) * (1 - y * 0.7) + 0.02;
        double mel = Math.Max(0, 1 - Math.Abs(c - 0.6) * 2.5) * (1 - f * 0.8) + 0.02;
        double dark = Math.Max(0, c - 0.45) * 1.5 + y * 0.8;
        double fear = Math.Max(0, f - 0.35) * 2.5;
        return new[] { (Comfy, comfy), (Melancholy, mel), (Dark, dark), (Fear, fear) };
    }

    private static Instrument Draw(IEnumerable<(Pole p, double w)> poles, Func<Pole, (Instrument i, double w)[]> role, Random rng,
                                   Instrument? avoid = null)
    {
        var pool = new Dictionary<Instrument, double>();
        foreach (var (p, w) in poles)
            foreach (var (i, iw) in role(p))
                pool[i] = pool.GetValueOrDefault(i) + w * iw;
        if (avoid is { } a && pool.Count > 1) pool.Remove(a);
        double tot = pool.Values.Sum(), r = rng.NextDouble() * tot;
        foreach (var (i, w) in pool) { r -= w; if (r <= 0) return i; }
        return pool.Keys.Last();
    }

    public static Orchestration Choose(MusicMoodState m, int meter, double bpm, Random rng)
    {
        var poles = Weights(m);
        double tot = poles.Sum(p => p.w);
        var o = new Orchestration();
        var main = poles.OrderByDescending(p => p.w).First().p;
        o.Pole = main.Name;
        o.Melody = Draw(poles, p => p.Melody, rng);
        o.MelodyAlt = Draw(poles, p => p.Melody, rng, o.Melody);
        o.Ostinato = Draw(poles, p => p.Ostinato, rng);
        o.Pad = Draw(poles, p => p.Pad, rng, o.Melody);
        o.PadAlt = Draw(poles, p => p.Pad, rng, o.Pad);
        o.Counter = Draw(poles, p => p.Counter, rng, o.Melody);
        o.Bass = Draw(poles, p => p.Bass, rng);
        o.Drone = Instrument.Bourdon;
        o.Perc = main.Perc;
        double Chance(Func<Pole, double> f) => poles.Sum(p => p.w * f(p.p)) / tot;
        o.Percussion = rng.NextDouble() < Chance(p => p.PercChance);
        o.Bell = rng.NextDouble() < Chance(p => p.BellChance);
        o.Ambience = m.Mystery > 0.45f && rng.NextDouble() < Chance(p => p.AmbienceChance) + m.Mystery * 0.3;
        o.Liveliness = (float)(poles.Sum(p => p.w * p.p.Liveliness) / tot);
        o.Chant = (float)(poles.Sum(p => p.w * p.p.Chant) / tot);
        o.Fall = m.Coldness;
        o.OpenVoicing = main == Dark || main == Fear || rng.NextDouble() < m.Coldness * 0.5;

        // The ostinato's figure: chord-tone indices (0 root, 1 third, 2 fifth, 3 octave) per step.
        if (o.Ostinato == Instrument.Timpani || main == Fear && rng.NextDouble() < 0.5)
        {
            o.OstinatoStyle = OstinatoStyle.Pulse;
            o.OstinatoPattern = meter == 3 ? new[] { 0, 2, 2 } : new[] { 0, 2, 0, 2 };
            o.OstinatoStep = 1.0;
        }
        else
        {
            int[][] four = { new[] { 0, 2, 3, 2, 1, 2, 3, 2 }, new[] { 0, 1, 2, 3, 2, 1, 2, 1 }, new[] { 0, 2, 3, 2 }, new[] { 0, 3, 2, 1 }, new[] { 0, 2, 1, 2, 0, 2, 1, 2 } };
            int[][] three = { new[] { 0, 2, 3, 2, 1, 2 }, new[] { 0, 1, 2, 3, 2, 1 }, new[] { 0, 2, 3 }, new[] { 0, 2, 1, 2, 3, 2 } };
            var pool = meter == 3 ? three : four;
            o.OstinatoPattern = pool[rng.Next(pool.Length)];
            o.OstinatoStep = (double)meter / o.OstinatoPattern.Length;
            o.OstinatoStyle = o.OstinatoPattern.Length > meter ? OstinatoStyle.Arpeggio : OstinatoStyle.Broken;
            // Fast arpeggios at a brisk tempo turn into a blur; slow them to quarters.
            if (o.OstinatoStep < 0.75 && bpm > 80) { o.OstinatoPattern = o.OstinatoPattern.Where((_, i) => i % 2 == 0).ToArray(); o.OstinatoStep *= 2; }
        }
        return o;
    }

    public static double Tempo(MusicMoodState m, Random rng)
    {
        double bpm = 74 - 18 * m.Coldness + 26 * m.Fear - 8 * m.Mystery + (rng.NextDouble() - 0.5) * 8;
        return Math.Clamp(bpm, 50, 100);
    }

    public static int Meter(MusicMoodState m, Random rng)
    {
        double three = 0.25 + Math.Max(0, 0.45 - m.Coldness) * 0.8 - m.Fear * 0.2;
        return rng.NextDouble() < three ? 3 : 4;
    }

    /// <summary>Level of each layer in the mix.</summary>
    public static float PartLevel(Part p) => p switch
    {
        Part.Drone => 0.42f, Part.Pad => 0.42f, Part.Melody => 0.8f, Part.Counter => 0.5f, Part.Ostinato => 0.55f,
        Part.Bass => 0.45f, Part.Perc => 0.55f, Part.Bell => 0.4f, Part.Ambience => 0.3f, Part.Fight => 0.6f,
        _ => 1f,
    };

    /// <summary>Per-instrument trim so that the same velocity sounds about as loud on each.</summary>
    public static float Trim(Instrument i) => i switch
    {
        Instrument.Organ => 0.55f, Instrument.OrganFlute => 0.75f, Instrument.Choir => 0.85f, Instrument.ChoirOoh => 1.0f,
        Instrument.Strings => 0.85f, Instrument.DarkPad => 0.8f, Instrument.Recorder => 0.75f, Instrument.Harpsichord => 0.6f,
        Instrument.Harp => 0.95f, Instrument.Lute => 0.85f, Instrument.Bell => 0.6f, Instrument.Timpani => 0.9f,
        Instrument.HandDrum => 0.8f, Instrument.Bourdon => 0.8f, Instrument.Horn => 0.6f, Instrument.SquareLead => 0.5f,
        Instrument.Wind => 0.6f, _ => 1f,
    };

    /// <summary>The room and the tape for a mood.</summary>
    public static void ApplyBus(Synth s, MusicMoodState m)
    {
        float t60 = 2.0f + 5.0f * m.Mystery + 2.0f * m.Coldness;
        float damp = 5200f - 2400f * m.Coldness - 800f * m.Mystery;
        s.SetHall(t60, damp, 0.015f + 0.035f * m.Mystery);
        s.SetTape(9000f - 2600f * m.Coldness - 600f * m.Mystery, 3f + 3.5f * m.Mystery, -60f + 3f * m.Coldness, 1.3f + 0.5f * m.Fear);
        s.ChorusDepth.Target = 0.45f + 0.45f * m.Mystery;
        s.ReverbReturn = 0.75f + 0.3f * m.Mystery;
    }
}
