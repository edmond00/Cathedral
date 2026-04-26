using System.Threading;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Multimedia;

namespace Cathedral.Audio;

/// <summary>
/// The procedural medieval ambient music engine.
/// Opens a MIDI output device and runs 1-4 background track loops.
/// Thread-safe: SetMood / SetActiveTrackCount / PlaySoundEffect may be called from any thread.
/// Implements IDisposable  Ealways dispose when done.
/// </summary>
public sealed class AmbianceEngine : IDisposable
{
    // ── State ────────────────────────────────────────────────────────────────

    private volatile bool _running = false;
    private int _activeTrackCount = 4; // default: all tracks active
    private readonly object _moodLock = new();

    // Mood transition: _targetMood set by SetMood; _activeMood smoothly lerped toward it
    private MusicMoodState _targetMood = MusicMoodState.Neutral;
    private MusicMoodState _activeMood = MusicMoodState.Neutral;

    // Shared scale: Drone picks it, all tracks follow  Eprevents harmonic chaos
    private int[] _sharedScale = ModalScale.Dorian;

    // Beat-grid epoch  Eall tracks snap phrase starts to beat boundaries relative to this
    private long _epochMs = 0;

    // Drone's current sounding MIDI note (broadcast for harmonic awareness)
    private volatile int _droneCurrentNote = 57; // A3 default

    // Direction of the most recent Melody phrase: +1=ascending, 0=level, -1=descending
    private volatile int _melodyDirection = 0;

    // Pitch-interval contour of the last Melody phrase (scale index deltas).
    // Passed back into GenerateMelodyPhrase for motif repetition (~30% chance).
    private int[] _melodyContour = Array.Empty<int>();

    // Last scale index played by Melody: lets Texture hover near the melody register.
    private volatile int _melodyLastScaleIdx = -1;
    // Timestamp (TickCount64 ms) when the Melody phrase last finished playing.
    // Counter reads this to decide whether to wait and "respond" rather than overlap.
    // Access via Interlocked (volatile long is not allowed in C#).
    private long _melodyPhraseEndMs = 0;

    // How many drone note cycles to hold the current scale before considering a change.
    // Reset each time a new scale is chosen. Prevents jarring random modulations.
    private int _droneScaleHoldCycles  = 0; // counts down
    // Dynamics swell: slow sine wave over 60 s, range 0.70 E.00
    private volatile float _dynamicsLevel = 1.0f;

    private OutputDevice? _device;
    private readonly List<Task> _trackTasks = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed = false;

    // ── Diagnostics (read by UI thread) ──────────────────────────────────────

    /// <summary>Most recent scale name selected by the Drone track.</summary>
    public string CurrentScaleName { get; private set; } = "—";

    /// <summary>Current tempo in BPM.</summary>
    public double CurrentBpm { get; private set; } = 70;

    // ── Properties ───────────────────────────────────────────────────────────

    public MusicMoodState CurrentMood
    {
        get { lock (_moodLock) return _activeMood; }
    }

    public int ActiveTrackCount => _activeTrackCount;

    public bool IsDeviceOpen => _device != null;

    public string DeviceName { get; private set; } = "(none)";

    // ── Startup / shutdown ───────────────────────────────────────────────────

    /// <summary>Opens MIDI device and starts background loops. Returns true if a device was found.</summary>
    public bool Start()
    {
        if (_running) return IsDeviceOpen;

        _device = OpenBestDevice();
        if (_device != null)
        {
            _device.PrepareForEventsSending();
            DeviceName = _device.Name;
        }

        _epochMs = Environment.TickCount64;
        _running = true;

        // Start 4 track tasks (tracks 1-3 sleep immediately if inactive)
        foreach (TrackRole role in Enum.GetValues<TrackRole>())
        {
            var captured = role;
            var rng = new Random(Environment.TickCount + (int)role * 137);
            _trackTasks.Add(Task.Run(() => TrackLoop(captured, rng, _cts.Token)));
        }

        // Background support tasks
        _trackTasks.Add(Task.Run(() => MoodSmootherTask(_cts.Token)));
        _trackTasks.Add(Task.Run(() => DynamicsTask(_cts.Token)));

        return IsDeviceOpen;
    }

    public void Stop()
    {
        if (!_running) return;
        _running = false;
        _cts.Cancel();
        try
        {
            Task.WhenAll(_trackTasks).Wait(TimeSpan.FromSeconds(4));
        }
        catch { /* ignored on shutdown */ }
        TurnAllNotesOff();
    }

