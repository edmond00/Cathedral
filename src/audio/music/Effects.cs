using System;

namespace Cathedral.Audio.Music;

/// <summary>
/// The hall. An eight-line feedback delay network with a Hadamard mix, a low-pass in every loop so
/// the tail darkens as it decays (stone, not glass), slowly modulated line lengths so long tails do
/// not ring metallic, and a diffusing input. Decay and darkness are set per mood: a tavern is a
/// room, a cathedral or a cave holds a note for eight seconds.
/// </summary>
internal sealed class Reverb
{
    private static readonly float[] BaseMs = { 31.7f, 37.9f, 41.3f, 47.1f, 53.9f, 61.3f, 69.7f, 79.9f };
    private const int N = 8;
    private readonly DelayLine[] _lines = new DelayLine[N];
    private readonly float[] _len = new float[N];
    private readonly float[] _g = new float[N];
    private readonly float[] _z = new float[N];
    private readonly float[] _x = new float[N];
    private readonly float[] _modPh = new float[N];
    private readonly DelayLine _pre;
    private readonly Allpass[] _diffL, _diffR;
    private float _damp;
    private float _preDelay;
    private Noise _rng = new(7);

    private struct Allpass
    {
        public DelayLine Line;
        public int Len;
        public float G;

        public float Process(float x)
        {
            float d = Line.ReadInt(Len);
            float y = -G * x + d;
            Line.Write(x + G * y);
            return y;
        }
    }

    public Reverb(float size = 1.3f)
    {
        for (int i = 0; i < N; i++)
        {
            _len[i] = BaseMs[i] * size * Dsp.SampleRate / 1000f;
            _lines[i] = new DelayLine((int)(_len[i] + 64));
            _modPh[i] = i * 0.77f;
        }
        _pre = new DelayLine(Dsp.SampleRate / 5);
        _diffL = MakeDiffusers(new[] { 4.7f, 3.6f, 12.7f, 9.3f });
        _diffR = MakeDiffusers(new[] { 5.1f, 3.3f, 11.9f, 9.9f });
        Set(4f, 3500f, 0.03f);
    }

    private static Allpass[] MakeDiffusers(float[] ms)
    {
        var a = new Allpass[ms.Length];
        for (int i = 0; i < ms.Length; i++)
        {
            int len = (int)(ms[i] * Dsp.SampleRate / 1000f);
            a[i] = new Allpass { Line = new DelayLine(len + 2), Len = len, G = 0.6f };
        }
        return a;
    }

