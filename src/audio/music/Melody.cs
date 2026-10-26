using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Audio.Music;

/// <summary>One note of a planned line: when (beats from the start of its section), how long, which degree.</summary>
internal struct LineNote
{
    public double Beat, Dur;
    public int Degree;
    public float Vel;

    public LineNote(double beat, double dur, int degree, float vel = 1f)
    { Beat = beat; Dur = dur; Degree = degree; Vel = vel; }
}

/// <summary>
/// A motif: one bar of rhythm and a contour of scale steps relative to its first note. This is what
/// makes a piece memorable. It is stated, repeated, moved along the harmony, inverted, ornamented,
/// and its rhythm survives every one of those, which is how the ear recognises it.
/// </summary>
internal sealed class Motif
{
    public double[] Rhythm;     // durations in beats; they fill the bar
    public int[] Contour;       // degrees relative to the first note (Contour[0] == 0)

    public Motif(double[] rhythm, int[] contour) { Rhythm = rhythm; Contour = contour; }

    public int Count => Rhythm.Length;
    public int Low => Contour.Min();
    public int High => Contour.Max();

    /// <summary>The contour scaled to a mode of another size: a seven-note motif played in five.</summary>
    public Motif ForModeSize(int size) => size >= 7 ? this
        : new(Rhythm, Contour.Select(c => (int)Math.Round(c * size / 7.0)).ToArray());

    public Motif Inverted() => new(Rhythm, Contour.Select(c => -c).ToArray());

    public Motif Retrograde()
    {
        var c = Contour.Reverse().ToArray();
        int first = c[0];
        return new(Rhythm, c.Select(x => x - first).ToArray());
    }

    /// <summary>The last note moved a step: the commonest variation in folk tunes.</summary>
    public Motif WithEnding(int delta)
    {
        var c = (int[])Contour.Clone();
        c[^1] += delta;
        return new(Rhythm, c);
    }

    /// <summary>A long note split to fill a third with its passing note, or to turn round a neighbour.</summary>
    public Motif Ornamented(Random rng)
    {
        var r = new List<double>();
        var c = new List<int>();
        bool done = false;
        for (int i = 0; i < Count; i++)
        {
            int next = i + 1 < Count ? Contour[i + 1] : Contour[i];
            int gap = next - Contour[i];
            if (!done && Rhythm[i] >= 1.0 && (Math.Abs(gap) == 2 || (gap == 0 && rng.NextDouble() < 0.5)) && i + 1 < Count)
            {
                double half = Rhythm[i] >= 2 ? 1.0 : Rhythm[i] / 2;
                r.Add(Rhythm[i] - half); c.Add(Contour[i]);
                r.Add(half); c.Add(gap == 0 ? Contour[i] + (rng.NextDouble() < 0.6 ? -1 : 1) : Contour[i] + Math.Sign(gap));
                done = true;
            }
            else { r.Add(Rhythm[i]); c.Add(Contour[i]); }
        }
        return new(r.ToArray(), c.ToArray());
    }
}

