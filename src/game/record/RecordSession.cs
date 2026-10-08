using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Cathedral.Glyph;
using OpenTK.Mathematics;

namespace Cathedral.Game.Record;

/// <summary>
/// The lifetime of one <c>--record</c> run: started when the window has loaded, finished after it
/// closes. Owns the pointer and the recorder, and writes the manifest that tools/video reads first.
/// </summary>
public static class RecordSession
{
    public static RecordPointer? Pointer { get; private set; }
    public static FrameRecorder? Recorder { get; private set; }

    private static string[] _args = Array.Empty<string>();

    /// <summary>Remembers the command line for the manifest; called from Program.cs while parsing.</summary>
    public static void Configure(string[] args) => _args = args;

    public static void Start(GlyphSphereCore core)
    {
        RecordMode.Begin(_args);

        // The game's own click and hover, as WAV, beside the footage — what the cutter lays under the
        // drawn cursor's presses.
        try { Cathedral.Audio.UiSfxPlayer.ExportWavs(Path.Combine(RecordMode.OutputDir, "sfx")); }
        catch (Exception ex) { Console.WriteLine($"[record] could not export the UI sounds: {ex.Message}"); }

        Pointer = new RecordPointer(new Vector2(core.ClientSize.X * 0.5f, core.ClientSize.Y * 0.55f));
        core.VirtualPointerOnly = true;
        Recorder = new FrameRecorder(Pointer);
        core.FrameSink = Recorder;
    }

    public static void Finish()
    {
        if (Pointer == null) return;
        double duration = RecordMode.Now;

        Recorder?.Dispose();

        string midi = Path.Combine(RecordMode.OutputDir, "music.mid");
        try
        {
            Cathedral.Audio.MidiCapture.Write(midi);
            Console.WriteLine($"[record] {Cathedral.Audio.MidiCapture.Count} MIDI events written to {midi}");
        }
        catch (Exception ex) { Console.WriteLine($"[record] could not write music.mid: {ex.Message}"); }

        RecordMode.End();

        var manifest = new Dictionary<string, object?>
        {
            ["fps"] = RecordMode.Fps,
            ["width"] = RecordMode.Width,
            ["height"] = RecordMode.Height,
            ["frames"] = Recorder?.FramesWritten ?? 0,
            // Seconds the footage lags the timeline: frame n shows timeline time n/fps + this. Zero since
            // the recorder stopped dropping the slots before its first frame; tools/video estimates it
            // for older recordings, which lack the field.
            ["video_offset"] = 0.0,
            ["duration"] = Math.Round(duration, 3),
            ["seed"] = Cathedral.Config.Rng.Seed,
            ["args"] = string.Join(' ', _args),
            ["failed"] = Cathedral.Game.Cli.CliMode.HasFailedAssertion,
            ["files"] = new Dictionary<string, string>
            {
                ["video"] = "game.mkv",
                ["timeline"] = "timeline.jsonl",
                ["music"] = "music.mid",
                ["click"] = "sfx/click.wav",
                ["hover"] = "sfx/hover.wav",
            },
        };
        File.WriteAllText(Path.Combine(RecordMode.OutputDir, "manifest.json"),
            JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine($"[record] done: {RecordMode.OutputDir}");
        Pointer = null;
    }
}
