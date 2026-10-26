using System;

namespace Cathedral.Audio.Music;

/// <summary>
/// The sample rate everything in the music synthesiser runs at. 32 kHz on purpose: the samplers and
/// ROMplers this music imitates ran at 32 kHz or less, nothing in the mix should live above 8 kHz
/// anyway, and it is a quarter less work than 44.1 kHz for the same voices. OpenAL and WinMM both
/// resample to the device on the way out.
/// </summary>
internal static class Dsp
{
    public const int SampleRate = 32000;
    public const float Nyquist = SampleRate / 2f;
    public const float TwoPi = MathF.PI * 2f;

    public static float MidiToHz(float midi) => 440f * MathF.Pow(2f, (midi - 69f) / 12f);

    public static float DbToGain(float db) => MathF.Pow(10f, db / 20f);

    /// <summary>One-pole smoothing coefficient for a time constant in seconds.</summary>
    public static float Coef(float seconds) =>
        seconds <= 0 ? 0f : MathF.Exp(-1f / (seconds * SampleRate));
}

/// <summary>
/// A small seeded generator for the synthesiser's own noise: breath, plucks, hiss, drift. Not
/// <see cref="System.Random"/>: this runs per sample, and it is seeded from the composer's seed so a
/// render with a given <c>--seed</c> is the same render, sample for sample.
/// </summary>
internal struct Noise
{
    private uint _s;

    public Noise(int seed) => _s = (uint)seed * 2654435761u | 1u;

    public uint NextUInt()
    {
        _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5;
        return _s;
    }

    /// <summary>White, -1..1.</summary>
    public float Next() => (NextUInt() >> 8) * (2f / 16777216f) - 1f;

    /// <summary>0..1.</summary>
    public float Unit() => (NextUInt() >> 8) * (1f / 16777216f);
}

/// <summary>
/// Band-limited single-cycle tables, one per octave of harmonic content, so a saw is a saw at A1 and
/// still does not alias at A6. Built once from a harmonic recipe and shared by every voice.
/// </summary>
internal sealed class Wavetable
{
    public const int Size = 2048;
    private const int Mask = Size - 1;
    private readonly float[][] _levels;     // _levels[k] holds harmonics up to 2^k
    private readonly int _maxHarmonic;

    public Wavetable(Func<int, float> amplitude, int maxHarmonic = 256, Func<int, float>? phase = null)
    {
        _maxHarmonic = maxHarmonic;
        int nLevels = (int)Math.Ceiling(Math.Log2(maxHarmonic)) + 1;
        _levels = new float[nLevels][];
        float peak = 0f;
        for (int k = 0; k < nLevels; k++)
        {
            int hMax = Math.Min(maxHarmonic, 1 << k);
            var t = new float[Size + 1];
            for (int h = 1; h <= hMax; h++)
            {
                float a = amplitude(h);
                if (a == 0f) continue;
                float ph = phase?.Invoke(h) ?? 0f;
                for (int i = 0; i < Size; i++)
                    t[i] += a * MathF.Sin(Dsp.TwoPi * h * i / Size + ph);
            }
            t[Size] = t[0];
            _levels[k] = t;
            if (k == nLevels - 1)
                for (int i = 0; i < Size; i++) peak = MathF.Max(peak, MathF.Abs(t[i]));
        }
        // Normalise every level by the fullest one, so a note does not get louder as it rises.
        if (peak > 0)
            foreach (var t in _levels)
                for (int i = 0; i <= Size; i++) t[i] /= peak;
    }

    /// <summary>The table for a fundamental of <paramref name="hz"/>: as many harmonics as fit under 0.45 sr.</summary>
    public float[] For(float hz)
    {
        int fit = (int)(Dsp.SampleRate * 0.45f / Math.Max(hz, 1f));
        int k = 0;
        while (k + 1 < _levels.Length && (1 << (k + 1)) <= fit) k++;
        return _levels[k];
    }

    public static float Read(float[] table, float phase)
    {
        float x = phase * Size;
        int i = (int)x;
        float f = x - i;
        i &= Mask;
        return table[i] + (table[i + 1] - table[i]) * f;
    }

    // The shared recipes.
    public static readonly Wavetable Saw = new(h => 1f / h);
    public static readonly Wavetable Square = new(h => (h & 1) == 1 ? 1f / h : 0f);
    public static readonly Wavetable Sine = new(h => h == 1 ? 1f : 0f, 1);
    /// <summary>A saw with its top rolled off: the soft string-machine wave.</summary>
    public static readonly Wavetable SoftSaw = new(h => 1f / h * MathF.Exp(-h / 24f));
    /// <summary>A narrow pulse, 25% duty: nasal, reedy, the cheap keyboard's "oboe" and "horn".</summary>
    public static readonly Wavetable Pulse25 = new(h => MathF.Sin(MathF.PI * h * 0.25f) / h);
}

/// <summary>Zavalishin's topology-preserving state-variable filter. Low, band and high from one pass.</summary>
internal struct Svf
{
    private float _ic1, _ic2, _g, _k, _a1, _a2, _a3;