    // ── Public controls ──────────────────────────────────────────────────────

    /// <summary>Sets the target mood; the active mood lerps toward it over ~3 s. Thread-safe.</summary>
    public void SetMood(MusicMoodState mood)
    {
        lock (_moodLock)
        {
            _targetMood = new MusicMoodState(
                Math.Clamp(mood.Sadness,   0f, 1f),
                Math.Clamp(mood.Fear,      0f, 1f),
                Math.Clamp(mood.Mystery,   0f, 1f),
                Math.Clamp(mood.Intensity, 0f, 1f));
        }
    }

    /// <summary>Sets number of active tracks (1–4). Extra tracks fade out on next note.</summary>
    public void SetActiveTrackCount(int count)
    {
        _activeTrackCount = Math.Clamp(count, 1, 4);
    }

    /// <summary>
    /// Returns a 0–1 volume multiplier for a track based on the Intensity gauge.
    /// Each track fades in over a 10% window: Drone 0–0.10, Melody 0.25–0.35,
    /// Counter 0.50–0.60, Texture 0.75–0.85.
    /// </summary>
    private static float GetTrackIntensityVolume(int trackIndex, float intensity)
    {
        // Per-track Intensity fade-in windows [start, end] in the 0–1 Intensity range.
        // Noise (index 4) activates at mid-intensity — between Counter and Texture.
        var (start, end) = trackIndex switch
        {
            0 => (0.00f, 0.10f), // Drone — always on
            1 => (0.25f, 0.35f), // Melody
            2 => (0.50f, 0.60f), // Counter
            3 => (0.75f, 0.85f), // Texture
            4 => (0.25f, 0.35f), // Noise — fades in alongside Melody, present most of the time
            _ => (1.00f, 1.10f), // unused: always off
        };
        return Math.Clamp((intensity - start) / (end - start), 0f, 1f);
    }

    /// <summary>Fires a short sound effect asynchronously on the SFX MIDI channel.</summary>
    public void PlaySoundEffect(SoundEffectType sfx)
    {
        if (_device == null) return;
        _ = Task.Run(() => PlaySfxInternal(sfx));
    }

    // ── Track loop ────────────────────────────────────────────────────────────

