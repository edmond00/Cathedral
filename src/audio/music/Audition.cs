using System;
using System.Collections.Generic;
using System.IO;

namespace Cathedral.Audio.Music;

/// <summary>
/// <c>--music-audition &lt;dir&gt;</c>: every instrument alone, through the full chain, as one WAV each
/// (a low note, a phrase, a held chord) plus <c>all.wav</c> with every instrument in turn. For the
/// evaluation harness's timbre sheets, and for anyone tuning a patch.
/// </summary>
public static class Audition
{
    public static int Run(string[] args)
    {
        string dir = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "audition";
        Directory.CreateDirectory(dir);
        var all = new List<Instrument>(Enum.GetValues<Instrument>());
        foreach (var inst in all)
        {
            var synth = new Synth(1);
            synth.SetHall(3.5f, 3500f, 0.03f);
            List<(double t, float midi, double dur, float vel)> events;
            using (var wav = new WavWriter(Path.Combine(dir, $"{inst}.wav"), Dsp.SampleRate))
                events = Play(synth, wav, inst);
            // The notes too, in the harness's score format, so the same phrase can be rendered on a
            // reference instrument and compared.
            var notes = string.Join(",\n", events.ConvertAll(e => FormattableString.Invariant(
                $"{{\"t\":{e.t:F4},\"d\":{e.dur:F4},\"p\":{e.midi:F0},\"v\":{e.vel:F3},\"part\":\"melody\",\"inst\":\"{inst}\"}}")));
            File.WriteAllText(Path.Combine(dir, $"{inst}.score.json"),
                $"{{\"meta\":{{\"instrument\":\"{inst}\",\"seconds\":{events[^1].t + 7:F1}}},\"notes\":[\n{notes}]}}");
        }
        Console.WriteLine($"--music-audition: {all.Count} instruments written to {dir}");
        return 0;
    }

    private static List<(double t, float midi, double dur, float vel)> Play(Synth synth, WavWriter wav, Instrument inst)
    {
        // D dorian: a phrase, then a held open fifth and triad.
        int root = inst is Instrument.Bourdon or Instrument.Timpani or Instrument.HandDrum or Instrument.Bodhran or Instrument.Kick or Instrument.WarDrum or Instrument.BentDrum ? 38
                 : inst is Instrument.Distortion or Instrument.PowerChord or Instrument.Staccato ? 45
                 : inst is Instrument.Bell or Instrument.Whistle or Instrument.Glass ? 74 : 62;
        int[] phrase = { 0, 2, 3, 5, 7, 5, 3, 2, 0 };
        double beat = 0.55;
        var events = new List<(double t, float midi, double dur, float vel)>();
        for (int i = 0; i < phrase.Length; i++)
            events.Add((i * beat, root + phrase[i], beat * 0.9, 0.7f));
        double t0 = phrase.Length * beat + 0.5;
        foreach (int iv in new[] { 0, 7, 12 }) events.Add((t0, root - 12 + iv, 3.0, 0.6f));
        double t1 = t0 + 4.5;
        foreach (int iv in new[] { 0, 3, 7 }) events.Add((t1, root + iv, 3.0, 0.6f));
        double end = t1 + 7;

        events.Sort((a, b) => a.t.CompareTo(b.t));
        var l = new float[Synth.Block];
        var r = new float[Synth.Block];
        int e = 0;
        while (synth.Seconds < end)
        {
            while (e < events.Count && events[e].t <= synth.Seconds)
            {
                var (_, midi, dur, vel) = events[e++];
                synth.NoteOn(Part.Melody, inst, midi, vel, dur, (e % 3 - 1) * 0.5f);
            }
            synth.Render(l, r, 0, Synth.Block);
            wav.Write(l, r, Synth.Block);
        }
        return events;
    }
}
