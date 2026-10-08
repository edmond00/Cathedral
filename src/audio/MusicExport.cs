using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Cathedral.Audio;

/// <summary>
/// <c>--export-music &lt;out.mid&gt; [--seconds n] [--mood name] [--tracks n]</c>: runs the game's own
/// <see cref="AmbianceEngine"/> for <c>n</c> seconds with no device open and writes what it composed as
/// a MIDI file. A soundtrack for a video made by the composer the game uses, at whichever of its mood
/// presets fits (<c>Neutral</c>, <c>Lament</c>, <c>Tavern</c>, <c>DarkDungeon</c>…). Real time: the
/// engine paces itself by the clock, so a two-minute bed takes two minutes.
/// </summary>
public static class MusicExport
{
    public static int Run(string[] args)
    {
        string outPath = args.Length > 1 && !args[1].StartsWith("--") ? args[1] : "music.mid";
        double seconds = double.TryParse(Arg(args, "--seconds"), System.Globalization.NumberStyles.Float,
                                         System.Globalization.CultureInfo.InvariantCulture, out var s) ? s : 60;
        int tracks = int.TryParse(Arg(args, "--tracks"), out var t) ? t : 4;
        string moodName = Arg(args, "--mood") ?? "Neutral";

        var presets = typeof(MusicMoodState).GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(MusicMoodState)).ToDictionary(f => f.Name, f => (MusicMoodState)f.GetValue(null)!,
                                                                             StringComparer.OrdinalIgnoreCase);
        if (!presets.TryGetValue(moodName, out var mood))
        {
            Console.Error.WriteLine($"--export-music: no mood '{moodName}'. Moods: {string.Join(", ", presets.Keys)}");
            return 1;
        }

        Config.Debug.Silent = true;
        var clock = Stopwatch.StartNew();
        MidiCapture.Start(() => clock.Elapsed.TotalSeconds);

        using (var engine = new AmbianceEngine())
        {
            engine.Start();
            engine.SetMood(mood);
            engine.SetActiveTrackCount(tracks);
            Console.WriteLine($"--export-music: composing {seconds:F0}s of {moodName} on {tracks} track(s)…");
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
        }

        MidiCapture.Write(outPath);
        Console.WriteLine($"--export-music: {MidiCapture.Count} events written to {outPath}");
        return 0;
    }

    private static string? Arg(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
