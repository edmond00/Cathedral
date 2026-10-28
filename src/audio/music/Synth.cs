using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Audio.Music;

/// <summary>
/// The layers of the music. Each is a bus with its own gain, so a layer can enter and leave as a
/// swell rather than a cut, and its own sends to the hall and the ensemble chorus.
/// </summary>
public enum Part
{
    Drone, Pad, Melody, Counter, Ostinato, Bass, Perc, Bell, Ambience, Cantus, Fight, Sfx,
}

/// <summary>
/// The music's own synthesiser: voices into part buses, buses into a chorus and a hall, all of it
/// through a cassette and a limiter. Single-threaded and sample-clocked: whoever owns it calls
/// <see cref="Render"/> for the next block and schedules notes between calls, which is what makes
/// the offline render and the live one the same code producing the same samples.
/// </summary>
internal sealed class Synth
{
    public const int Block = 64;
    private const int MaxVoices = 72;
    private static readonly int PartCount = Enum.GetValues<Part>().Length;

    private sealed class Bus
    {
        public float[] L = new float[Block], R = new float[Block];
        public Smoothed Gain = new(1f, 0.5f);
        public float Reverb = 0.3f, Chorus = 0f;
        public float PanSpread = 0.3f;
    }

    private readonly Bus[] _bus;
    private readonly List<Voice> _voices = new();
    private readonly float[] _mono = new float[Block];
    private readonly float[] _dryL = new float[Block], _dryR = new float[Block];
    private readonly float[] _sfxL = new float[Block], _sfxR = new float[Block];
    private readonly float[] _revInL = new float[Block], _revInR = new float[Block];
    private readonly float[] _choInL = new float[Block], _choInR = new float[Block];
    private readonly Reverb _reverb = new();
    private readonly Chorus _chorus = new();
    private readonly Tape _tape;
    private readonly Limiter _limiter = new();
    private int _voiceSeed;
    // Per-part energy, for the harness: how loud each layer is while it plays (its "stem" level).
    private readonly double[] _stemEnergy = new double[PartCount];
    private readonly long[] _stemFrames = new long[PartCount];
    private double _dryEnergy, _wetEnergy;

    /// <summary>The hall's return against the dry signal over the whole render, dB: how wet the mix is.</summary>
    public double WetDb => 10 * Math.Log10((_wetEnergy + 1e-12) / (_dryEnergy + 1e-12));
    private readonly int _seed;

    public long Sample { get; private set; }
    public double Seconds => Sample / (double)Dsp.SampleRate;

    public Smoothed MusicGain = new(1f, 0.3f);
    public Smoothed SfxGain = new(1f, 0.05f);
    public Smoothed ChorusDepth = new(0.6f, 2f);
    public float ReverbReturn = 0.9f;
    /// <summary>Everything into the tape at this level: the mix's headroom.</summary>
    public float PreGain = 0.22f;

    public Synth(int seed)
    {
        _seed = seed;
        _tape = new Tape(seed);
        _bus = new Bus[PartCount];
        for (int i = 0; i < PartCount; i++) _bus[i] = new Bus();
        // Defaults per layer; the composer's orchestration overrides them per section.
        Configure(Part.Drone, reverb: 0.25f, chorus: 0.1f, spread: 0.1f);
        Configure(Part.Pad, reverb: 0.45f, chorus: 0.7f, spread: 0.5f);
        Configure(Part.Melody, reverb: 0.4f, chorus: 0.15f, spread: 0.25f);
        Configure(Part.Counter, reverb: 0.45f, chorus: 0.2f, spread: 0.45f);
        Configure(Part.Ostinato, reverb: 0.35f, chorus: 0.1f, spread: 0.6f);
        Configure(Part.Bass, reverb: 0.2f, chorus: 0f, spread: 0.05f);
        Configure(Part.Perc, reverb: 0.4f, chorus: 0f, spread: 0.3f);
        Configure(Part.Bell, reverb: 0.7f, chorus: 0f, spread: 0.6f);
        Configure(Part.Ambience, reverb: 0.6f, chorus: 0.3f, spread: 0.8f);
        // The distant choir: almost all hall, wide, as if from the far end of a nave or across a valley.
        Configure(Part.Cantus, reverb: 0.9f, chorus: 0.35f, spread: 0.85f);
        Configure(Part.Fight, reverb: 0.4f, chorus: 0.1f, spread: 0.5f);
        Configure(Part.Sfx, reverb: 0.25f, chorus: 0f, spread: 0.2f);
    }