    public void Set(float cutoffHz, float q)
    {
        cutoffHz = Math.Clamp(cutoffHz, 20f, Dsp.Nyquist * 0.92f);
        _g = MathF.Tan(MathF.PI * cutoffHz / Dsp.SampleRate);
        _k = 1f / Math.Max(q, 0.3f);
        _a1 = 1f / (1f + _g * (_g + _k));
        _a2 = _g * _a1;
        _a3 = _g * _a2;
    }

    /// <summary>Processes one sample; returns low-pass and puts band-pass in <paramref name="band"/>.</summary>
    public float Process(float x, out float band)
    {
        float v3 = x - _ic2;
        float v1 = _a1 * _ic1 + _a2 * v3;
        float v2 = _ic2 + _a2 * _ic1 + _a3 * v3;
        _ic1 = 2 * v1 - _ic1;
        _ic2 = 2 * v2 - _ic2;
        band = v1;
        return v2;
    }

    public float Low(float x) => Process(x, out _);

    public float Band(float x) { Process(x, out float b); return b; }

    public float High(float x)
    {
        float lp = Process(x, out float bp);
        return x - _k * bp - lp;
    }

    public void Reset() { _ic1 = _ic2 = 0; }
}

/// <summary>One-pole low-pass: the cheapest darkening there is.</summary>
internal struct OnePole
{
    private float _z, _a;

    public void Set(float cutoffHz) => _a = MathF.Exp(-Dsp.TwoPi * Math.Clamp(cutoffHz, 1f, Dsp.Nyquist) / Dsp.SampleRate);

    public float Low(float x) => _z = x + (_z - x) * _a;

    public float High(float x) => x - Low(x);
}

/// <summary>
/// Attack, decay, sustain, release. The attack is linear, the rest exponential, which is how analog
/// and early digital envelopes sound: a swell, then a natural fall.
/// </summary>
internal struct Envelope
{
    public float Attack, Decay, Sustain, Release;   // seconds, seconds, level, seconds
    private float _level, _attInc, _decCoef, _relCoef;
    private int _stage;                             // 0 attack, 1 decay/sustain, 2 release, 3 done

    public Envelope(float a, float d, float s, float r)
    {
        Attack = a; Decay = d; Sustain = s; Release = r;
        _level = 0; _stage = 0;
        _attInc = 1f / Math.Max(1f, a * Dsp.SampleRate);
        _decCoef = Dsp.Coef(d / 3f);
        _relCoef = Dsp.Coef(r / 4.6f);   // -40 dB by the release time
    }

    public bool Done => _stage == 3;
    public bool Releasing => _stage >= 2;
    public float Level => _level;

    public void NoteOff() { if (_stage < 2) _stage = 2; }

    /// <summary>Releases at a different rate: a damper falling, a voice stolen.</summary>
    public void Kill(float seconds) { _relCoef = Dsp.Coef(seconds / 4.6f); _stage = 2; }

    public float Next()
    {
        switch (_stage)
        {
            case 0:
                _level += _attInc;
                if (_level >= 1f) { _level = 1f; _stage = 1; }
                break;
            case 1:
                _level = Sustain + (_level - Sustain) * _decCoef;
                break;
            case 2:
                _level *= _relCoef;
                if (_level < 1e-4f) { _level = 0; _stage = 3; }
                break;
        }
        return _level;
    }
}

/// <summary>A delay line with fractional, interpolated reads: chorus, wow and flutter, the reverb.</summary>
internal sealed class DelayLine
{
    private readonly float[] _buf;
    private int _w;

    public DelayLine(int maxSamples) => _buf = new float[maxSamples + 2];

    public int Capacity => _buf.Length - 2;

    public void Write(float x)
    {
        _buf[_w] = x;
        if (++_w == _buf.Length) _w = 0;
    }

    /// <summary>The sample written <paramref name="delay"/> samples ago (at least 1).</summary>
    public float Read(float delay)
    {
        float r = _w - delay;
        while (r < 0) r += _buf.Length;
        int i = (int)r;
        float f = r - i;
        if (i >= _buf.Length) i -= _buf.Length;   // r rounded up to the length itself
        int j = i + 1 == _buf.Length ? 0 : i + 1;
        return _buf[i] + (_buf[j] - _buf[i]) * f;
    }

    public float ReadInt(int delay)
    {
        int r = _w - delay;
        if (r < 0) r += _buf.Length;
        return _buf[r];
    }

    public void Clear() => Array.Clear(_buf);
}

/// <summary>A value that glides to its target instead of jumping: gains, cutoffs, sends.</summary>
internal struct Smoothed
{
    public float Value, Target;
    private float _coef;

    public Smoothed(float v, float seconds) { Value = Target = v; _coef = Dsp.Coef(seconds); }

    public void SetTime(float seconds) => _coef = Dsp.Coef(seconds);

    public float Next() => Value = Target + (Value - Target) * _coef;

    /// <summary>Advances by a whole block at once: for parameters read once per block.</summary>
    public float Block(int n)
    {
        Value = Target + (Value - Target) * MathF.Pow(_coef, n);
        return Value;
    }
}
