using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Core;

namespace Cathedral.Audio.Music;

/// <summary>
/// Composer and synthesiser on one sample clock. Everything happens inside <see cref="Render"/>:
/// commands from other threads (a new mood, a game event, a filter) are queued and applied at the
/// next block, the composer is asked to plan a little ahead, due notes are started, and the block is
/// synthesised. The live engine calls it from its audio thread; <c>--render-music</c> calls it in a
/// loop as fast as the machine allows. Same code, same samples.
/// </summary>
internal sealed class MusicCore : IScoreObserver
{
    private const double Lookahead = 0.3;

    public readonly Synth Synth;
    private readonly Composer _composer;
    private readonly PriorityQueue<ScoreEvent, double> _due = new();
    private readonly List<ScoreEvent> _scratch = new();
    private readonly ConcurrentQueue<Action> _commands = new();
    private readonly float[] _level = new float[Enum.GetValues<Part>().Length];
    private readonly float[] _gate = new float[Enum.GetValues<Part>().Length];
    private MusicMoodState _target = MusicMoodState.Neutral, _active = MusicMoodState.Neutral;
    private int _trackCount = 4;
    private double _nextBusUpdate;
    private readonly float[] _l = new float[Synth.Block], _r = new float[Synth.Block];

    public ScoreLog? Log;
    /// <summary>Time spent composing and synthesising, in stopwatch ticks: where a render's time goes.</summary>
    public long ComposeTicks, SynthTicks;
    /// <summary>When set, every note is also written as MIDI (for <c>--record</c>), on that clock.</summary>
    public Func<double>? MidiClock;

    public MusicCore(int seed, Func<int, Random> pieceRng)
    {
        Synth = new Synth(seed);
        _composer = new Composer(pieceRng, this);
        Array.Fill(_level, 0f);
        Array.Fill(_gate, 1f);
        _level[(int)Part.Sfx] = 1f;
        _level[(int)Part.Fight] = Orchestra.PartLevel(Part.Fight);
        foreach (Part p in Enum.GetValues<Part>()) Synth.SetPartGain(p, _level[(int)p], 0.01f);
        Orchestra.ApplyBus(Synth, _active);
    }

    public double Seconds => Synth.Seconds;
    public MusicMoodState ActiveMood => _active;
    public string ModeName => _composer.ModeName;
    public double Bpm => _composer.Bpm;

    // ── Commands (any thread) ────────────────────────────────────────────────

    public void Post(Action a) => _commands.Enqueue(a);

    public void SetMood(MusicMoodState m) => Post(() => _target = m);

    /// <summary>Starts at <paramref name="m"/> with no glide: a render or a session that begins somewhere.</summary>
    public void JumpMood(MusicMoodState m) => Post(() => { _target = _active = m; _composer.Mood = m; Orchestra.ApplyBus(Synth, m); });

    public void SetTrackCount(int n) => Post(() => _trackCount = Math.Clamp(n, 0, 4));

    public void Positive() => Post(_composer.Positive);
    public void Negative() => Post(_composer.Negative);
    public void Neutral() => Post(_composer.Neutral);

    public void SetFighting(bool on) => Post(() => _composer.Fighting = on);
    public void SetTraveling(bool on) => Post(() => _composer.Traveling = on);

    /// <summary>A sound effect: notes on the SFX bus, starting now, outside the composer.</summary>
    public void Sting(IEnumerable<(double at, Instrument inst, int midi, double dur, float vel)> notes) => Post(() =>
    {
        double now = Seconds;
        foreach (var (at, inst, midi, dur, vel) in notes)
            _due.Enqueue(new ScoreEvent(now + at, dur, Part.Sfx, inst, midi, vel, 0f), now + at);
    });

    public void SetVolumes(float music, float sfx) => Post(() => { Synth.MusicGain.Target = music; Synth.SfxGain.Target = sfx; });

    // ── Rendering (one thread) ───────────────────────────────────────────────

    /// <summary>Renders <paramref name="n"/> frames into the two channels at <paramref name="offset"/>.</summary>
    public void Render(float[] left, float[] right, int offset, int n)
    {
        int done = 0;
        while (done < n)
        {
            int m = Math.Min(Synth.Block, n - done);
            long t0 = System.Diagnostics.Stopwatch.GetTimestamp();
            Step(m);
            long t1 = System.Diagnostics.Stopwatch.GetTimestamp();
            Synth.Render(_l, _r, 0, m);
            ComposeTicks += t1 - t0;
            SynthTicks += System.Diagnostics.Stopwatch.GetTimestamp() - t1;
            Array.Copy(_l, 0, left, offset + done, m);
            Array.Copy(_r, 0, right, offset + done, m);
            done += m;
        }
    }

