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
    // Dynamics swell: slow sine wave over 60 s, range 0.70–1.00
    private volatile float _dynamicsLevel = 1.0f;

    // ── Game event system ─────────────────────────────────────────────────────
    // Signal from TriggerGameEvent to the Melody track. Bitmask flags (combined per event):
    // Bit 0 (0x01) = ForceAccent    — loud opening note(s)
    // Bit 1 (0x02) = ForceRepeat    — replay last melody contour
    // Bit 2 (0x04) = ForceBreak     — abrupt 1-note stab with elevated fear
    // Bit 3 (0x08) = ForcePause     — multiply inter-phrase rest ×8
    // Bit 4 (0x10) = ForceHalfTime  — slow phrase to 3× duration
    // Bit 5 (0x20) = ForceHighReg   — shift phrase up one octave
    // Bit 6 (0x40) = ForceLowReg    — shift phrase down one octave
    // Bit 7 (0x80) = ForceDoubleTime — compress phrase to half duration (urgent)
    // Read+cleared atomically.
    private int _melodyEventSignal = 0;
    // Drone and Noise tracks each have their own copy so all three can consume independently.
    // Same bitmask flags; only a subset is used per track.
    private int _droneEventSignal = 0;
    private int _noiseEventSignal  = 0;
    // Interrupt source pulsed on every TriggerGameEvent so that any interruptible
    // Task.Delay wakes up immediately and the track loops can apply the new signal
    // on the very next iteration rather than waiting out a long rest.
    private TaskCompletionSource<bool> _eventInterrupt =
        new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

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

    /// <summary>Sets number of active tracks (0–4). 0 = Noise only; extra tracks fade out on next note.</summary>
    public void SetActiveTrackCount(int count)
    {
        _activeTrackCount = Math.Clamp(count, 0, 4);
    }

    /// <summary>
    /// Triggers a game event: fires the first SFX note synchronously for zero-latency
    /// feel, delegates delays/note-offs to a background thread, and sets a music signal
    /// for the next Melody phrase. Mood gauges are NOT modified — controlled externally.
    /// Thread-safe.
    /// </summary>
    public void TriggerGameEvent(GameEventType evt)
    {
        if (_device == null) return;

        // Set music signal before starting sound so the melody track can pick it up
        // as soon as possible.
        int signal = evt switch
        {
            GameEventType.SmallInteraction  => 0x01,        // ForceAccent
            GameEventType.StrongInteraction => 0x83,        // ForceRepeat | ForceAccent | ForceDoubleTime
            GameEventType.PositiveOutcome   => 0x21,        // ForceAccent | ForceHighReg
            GameEventType.NegativeOutcome   => 0x44,        // ForceBreak  | ForceLowReg
            GameEventType.NeutralOutcome    => 0x18,        // ForcePause  | ForceHalfTime
            _ => 0,
        };
        if (signal > 0)
            Interlocked.Exchange(ref _melodyEventSignal, signal);

        // Drone and Noise each get their own signal (consumed independently).
        int droneSignal = evt switch
        {
            GameEventType.PositiveOutcome   => 0x21,  // ForceAccent + ForceHighReg (leap up)
            GameEventType.NegativeOutcome   => 0x44,  // ForceLowReg + ForceBreak (dark drop, cut short)
            GameEventType.NeutralOutcome    => 0x08,  // ForcePause (hold note very long)
            _ => 0,
        };
        if (droneSignal > 0)
            Interlocked.Exchange(ref _droneEventSignal, droneSignal);

        int noiseSignal = evt switch
        {
            GameEventType.PositiveOutcome   => 0x01,  // ForceAccent (brief swell)
            GameEventType.NegativeOutcome   => 0x04,  // ForceBreak (max-velocity noise burst)
            GameEventType.NeutralOutcome    => 0x08,  // ForcePause (near-silence, one note)
            _ => 0,
        };
        if (noiseSignal > 0)
            Interlocked.Exchange(ref _noiseEventSignal, noiseSignal);

        // Pulse the interrupt: any track sleeping in InterruptibleDelay wakes up now
        // and will read the new event signal on its very next iteration.
        var prevInterrupt = Interlocked.Exchange(
            ref _eventInterrupt,
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously));
        prevInterrupt.TrySetResult(true);

        // Fire the first note-on synchronously on the calling thread so the sound
        // starts with zero scheduling latency. Note-offs / sequenced notes run async.
        PlayGameEventSfxImmediate(evt);
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

                    // Event signal for this phrase (Melody-only; Counter follows naturally)
                    bool applyForcePause  = false;
                    bool forceHighReg     = false;
                    bool forceLowReg      = false;
                    bool forceHalfTime    = false;
                    bool forceDoubleTime  = false;

                    NoteEvent[] phrase;
                    if (role == TrackRole.Melody)
                    {
                        // ── Read and clear event signal (atomic) ─────────────────────
                        int eventSig     = Interlocked.Exchange(ref _melodyEventSignal, 0);
                        bool forceRepeat   = (eventSig & 0x02) != 0;
                        bool forceBreak    = (eventSig & 0x04) != 0;
                        bool forceAccent   = (eventSig & 0x01) != 0;
                        applyForcePause    = (eventSig & 0x08) != 0;
                        forceHighReg       = (eventSig & 0x20) != 0;
                        forceLowReg        = (eventSig & 0x40) != 0;
                        forceHalfTime      = (eventSig & 0x10) != 0;
                        forceDoubleTime    = (eventSig & 0x80) != 0;

                        if (forceBreak)
                        {
                            // NegativeOutcome: single jagged note stab with maximum fear
                            phrase = ProceduralMidiComposer.GenerateMelodyPhrase(
                                currentScale, scaleIdx, minIdx, maxIdx,
                                Math.Min(1f, mood.Sadness + 0.5f),
                                Math.Min(1f, mood.Fear    + 0.95f),
                                mood.Mystery, bpm, rng, null);
                            // Trim to 1 note: a brutal cut
                            if (phrase.Length > 1) phrase = phrase[..1];
                            // Force a long silence after the stab — the music “freezes”
                            applyForcePause = true;
                        }
                        else
                        {
                            phrase = ProceduralMidiComposer.GenerateMelodyPhrase(
                                currentScale, scaleIdx, minIdx, maxIdx,
                                mood.Sadness, mood.Fear, mood.Mystery, bpm, rng,
                                _melodyContour, forceMotifReplay: forceRepeat);
                        }

                        // ForceAccent: strong velocity spike on opening + mid notes
                        if (forceAccent && phrase.Length > 0)
                        {
                            // Accent all notes at diminishing intensity for a surging sweep
                            for (int ai = 0; ai < phrase.Length; ai++)
                            {
                                float mult = ai == 0 ? 3.2f : ai == 1 ? 2.4f : ai == 2 ? 1.9f : 1.5f;
                                phrase[ai] = phrase[ai] with { VelocityMult = phrase[ai].VelocityMult * mult, IsAccented = true };
                            }
                        }
                    }
                    else
                    {
                        // ── Counter imitation echo (~30% when melody contour available) ──────
                        // Replays the Melody's exact contour starting ~4 scale steps higher
                        // (roughly a 4th/5th), creating Renaissance canonic imitation.
                        bool doImitate = _melodyContour != null && rng.NextDouble() < 0.30;
                        if (doImitate)
                        {
                            int imitateStart = Math.Clamp(_melodyLastScaleIdx + 4, minIdx, maxIdx);
                            phrase = ProceduralMidiComposer.GenerateMelodyPhrase(
                                currentScale, imitateStart, minIdx, maxIdx,
                                mood.Sadness, mood.Fear, mood.Mystery, bpm, rng, _melodyContour);
                        }
                        else
                        {
                            phrase = ProceduralMidiComposer.GenerateCounterPhrase(
                                currentScale, scaleIdx, minIdx, maxIdx,
                                mood.Sadness, mood.Fear, mood.Mystery, bpm, rng, _melodyDirection);
                        }
                    }

                    // ── Register shift (~20% chance, or forced by event) ────────────────
                    // Shift all phrase notes up or down one scale-octave for spatial variety.
                    // ForceHighReg (Positive): always leap up. ForceLowReg (Negative): always drop.
                    if (forceHighReg || forceLowReg || rng.NextDouble() < 0.20)
                    {
                        int scaleLen  = currentScale.Length;
                        int octaveLen = scaleLen / 3; // each octave span in scale-index units
                        bool shiftUp  = forceHighReg ? true
                                      : forceLowReg  ? false
                                      : (_melodyDirection >= 0 ? rng.NextDouble() < 0.65 : rng.NextDouble() < 0.35);
                        int  delta    = shiftUp ? octaveLen : -octaveLen;
                        for (int pi = 0; pi < phrase.Length; pi++)
                        {
                            int shifted = Math.Clamp(phrase[pi].ScaleIdx + delta, 0, scaleLen - 1);
                            phrase[pi].ScaleIdx = shifted;
                            phrase[pi].MidiNote = currentScale[shifted];
                        }
                    }

                    // ── Half/double-time (~25% chance, or forced by event) ──
                    // ForceHalfTime (NeutralOutcome): always 3× duration — heavy weight.
                    // ForceDoubleTime (StrongInteraction): always ×0.5 — urgent replay.
                    if (forceHalfTime || forceDoubleTime || rng.NextDouble() < 0.25)
                    {
                        float timingMult;
                        if (forceHalfTime)
                        {
                            timingMult = 3.0f;
                        }
                        else if (forceDoubleTime)
                        {
                            timingMult = 0.5f;
                        }
                        else
                        {
                            double doubleW = 0.5 + mood.Fear * 0.4;
                            double halfW   = 0.5 + mood.Sadness * 0.4;
                            timingMult = rng.NextDouble() * (doubleW + halfW) < doubleW ? 0.5f : 2.0f;
                        }
                        for (int pi = 0; pi < phrase.Length; pi++)
                        {
                            phrase[pi].DurationMs  = Math.Max(40,  (int)(phrase[pi].DurationMs  * timingMult));
                            phrase[pi].RestAfterMs = Math.Max(0,   (int)(phrase[pi].RestAfterMs * timingMult));
                        }
                    }

                    // ── Occasional instrument swap (~1-in-8 phrases) ──────────────────
                    // Picks an alternate patch from within the same mood tier for timbral
                    // variety without crossing mood boundaries.
                    if (rng.NextDouble() < 0.125)
                    {
                        int altPatch = ProceduralMidiComposer.GetAlternateInstrumentPatch(role, mood.Sadness, mood.Fear, mood.Mystery, rng);
                        if (altPatch != lastPatch)
                        {
                            SendProgramChange(channel, altPatch);
                            lastPatch = altPatch;
                        }
                    }

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
                            // Humanize: tiny random pre-note delay (0–28 ms) so phrases breathe
                            // and notes don't all snap to the exact millisecond grid.
                            int humanMs = rng.Next(28);
                            if (humanMs > 0) await Task.Delay(humanMs, ct);
                            SendNoteOn(channel, noteEvent.MidiNote, velocity);
                            await Task.Delay(Math.Max(1, noteEvent.DurationMs - humanMs), ct);
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
                    // NeutralOutcome (ForcePause): triple the rest for a contemplative breath.
                    double beatMs = 60_000.0 / bpm;
                    double phraseRestBeats = role == TrackRole.Melody
                        ? Math.Max(0.05, mood.Sadness * 0.5 + rng.NextDouble() * (0.5 + mood.Sadness * 1.0) + mood.Mystery * 5.5 - mood.Fear * 1.5)
                        : Math.Max(0.05, mood.Sadness * 0.3 + rng.NextDouble() * (0.3 + mood.Sadness * 0.5) + mood.Mystery * 3.0 - mood.Fear * 0.8);
                    if (applyForcePause) phraseRestBeats *= 8.0;
                    // Interruptible: a new event wakes this up immediately so the next
                    // phrase starts with the fresh signal rather than waiting out the rest.
                    await InterruptibleDelay((int)(beatMs * phraseRestBeats), ct);
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
                    // Noise dynamics: allow a fuller swell so the pad texture breathes.
                    float noiseDyn = 0.88f + (_dynamicsLevel - 0.85f) * 1.5f; // range ~0.85–1.07
                    noiseDyn = Math.Clamp(noiseDyn, 0.70f, 1.10f);
                    // Floor at 18 so the wash remains audible even at low Intensity.
                    int vel = Math.Max(14, Math.Clamp((int)(rawVel * noiseDyn * noiseVol), 0, 115));

                    // ── Noise event effects ───────────────────────────────────────────
                    int noiseSig   = Interlocked.Exchange(ref _noiseEventSignal, 0);
                    bool nAccent   = (noiseSig & 0x01) != 0;
                    bool nBreak    = (noiseSig & 0x04) != 0;
                    bool nPause    = (noiseSig & 0x08) != 0;

                    if (nAccent) vel = Math.Clamp(vel + 10, 0, 115);
                    if (nBreak)  { vel = Math.Clamp(vel + 35, 0, 115); durationMs = Math.Max(durationMs, 900); } // moderate surge
                    if (nPause)  { vel = 3; durationMs = Math.Max(durationMs, 1800); } // deep near-silence, held long

                    // Brown-noise texture: play two notes a major 2nd apart (2 semitones).
                    // The slow beating/interference between the two deep pads creates a
                    // dense low rumble that mimics the character of brown noise.
                    int clusterNote = Math.Clamp(midiNote + 2, 21, 108);
                    int clusterVel  = Math.Max(10, vel - 14);

                    // Legato: start both notes together, hold, release together.
                    SendNoteOn(channel, midiNote,    vel);
                    SendNoteOn(channel, clusterNote, clusterVel);
                    await InterruptibleDelay(durationMs, ct);
                    SendNoteOff(channel, midiNote);
                    SendNoteOff(channel, clusterNote);
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

                    // ── Drone event effects ───────────────────────────────────────────
                    int droneSig    = Interlocked.Exchange(ref _droneEventSignal, 0);
                    bool dAccent    = (droneSig & 0x01) != 0;
                    bool dBreak     = (droneSig & 0x04) != 0;
                    bool dPause     = (droneSig & 0x08) != 0;
                    bool dHighReg   = (droneSig & 0x20) != 0;
                    bool dLowReg    = (droneSig & 0x40) != 0;

                    if (dAccent)  velocity     = Math.Clamp(velocity + 8,  0, 115);
                    if (dBreak)   { noteDuration /= 5; restDuration = (int)(60_000.0 / bpm * 2); } // snap off, short dark silence
                    if (dPause)   { velocity = Math.Clamp(velocity + 10, 0, 115); noteDuration = (int)(noteDuration * 3.0); } // gentle sustained resonance
                    if (dHighReg) { midiNote = Math.Clamp(midiNote + 12, 21, 108); }
                    if (dLowReg)  { midiNote = Math.Clamp(midiNote - 12, 21, 108); }

                    // ── Organum 5th ───────────────────────────────────────────────────
                    // Low fear: add a softer parallel 5th above the root — medieval organum.
                    // Mystery occasionally swaps the 5th for a 4th (more archaic/eerie).
                    // At high fear the 5th is dropped so dissonance isn't smoothed away.
                    int organum5th     = Math.Clamp(midiNote + (mood.Mystery > 0.55f && rng.NextDouble() < 0.40 ? 5 : 7), 21, 108);
                    bool playOrganum   = mood.Fear < 0.55f && velocity > 0;
                    int  organumVel    = Math.Clamp(velocity - 18, 0, 100); // softer than root

                    if (velocity > 0)
                    {
                        SendNoteOn(channel, midiNote, velocity);
                        if (playOrganum) SendNoteOn(channel, organum5th, organumVel);
                        await InterruptibleDelay(noteDuration, ct);
                        SendNoteOff(channel, midiNote);
                        if (playOrganum) SendNoteOff(channel, organum5th);
                    }
                    else
                    {
                        await InterruptibleDelay(noteDuration, ct);
                    }

                    if (restDuration > 0)
                        await InterruptibleDelay(restDuration, ct);

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

    /// <summary>
    /// Predetermined SFX for each GameEventType. Designed to be harmonically neutral
    /// (consistent across all moods) while emotionally matching the event.
    /// </summary>
    // Fire initial MIDI events synchronously (zero latency), then schedule
    // note-offs and remaining notes on a background thread.
    private void PlayGameEventSfxImmediate(GameEventType evt)
    {
        if (_device == null) return;
        int ch = ProceduralMidiComposer.SfxChannel;

        switch (evt)
        {
            case GameEventType.SmallInteraction:
            {
                // Closed hi-hat tick — ultra-short, dry, blends with the soundtrack.
                // Closed hi-hat (GM note 42) naturally decays in <40 ms; no sustain.
                const int drumCh = 9;
                SendNoteOn(drumCh, 42, 55); // Closed hi-hat
                _ = Task.Run(() => { Thread.Sleep(18); SendNoteOff(drumCh, 42); });
                break;
            }

            case GameEventType.StrongInteraction:
            {
                // Square lead two-note chord: A4 + E5 (fifth) — punchy, digital, not strident.
                // Square wave is more muted than sawtooth; fifth interval is open/strong.
                SendProgramChange(ch, ProceduralMidiComposer.PatchLeadSquare);
                SendNoteOn(ch, 69, 88); // A4
                SendNoteOn(ch, 76, 82); // E5 — clean fifth
                _ = Task.Run(() =>
                {
                    Thread.Sleep(28); SendNoteOff(ch, 69); SendNoteOff(ch, 76);
                    Thread.Sleep(25);
                    SendNoteOn(ch, 71, 80); // B4 — brief echo note
                    Thread.Sleep(20); SendNoteOff(ch, 71);
                });
                break;
            }

            case GameEventType.PositiveOutcome:
            {
                // Crystal patch rapid ascending staccato: C6→D6→E6→G6→C7.
                // Each ping is tight and digital; top note lingers briefly.
                SendProgramChange(ch, ProceduralMidiComposer.PatchFxCrystal);
                SendNoteOn(ch, 84, 80); // C6 — fired synchronously for zero latency
                _ = Task.Run(() =>
                {
                    Thread.Sleep(32); SendNoteOff(ch, 84);
                    SendNoteOn(ch, 86, 85); Thread.Sleep(32); SendNoteOff(ch, 86); // D6
                    SendNoteOn(ch, 88, 90); Thread.Sleep(32); SendNoteOff(ch, 88); // E6
                    SendNoteOn(ch, 91, 93); Thread.Sleep(40); SendNoteOff(ch, 91); // G6
                    SendNoteOn(ch, 96, 97); Thread.Sleep(220); SendNoteOff(ch, 96); // C7 — held
                });
                break;
            }

            case GameEventType.NegativeOutcome:
            {
                // Three-semitone sawtooth cluster: A3+Bb3+B3 simultaneously = maximum digital dissonance.
                // A low afterbite hits 300 ms later to close the crash.
                SendProgramChange(ch, ProceduralMidiComposer.PatchLeadSawtooth);
                SendNoteOn(ch, 57, 95); // A3
                SendNoteOn(ch, 58, 90); // Bb3 — semitone
                SendNoteOn(ch, 59, 85); // B3  — semitone cluster = digital screech
                _ = Task.Run(() =>
                {
                    Thread.Sleep(280);
                    SendNoteOff(ch, 57); SendNoteOff(ch, 58); SendNoteOff(ch, 59);
                    Thread.Sleep(35);
                    SendNoteOn(ch, 45, 82); // A2 — low gut-punch afterbite
                    Thread.Sleep(60); SendNoteOff(ch, 45);
                });
                break;
            }

            case GameEventType.NeutralOutcome:
            {
                // Echoes patch open fifth + octave: D4 + A4 + D5.
                // Three-voice voicing makes it clearly audible; internal echo decay lingers.
                SendProgramChange(ch, ProceduralMidiComposer.PatchFxEchoes);
                SendNoteOn(ch, 62, 82); // D4 — louder
                SendNoteOn(ch, 69, 70); // A4 — fifth
                SendNoteOn(ch, 74, 58); // D5 — octave above root, soft top
                _ = Task.Run(() =>
                {
                    Thread.Sleep(900);
                    SendNoteOff(ch, 62);
                    SendNoteOff(ch, 69);
                    SendNoteOff(ch, 74);
                });
                break;
            }
        }
    }

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

    /// <summary>
    /// Waits for <paramref name="ms"/> ms, or returns early if a game event fires.
    /// Only throws <see cref="OperationCanceledException"/> on engine shutdown.
    /// </summary>
    private async Task InterruptibleDelay(int ms, CancellationToken ct)
    {
        await Task.WhenAny(Task.Delay(ms, ct), _eventInterrupt.Task);
        ct.ThrowIfCancellationRequested();
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
