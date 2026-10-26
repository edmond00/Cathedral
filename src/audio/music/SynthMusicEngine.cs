using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Cathedral.Audio.Music;

/// <summary>
/// The music as the game plays it: <see cref="MusicCore"/> (the composer and the synthesiser) pulled
/// by an audio stream's thread. One sound on every platform: the same synthesiser renders on Windows
/// and Linux, and only the last step (waveOut or OpenAL) differs.
///
/// <para>Each piece takes its seed from <see cref="GameRng"/> by its number, so the music follows the
/// world: the boot seed gives the title screen the same first piece every launch, and a run reseeded
/// from its moon composes that world's own pieces.</para>
/// </summary>
internal sealed class SynthMusicEngine : IMusicEngine
{
    private readonly IUiSfx _uiSfx = AudioOutput.CreateUiSfx();
    private MusicCore? _core;
    private IPcmStream? _stream;
    private Thread? _captureThread;
    private volatile bool _running;
    private bool _moodSet;
    private int _trackCount = 4;
    private MusicMoodState _mood = MusicMoodState.Neutral;
    private volatile MusicFilter _filter = MusicFilter.None;
    private INoiseStream? _noise;
    private readonly object _filterLock = new();
    private float _musicVolume = 1f, _sfxVolume = 1f;
    private long _lastEventMs;

    public string CurrentScaleName => _core?.ModeName ?? "—";
    public double CurrentBpm => _core?.Bpm ?? 0;
    public MusicMoodState CurrentMood => _core?.ActiveMood ?? _mood;
    public int ActiveTrackCount => _trackCount;
    public bool IsDeviceOpen => _stream != null;
    public string DeviceName { get; private set; } = "(none)";
    public MusicFilter ActiveFilter => _filter;

    public bool Start()
    {
        if (_running) return IsDeviceOpen || _captureThread != null;
        _core = new MusicCore(GameRng.DerivedSeed("music.synth"), n => GameRng.For($"music.piece.{n}"));
        _core.SetVolumes(_musicVolume, _sfxVolume);
        _core.SetTrackCount(_trackCount);
        _running = true;

        // A quarter of a second rendered and thrown away before the stream opens: every DSP path is
        // compiled before the audio thread needs it, which is during the game's busiest seconds
        // (world generation, the language pipeline loading). Underruns at start-up came from there.
        var warmL = new float[Dsp.SampleRate / 4];
        var warmR = new float[warmL.Length];
        foreach (Instrument inst in Enum.GetValues<Instrument>())
        {
            var v = Voice.Create(inst);
            v.Init(60, 0.5f, 1);
            v.Render(warmL, 256);
        }
        _core.Render(warmL, warmR, 0, warmL.Length);

        // --silent opens nothing, which every caller already treats as a machine with no sound.
        _stream = Config.Debug.Silent ? null : PcmStreams.Open(Dsp.SampleRate, (l, r, n) => _core.Render(l, r, 0, n));
        if (_stream != null)
        {
            DeviceName = $"synthesiser, {_stream.Name}";
            return true;
        }
        // A capture (--record) plays nothing aloud but still composes, in real time, and keeps the
        // notes as MIDI on the recording's clock.
        if (MidiCapture.IsActive)
        {
            _core.MidiClock = () => MidiCapture.Now;
            _captureThread = new Thread(CaptureLoop) { IsBackground = true, Name = "MusicCapture" };
            _captureThread.Start();
            DeviceName = "capture (MIDI)";
            return true;
        }
        // --silent with nothing to capture: no sound and no composing, but an engine that still
        // answers what the game asked of it, so a script can read the music back (inspect music).
        if (Config.Debug.Silent)
        {
            _core = null;
            DeviceName = "silent";
            return true;
        }
        return false;
    }

    private void CaptureLoop()
    {
        var clock = Stopwatch.StartNew();
        var l = new float[512];
        var r = new float[512];
        while (_running)
        {
            while (_running && _core!.Seconds < clock.Elapsed.TotalSeconds + 0.05) _core.Render(l, r, 0, l.Length);
            Thread.Sleep(10);
        }
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;
        SetFilter(MusicFilter.None);
        _stream?.Dispose();
        _stream = null;
        _captureThread?.Join(500);
        _captureThread = null;
    }

    public void SetMood(MusicMoodState mood)
    {
        _mood = mood;
        if (_core == null) return;
        // The first mood a session is given is where it starts, not somewhere to glide to.
        if (!_moodSet) { _moodSet = true; _core.JumpMood(mood); }
        else _core.SetMood(mood);
    }

    public void Transition(float silenceSeconds)
    {
        // A first mood with no music before it needs no break.
        if (!_moodSet) return;
        _core?.Cut(silenceSeconds, 1.2f);
    }

    public void SetActiveTrackCount(int count)
    {
        _trackCount = Math.Clamp(count, 0, 4);
        _core?.SetTrackCount(_trackCount);
    }