    private void Step(int frames)
    {
        while (_commands.TryDequeue(out var a)) a();

        // Mood glides: about two seconds to most of the way, as the old engine did.
        float k = 1f - MathF.Exp(-(float)frames / Dsp.SampleRate / 1.8f);
        _active = new MusicMoodState(
            _active.Coldness + (_target.Coldness - _active.Coldness) * k,
            _active.Fear + (_target.Fear - _active.Fear) * k,
            _active.Mystery + (_target.Mystery - _active.Mystery) * k,
            _active.Intensity + (_target.Intensity - _active.Intensity) * k,
            _active.Festivity + (_target.Festivity - _active.Festivity) * k);
        _composer.Mood = _active;

        double now = Seconds;
        if (now >= _nextBusUpdate)
        {
            _nextBusUpdate = now + 0.25;
            Orchestra.ApplyBus(Synth, _active);
            UpdateGates();
        }

        _scratch.Clear();
        _composer.Immediate(now, _scratch);
        _composer.Fill(now + Lookahead, _scratch);
        foreach (var e in _scratch) _due.Enqueue(e, e.T);

        while (_due.TryPeek(out var e, out double t) && t <= now + 1e-9)
        {
            _due.Dequeue();
            int p = (int)e.Part;
            if (e.IsGain)
            {
                _level[p] = e.Vel;
                Synth.SetPartGain(e.Part, _level[p] * _gate[p], e.FadeSeconds);
                continue;
            }
            if (_gate[p] <= 0.01f) continue;   // a layer the track count has silenced: not even composed into sound
            Synth.NoteOn(e.Part, e.Inst, e.Midi, e.Vel, e.Dur, e.Pan);
            Log?.Note(now, e);
            if (MidiClock != null) CaptureMidi(e);
        }
    }

    /// <summary>
    /// What the track count and the intensity let through, per layer, on the old engine's ladder:
    /// one track is the drone and its pad, two adds the melody, three the counter and bass, four the
    /// ostinato and percussion. Intensity fades the same layers in over the same windows it always did.
    /// </summary>
    private void UpdateGates()
    {
        float I = _active.Intensity;
        bool dance = _composer.Current is { Dance: not Dance.None };
        static float Window(float x, float a, float b) => Math.Clamp((x - a) / (b - a), 0f, 1f);
        foreach (Part part in Enum.GetValues<Part>())
        {
            (int tracks, float a, float b) = part switch
            {
                Part.Ambience => (0, -1f, 0f),
                Part.Drone => (0, -1f, 0.10f),
                Part.Pad => (1, -1f, 0.10f),
                Part.Melody or Part.Bell => (2, 0.25f, 0.35f),
                Part.Counter or Part.Bass => (3, 0.50f, 0.60f),
                // In a dance the strummed chords and the drum are the floor the tune stands on, not
                // decoration: they come in with the melody, or a quieter festive place loses them.
                Part.Ostinato or Part.Perc when dance => (2, 0.25f, 0.35f),
                Part.Ostinato or Part.Perc => (4, 0.75f, 0.85f),
                _ => (0, -1f, 0f),
            };
            float g = (_trackCount >= tracks ? 1f : 0f) * (a < 0 ? 1f : Window(I, a, b));
            // The old engine's floor: a playing location never dropped its melody below 0.7 intensity,
            // so neither does this — but a scene that asks for fewer tracks gets fewer.
            if (Math.Abs(g - _gate[(int)part]) > 0.01f)
            {
                _gate[(int)part] = g;
                if (part != Part.Sfx && part != Part.Fight)
                    Synth.SetPartGain(part, _level[(int)part] * g, 1.5f);
            }
        }
    }

    // ── Capture for --record ─────────────────────────────────────────────────

    private static int Channel(Part p) => p switch
    {
        Part.Drone => 0, Part.Melody => 1, Part.Counter => 2, Part.Ostinato => 3, Part.Pad => 4,
        Part.Bass => 5, Part.Bell => 6, Part.Ambience => 7, Part.Perc => 9, Part.Fight => 8, _ => 14,
    };

    /// <summary>A General MIDI program that stands in for each instrument, for tools that read the MIDI.</summary>
    public static int GmProgram(Instrument i) => i switch
    {
        Instrument.Organ => 19, Instrument.OrganFlute => 19, Instrument.Choir => 52, Instrument.ChoirOoh => 53,
        Instrument.Strings => 49, Instrument.DarkPad => 95, Instrument.Recorder => 74, Instrument.Harpsichord => 6,
        Instrument.Harp => 46, Instrument.Lute => 24, Instrument.Bell => 14, Instrument.Timpani => 47,
        Instrument.HandDrum => 116, Instrument.Bourdon => 19, Instrument.Horn => 60, Instrument.SquareLead => 80,
        Instrument.Wind => 122, Instrument.Fiddle => 110, Instrument.Whistle => 78, Instrument.Pipes => 109,
        Instrument.Bodhran => 116, _ => 0,
    };

    private void CaptureMidi(ScoreEvent e)
    {
        int ch = Channel(e.Part);
        int note = Math.Clamp((int)MathF.Round(e.Midi), 0, 127);
        double t0 = MidiClock!();
        if (ch != 9) MidiCapture.RecordAt(t0, new ProgramChangeEvent((SevenBitNumber)GmProgram(e.Inst)) { Channel = (FourBitNumber)ch });
        MidiCapture.RecordAt(t0, new NoteOnEvent((SevenBitNumber)note, (SevenBitNumber)Math.Clamp((int)(e.Vel * 127), 1, 127)) { Channel = (FourBitNumber)ch });
        MidiCapture.RecordAt(t0 + e.Dur, new NoteOffEvent((SevenBitNumber)note, (SevenBitNumber)0) { Channel = (FourBitNumber)ch });
    }

    // ── IScoreObserver ───────────────────────────────────────────────────────

    void IScoreObserver.Piece(double t, Piece p) => Log?.Piece(t, p);
    void IScoreObserver.Section(double t, Section s, Piece p) => Log?.Section(t, s, p);
    void IScoreObserver.Chord(double t, double dur, int root, int[] pcs) => Log?.Chord(t, dur, root, pcs);
}