internal static class Rhythms
{
    // Durations in beats, per bar. Calm, measured, lively; the lively sets are for the tavern.
    private static readonly double[][] FourCalm =
    {
        new[] { 2.0, 2.0 }, new[] { 3.0, 1.0 }, new[] { 2.0, 1.0, 1.0 }, new[] { 1.0, 1.0, 2.0 }, new[] { 2.0, 1.5, 0.5 },
    };
    private static readonly double[][] FourMid =
    {
        new[] { 1.0, 1.0, 1.0, 1.0 }, new[] { 1.5, 0.5, 1.0, 1.0 }, new[] { 1.0, 0.5, 0.5, 2.0 }, new[] { 2.0, 1.0, 1.0 },
        new[] { 1.0, 1.0, 2.0 }, new[] { 1.5, 0.5, 2.0 }, new[] { 0.5, 0.5, 1.0, 2.0 }, new[] { 1.0, 2.0, 1.0 },
    };
    private static readonly double[][] FourLively =
    {
        new[] { 1.0, 0.5, 0.5, 1.0, 1.0 }, new[] { 0.5, 0.5, 0.5, 0.5, 1.0, 1.0 }, new[] { 1.5, 0.5, 1.0, 0.5, 0.5 },
        new[] { 1.0, 1.0, 0.5, 0.5, 1.0 }, new[] { 1.0, 0.5, 0.5, 2.0 },
    };
    private static readonly double[][] ThreeCalm = { new[] { 2.0, 1.0 }, new[] { 3.0 }, new[] { 1.0, 2.0 } };
    private static readonly double[][] ThreeMid =
    {
        new[] { 1.0, 1.0, 1.0 }, new[] { 2.0, 1.0 }, new[] { 1.5, 0.5, 1.0 }, new[] { 1.0, 0.5, 0.5, 1.0 },
    };
    private static readonly double[][] ThreeLively =
    {
        new[] { 0.5, 0.5, 1.0, 1.0 }, new[] { 1.0, 0.5, 0.5, 0.5, 0.5 }, new[] { 1.5, 0.5, 0.5, 0.5 }, new[] { 2.0, 0.5, 0.5 },
    };

    /// <summary>A bar's rhythm. <paramref name="liveliness"/> 0 calm .. 1 dancing.</summary>
    public static double[] Pick(int meter, float liveliness, Random rng)
    {
        double x = liveliness + (rng.NextDouble() - 0.5) * 0.5;
        double[][] pool = meter == 3
            ? (x < 0.3 ? ThreeCalm : x < 0.75 ? ThreeMid : ThreeLively)
            : (x < 0.3 ? FourCalm : x < 0.75 ? FourMid : FourLively);
        return pool[rng.Next(pool.Length)];
    }

    /// <summary>The cadence bar: shorter notes leading to a long one.</summary>
    public static double[] Cadence(int meter, Random rng)
    {
        double[][] pool = meter == 3
            ? new[] { new[] { 1.0, 2.0 }, new[] { 3.0 }, new[] { 0.5, 0.5, 2.0 } }
            : new[] { new[] { 1.0, 1.0, 2.0 }, new[] { 2.0, 2.0 }, new[] { 1.0, 3.0 }, new[] { 4.0 }, new[] { 0.5, 0.5, 1.0, 2.0 } };
        return pool[rng.Next(pool.Length)];
    }
}

internal static class Melodist
{
    /// <summary>
    /// A contour by a first-order walk over scale steps: mostly seconds, some thirds, the odd fourth;
    /// a leap is followed by a step back the other way (gap fill), and the range is held to a sixth.
    /// <paramref name="chant"/> leans to repeated notes and seconds; <paramref name="fall"/> leans downward.
    /// </summary>
    public static int[] Contour(int n, Random rng, float chant, float fall, int modeSize = 7)
    {
        // A step of a five-note mode is a second or a third already: its leaps are capped at two
        // steps, or a "fourth" of the walk lands a seventh away.
        int maxLeap = modeSize < 7 ? 2 : 4;
        var c = new int[n];
        int prevStep = 0;
        for (int i = 1; i < n; i++)
        {
            var w = new List<(int step, double w)>
            {
                (-1, 30 + fall * 10), (1, 26 - fall * 6), (0, 6 + chant * 14), (-2, 11), (2, 10 - chant * 4),
                (-3, 3 - chant * 2), (3, 4 - chant * 2), (4, 2.5 - chant * 2), (-4, 1.5),
            };
            for (int k = 0; k < w.Count; k++)
            {
                var (s, ww) = w[k];
                if (Math.Abs(prevStep) >= 3)
                    ww *= Math.Sign(s) == -Math.Sign(prevStep) && Math.Abs(s) <= 2 ? 3.0 : Math.Sign(s) == Math.Sign(prevStep) ? 0.2 : 1.0;
                else if (prevStep != 0 && Math.Sign(s) == Math.Sign(prevStep) && Math.Abs(s) == 1)
                    ww *= 1.25;   // a run of steps continues
                int next = c[i - 1] + s;
                if (next > 5 || next < -4 || Math.Abs(s) > maxLeap) ww = 0;
                w[k] = (s, Math.Max(0, ww));
            }
            prevStep = Pick(w, rng);
            c[i] = c[i - 1] + prevStep;
        }
        return c;
    }

