using System;

namespace Cathedral.Audio;

/// <summary>What the game asks of a music engine. Two exist; <see cref="AmbianceEngine"/> picks one.</summary>
internal interface IMusicEngine : IDisposable
{
    string CurrentScaleName { get; }
    double CurrentBpm { get; }
    MusicMoodState CurrentMood { get; }
    int ActiveTrackCount { get; }
    bool IsDeviceOpen { get; }
    string DeviceName { get; }
    MusicFilter ActiveFilter { get; }
    bool Start();
    void Stop();
    void SetFilter(MusicFilter filter);
    void SetMood(MusicMoodState mood);
    void SetActiveTrackCount(int count);
    void TriggerGameEvent(GameEventType evt);
    void PlaySoundEffect(SoundEffectType sfx);
    void SetMasterMusicVolume(float v01);
    void SetMasterSfxVolume(float v01);
}

/// <summary>Which composer and instrument play the music (<c>--music-engine</c>).</summary>
public enum MusicEngineKind
{
    /// <summary>
    /// The hierarchical composer and the game's own synthesiser (<see cref="Music.SynthMusicEngine"/>):
    /// one sound on every platform, streamed as PCM through WinMM on Windows and OpenAL elsewhere.
    /// </summary>
    Synth,

    /// <summary>
    /// The first engine: per-note random walks sent as MIDI to the system synthesiser on Windows
    /// (the GS Wavetable Synth) or to MeltySynth and a SoundFont elsewhere. Kept for comparison.
    /// </summary>
    Legacy,
}

/// <summary>
/// The game's music and sound effects. Thread-safe: every method may be called from any thread.
/// Always dispose when done.
///
/// <para>A thin front: the work is done by whichever <see cref="IMusicEngine"/>
/// <see cref="Config.Debug.MusicEngine"/> names. The public surface is the one the game has always
/// used, so nothing outside src/audio knows which is playing.</para>
/// </summary>
public sealed class AmbianceEngine : IDisposable
{
    private readonly IMusicEngine _engine = Config.Debug.MusicEngine == MusicEngineKind.Legacy
        ? new LegacyAmbianceEngine()
        : new Music.SynthMusicEngine();

    /// <summary>
    /// Raised by every <see cref="TriggerGameEvent"/> call, before any audio work and
    /// regardless of whether a device is open. Lets non-audio systems react to the
    /// same signal — the glyph renderer hangs its dither pulses off this, so the visual
    /// feedback fires from exactly the same sites as the sound it accompanies.
    /// Raised on the caller's thread; handlers must be thread-safe and quick.
    /// </summary>
    public static event Action<GameEventType>? GameEventFired;

    /// <summary>Most recent mode the music is in.</summary>
    public string CurrentScaleName => _engine.CurrentScaleName;

    /// <summary>Current tempo in BPM.</summary>
    public double CurrentBpm => _engine.CurrentBpm;

    public MusicMoodState CurrentMood => _engine.CurrentMood;

    public int ActiveTrackCount => _engine.ActiveTrackCount;

    public bool IsDeviceOpen => _engine.IsDeviceOpen;

    public string DeviceName => _engine.DeviceName;

    /// <summary>Currently active postprocess filter (or <see cref="MusicFilter.None"/>).</summary>
    public MusicFilter ActiveFilter => _engine.ActiveFilter;

    /// <summary>Opens the output and starts the music. True if anything will hear it (a device, or a capture).</summary>
    public bool Start() => _engine.Start();

    public void Stop() => _engine.Stop();

    /// <summary>Activates or deactivates a postprocess filter. <see cref="MusicFilter.None"/> stops any running one.</summary>
    public void SetFilter(MusicFilter filter) => _engine.SetFilter(filter);

    /// <summary>Sets the target mood; the music glides toward it over a few seconds.</summary>
    public void SetMood(MusicMoodState mood) => _engine.SetMood(mood);

    /// <summary>Sets the number of active tracks (0–4).</summary>
    public void SetActiveTrackCount(int count) => _engine.SetActiveTrackCount(count);

    /// <summary>
    /// A game event: its sound effect at once, and its effect on the music at the next beat or bar.
    /// Mood gauges are not modified; they are controlled by the caller.
    /// </summary>
    public void TriggerGameEvent(GameEventType evt)
    {
        // Before any audio work: the visual response should not depend on whether audio came up.
        GameEventFired?.Invoke(evt);
        _engine.TriggerGameEvent(evt);
    }

    /// <summary>Fires a short sound effect.</summary>
    public void PlaySoundEffect(SoundEffectType sfx) => _engine.PlaySoundEffect(sfx);

    /// <summary>Sets the master music volume (0..1).</summary>
    public void SetMasterMusicVolume(float v01) => _engine.SetMasterMusicVolume(v01);

    /// <summary>Sets the master sound-effects volume (0..1): the stings and the UI ticks.</summary>
    public void SetMasterSfxVolume(float v01) => _engine.SetMasterSfxVolume(v01);

    public void Dispose() => _engine.Dispose();
}
