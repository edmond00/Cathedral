using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

namespace Cathedral.Game.Record;

/// <summary>
/// <c>--record &lt;dir&gt;</c>: the CLI's video twin. A script (or an agent on stdin) plays the game, and
/// the run is filmed — every frame, a drawn mouse cursor travelling to whatever is clicked, and a
/// timeline of what happened when, so the footage can be cut down to the moments worth keeping.
///
/// <para><b>Only what a player can do.</b> The command set is the CLI's, minus everything that reaches
/// past the screen (<c>strategy</c>, <c>wound</c>, <c>clock</c>…). And every click goes through the
/// window's real pointer path — the hover that lights a button, the ray that picks a vertex — at a pixel
/// the recorder had to find first. A command whose target cannot be located on screen is refused rather
/// than performed some other way, because a recording of a click nobody could have made is a lie. The
/// starting-condition flags (<c>--seed</c>, <c>--start-at</c>, <c>--location-type</c>…) stay available:
/// they decide what world the player wakes in, not what the player can do in it.</para>
///
/// <para><b>Runs in the background.</b> The window is hidden unless <c>--record-visible</c>, the OS mouse
/// is ignored (<see cref="Cathedral.Glyph.GlyphSphereCore.VirtualPointerOnly"/>), no audio device is
/// opened, and the language model runs on the CPU unless <c>--gpu</c> is passed. The music the game
/// would have played is captured as MIDI instead (<see cref="Cathedral.Audio.MidiCapture"/>).</para>
///
/// <para>Output, all in the one directory: <c>game.mkv</c> (the footage, real time),
/// <c>timeline.jsonl</c> (one event per line, <c>t</c> in seconds on the footage's clock),
/// <c>music.mid</c> and <c>manifest.json</c>. tools/video turns these into a finished video.</para>
/// </summary>
public static class RecordMode
{
    public static bool IsActive { get; set; }

    /// <summary>Where the footage, timeline, music and manifest are written.</summary>
    public static string OutputDir { get; set; } = "recording";

    public static int Fps { get; set; } = 30;
    public static int Width { get; set; } = 1440;
    public static int Height { get; set; } = 1080;

    /// <summary>Show the window while recording. Off by default: a recording is meant to run unattended.</summary>
    public static bool Visible { get; set; }

    /// <summary>Explicit ffmpeg; otherwise <see cref="FindFfmpeg"/> looks in the usual places.</summary>
    public static string? FfmpegPath { get; set; }

    /// <summary>How fast the drawn cursor travels, in pixels per second (before easing and clamping).</summary>
    public static double CursorSpeed { get; set; } = 1500;

    /// <summary>The footage's clock. Frame <c>n</c> of game.mkv shows the game at <c>n / Fps</c> on it.</summary>
    public static readonly Stopwatch Clock = new();

    public static double Now => Clock.Elapsed.TotalSeconds;

    private static StreamWriter? _timeline;
    private static readonly object TimelineLock = new();

    /// <summary>Opens the output directory and starts the clock. Called once, as the window loads.</summary>
    public static void Begin(IReadOnlyList<string> args)
    {
        Directory.CreateDirectory(OutputDir);
        _timeline = new StreamWriter(Path.Combine(OutputDir, "timeline.jsonl"), append: false) { AutoFlush = true };
        Clock.Restart();
        Log("start", new()
        {
            ["fps"] = Fps, ["width"] = Width, ["height"] = Height,
            ["args"] = string.Join(' ', args),
            ["started"] = DateTime.Now.ToString("s"),
        });
    }

    /// <summary>
    /// Appends one event to timeline.jsonl. <c>t</c> is stamped here, on the footage's clock, so an
    /// event and the frame showing it agree. Thread-safe: the music threads log too.
    /// </summary>
    public static void Log(string type, Dictionary<string, object?>? fields = null)
    {
        if (_timeline == null) return;
        var row = new Dictionary<string, object?> { ["t"] = Math.Round(Now, 3), ["type"] = type };
        if (fields != null) foreach (var (k, v) in fields) row[k] = v;
        string json = JsonSerializer.Serialize(row);
        lock (TimelineLock) _timeline?.WriteLine(json);
    }

    /// <summary>Closes the timeline. The recorder and the MIDI capture write their own files.</summary>
    public static void End()
    {
        Log("end");
        lock (TimelineLock) { _timeline?.Dispose(); _timeline = null; }
    }

    /// <summary>
    /// ffmpeg: <see cref="FfmpegPath"/>, then <c>CATHEDRAL_FFMPEG</c>, then the one the video tools'
    /// virtualenv ships (imageio-ffmpeg), then whatever is on PATH.
    /// </summary>
    public static string? FindFfmpeg()
    {
        if (FfmpegPath is { } p && File.Exists(p)) return p;
        if (Environment.GetEnvironmentVariable("CATHEDRAL_FFMPEG") is { } env && File.Exists(env)) return env;

        foreach (var root in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            var dir = new DirectoryInfo(root);
            for (int up = 0; dir != null && up < 6; up++, dir = dir.Parent)
            {
                var bin = Path.Combine(dir.FullName, "tools", "video", ".venv", "Lib", "site-packages", "imageio_ffmpeg", "binaries");
                if (!Directory.Exists(bin)) continue;
                foreach (var exe in Directory.GetFiles(bin, "ffmpeg*.exe")) return exe;
            }
        }

        foreach (var dir in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            var exe = Path.Combine(dir.Trim(), "ffmpeg.exe");
            if (File.Exists(exe)) return exe;
        }
        return null;
    }
}
