using System;
using System.IO;
using System.Linq;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace Cathedral.Audio;

/// <summary>Which path the game's sound takes to the speakers.</summary>
public enum AudioBackend
{
    /// <summary>
    /// Windows' own: UI ticks and the loading wash through WinMM, music through the system MIDI
    /// synthesiser (the Microsoft GS Wavetable Synth). The default on Windows, and what every
    /// Windows player has always heard.
    /// </summary>
    WinMM,

    /// <summary>
    /// Cross-platform: everything through OpenAL Soft, with music rendered in-process from a
    /// SoundFont by MeltySynth. The only path off Windows — Linux has no system General MIDI synth —
    /// and selectable on Windows with <c>--audio openal</c> so the two can be compared.
    /// </summary>
    OpenAL
}

/// <summary>The hover and click ticks.</summary>
internal interface IUiSfx : IDisposable
{
    void SetVolume(float v01);
    void PlayHover();
    void PlayClick();
}

/// <summary>The looping brown-noise wash under a model call, a dice roll or a journey.</summary>
internal interface INoiseStream : IDisposable
{
    void Start();
}

/// <summary>Where the music engine's MIDI goes. Null from <see cref="AudioOutput.OpenMidi"/> means nowhere.</summary>
internal interface IMidiOut : IDisposable
{
    string Name { get; }
    void SendEvent(MidiEvent ev);
    void TurnAllNotesOff();
}

/// <summary>
/// Chooses the backend once per process and builds its three outputs. <see cref="AmbianceEngine"/>
/// asks this for each and otherwise does not know which path it is on.
/// </summary>
internal static class AudioOutput
{
    private static AudioBackend? _backend;

    public static AudioBackend Backend => _backend ??= Resolve();

    private static AudioBackend Resolve()
    {
        var asked = Config.Debug.AudioBackend;
        if (!OperatingSystem.IsWindows())
        {
            if (asked == AudioBackend.WinMM)
                Console.Error.WriteLine("[audio] --audio winmm: WinMM exists only on Windows; using OpenAL.");
            return AudioBackend.OpenAL;
        }
        return asked ?? AudioBackend.WinMM;
    }

    public static IUiSfx CreateUiSfx() =>
        Backend == AudioBackend.OpenAL ? new OpenAlUiSfx() : new UiSfxPlayer();

    public static INoiseStream CreateNoise(float amplitude) =>
        Backend == AudioBackend.OpenAL ? new OpenAlNoiseStream(amplitude) : new BrownNoiseStreamer(amplitude);

    /// <summary>The music output, or null when there is none — a state the engine already handles.</summary>
    public static IMidiOut? OpenMidi()
    {
        IMidiOut? output = Backend == AudioBackend.OpenAL ? SoundFontMidiOut.TryOpen() : DeviceMidiOut.TryOpenBest();
        Console.WriteLine(output == null
            ? $"[audio] {Backend}: no music output."
            : $"[audio] {Backend}: music through {output.Name}.");
        return output;
    }
}

/// <summary>
/// The system MIDI device through DryWetMidi — the Windows path, unchanged: it prefers the
/// Microsoft GS Wavetable Synth and otherwise takes the first device there is.
/// </summary>
internal sealed class DeviceMidiOut : IMidiOut
{
    private readonly OutputDevice _device;

    private DeviceMidiOut(OutputDevice device)
    {
        _device = device;
        _device.PrepareForEventsSending();
    }

    public string Name => _device.Name;

    public static DeviceMidiOut? TryOpenBest()
    {
        try
        {
            var all = OutputDevice.GetAll();
            if (!all.Any()) return null;

            // Prefer the Windows GS Wavetable Synth
            var gs = all.FirstOrDefault(d =>
                d.Name.Contains("GS Wavetable", StringComparison.OrdinalIgnoreCase) ||
                d.Name.Contains("Microsoft", StringComparison.OrdinalIgnoreCase));

            return new DeviceMidiOut(gs ?? all.First());
        }
        catch
        {
            return null;
        }
    }

    public void SendEvent(MidiEvent ev) => _device.SendEvent(ev);

    public void TurnAllNotesOff() => _device.TurnAllNotesOff();

    public void Dispose() => _device.Dispose();
}

/// <summary>
/// Where the SoundFont is. It has no name of its own, like <c>models/model.gguf</c>: it is always
/// <c>assets/audio/soundfont.sf2</c>, and choosing a different one means replacing that file — or,
/// for comparing candidates, <c>--soundfont &lt;file&gt;</c>. See <c>assets/audio/README.md</c>.
/// </summary>
internal static class SoundFontLocator
{
    public const string RelativePath = "assets/audio/soundfont.sf2";

    public static string? Find()
    {
        if (Config.Debug.SoundFontPath is { } forced)
            return File.Exists(forced) ? Path.GetFullPath(forced) : null;

        // The working directory first, as the fonts are found; then up from the executable, which
        // is where a development build (bin/Debug/net8.0/...) finds the repository's assets.
        if (File.Exists(RelativePath)) return Path.GetFullPath(RelativePath);
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int up = 0; dir != null && up < 6; up++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, RelativePath);
            if (File.Exists(candidate)) return candidate;
        }
        return null;
    }
}