    public void Configure(Part p, float reverb, float chorus, float spread)
    {
        var b = _bus[(int)p];
        b.Reverb = reverb; b.Chorus = chorus; b.PanSpread = spread;
    }

    public void SetPartGain(Part p, float gain, float seconds)
    {
        ref var g = ref _bus[(int)p].Gain;
        g.SetTime(Math.Max(0.01f, seconds / 3f));
        g.Target = gain;
    }

    public float PartGain(Part p) => _bus[(int)p].Gain.Target;

    public void SetHall(float t60, float dampHz, float preDelay) => _reverb.Set(t60, dampHz, preDelay);

    public void SetTape(float cutoffHz, float wowCents, float hissDb, float drive)
    {
        if (Math.Abs(cutoffHz - _tape.CutoffHz) > 20f) _tape.SetCutoff(cutoffHz);
        _tape.WowCents = wowCents;
        _tape.HissDb = hissDb;
        _tape.Drive = drive;
    }

    /// <summary>Starts a note now. <paramref name="pan"/> is -1..1 inside the part's spread.</summary>
    public void NoteOn(Part part, Instrument inst, float midi, float velocity, double seconds, float pan = 0f, float glide = 0f)
    {
        if (velocity <= 0f) return;
        if (_voices.Count >= MaxVoices) StealOne();
        var v = Voice.Create(inst);
        v.Part = (int)part;
        v.Pan = Math.Clamp(pan, -1f, 1f) * _bus[(int)part].PanSpread;
        v.StartSample = Sample;
        v.GateEnd = Sample + Math.Max(1, (long)(seconds * Dsp.SampleRate));
        v.Glide = glide;
        v.Init(midi, velocity, unchecked(_seed * 31 + ++_voiceSeed * 7919));
        _voices.Add(v);
    }

    /// <summary>Releases every voice of a part over <paramref name="seconds"/>.</summary>
    public void ReleasePart(Part part, float seconds)
    {
        foreach (var v in _voices)
            if (v.Part == (int)part) v.Kill(seconds);
    }

    public int VoiceCount => _voices.Count;

    /// <summary>Sounding voices by instrument, for diagnosing a render.</summary>
    public string VoiceSummary() => string.Join(" ", _voices.GroupBy(v => v.GetType().Name + (v.Releasing ? "~" : ""))
        .Select(g => $"{g.Key}:{g.Count()}"));

    private void StealOne()
    {
        // The quietest releasing voice; failing that, the oldest.
        Voice? pick = null;
        foreach (var v in _voices)
            if (v.Releasing && (pick == null || v.Level < pick.Level)) pick = v;
        pick ??= _voices[0];
        _voices.Remove(pick);
    }

    /// <summary>Mean level of each part while it was sounding, dBFS before the master chain.</summary>
    public IEnumerable<(Part part, double db, double seconds)> StemLevels()
    {
        for (int p = 0; p < PartCount; p++)
            if (_stemFrames[p] > 0)
                yield return ((Part)p, 10 * Math.Log10(_stemEnergy[p] / (2.0 * _stemFrames[p]) + 1e-20) + 20 * Math.Log10(PreGain),
                              _stemFrames[p] / (double)Dsp.SampleRate);
    }