    /// <summary>Decay to -60 dB in <paramref name="t60"/> seconds; the loop's high cut; pre-delay.</summary>
    public void Set(float t60, float dampHz, float preDelaySec)
    {
        for (int i = 0; i < N; i++)
            _g[i] = MathF.Pow(10f, -3f * _len[i] / (Math.Max(0.3f, t60) * Dsp.SampleRate));
        _damp = MathF.Exp(-Dsp.TwoPi * dampHz / Dsp.SampleRate);
        _preDelay = Math.Clamp(preDelaySec * Dsp.SampleRate, 1f, _pre.Capacity - 2);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public void Process(float[] inL, float[] inR, float[] outL, float[] outR, int n)
    {
        float modRate = 0.31f * Dsp.TwoPi / Dsp.SampleRate;
        for (int s = 0; s < n; s++)
        {
            float mono = (inL[s] + inR[s]) * 0.5f;
            _pre.Write(mono);
            float x = _pre.Read(_preDelay);
            float dl = x, dr = x;
            for (int k = 0; k < _diffL.Length; k++) { dl = _diffL[k].Process(dl); dr = _diffR[k].Process(dr); }

            // Read the lines (two of them gently modulated), damp, mix.
            for (int i = 0; i < N; i++)
            {
                float len = _len[i];
                if (i < 4)
                {
                    _modPh[i] += modRate * (1f + i * 0.13f);
                    if (_modPh[i] > Dsp.TwoPi) _modPh[i] -= Dsp.TwoPi;
                    len += MathF.Sin(_modPh[i]) * 6f;
                }
                float y = _lines[i].Read(len);
                _z[i] = y + (_z[i] - y) * _damp;
                _x[i] = _z[i] * _g[i];
            }
            Hadamard8(_x);
            for (int i = 0; i < N; i++)
                _lines[i].Write(_x[i] + ((i & 1) == 0 ? dl : dr) * 0.35f);
            outL[s] += (_z[0] - _z[2] + _z[4] - _z[6] + _z[1] * 0.5f) * 0.45f;
            outR[s] += (_z[1] - _z[3] + _z[5] - _z[7] + _z[2] * 0.5f) * 0.45f;
        }
    }

    private static void Hadamard8(float[] a)
    {
        for (int h = 1; h < 8; h <<= 1)
            for (int i = 0; i < 8; i += h << 1)
                for (int j = i; j < i + h; j++)
                {
                    float x = a[j], y = a[j + h];
                    a[j] = x + y; a[j + h] = x - y;
                }
        const float norm = 0.35355339f;   // 1/sqrt(8): the mix is lossless, the loop gains do the decay
        for (int i = 0; i < 8; i++) a[i] *= norm;
    }
}

/// <summary>
/// Ensemble: three modulated taps per side, the string machine's and the cheap pad's shimmer.
/// </summary>
internal sealed class Chorus
{
    private readonly DelayLine _l = new(Dsp.SampleRate / 20), _r = new(Dsp.SampleRate / 20);
    private float _ph;

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public void Process(float[] inL, float[] inR, float[] outL, float[] outR, int n, float depth)
    {
        float rate = 0.6f * Dsp.TwoPi / Dsp.SampleRate;
        float baseD = 0.012f * Dsp.SampleRate, mod = 0.0035f * Dsp.SampleRate * depth;
        for (int i = 0; i < n; i++)
        {
            _l.Write(inL[i]);
            _r.Write(inR[i]);
            _ph += rate; if (_ph > Dsp.TwoPi) _ph -= Dsp.TwoPi;
            float a = MathF.Sin(_ph), b = MathF.Sin(_ph + 2.094f), c = MathF.Sin(_ph + 4.189f);
            outL[i] += (_l.Read(baseD + mod * a) + _l.Read(baseD * 1.3f + mod * b)) * 0.5f;
            outR[i] += (_r.Read(baseD * 1.1f + mod * c) + _r.Read(baseD * 1.4f + mod * a)) * 0.5f;
        }
    }
}

/// <summary>
/// The cassette: wow and flutter (a slow and a fast wobble of the playback speed, plus a little
/// drift), soft saturation, a gentle head bump, highs rolled off, and hiss. This is the patina the
/// genre's tapes have, and the single most "retro" thing in the chain.
/// </summary>
internal sealed class Tape
{
    private readonly DelayLine _l = new(Dsp.SampleRate / 10), _r = new(Dsp.SampleRate / 10);
    private float _wow, _flutter, _driftV, _driftT;
    private int _driftCount;
    private Svf _lpL1, _lpR1, _bumpL, _bumpR;
    private OnePole _lpL2, _lpR2, _hpL, _hpR, _hissLp;
    private Noise _rng;
    private float _pink;

    public float WowCents = 4f;       // depth of the slow wobble
    public float FlutterCents = 1.2f;
    public float Drive = 1.4f;
    public float HissDb = -60f;
    public float CutoffHz = 7000f;

    public Tape(int seed)
    {
        _rng = new Noise(seed ^ 0x5eed);
        SetCutoff(CutoffHz);
        _bumpL.Set(110f, 1.2f);
        _bumpR.Set(110f, 1.2f);
        _hpL.Set(28f);
        _hpR.Set(28f);
        _hissLp.Set(6000f);
    }