    private async Task TrackLoop(TrackRole role, Random rng, CancellationToken ct)
    {
        int trackIndex = (int)role;
        int channel = ProceduralMidiComposer.GetChannel(role);
        int lastPatch = -1;

        // Start each track at a different point in the scale to avoid unison
        int scaleIdx = trackIndex * 3;

        // Drone is the sole scale authority  Eit picks and broadcasts _sharedScale.
        // All other tracks follow: they read _sharedScale at every phrase boundary.
        int[] currentScale = ModalScale.Dorian;
        bool needNewScale  = (role == TrackRole.Drone);

        // Stagger non-Drone tracks on each (re)activation so they don't attack together
        bool justActivated = true;

        try
        {
            while (!ct.IsCancellationRequested && _running)
            {
                // Wait if this track is not yet active.
                // Noise bypasses this gate — it's always on, faded only by Intensity.
                if (role != TrackRole.Noise && trackIndex >= _activeTrackCount)
                {
                    justActivated = true; // will stagger when re-enabled
                    await Task.Delay(200, ct);
                    continue;
                }

                // Stagger entry  Edelay non-Drone tracks so phrases interleave
                if (justActivated && role != TrackRole.Drone)
                {
                    justActivated = false;
                    MusicMoodState sm; lock (_moodLock) sm = _activeMood;
                    double sBpm = ProceduralMidiComposer.GetTempoBpm(sm.Sadness, sm.Fear);
                    // Melody: ~0.75 beat, Counter: ~1.5 beats, Texture: ~2.25 beats
                    double offsetBeats = trackIndex * 0.75 + rng.NextDouble() * 0.5;
                    await Task.Delay((int)(60_000.0 / sBpm * offsetBeats), ct);
                    continue;
                }
                justActivated = false;

                MusicMoodState mood;
                lock (_moodLock) mood = _activeMood;

                // Scale management ─────────────────────────────────────────────
                // Drone picks scale and broadcasts it; all others always follow.
                if (role == TrackRole.Drone)
                {
                    if (needNewScale)
                    {
                        var newScale = ModalScale.GetScaleForMood(mood.Sadness, mood.Mystery, mood.Fear, rng);
                        lock (_moodLock) _sharedScale = newScale;
                        currentScale = newScale;
                        needNewScale = false;
                        // Hold this scale for 6-14 drone cycles before considering a new one
                        _droneScaleHoldCycles = rng.Next(6, 15);
                    }
                }
                else
                {
                    lock (_moodLock) currentScale = _sharedScale;
                }

                // Update diagnostics (only Drone sets global scale name)
                if (role == TrackRole.Drone)
                {
                    CurrentScaleName = ModalScale.GetScaleName(currentScale);
                    CurrentBpm = ProceduralMidiComposer.GetTempoBpm(mood.Sadness, mood.Fear);
                }

                var (minIdx, maxIdx) = ModalScale.GetNoteRange(currentScale.Length, role);
                scaleIdx = Math.Clamp(scaleIdx, minIdx, maxIdx);

                // Change instrument if patch changed
                int patch = ProceduralMidiComposer.GetInstrumentPatch(role, mood.Sadness, mood.Fear, mood.Mystery);
                if (patch != lastPatch)
                {
                    SendProgramChange(channel, patch);
                    lastPatch = patch;
                }

                double bpm = ProceduralMidiComposer.GetTempoBpm(mood.Sadness, mood.Fear);

                if (role == TrackRole.Melody || role == TrackRole.Counter)
                {
                    // ── Phrase-based generation ────────────────────────────────────────

                    // Snap to next beat boundary so all phrases start on the global grid
                    await AlignToNextBeatAsync(bpm, ct);

                    // Counter call-and-response: ~55% chance to wait for the Melody phrase
                    // to land before playing, creating alternating phrase dialogue.
                    if (role == TrackRole.Counter)
                    {
                        long msSinceMelodyEnd = Environment.TickCount64 - Interlocked.Read(ref _melodyPhraseEndMs);
                        double beatMs2 = 60_000.0 / bpm;
                        if (msSinceMelodyEnd < (long)(beatMs2 * 3) && rng.NextDouble() < 0.55)
                        {
                            long waitMs = (long)(beatMs2 * 3) - msSinceMelodyEnd + (long)(rng.NextDouble() * beatMs2 * 0.5);
                            if (waitMs > 0) await Task.Delay((int)waitMs, ct);
                        }
                    }

                    var phrase = role == TrackRole.Melody
                        ? ProceduralMidiComposer.GenerateMelodyPhrase(currentScale, scaleIdx, minIdx, maxIdx, mood.Sadness, mood.Fear, mood.Mystery, bpm, rng, _melodyContour)
                        : ProceduralMidiComposer.GenerateCounterPhrase(currentScale, scaleIdx, minIdx, maxIdx, mood.Sadness, mood.Fear, mood.Mystery, bpm, rng, _melodyDirection);

                    foreach (var noteEvent in phrase)
                    {
                        if (ct.IsCancellationRequested || trackIndex >= _activeTrackCount) break;

                        lock (_moodLock) mood = _activeMood;
                        float trackVol = GetTrackIntensityVolume(trackIndex, mood.Intensity);
                        int rawVel = ProceduralMidiComposer.GetVelocity(mood.Sadness, mood.Fear, role, rng);
                        if (noteEvent.IsAccented) rawVel = Math.Clamp(rawVel + 14, 0, 127);
                        rawVel = Math.Clamp((int)(rawVel * noteEvent.VelocityMult), 0, 127);
                        int velocity = Math.Clamp((int)(rawVel * _dynamicsLevel * trackVol), 0, 115);

                        if (velocity > 0)
                        {
                            SendNoteOn(channel, noteEvent.MidiNote, velocity);
                            await Task.Delay(noteEvent.DurationMs, ct);
                            SendNoteOff(channel, noteEvent.MidiNote);
                        }
                        else
                        {
                            await Task.Delay(noteEvent.DurationMs + noteEvent.RestAfterMs, ct);
                            scaleIdx = noteEvent.ScaleIdx;
                            continue;
                        }

                        if (noteEvent.RestAfterMs > 0)
                            await Task.Delay(noteEvent.RestAfterMs, ct);

                        scaleIdx = noteEvent.ScaleIdx;
                    }

                    // After Melody phrase, store contour for motif replay, broadcast direction,
                    // and record end-time for Counter call-and-response timing.
                    if (role == TrackRole.Melody && phrase.Length >= 2)
                    {
                        _melodyDirection    = Math.Sign(phrase[^1].MidiNote - phrase[0].MidiNote);
                        var contour = new int[phrase.Length - 1];
                        for (int ci = 0; ci < contour.Length; ci++)
                            contour[ci] = phrase[ci + 1].ScaleIdx - phrase[ci].ScaleIdx;
                        _melodyContour      = contour;
                        _melodyLastScaleIdx = phrase[^1].ScaleIdx;
                        _melodyPhraseEndMs  = Environment.TickCount64;
                    }

                    // Phrase-level rest between phrases.
                    // Mystery dramatically increases silence; tension shortens it.
                    double beatMs = 60_000.0 / bpm;
                    double phraseRestBeats = role == TrackRole.Melody
                        ? Math.Max(0.05, mood.Sadness * 0.5 + rng.NextDouble() * (0.5 + mood.Sadness * 1.0) + mood.Mystery * 5.5 - mood.Fear * 1.5)
                        : Math.Max(0.05, mood.Sadness * 0.3 + rng.NextDouble() * (0.3 + mood.Sadness * 0.5) + mood.Mystery * 3.0 - mood.Fear * 0.8);
                    await Task.Delay((int)(beatMs * phraseRestBeats), ct);
                }
                else if (role == TrackRole.Texture)
                {
                    // ── Texture: quick arpeggio figure ────────────────────────────────────
                    await AlignToNextBeatAsync(bpm, ct);
                    var arpPhrase = ProceduralMidiComposer.GenerateArpeggioPhrase(currentScale, minIdx, maxIdx, mood.Sadness, mood.Fear, mood.Mystery, bpm, rng, _melodyLastScaleIdx);
                    foreach (var noteEvent in arpPhrase)
                    {
                        if (ct.IsCancellationRequested || trackIndex >= _activeTrackCount) break;
                        float trackVol = GetTrackIntensityVolume(trackIndex, mood.Intensity);
                        int rawVel = ProceduralMidiComposer.GetVelocity(mood.Sadness, mood.Fear, role, rng);
                        rawVel = Math.Clamp((int)(rawVel * noteEvent.VelocityMult), 0, 127);
                        int vel = Math.Clamp((int)(rawVel * _dynamicsLevel * trackVol), 0, 115);
                        if (vel > 0)
                        {
                            SendNoteOn(channel, noteEvent.MidiNote, vel);
                            await Task.Delay(noteEvent.DurationMs, ct);
                            SendNoteOff(channel, noteEvent.MidiNote);
                        }
                        else
                        {
                            await Task.Delay(noteEvent.DurationMs + noteEvent.RestAfterMs, ct);
                            scaleIdx = noteEvent.ScaleIdx;
                            continue;
                        }
                        if (noteEvent.RestAfterMs > 0)
                            await Task.Delay(noteEvent.RestAfterMs, ct);
                        scaleIdx = noteEvent.ScaleIdx;
                    }
                    // Rest between arpeggio figures (mystery = longer silence)
                    double arpBeatMs = 60_000.0 / bpm;
                    int arpRestMs = (int)(arpBeatMs * Math.Max(0.05, mood.Sadness * 0.3 + rng.NextDouble() * (0.4 + mood.Sadness * 0.6) + mood.Mystery * 2.5 - mood.Fear * 0.5));
                    await Task.Delay(arpRestMs, ct);
                }
                else if (role == TrackRole.Noise)
                {
                    // ── Noise: continuous ambient pad wash, always present ────────────────
                    // Uses legato: new NoteOn fires before old NoteOff so there is never
                    // a moment of silence between note transitions.
                    lock (_moodLock) currentScale = _sharedScale;
                    var (noiseMin, noiseMax) = ModalScale.GetNoteRange(currentScale.Length, role);

                    int noisePatch = ProceduralMidiComposer.GetInstrumentPatch(role, mood.Sadness, mood.Fear, mood.Mystery);
                    if (noisePatch != lastPatch)
                    {
                        SendProgramChange(channel, noisePatch);
                        lastPatch = noisePatch;
                    }

                    var (midiNote, durationMs) = ProceduralMidiComposer.GenerateNoiseNote(
                        currentScale, noiseMin, noiseMax, mood.Sadness, mood.Fear, mood.Mystery, bpm, rng);

                    int rawVel = ProceduralMidiComposer.GetVelocity(mood.Sadness, mood.Fear, role, rng);
                    float noiseVol = GetTrackIntensityVolume(trackIndex, mood.Intensity);
                    // Noise bypasses most of the dynamics swell — keep it steady, only a 5% swell
                    float noiseDyn = 0.95f + (_dynamicsLevel - 0.85f) * 0.33f; // range ~0.95–0.98
                    // Floor at 12 so the wash is always faintly present regardless of Intensity
                    int vel = Math.Max(12, Math.Clamp((int)(rawVel * noiseDyn * noiseVol), 0, 115));

                    // Legato: start new note, wait for its duration, then release old note.
                    // The brief overlap prevents any gap between consecutive pad swells.
                    SendNoteOn(channel, midiNote, vel);
                    await Task.Delay(durationMs, ct);
                    SendNoteOff(channel, midiNote);
                }
                else
                {
                    // ── Drone: sustained pedal tone ───────────────────────────────────────
                    scaleIdx = ProceduralMidiComposer.GetNextNote(currentScale, scaleIdx, rng, mood.Sadness, minIdx, maxIdx);
                    int midiNote     = currentScale[scaleIdx];
                    float droneVol   = GetTrackIntensityVolume(trackIndex, mood.Intensity);
                    int rawVelocity  = ProceduralMidiComposer.GetVelocity(mood.Sadness, mood.Fear, role, rng);
                    int velocity     = Math.Clamp((int)(rawVelocity * _dynamicsLevel * droneVol), 0, 115);
                    int noteDuration = ProceduralMidiComposer.GetNoteDurationMs(mood.Sadness, mood.Fear, bpm, role);
                    int restDuration = ProceduralMidiComposer.GetRestMs(mood.Sadness, mood.Fear, bpm, role, rng);

                    _droneCurrentNote = midiNote; // broadcast for harmonic awareness

                    if (velocity > 0)
                    {
                        SendNoteOn(channel, midiNote, velocity);
                        await Task.Delay(noteDuration, ct);
                        SendNoteOff(channel, midiNote);
                    }
                    else
                    {
                        await Task.Delay(noteDuration, ct);
                    }

                    if (restDuration > 0)
                        await Task.Delay(restDuration, ct);

                    // Scale stability: count down the hold timer; only then consider a change
                    if (_droneScaleHoldCycles > 0)
                        _droneScaleHoldCycles--;
                    else if (rng.NextDouble() < 0.35) // higher rate now — but only fires after hold expires
                        needNewScale = true;
                }
            }
        }
        catch (OperationCanceledException) { /* expected on shutdown */ }
        finally
        {
            // Silence this channel
            if (_device != null)
            {
                try { SendNoteOff(channel, 0); } catch { /* ignore */ }
            }
        }
    }

