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
    /// <summary>
    /// The session's way: a second instrument plays the same tune, an octave down or in unison, leaving
    /// out a note here and holding one there. Only the festive pieces use it.
    /// </summary>
    Heterophony,
}

/// <summary>
/// The accompaniment's figure. Comp is the strummed boom-chick under a dance tune; Riff the
/// distorted guitar under a fight, played in a section's <see cref="RiffStyle"/>.
/// </summary>
internal enum OstinatoStyle { Arpeggio, Broken, Pulse, Comp, Riff }

/// <summary>
/// How a fight's section drives. Gallop: palm-muted eighth-and-two-sixteenths under power-chord
/// hits, the drums on the same figure. Tremolo: the riff picked in sixteenths an octave up over a
/// double-bass kick. HalfTime: power chords left to ring, chugs between, the snare on three.
/// </summary>
internal enum RiffStyle { Gallop, Tremolo, HalfTime }

/// <summary>
/// Which dance a festive piece is. A jig is in 6/8, written here as two dotted-quarter beats a bar
/// with triplet eighths (Meter 2); a reel is in 4/4 in running eighths (Meter 4).
/// </summary>
internal enum Dance { None, Jig, Reel }

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
    public Dance Dance;
}

/// <summary>
/// The genre's poles as instrument palettes, and the mood's pull towards each. A mood is a blend,
/// so a piece draws each role from the blend: a quiet room is mostly comfy (recorder, harpsichord,
/// hand drum, strings), a dungeon mostly dark (organ, choir, timpani, a sweeping pad, wind), a lament
/// melancholic (harp, strings, choir "ooh", bells), a fight the fear palette (horns, low pulse), a
/// festive tavern, market or port the session (fiddle, whistle, pipes, lute, bodhrán).
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

    /// <summary>
    /// The festive pole: a session in a tavern. Fiddle, whistle and pipes on the tune, a strummed lute,
    /// the bodhrán. No bells, no wind, nothing sacred.
    /// </summary>
    private static readonly Pole Festive = new("festive",
        new[] { (Instrument.Fiddle, 3.0), (Instrument.Whistle, 3.0), (Instrument.Pipes, 1.2), (Instrument.Recorder, 0.5) },
        new[] { (Instrument.Lute, 4.0), (Instrument.Harp, 0.8) },
        new[] { (Instrument.Strings, 1.0), (Instrument.OrganFlute, 0.5) },
        new[] { (Instrument.Fiddle, 2.0), (Instrument.Whistle, 2.0), (Instrument.Pipes, 0.6) },
        new[] { (Instrument.Strings, 1.0), (Instrument.Bourdon, 0.4) },
        Instrument.Bodhran, 0.95, 0.0, 0.0, 1.0f, 0.0f);

    /// <summary>
    /// A fight: dungeon synth leaning into metal, as the genre does at its most violent. The epic
    /// lead stays the genre's (horn, choir, strings, the square lead); under it a distorted riff, a
    /// drum kit, a dark pad. No drone, no bells, no wind: nothing that waits.
    /// </summary>
    public static Orchestration ChooseFight(MusicMoodState m, Random rng)
    {
        Instrument Pick((Instrument i, double w)[] pool, Instrument? avoid = null)
        {
            var p = pool.Where(x => x.i != avoid).ToArray();
            double r = rng.NextDouble() * p.Sum(x => x.w);
            foreach (var (i, w) in p) { r -= w; if (r <= 0) return i; }
            return p[^1].i;
        }
        var lead = new[] { (Instrument.Horn, 3.0), (Instrument.Choir, 2.0), (Instrument.Strings, 1.0), (Instrument.SquareLead, 1.0), (Instrument.Organ, 1.0) };
        var pads = new[] { (Instrument.DarkPad, 3.0), (Instrument.Choir, 2.0), (Instrument.Organ, 1.0) };
        var o = new Orchestration { Pole = "fight" };
        o.Melody = Pick(lead);
        o.MelodyAlt = Pick(lead, o.Melody);
        o.Counter = Pick(new[] { (Instrument.Choir, 2.0), (Instrument.Strings, 2.0), (Instrument.Horn, 1.0) }, o.Melody);
        o.Pad = Pick(pads, o.Melody);
        o.PadAlt = Pick(pads, o.Pad);
        o.Drone = Instrument.Bourdon;
        o.Ostinato = Instrument.Distortion;
        o.Bass = Instrument.Bourdon;
        o.Perc = Instrument.Kick;
        o.Percussion = true;
        o.OstinatoStyle = OstinatoStyle.Riff;
        o.OstinatoPattern = new[] { 0 };
        o.OstinatoStep = 0.25;
        o.Liveliness = 0.6f;
        o.Chant = 0.2f;
        o.Fall = m.Coldness;
        o.OpenVoicing = true;
        return o;
    }

    /// <summary>A fight's tempo: a quarter at 84-100, felt in sixteenths by the riff and the kick.</summary>
    public static double FightTempo(MusicMoodState m, Random rng) => 84 + rng.NextDouble() * 12 + 6 * (1 - m.Coldness);

    /// <summary>The mood's weight on each pole.</summary>
    private static (Pole p, double w)[] Weights(MusicMoodState m)
    {
        double c = m.Coldness, f = m.Fear, y = m.Mystery;
        double comfy = Math.Pow(Math.Max(0, 1 - c * 1.6), 1.5) * (1 - f) * (1 - y * 0.7) + 0.02;
        double mel = Math.Max(0, 1 - Math.Abs(c - 0.6) * 2.5) * (1 - f * 0.8) + 0.02;
        double dark = Math.Max(0, c - 0.45) * 1.5 + y * 0.8;
        double fear = Math.Max(0, f - 0.35) * 2.5;
        // Festivity outweighs the rest once it is past about a half; fear and strangeness damp it, a
        // haunted market does not dance.
        double festive = m.Festivity * m.Festivity * 4.0 * (1 - f * 0.7) * (1 - y * 0.6);
        return new[] { (Comfy, comfy), (Melancholy, mel), (Dark, dark), (Fear, fear), (Festive, festive) };
    }

    /// <summary>The dance a mood asks for: none unless the festive pole leads, then a jig or a reel.</summary>
    public static Dance DanceFor(MusicMoodState m, Random rng)
    {
        var main = Weights(m).OrderByDescending(p => p.w).First().p;
        if (main != Festive) return Dance.None;
        return rng.NextDouble() < 0.55 ? Dance.Jig : Dance.Reel;
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
        var main = poles.OrderByDescending(p => p.w).First().p;
        // A session is a session: when the festive pole leads, nobody else's instruments sit in.
        if (main == Festive) poles = new[] { (Festive, 1.0) };
        double tot = poles.Sum(p => p.w);
        var o = new Orchestration();
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
        if (main == Festive)
        {
            // The session: the tune never alone, the drum nearly always, a pipes drone some of the time.
            o.Percussion = true;
            o.Drone = rng.NextDouble() < 0.45 ? Instrument.Pipes : Instrument.Bourdon;
            if (o.Counter == o.Melody) o.Counter = o.Melody == Instrument.Fiddle ? Instrument.Whistle : Instrument.Fiddle;
        }
        double Chance(Func<Pole, double> f) => poles.Sum(p => p.w * f(p.p)) / tot;
        o.Percussion = o.Percussion || rng.NextDouble() < Chance(p => p.PercChance);
        o.Bell = rng.NextDouble() < Chance(p => p.BellChance);
        o.Ambience = m.Mystery > 0.45f && rng.NextDouble() < Chance(p => p.AmbienceChance) + m.Mystery * 0.3;
        o.Liveliness = (float)(poles.Sum(p => p.w * p.p.Liveliness) / tot);
        o.Chant = (float)(poles.Sum(p => p.w * p.p.Chant) / tot);
        o.Fall = m.Coldness;
        o.OpenVoicing = main == Dark || main == Fear || rng.NextDouble() < m.Coldness * 0.5;

        // The ostinato's figure: chord-tone indices (0 root, 1 third, 2 fifth, 3 octave) per step.
        if (main == Festive)
        {
            o.OstinatoStyle = OstinatoStyle.Comp;   // the composer strums it by the dance's own pattern
            o.OstinatoPattern = new[] { 0 };
            o.OstinatoStep = 1.0;
        }
        else if (o.Ostinato == Instrument.Timpani || main == Fear && rng.NextDouble() < 0.5)
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

    /// <summary>
    /// A dance's tempo: a jig's dotted quarter or a reel's quarter. Slower than a session plays them,
    /// as background to a room rather than for dancers, and slower again when the place is cold.
    /// </summary>
    public static double DanceTempo(Dance d, MusicMoodState m, Random rng)
    {
        double slow = 1.0 - Math.Max(0, m.Coldness - 0.3) * 0.25;
        double bpm = d == Dance.Jig ? 98 + rng.NextDouble() * 16 : 94 + rng.NextDouble() * 16;
        return bpm * slow;
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
        Part.Drone => 0.2f, Part.Pad => 0.42f, Part.Melody => 0.8f, Part.Counter => 0.5f, Part.Ostinato => 0.6f,
        Part.Bass => 0.45f, Part.Perc => 0.55f, Part.Bell => 0.8f, Part.Ambience => 0.15f, Part.Fight => 0.6f,
        _ => 1f,
    };

    /// <summary>
    /// Per-instrument taste. The voices are levelled already (Voice.Levels, from the audition); this
    /// is only where an instrument should sit above or below its peers: the organ and the square lead
    /// are dense and cut through, the bells are an accent.
    /// </summary>
    public static float Trim(Instrument i) => i switch
    {
        Instrument.Organ => 0.8f, Instrument.SquareLead => 0.8f, Instrument.Bell => 0.9f, Instrument.Horn => 0.9f,
        // The riff and the kit drive the fight; the tune still has to sit on them.
        Instrument.Distortion => 0.75f, Instrument.PowerChord => 0.75f,
        Instrument.Kick => 0.75f, Instrument.Snare => 0.75f, Instrument.Cymbal => 0.35f,
        _ => 1f,
    };

    /// <summary>The room and the tape for a mood. A fight is played close: a smaller room, less of it.</summary>
    public static void ApplyBus(Synth s, MusicMoodState m, bool fight = false)
    {
        // A festive place is a room full of people: a shorter hall and less of it.
        float t60 = (2.0f + 5.0f * m.Mystery + 2.0f * m.Coldness) * (1f - 0.45f * m.Festivity);
        float damp = 5200f - 2400f * m.Coldness - 800f * m.Mystery;
        s.SetHall(t60, damp, 0.015f + 0.035f * m.Mystery);
        s.SetTape(9000f - 2600f * m.Coldness - 600f * m.Mystery, 3f + 3.5f * m.Mystery, -60f + 3f * m.Coldness, 1.3f + 0.5f * m.Fear);
        s.ChorusDepth.Target = 0.45f + 0.45f * m.Mystery;
        s.ReverbReturn = (1.05f + 0.45f * m.Mystery + 0.2f * m.Coldness) * (1f - 0.35f * m.Festivity);
        // A full band in a small room adds up: the festive mix goes into the tape a little lower.
        s.PreGain = 0.22f * (1f - 0.15f * m.Festivity);
        if (fight)
        {
            s.SetHall(t60 * 0.7f, damp, 0.012f);
            s.ReverbReturn *= 0.8f;
            s.ChorusDepth.Target *= 0.7f;
            // Drums and a clipped riff are dense: into the tape lower, or the fight is the loudest thing in the game.
            s.PreGain = 0.16f;
        }
    }
}