    /// <summary>Renders <paramref name="n"/> frames (a multiple of <see cref="Block"/> is cheapest).</summary>
    public void Render(float[] outL, float[] outR, int offset, int n)
    {
        int done = 0;
        while (done < n)
        {
            int m = Math.Min(Block, n - done);
            RenderBlock(m);
            Array.Copy(_dryL, 0, outL, offset + done, m);
            Array.Copy(_dryR, 0, outR, offset + done, m);
            done += m;
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    private void RenderBlock(int n)
    {
        foreach (var b in _bus) { Array.Clear(b.L, 0, n); Array.Clear(b.R, 0, n); }

        for (int i = _voices.Count - 1; i >= 0; i--)
        {
            var v = _voices[i];
            if (Sample >= v.GateEnd && !v.Releasing) v.NoteOff();
            Array.Clear(_mono, 0, n);
            v.Render(_mono, n);
            var bus = _bus[v.Part];
            float a = (v.Pan + 1f) * MathF.PI * 0.25f;
            float gl = MathF.Cos(a) * 1.4142f, gr = MathF.Sin(a) * 1.4142f;
            for (int s = 0; s < n; s++) { bus.L[s] += _mono[s] * gl; bus.R[s] += _mono[s] * gr; }
            if (v.Done) _voices.RemoveAt(i);
        }

        Array.Clear(_dryL, 0, n); Array.Clear(_dryR, 0, n);
        Array.Clear(_sfxL, 0, n); Array.Clear(_sfxR, 0, n);
        Array.Clear(_revInL, 0, n); Array.Clear(_revInR, 0, n);
        Array.Clear(_choInL, 0, n); Array.Clear(_choInR, 0, n);
        float music = MusicGain.Block(n), sfx = SfxGain.Block(n);
        for (int p = 0; p < _bus.Length; p++)
        {
            var b = _bus[p];
            float g = b.Gain.Block(n) * (p == (int)Part.Sfx ? sfx : music);
            if (g < 1e-5f) continue;
            float[] dl = p == (int)Part.Sfx ? _sfxL : _dryL, dr = p == (int)Part.Sfx ? _sfxR : _dryR;
            float rv = b.Reverb, ch = b.Chorus;
            double e = 0;
            for (int q = 0; q < n; q++) e += b.L[q] * b.L[q] + b.R[q] * b.R[q];
            e *= g * g;
            if (e > 1e-9 * n) { _stemEnergy[p] += e; _stemFrames[p] += n; }
            for (int s = 0; s < n; s++)
            {
                float l = b.L[s] * g, r = b.R[s] * g;
                dl[s] += l * (1f - ch * 0.5f); dr[s] += r * (1f - ch * 0.5f);
                _revInL[s] += l * rv; _revInR[s] += r * rv;
                _choInL[s] += l * ch; _choInR[s] += r * ch;
            }
        }
        _chorus.Process(_choInL, _choInR, _dryL, _dryR, n, ChorusDepth.Block(n));
        // The chorused signal reaches the hall too.
        for (int s = 0; s < n; s++) { _revInL[s] += _choInL[s] * 0.3f; _revInR[s] += _choInR[s] * 0.3f; }
        Array.Clear(_choInL, 0, n); Array.Clear(_choInR, 0, n);
        _reverb.Process(_revInL, _revInR, _choInL, _choInR, n);
        for (int s = 0; s < n; s++)
        {
            _dryEnergy += _dryL[s] * _dryL[s] + _dryR[s] * _dryR[s];
            float wl = _choInL[s] * ReverbReturn, wr = _choInR[s] * ReverbReturn;
            _wetEnergy += wl * wl + wr * wr;
            _dryL[s] = (_dryL[s] + _choInL[s] * ReverbReturn + _sfxL[s]) * PreGain;
            _dryR[s] = (_dryR[s] + _choInR[s] * ReverbReturn + _sfxR[s]) * PreGain;
        }
        _tape.Process(_dryL, _dryR, n);
        _limiter.Process(_dryL, _dryR, n);
        Sample += n;
    }
}