    // ── Support tasks ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lerps _activeMood toward _targetMood at ~α=0.08 per 150 ms tick.
    /// Reaches ~95 % of target in ~3.5 s  Esmooth enough to avoid jarring key changes.
    /// </summary>
    private async Task MoodSmootherTask(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _running)
            {
                await Task.Delay(150, ct);
                lock (_moodLock)
                {
                    const float alpha = 0.08f;
                    _activeMood = new MusicMoodState(
                        _activeMood.Sadness    + alpha * (_targetMood.Sadness    - _activeMood.Sadness),
                        _activeMood.Fear       + alpha * (_targetMood.Fear       - _activeMood.Fear),
                        _activeMood.Mystery    + alpha * (_targetMood.Mystery    - _activeMood.Mystery),
                        _activeMood.Intensity  + alpha * (_targetMood.Intensity  - _activeMood.Intensity));
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Drives a slow sine-wave dynamics swell (0.70 E.00) over a 60 s cycle.
    /// Tracks multiply their computed velocity by _dynamicsLevel for a natural
    /// "breathing" quality without coordination overhead.
    /// </summary>
    private async Task DynamicsTask(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _running)
            {
                await Task.Delay(200, ct);
                _dynamicsLevel = 0.85f + 0.15f * (float)Math.Sin(Environment.TickCount64 * Math.PI / 30_000.0);
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Waits until the next beat boundary relative to the session epoch.
    /// Capped at one full beat so it never blocks longer than that.
    /// </summary>
    private Task AlignToNextBeatAsync(double bpm, CancellationToken ct)
    {
        double beatMs  = 60_000.0 / bpm;
        long elapsed   = Environment.TickCount64 - _epochMs;
        long beatCount = (long)(elapsed / beatMs);
        int waitMs = (int)((beatCount + 1) * beatMs - elapsed);
        waitMs = Math.Clamp(waitMs, 0, (int)beatMs); // cap at 1 beat
        return waitMs < 10 ? Task.CompletedTask : Task.Delay(waitMs, ct);
    }

    // ── SFX ──────────────────────────────────────────────────────────────────────────

    private void PlaySfxInternal(SoundEffectType sfx)
    {
        if (_device == null) return;

        int ch = ProceduralMidiComposer.SfxChannel;
        SendProgramChange(ch, ProceduralMidiComposer.PatchHarpsichord);

        MusicMoodState mood;
        lock (_moodLock) mood = _activeMood;

        int root = 57; // A3

        switch (sfx)
        {
            case SoundEffectType.ButtonClick:
                PlayNote(ch, root + 12, 55, 60); // A4 short click
                break;

            case SoundEffectType.MenuSelect:
                PlayNote(ch, root + 7,  55, 80);  // E4
                Thread.Sleep(85);
                PlayNote(ch, root + 12, 60, 70);  // A4
                break;

            case SoundEffectType.NarrativeReveal:
                PlayNote(ch, root,      50, 80);
                Thread.Sleep(120);
                PlayNote(ch, root + 5,  50, 80);
                Thread.Sleep(120);
                PlayNote(ch, root + 10, 50, 80);
                break;

            case SoundEffectType.TransitionUp:
                PlayNote(ch, root,      60, 90);
                Thread.Sleep(90);
                PlayNote(ch, root + 5,  60, 90);
                Thread.Sleep(90);
                PlayNote(ch, root + 10, 60, 90);
                Thread.Sleep(90);
                PlayNote(ch, root + 12, 65, 90);
                break;

            case SoundEffectType.TransitionDown:
                PlayNote(ch, root + 12, 60, 90);
                Thread.Sleep(90);
                PlayNote(ch, root + 7,  60, 90);
                Thread.Sleep(90);
                PlayNote(ch, root + 3,  60, 90);
                Thread.Sleep(90);
                PlayNote(ch, root,      65, 90);
                break;

            case SoundEffectType.MemoryFragment:
                // Two notes a tritone apart  Eevocative, slightly dissonant
                SendProgramChange(ch, ProceduralMidiComposer.PatchChoirAahs);
                PlayNote(ch, root,      35, 200);
                Thread.Sleep(300);
                PlayNote(ch, root + 6,  30, 500);
                break;
        }

        // Silence the SFX channel
        SendNoteOff(ch, 0);
    }

    private void PlayNote(int ch, int note, int velocity, int durationMs)
    {
        SendNoteOn(ch, note, velocity);
        Thread.Sleep(durationMs);
        SendNoteOff(ch, note);
    }

    // ── MIDI send helpers ─────────────────────────────────────────────────────

    private void SendNoteOn(int ch, int note, int velocity)
    {
        if (_device == null) return;
        try
        {
            var ev = new NoteOnEvent((SevenBitNumber)note, (SevenBitNumber)velocity);
            ev.Channel = (FourBitNumber)ch;
            _device.SendEvent(ev);
        }
        catch { /* device disconnected */ }
    }

    private void SendNoteOff(int ch, int note)
    {
        if (_device == null) return;
        try
        {
            var ev = new NoteOffEvent((SevenBitNumber)(note & 0x7F), (SevenBitNumber)0);
            ev.Channel = (FourBitNumber)ch;
            _device.SendEvent(ev);
        }
        catch { /* device disconnected */ }
    }

    private void SendProgramChange(int ch, int patch)
    {
        if (_device == null) return;
        try
        {
            var ev = new ProgramChangeEvent((SevenBitNumber)patch);
            ev.Channel = (FourBitNumber)ch;
            _device.SendEvent(ev);
        }
        catch { /* device disconnected */ }
    }

    private void TurnAllNotesOff()
    {
        if (_device == null) return;
        try { _device.TurnAllNotesOff(); } catch { /* ignore */ }
    }

    // ── Device discovery ──────────────────────────────────────────────────────

    private static OutputDevice? OpenBestDevice()
    {
        try
        {
            var all = OutputDevice.GetAll();
            if (!all.Any()) return null;

            // Prefer the Windows GS Wavetable Synth
            var gs = all.FirstOrDefault(d =>
                d.Name.Contains("GS Wavetable", StringComparison.OrdinalIgnoreCase) ||
                d.Name.Contains("Microsoft", StringComparison.OrdinalIgnoreCase));

            return gs ?? all.First();
        }
        catch
        {
            return null;
        }
    }

    // ── IDisposable ───────────────────────────────────────────────────────────

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
        _cts.Dispose();
        try { _device?.Dispose(); } catch { /* ignore */ }
    }
}