    public static int Pick(List<(int v, double w)> items, Random rng)
    {
        double tot = items.Sum(x => x.w);
        double r = rng.NextDouble() * tot;
        foreach (var (v, w) in items) { r -= w; if (r <= 0) return v; }
        return items[^1].v;
    }

    /// <summary>
    /// How good a motif is as a motif, by rules of thumb from folk tunes and chant: three to five
    /// notes, three different pitches or more, a span of a third to a fifth, few repeated notes, a
    /// leap answered by a step back, an end away from the start, a rhythm with a long note in it.
    /// </summary>
    public static double Quality(Motif m)
    {
        var c = m.Contour;
        int n = c.Length;
        double q = n is >= 3 and <= 5 ? 2 : 0;
        int distinct = c.Distinct().Count();
        q += distinct >= 3 ? 2 : distinct == 2 ? 0 : -2;
        int span = c.Max() - c.Min();
        q += span is >= 2 and <= 4 ? 2 : span == 5 ? 0 : -2;
        for (int i = 1; i < n; i++)
        {
            int step = c[i] - c[i - 1];
            if (step == 0) q -= 1.5;
            if (Math.Abs(step) >= 2 && i + 1 < n && Math.Sign(c[i + 1] - c[i]) == -Math.Sign(step) && Math.Abs(c[i + 1] - c[i]) == 1) q += 1;
        }
        if (c[^1] != c[0]) q += 0.5;
        if (m.Rhythm.Distinct().Count() > 1) q += 0.5;
        if (m.Rhythm.Max() >= 2 * m.Rhythm.Min()) q += 0.5;
        return q;
    }

    /// <summary>The best of <paramref name="candidates"/> fresh motifs by <see cref="Quality"/>.</summary>
    public static Motif BestMotif(int candidates, int meter, float liveliness, float chant, float fall, Random rng, int modeSize = 7)
    {
        Motif best = NewMotif(meter, liveliness, chant, fall, rng, modeSize);
        double bq = Quality(best);
        for (int i = 1; i < candidates; i++)
        {
            var m = NewMotif(meter, liveliness, chant, fall, rng, modeSize);
            double q = Quality(m);
            if (q > bq) { best = m; bq = q; }
        }
        return best;
    }

    public static Motif NewMotif(int meter, float liveliness, float chant, float fall, Random rng, int modeSize = 7)
    {
        var rhythm = Rhythms.Pick(meter, liveliness, rng);
        // One-note bars make poor motifs; ask again for something with a shape.
        for (int tries = 0; tries < 4 && rhythm.Length < 3; tries++) rhythm = Rhythms.Pick(meter, liveliness + 0.2f, rng);
        // A motif needs a shape: a span of at least a third, and three notes or more not all one
        // neighbour figure. The walk is cheap; ask it again until the figure has somewhere to go.
        int[] best = Contour(rhythm.Length, rng, chant, fall, modeSize);
        for (int tries = 0; tries < 12; tries++)
        {
            int span = best.Max() - best.Min();
            int distinct = best.Distinct().Count();
            if (span >= Math.Min(3, rhythm.Length - 1) && distinct >= Math.Min(3, rhythm.Length)) break;
            best = Contour(rhythm.Length, rng, chant, fall, modeSize);
        }
        return new Motif(rhythm, best);
    }