    public void SetFilter(MusicFilter filter)
    {
        lock (_filterLock)
        {
            if (_filter == filter) return;
            _filter = filter;
            _noise?.Dispose();
            _noise = null;
            _core?.SetFighting(filter == MusicFilter.Fighting);
            _core?.SetTraveling(filter == MusicFilter.Traveling);
            // The brown-noise wash under a model call, a roll or a journey: the same stream as ever,
            // apart from the music, on the SFX volume.
            if (filter is MusicFilter.Loading or MusicFilter.DiceRoll or MusicFilter.Traveling && !Config.Debug.Silent && _running)
            {
                _noise = AudioOutput.CreateNoise(amplitude: 0.22f * _sfxVolume);
                _noise.Start();
            }
        }
    }

    public void TriggerGameEvent(GameEventType evt)
    {
        // Hover and click are PCM ticks, outside the music entirely.
        if (evt == GameEventType.SmallInteraction) { _uiSfx.PlayHover(); return; }
        if (evt == GameEventType.StrongInteraction) { _uiSfx.PlayClick(); return; }
        if (_core == null) return;

        // The sting always plays; the music's answer is held back within 400 ms of the last one, so a
        // burst of outcomes does not stack timpani strokes.
        Sting(evt);
        long now = Environment.TickCount64;
        if (now - Interlocked.Read(ref _lastEventMs) < 400) return;
        Interlocked.Exchange(ref _lastEventMs, now);
        switch (evt)
        {
            case GameEventType.PositiveOutcome: _core.Positive(); break;
            case GameEventType.NegativeOutcome: _core.Negative(); break;
            case GameEventType.NeutralOutcome: _core.Neutral(); break;
        }
    }

    /// <summary>
    /// The outcome stings, on the same notes the first engine used (they were written to be the same
    /// in every mood, so they do not follow the key): success climbs, failure is a cluster with a low
    /// afterbite, a neutral outcome is an open fifth that lingers.
    /// </summary>
    private void Sting(GameEventType evt)
    {
        var sq = Instrument.SquareLead;
        var notes = evt switch
        {
            GameEventType.PositiveOutcome => new List<(double, Instrument, int, double, float)>
            {
                (0.000, sq, 72, 0.034, 0.80f), (0.036, sq, 76, 0.034, 0.84f), (0.072, sq, 79, 0.040, 0.88f),
                (0.112, sq, 84, 0.32, 0.92f), (0.112, sq, 88, 0.32, 0.80f),
            },
            GameEventType.NegativeOutcome => new()
            {
                (0.0, Instrument.Horn, 57, 0.28, 0.85f), (0.0, Instrument.Horn, 58, 0.28, 0.8f), (0.0, Instrument.Horn, 59, 0.28, 0.75f),
                (0.315, Instrument.Timpani, 45, 0.4, 0.8f),
            },
            GameEventType.NeutralOutcome => new()
            {
                (0.0, Instrument.Bell, 62, 0.9, 0.6f), (0.0, Instrument.Bell, 69, 0.9, 0.5f), (0.0, Instrument.Bell, 74, 0.9, 0.4f),
            },
            _ => null,
        };
        if (notes != null) _core!.Sting(notes);
    }

    public void PlaySoundEffect(SoundEffectType sfx)
    {
        if (_core == null) return;
        var sq = Instrument.SquareLead;
        const int root = 57;
        var notes = sfx switch
        {
            SoundEffectType.ButtonClick => new List<(double, Instrument, int, double, float)> { (0, sq, root + 12, 0.06, 0.45f) },
            SoundEffectType.MenuSelect => new() { (0, sq, root + 7, 0.08, 0.45f), (0.085, sq, root + 12, 0.07, 0.5f) },
            SoundEffectType.NarrativeReveal => new() { (0, sq, root, 0.08, 0.4f), (0.2, sq, root + 5, 0.08, 0.4f), (0.4, sq, root + 10, 0.08, 0.4f) },
            SoundEffectType.TransitionUp => new() { (0, sq, root, 0.09, 0.5f), (0.18, sq, root + 5, 0.09, 0.5f), (0.36, sq, root + 10, 0.09, 0.5f), (0.54, sq, root + 12, 0.09, 0.55f) },
            SoundEffectType.TransitionDown => new() { (0, sq, root + 12, 0.09, 0.5f), (0.18, sq, root + 7, 0.09, 0.5f), (0.36, sq, root + 3, 0.09, 0.5f), (0.54, sq, root, 0.09, 0.55f) },
            SoundEffectType.MemoryFragment => new() { (0, Instrument.Choir, root, 0.2, 0.3f), (0.5, Instrument.Choir, root + 6, 0.5, 0.28f) },
            _ => null,
        };
        if (notes != null) _core.Sting(notes);
    }

    public void SetMasterMusicVolume(float v01)
    {
        _musicVolume = Math.Clamp(v01, 0f, 1f);
        _core?.SetVolumes(_musicVolume, _sfxVolume);
    }

    public void SetMasterSfxVolume(float v01)
    {
        _sfxVolume = Math.Clamp(v01, 0f, 1f);
        _core?.SetVolumes(_musicVolume, _sfxVolume);
        _uiSfx.SetVolume(_sfxVolume);
    }

    public void Dispose()
    {
        Stop();
        _uiSfx.Dispose();
    }
}
