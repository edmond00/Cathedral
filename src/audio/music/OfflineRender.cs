using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace Cathedral.Audio.Music;

/// <summary>
/// <c>--render-music &lt;out.wav&gt; [--seconds n] [--mood name] [--seed n] [--tracks n] [--journey j.json]</c>:
/// the game's music, rendered as fast as the machine allows, with its score beside it
/// (<c>out.score.json</c>). The same composer and synthesiser the game plays, driven by the sample
/// clock instead of the wall clock: ten minutes of music in well under a minute.
///
/// <para>A journey is the game's side of the conversation scripted: a JSON list of
/// <c>{"t": seconds, "mood": "Tavern" | [cold, fear, mystery, intensity], "event": "positive" |
/// "negative" | "neutral", "filter": "fighting" | "traveling" | "none", "tracks": n}</c>, each applied at
/// its time. It is how the harness measures transitions, stings and fights, not just still moods.</para>
/// </summary>
public static class OfflineRender
{
    public static int Run(string[] args)
    {
        string outPath = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "music.wav";
        double seconds = double.TryParse(Arg(args, "--seconds"), NumberStyles.Float, CultureInfo.InvariantCulture, out var s) ? s : 120;
        int seed = int.TryParse(Arg(args, "--seed"), out var sd) ? sd : 1;
        int tracks = int.TryParse(Arg(args, "--tracks"), out var t) ? t : 4;
        string moodName = Arg(args, "--mood") ?? "Neutral";
        if (!TryMood(moodName, out var mood))
        {
            Console.Error.WriteLine($"--render-music: no mood '{moodName}'. Moods: {string.Join(", ", Presets().Select(p => p.Name))}");
            return 1;
        }
        JsonElement[] journey = Arg(args, "--journey") is { } jp
            ? JsonDocument.Parse(File.ReadAllText(jp)).RootElement.EnumerateArray().OrderBy(e => e.GetProperty("t").GetDouble()).ToArray()
            : Array.Empty<JsonElement>();

        // Each piece draws from its own generator, derived from the seed and the piece's number,
        // exactly as the game derives them from its master seed.
        var core = new MusicCore(seed, n => GameRng.ForWorld(seed, $"music.piece.{n}"));
        var log = new ScoreLog();
        core.Log = log;
        log.Meta("engine", "scriptorium");
        log.Meta("mood", moodName);
        log.Meta("seed", seed);
        log.Meta("seconds", seconds);
        log.Meta("sampleRate", Dsp.SampleRate);
        core.JumpMood(mood);
        core.SetTrackCount(tracks);

        var clock = Stopwatch.StartNew();
        const int chunk = 4096;
        var l = new float[chunk];
        var r = new float[chunk];
        long total = (long)(seconds * Dsp.SampleRate);
        int j = 0;
        int maxVoices = 0;
        bool verbose = args.Contains("--verbose");
        using (var wav = new WavWriter(outPath, Dsp.SampleRate))
        {
            for (long done = 0; done < total;)
            {
                while (j < journey.Length && journey[j].GetProperty("t").GetDouble() <= core.Seconds)
                    Apply(core, journey[j++]);
                int n = (int)Math.Min(chunk, total - done);
                // Never step past the next journey point by more than a block.
                if (j < journey.Length)
                {
                    long until = (long)(journey[j].GetProperty("t").GetDouble() * Dsp.SampleRate) - done;
                    if (until > 0) n = (int)Math.Min(n, Math.Max(Synth.Block, until));
                }
                core.Render(l, r, 0, n);
                wav.Write(l, r, n);
                done += n;
                maxVoices = Math.Max(maxVoices, core.Synth.VoiceCount);
                if (verbose && done / Dsp.SampleRate / 30 != (done - n) / Dsp.SampleRate / 30)
                    Console.WriteLine($"  {done / Dsp.SampleRate,5}s  voices {core.Synth.VoiceCount,3}  compose {core.ComposeTicks * 1000 / System.Diagnostics.Stopwatch.Frequency}ms synth {core.SynthTicks * 1000 / System.Diagnostics.Stopwatch.Frequency}ms  {core.Synth.VoiceSummary()}");
            }
        }
        string scorePath = Path.ChangeExtension(outPath, ".score.json");
        log.Meta("renderSeconds", clock.Elapsed.TotalSeconds);
        log.Meta("maxVoices", maxVoices);
        log.Stems(core.Synth.StemLevels());
        log.Write(scorePath);
        Console.WriteLine($"--render-music: {seconds:F0}s of {moodName} (seed {seed}) in {clock.Elapsed.TotalSeconds:F1}s " +
                          $"({seconds / clock.Elapsed.TotalSeconds:F0}x real time) -> {outPath}, {scorePath}");
        return 0;
    }

    private static void Apply(MusicCore core, JsonElement e)
    {
        if (e.TryGetProperty("mood", out var m))
        {
            if (m.ValueKind == JsonValueKind.String && TryMood(m.GetString()!, out var mood)) core.SetMood(mood);
            else if (m.ValueKind == JsonValueKind.Array)
            {
                var v = m.EnumerateArray().Select(x => (float)x.GetDouble()).ToArray();
                core.SetMood(new MusicMoodState(v[0], v[1], v[2], v.Length > 3 ? v[3] : 1f));
            }
        }
        if (e.TryGetProperty("event", out var ev))
            switch (ev.GetString())
            {
                case "positive": core.Positive(); break;
                case "negative": core.Negative(); break;
                case "neutral": core.Neutral(); break;
            }
        if (e.TryGetProperty("filter", out var f))
        {
            string name = f.GetString() ?? "none";
            core.SetFighting(name == "fighting");
            core.SetTraveling(name == "traveling");
        }
        if (e.TryGetProperty("tracks", out var tr)) core.SetTrackCount(tr.GetInt32());
    }

    private static (string Name, MusicMoodState Mood)[] Presets() =>
        typeof(MusicMoodState).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(MusicMoodState))
            .Select(f => (f.Name, (MusicMoodState)f.GetValue(null)!)).ToArray();

    /// <summary>A preset by name, or a location type sampled as the game samples it (<c>tavern:12</c>).</summary>
    private static bool TryMood(string name, out MusicMoodState mood)
    {
        foreach (var (n, m) in Presets())
            if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) { mood = m; return true; }
        int colon = name.IndexOf(':');
        if (colon > 0 && int.TryParse(name[(colon + 1)..], out int id))
        {
            mood = LocationMoodProfiles.SampleMood(name[..colon], id);
            return true;
        }
        mood = default;
        return false;
    }

    private static string? Arg(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