    /// <summary>
    /// The degree, among the chord's tones, nearest to <paramref name="near"/>, preferring a step away
    /// over a repeat and the root or fifth over the third on a downbeat that opens something.
    /// </summary>
    public static int NearestChordTone(Key key, Chord chord, int near, int lo, int hi, Random rng, bool preferStable = false)
    {
        int best = near, bestScore = int.MaxValue;
        for (int d = lo; d <= hi; d++)
        {
            if (!chord.Has(key, d)) continue;
            int score = Math.Abs(d - near) * 10;
            if (preferStable && Key.Mod(d - chord.Root, key.Mode.Size) != 0) score += 6;
            score += rng.Next(5);
            if (score < bestScore) { bestScore = score; best = d; }
        }
        return best;
    }

    /// <summary>
    /// Lays a motif into a bar at an anchor degree, then bends strong-beat notes that are not chord
    /// tones onto one unless they pass by step between two that are.
    /// </summary>
    public static void PlaceBar(List<LineNote> line, Motif m, double barBeat, int anchor, Key key, Chord chord,
                                int meter, int lo, int hi, float vel)
    {
        double t = 0;
        int start = line.Count;
        for (int i = 0; i < m.Count; i++)
        {
            int d = Math.Clamp(anchor + m.Contour[i], lo, hi);
            line.Add(new LineNote(barBeat + t, m.Rhythm[i], d, vel));
            t += m.Rhythm[i];
        }
        for (int i = start; i < line.Count; i++)
        {
            var n = line[i];
            double inBar = n.Beat - barBeat;
            bool strong = inBar < 1e-6 || (meter == 4 && Math.Abs(inBar - 2) < 1e-6);
            if (chord.Has(key, n.Degree)) continue;
            // A held note that rubs a semitone or a tritone against the chord the pad is sustaining is
            // heard as a wrong note, not a passing one: it moves to the nearest chord tone. Short
            // notes off the beat may pass through.
            // A step either side makes it a passing or neighbour tone: those may rub, if they are not
            // held too long. Moving one onto the chord tone beside it only turns a figure into a
            // repeated note.
            bool stepIn = i > start && Math.Abs(line[i - 1].Degree - n.Degree) == 1;
            bool stepOut = i + 1 < line.Count && Math.Abs(line[i + 1].Degree - n.Degree) == 1;
            bool figure = stepIn && stepOut;
            bool held = n.Dur >= 1.5 || (n.Dur >= 1.0 && !figure);
            if (!strong && !(held && Clashes(key, chord, n.Degree))) continue;
            if (figure && inBar > 0 && n.Dur < 1.5) continue;
            int up = n.Degree + 1, down = n.Degree - 1;
            n.Degree = chord.Has(key, down) && down >= lo ? down : chord.Has(key, up) && up <= hi ? up : n.Degree;
            line[i] = n;
        }
    }

    /// <summary>Whether a degree sounds a semitone or a tritone against any tone of the chord.</summary>
    public static bool Clashes(Key key, Chord chord, int degree)
    {
        int p = key.Pitch(degree);
        foreach (int pc in chord.PitchClasses(key))
        {
            int d = Key.Mod(p - pc, 12);
            if (d is 1 or 11 or 6) return true;
        }
        return false;
    }

    /// <summary>
    /// The cadence bar: notes stepping onto <paramref name="target"/>, from above most often (the
    /// line's gravity), sometimes from below.
    /// </summary>
    public static void PlaceCadence(List<LineNote> line, double[] rhythm, double barBeat, int target, int lo, int hi,
                                    Random rng, float vel)
    {
        int n = rhythm.Length;
        bool fromAbove = rng.NextDouble() < 0.7;
        double t = 0;
        for (int i = 0; i < n; i++)
        {
            int away = n - 1 - i;
            int d = fromAbove ? target + away : target - away;
            if (away == 2 && rng.NextDouble() < 0.4) d = fromAbove ? target + 1 : target - 1; // turn: 2 1 0 -> 1 ...
            line.Add(new LineNote(barBeat + t, rhythm[i], Math.Clamp(d, lo, hi), vel * (i == n - 1 ? 0.9f : 1f)));
            t += rhythm[i];
        }
    }
}