    public void SetCutoff(float hz)
    {
        CutoffHz = hz;
        _lpL1.Set(hz, 0.6f);
        _lpR1.Set(hz, 0.6f);
        _lpL2.Set(hz * 1.4f);
        _lpR2.Set(hz * 1.4f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public void Process(float[] l, float[] r, int n)
    {
        // A delay modulated by d(t) = D sin(wt) shifts pitch by D*w; pick D for the depth in cents.
        float wowRate = 0.45f, flutRate = 6.3f;
        float wowD = (MathF.Pow(2f, WowCents / 1200f) - 1f) / (Dsp.TwoPi * wowRate) * Dsp.SampleRate;
        float flutD = (MathF.Pow(2f, FlutterCents / 1200f) - 1f) / (Dsp.TwoPi * flutRate) * Dsp.SampleRate;
        float hiss = Dsp.DbToGain(HissDb) * 4f;
        float drive = Drive, inv = 1f / MathF.Tanh(drive);
        float centre = wowD + flutD + 8f;
        for (int i = 0; i < n; i++)
        {
            _l.Write(l[i]);
            _r.Write(r[i]);
            _wow += wowRate * Dsp.TwoPi / Dsp.SampleRate; if (_wow > Dsp.TwoPi) _wow -= Dsp.TwoPi;
            _flutter += flutRate * Dsp.TwoPi / Dsp.SampleRate; if (_flutter > Dsp.TwoPi) _flutter -= Dsp.TwoPi;
            if (--_driftCount <= 0) { _driftT = _rng.Next() * wowD * 0.6f; _driftCount = Dsp.SampleRate; }
            _driftV += (_driftT - _driftV) * 0.00003f;
            float d = centre + MathF.Sin(_wow) * wowD + MathF.Sin(_flutter) * flutD + _driftV;
            float a = _l.Read(d), b = _r.Read(d + 0.7f);

            // Saturation, normalised so small signals pass at unity.
            a = MathF.Tanh(a * drive) * inv;
            b = MathF.Tanh(b * drive) * inv;
            a += _bumpL.Band(a) * 0.25f;
            b += _bumpR.Band(b) * 0.25f;
            a = _hpL.High(_lpL2.Low(_lpL1.Low(a)));
            b = _hpR.High(_lpR2.Low(_lpR1.Low(b)));

            // Hiss: white leaned towards pink, low-passed like everything else on the tape.
            float w = _rng.Next();
            _pink = _pink * 0.97f + w * 0.03f;
            float h = _hissLp.Low(_pink * 3f + w * 0.15f) * hiss;
            l[i] = a + h;
            r[i] = b + h * 0.9f;
        }
    }
}

/// <summary>A peak limiter with 3 ms of look-ahead: never clips, rarely heard.</summary>
internal sealed class Limiter
{
    private readonly DelayLine _l = new(256), _r = new(256);
    private readonly int _look = (int)(0.003f * Dsp.SampleRate);
    private float _gain = 1f, _env;
    public float Ceiling = Dsp.DbToGain(-1f);

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public void Process(float[] l, float[] r, int n)
    {
        float rel = Dsp.Coef(0.25f), att = Dsp.Coef(0.002f);
        for (int i = 0; i < n; i++)
        {
            _l.Write(l[i]); _r.Write(r[i]);
            float peak = MathF.Max(MathF.Abs(l[i]), MathF.Abs(r[i]));
            _env = peak > _env ? peak : _env * rel + peak * (1 - rel);
            float want = _env > Ceiling ? Ceiling / _env : 1f;
            _gain = want < _gain ? want + (_gain - want) * att : want + (_gain - want) * rel;
            float a = _l.ReadInt(_look) * _gain, b = _r.ReadInt(_look) * _gain;
            l[i] = Math.Clamp(a, -Ceiling, Ceiling);
            r[i] = Math.Clamp(b, -Ceiling, Ceiling);
        }
    }
}
