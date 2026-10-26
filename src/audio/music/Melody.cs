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
    public static int[] Contour(int n, Random rng, float chant, float fall)
    {
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
                if (next > 5 || next < -4) ww = 0;
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

    public static Motif NewMotif(int meter, float liveliness, float chant, float fall, Random rng)
    {
        var rhythm = Rhythms.Pick(meter, liveliness, rng);
        // One-note bars make poor motifs; ask again for something with a shape.
        for (int tries = 0; tries < 4 && rhythm.Length < 3; tries++) rhythm = Rhythms.Pick(meter, liveliness + 0.2f, rng);
        // A motif needs a shape: a span of at least a third, and three notes or more not all one
        // neighbour figure. The walk is cheap; ask it again until the figure has somewhere to go.
        int[] best = Contour(rhythm.Length, rng, chant, fall);
        for (int tries = 0; tries < 12; tries++)
        {
            int span = best.Max() - best.Min();
            int distinct = best.Distinct().Count();
            if (span >= Math.Min(3, rhythm.Length - 1) && distinct >= Math.Min(3, rhythm.Length)) break;
            best = Contour(rhythm.Length, rng, chant, fall);
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
            if (!strong || chord.Has(key, n.Degree)) continue;
            bool passing = i > start && i + 1 < line.Count
                && Math.Abs(line[i - 1].Degree - n.Degree) == 1 && Math.Abs(line[i + 1].Degree - n.Degree) == 1
                && Math.Sign(line[i + 1].Degree - n.Degree) == Math.Sign(n.Degree - line[i - 1].Degree);
            if (passing && inBar > 0) continue;
            int up = n.Degree + 1, down = n.Degree - 1;
            n.Degree = chord.Has(key, down) && down >= lo ? down : chord.Has(key, up) && up <= hi ? up : n.Degree;
            line[i] = n;
        }
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
