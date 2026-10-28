using System;

namespace Cathedral.Audio.Music;

/// <summary>
/// Every sound the music is made of. All of them are synthesised here: no samples, no SoundFont,
/// nothing to licence. Each imitates the preset a 1990s keyboard or ROMpler would have offered under
/// that name, not the real instrument: the genre was made on those keyboards, and that is the sound.
/// </summary>
public enum Instrument
{
    /// <summary>Pipe organ, principal chorus: 8' 4' 2 2/3' 2'. The cathedral.</summary>
    Organ,
    /// <summary>Stopped flute pipes, 8' alone: the small positive organ, soft and hollow.</summary>
    OrganFlute,
    /// <summary>
    /// Choir "aah": six singers in two sections, each with a breathy glottal source, their own tuning,
    /// vibrato and entry, through the sections' own vocal tracts. See <see cref="ChoirVoice"/>.
    /// </summary>
    Choir,
    /// <summary>Choir "ooh": the same choir, darker vowel, softer.</summary>
    ChoirOoh,
    /// <summary>String machine: detuned soft saws, low-passed, slow swell. The 80s string pad.</summary>
    Strings,
    /// <summary>Dark sweeping pad: saws and a sub square under a slowly breathing resonant filter.</summary>
    DarkPad,
    /// <summary>Recorder: a near-sine with breath and a delayed vibrato.</summary>
    Recorder,
    /// <summary>Plucked harpsichord: two 8' strings and a 4', Karplus-Strong.</summary>
    Harpsichord,
    /// <summary>Harp: one soft, long-ringing plucked string.</summary>
    Harp,
    /// <summary>Lute: a plucked string damped sooner, warmer than the harpsichord.</summary>
    Lute,
    /// <summary>Bell: church-bell partials, minor-third tierce and all.</summary>
    Bell,
    /// <summary>Timpani: pitched membrane modes and a felt thump.</summary>
    Timpani,
    /// <summary>Hand drum: a short, low frame-drum thud for the tavern.</summary>
    HandDrum,
    /// <summary>Bourdon: the drone, two saws and a sub, very slow, very dark.</summary>
    Bourdon,
    /// <summary>Horn: brassy saws whose filter opens with the attack. The "brass" preset of the genre.</summary>
    Horn,
    /// <summary>Square lead: the chip-like retro voice, for stings.</summary>
    SquareLead,
    /// <summary>Wind: band-passed noise drifting around a pitch. Air in a ruin.</summary>
    Wind,
    /// <summary>Fiddle: one bowed saw through a wooden body's resonances, bow noise and a late vibrato.</summary>
    Fiddle,
    /// <summary>Tin whistle: a breathy near-sine with a chiff at each note, sounding an octave above the line.</summary>
    Whistle,
    /// <summary>Pipes: a reedy chanter, nasal and steady, no vibrato; also the festive drone.</summary>
    Pipes,
    /// <summary>Bodhrán: the frame drum of the session, a low skin, a felt thump and the tipper's click.</summary>
    Bodhran,
    /// <summary>
    /// Distorted guitar, one string: two saws a few cents apart (a double-tracked part) into a hard
    /// clipper and a small cabinet. Struck below velocity 0.55 it is palm-muted: darker, clipped short.
    /// The fight's riff. A keyboard's "distortion guitar" preset, not a guitar.
    /// </summary>
    Distortion,
    /// <summary>The same distorted guitar on a power chord: the note, its fifth and its octave.</summary>
    PowerChord,
    /// <summary>Kick drum: a sine falling from a punch to a low thud, and the beater's click.</summary>
    Kick,
    /// <summary>Snare: two drumhead modes under a burst of band-passed wire noise.</summary>
    Snare,
    /// <summary>
    /// Cymbal: six detuned squares, as the drum machines made them, through a high-pass. Struck at
    /// velocity 0.5 and over it is a crash ringing for seconds; under it a closed hi-hat's tick.
    /// </summary>
    Cymbal,
    /// <summary>
    /// Staccato strings: the keyboard preset, a bowed section bitten short. Detuned saws and a sub an
    /// octave down, a filter that opens on the attack and closes at once. The fight's ostinato.
    /// </summary>
    Staccato,
    /// <summary>War drum: a deep, wide membrane struck hard, its pitch settling as it rings, and the slap of the beater.</summary>
    WarDrum,
    /// <summary>Metallophone: struck metal bars, inharmonic partials, each note two bars a few hertz apart so they beat.</summary>
    Metallophone,
    /// <summary>Buzzing string: a plucked string whose bridge rattles against it, a nasal sustained buzz; it can bend.</summary>
    BuzzString,
    /// <summary>Reed: a warm double reed, breathy, a slow late vibrato, sliding into its notes.</summary>
    Reed,
    /// <summary>Glass: bowed glass or metal, near-sine partials slowly beating, no attack to speak of.</summary>
    Glass,
    /// <summary>Bent drum: a hand drum whose skin is pressed as it rings, so its pitch rises after the stroke.</summary>
    BentDrum,
}

/// <summary>One sounding note. Renders mono; the synthesiser pans it into its part's bus.</summary>
internal abstract class Voice
{
    public int Part;
    public float Pan;
    public long GateEnd;           // sample at which the key is released
    public long StartSample;
    public float Midi;
    protected float Amp;
    protected Envelope Env;
    protected Noise Rng;

    public bool Done => Env.Done;
    public bool Releasing => Env.Releasing;
    public float Level => Env.Level;

    /// <summary>
    /// Brings each instrument to a common level: a velocity-0.7 phrase on any of them measures about
    /// the same (sustained voices -16 dB, plucked and struck ones a little under, their attacks carry).
    /// Measured with --music-audition; see tools/music/levels.py. The mix's balance is then
    /// <see cref="Orchestra.Trim"/> and the part levels, which are taste, not correction.
    /// </summary>
    public float Level0 = 1f;

    /// <summary>Semitones the note starts away from its pitch and slides onto it; read by the voices that can bend.</summary>
    public float Glide;

    public void Init(float midi, float velocity, int seed)
    {
        Midi = midi;
        Rng = new Noise(seed);
        Amp = MathF.Pow(Math.Clamp(velocity, 0f, 1f), 1.5f) * Level0;
        Start(Dsp.MidiToHz(midi), velocity);
    }

    protected abstract void Start(float hz, float velocity);

    public virtual void NoteOff() => Env.NoteOff();

    public void Kill(float seconds) => Env.Kill(seconds);

    /// <summary>Adds <paramref name="n"/> samples into <paramref name="buf"/>.</summary>
    public abstract void Render(float[] buf, int n);

    public static Voice Create(Instrument inst)
    {
        var v = Make(inst);
        v.Level0 = Levels.TryGetValue(inst, out float g) ? g : 1f;
        return v;
    }

    /// <summary>Level corrections from the audition, dB converted. Regenerate with tools/music/levels.py.</summary>
    internal static readonly System.Collections.Generic.Dictionary<Instrument, float> Levels = new()
    {
        // LEVELS-BEGIN
        [Instrument.Bell] = 2.214f,
        [Instrument.BentDrum] = 2.791f,
        [Instrument.Bodhran] = 4.035f,
        [Instrument.Bourdon] = 2.830f,
        [Instrument.BuzzString] = 6.216f,
        [Instrument.Choir] = 5.179f,
        [Instrument.ChoirOoh] = 2.465f,
        [Instrument.Cymbal] = 13.148f,
        [Instrument.DarkPad] = 3.185f,
        [Instrument.Distortion] = 2.338f,
        [Instrument.Fiddle] = 3.632f,
        [Instrument.Glass] = 2.537f,
        [Instrument.HandDrum] = 3.745f,
        [Instrument.Harp] = 4.379f,
        [Instrument.Harpsichord] = 2.913f,
        [Instrument.Horn] = 2.373f,
        [Instrument.Kick] = 1.191f,
        [Instrument.Lute] = 4.582f,
        [Instrument.Metallophone] = 1.850f,
        [Instrument.Organ] = 1.314f,
        [Instrument.OrganFlute] = 0.970f,
        [Instrument.Pipes] = 10.502f,
        [Instrument.PowerChord] = 3.180f,
        [Instrument.Recorder] = 0.994f,
        [Instrument.Reed] = 10.924f,
        [Instrument.Snare] = 3.265f,
        [Instrument.SquareLead] = 3.268f,
        [Instrument.Staccato] = 5.722f,
        [Instrument.Strings] = 1.604f,
        [Instrument.Timpani] = 1.346f,
        [Instrument.WarDrum] = 2.086f,
        [Instrument.Whistle] = 0.987f,
        [Instrument.Wind] = 19.846f,        // LEVELS-END
    };

    private static Voice Make(Instrument inst) => inst switch
    {
        Instrument.Organ       => new OrganVoice(OrganVoice.Principal, chiff: 0.10f),
        Instrument.OrganFlute  => new OrganVoice(OrganVoice.Gedackt, chiff: 0.18f),
        Instrument.Choir       => new ChoirVoice(ooh: false),
        Instrument.ChoirOoh    => new ChoirVoice(ooh: true),
        Instrument.Strings     => new StringsVoice(),
        Instrument.DarkPad     => new DarkPadVoice(),
        Instrument.Recorder    => new RecorderVoice(),
        Instrument.Harpsichord => new PluckVoice(PluckVoice.Kind.Harpsichord),
        Instrument.Harp        => new PluckVoice(PluckVoice.Kind.Harp),
        Instrument.Lute        => new PluckVoice(PluckVoice.Kind.Lute),
        Instrument.Bell        => new BellVoice(),
        Instrument.Timpani     => new DrumVoice(hand: false),
        Instrument.HandDrum    => new DrumVoice(hand: true),
        Instrument.Bourdon     => new BourdonVoice(),
        Instrument.Horn        => new HornVoice(),
        Instrument.SquareLead  => new SquareVoice(),
        Instrument.Wind        => new WindVoice(),
        Instrument.Fiddle      => new FiddleVoice(),
        Instrument.Whistle     => new WhistleVoice(),
        Instrument.Pipes       => new PipesVoice(),
        Instrument.Bodhran     => new BodhranVoice(),
        Instrument.Distortion  => new DistortionVoice(power: false),
        Instrument.PowerChord  => new DistortionVoice(power: true),
        Instrument.Kick        => new KickVoice(),
        Instrument.Snare       => new SnareVoice(),
        Instrument.Cymbal      => new CymbalVoice(),
        Instrument.Staccato    => new StaccatoVoice(),
        Instrument.WarDrum     => new WarDrumVoice(),
        Instrument.Metallophone => new MetallophoneVoice(),
        Instrument.BuzzString  => new BuzzStringVoice(),
        Instrument.Reed        => new ReedVoice(),
        Instrument.Glass       => new GlassVoice(),
        Instrument.BentDrum    => new BentDrumVoice(),
        _ => new OrganVoice(OrganVoice.Principal, 0.1f),
    };
}

/// <summary>A slowly wandering pitch offset in cents: no two held notes stay exactly in tune.</summary>
internal struct Drift
{
    private float _v, _target;
    private int _count;

    public float Next(ref Noise rng, float cents)
    {
        if (--_count <= 0) { _target = rng.Next() * cents; _count = Dsp.SampleRate / 3; }
        _v += (_target - _v) * 0.00005f;
        return _v;
    }
}

internal sealed class OrganVoice : Voice
{
    public static readonly Wavetable Principal = new(h => h switch
    {
        1 => 1f, 2 => 0.55f, 3 => 0.28f, 4 => 0.33f, 5 => 0.06f, 6 => 0.14f, 8 => 0.12f, 10 => 0.04f, 12 => 0.05f, 16 => 0.03f, _ => 0f,
    }, 16);
    public static readonly Wavetable Gedackt = new(h => h switch { 1 => 1f, 2 => 0.04f, 3 => 0.16f, 5 => 0.03f, _ => 0f }, 8);

    private readonly Wavetable _wt;
    private readonly float _chiffAmt;
    private float[] _t = Array.Empty<float>();
    private float _p1, _p2, _inc1, _inc2, _chiff;
    private Svf _chiffBp;
    private float _wobble;

    public OrganVoice(Wavetable wt, float chiff) { _wt = wt; _chiffAmt = chiff; }

    protected override void Start(float hz, float velocity)
    {
        _t = _wt.For(hz * 2f);
        _inc1 = hz * MathF.Pow(2f, 2.5f / 1200f) / Dsp.SampleRate;
        _inc2 = hz * MathF.Pow(2f, -2.5f / 1200f) / Dsp.SampleRate;
        _p2 = Rng.Unit();
        Env = new Envelope(0.035f, 0.12f, 0.9f, 0.22f);
        _chiffBp.Set(Math.Min(hz * 3f, 7000f), 4f);
        _chiff = _chiffAmt;
        _wobble = Rng.Unit() * Dsp.TwoPi;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public override void Render(float[] buf, int n)
    {
        float chiffDecay = Dsp.Coef(0.035f);
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            float s = Wavetable.Read(_t, _p1) + Wavetable.Read(_t, _p2);
            _p1 += _inc1; if (_p1 >= 1f) _p1 -= 1f;
            _p2 += _inc2; if (_p2 >= 1f) _p2 -= 1f;
            float c = _chiffBp.Band(Rng.Next()) * _chiff;
            _chiff *= chiffDecay;
            buf[i] += (s * 0.5f * e + c) * Amp;
        }
    }
}

/// <summary>
/// A choir: six singers in two sections, not one voice with a chorus. Each singer has a glottal
/// source (falling faster than a saw), its own tuning, wander, vibrato and loudness, a scoop up
/// into the note and a staggered entry; each section has its own vocal tract (four formants, the
/// second a little shorter), and the vowel drifts as a held vowel does. Breath, pulsing with the
/// voice's own cycle and strongest at the onset, goes through the same tract, so it is the
/// vowel's breath. "Ooh" is the closed vowel, darker and softer.
/// </summary>
internal sealed class ChoirVoice : Voice
{
    private const int Singers = 6;
    private static readonly Wavetable Glottal = new(h => MathF.Pow(h, -1.3f));
    private readonly bool _ooh;
    private float[] _t = Array.Empty<float>();
    private readonly float[] _ph = new float[Singers], _inc = new float[Singers], _vibPh = new float[Singers],
        _vibRate = new float[Singers], _vibDepth = new float[Singers], _vibOnset = new float[Singers],
        _entry = new float[Singers], _scoop = new float[Singers], _amp = new float[Singers], _ampTarget = new float[Singers];
    private readonly Drift[] _drift = new Drift[Singers];
    private readonly Svf[] _ta = new Svf[4], _tb = new Svf[4];
    private Svf _chest, _air;
    private float _chestGain;
    private readonly float[] _fHz = new float[4], _q = new float[4], _g = new float[4];
    private readonly float[] _step = new float[Singers], _gain = new float[Singers];
    private float _age, _vowelPh, _vowelRate, _breath;
    private int _count;

    public ChoirVoice(bool ooh) => _ooh = ooh;

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    protected override void Start(float hz, float velocity)
    {
        _t = Glottal.For(hz);
        for (int k = 0; k < Singers; k++)
        {
            float cents = (Rng.Next() + Rng.Next()) * 7f;              // a singer's own tuning, about +-10 cents
            _inc[k] = hz * MathF.Pow(2f, cents / 1200f) / Dsp.SampleRate;
            _ph[k] = Rng.Unit();
            _vibPh[k] = Rng.Unit() * Dsp.TwoPi;
            _vibRate[k] = (4.6f + Rng.Unit() * 1.2f) * Dsp.TwoPi / Dsp.SampleRate;
            _vibDepth[k] = 22f + Rng.Unit() * 18f;
            _vibOnset[k] = 0.25f + Rng.Unit() * 0.3f;
            _entry[k] = k == 0 ? 0f : Rng.Unit() * 0.07f;
            _scoop[k] = -(18f + Rng.Unit() * 22f);
            _amp[k] = _ampTarget[k] = 0.8f + 0.2f * Rng.Unit();
        }
        // Formants blend from men's to women's voices across the range.
        float hi = Math.Clamp((Midi - 52f) / 24f, 0f, 1f);
        if (_ooh)
        {
            // Sung "ooh" sits nearer "oh" than the spoken vowel: a choir opens it to carry.
            _fHz[0] = Lerp(360, 420, hi); _fHz[1] = Lerp(880, 1050, hi); _fHz[2] = Lerp(2400, 2700, hi); _fHz[3] = Lerp(3100, 3400, hi);
            _q[0] = 3.5f; _q[1] = 4.5f; _q[2] = 8f; _q[3] = 9f;
            _g[0] = 0.9f; _g[1] = 0.55f; _g[2] = 0.3f; _g[3] = 0.15f;
            _chestGain = 0.45f;
        }
        else
        {
            _fHz[0] = Lerp(650, 820, hi); _fHz[1] = Lerp(1080, 1200, hi); _fHz[2] = Lerp(2600, 2900, hi); _fHz[3] = Lerp(3200, 3500, hi);
            _q[0] = 4.5f; _q[1] = 6.5f; _q[2] = 10f; _q[3] = 9f;
            _g[0] = 1.0f; _g[1] = 0.6f; _g[2] = 0.45f; _g[3] = 0.4f;
            _chestGain = 0.25f;
        }
        // The chest under the vowel, and the air above it.
        _chest.Set(Math.Max(hz * 1.2f, 160f), 0.8f);
        _air.Set(5000f, 0.7f);
        _vowelPh = Rng.Unit() * Dsp.TwoPi;
        _vowelRate = (0.15f + 0.15f * Rng.Unit()) * Dsp.TwoPi / Dsp.SampleRate * 32f;
        SetTracts();
        _breath = 1f;
        Env = _ooh ? new Envelope(0.3f, 0.4f, 0.88f, 1.0f) : new Envelope(0.22f, 0.4f, 0.88f, 0.9f);
    }

    private void SetTracts()
    {
        // The vowel drifts a few percent, the two sections in opposite directions; the second
        // section's tract is a little shorter.
        float w = 0.035f * MathF.Sin(_vowelPh);
        for (int f = 0; f < 4; f++)
        {
            _ta[f].Set(_fHz[f] * (1f + w), _q[f]);
            _tb[f].Set(_fHz[f] * 1.07f * (1f - w), _q[f]);
        }
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        // Pitch, loudness and vowel move at a control rate (every 16 samples, half a millisecond):
        // far finer than any vibrato, and it keeps six singers affordable.
        const int Ctl = 16;
        float cdt = (float)Ctl / Dsp.SampleRate;
        float sd = MathF.Pow(Dsp.Coef(0.07f), Ctl), bd = Dsp.Coef(0.12f), ad = 1f - MathF.Pow(Dsp.Coef(0.2f), Ctl);
        for (int i = 0; i < n; i++)
        {
            if (_count % Ctl == 0)
            {
                _age += cdt;
                for (int k = 0; k < Singers; k++)
                {
                    float sung = _age - _entry[k];
                    if (sung < 0) { _step[k] = 0; continue; }
                    float vib = MathF.Sin(_vibPh[k]) * _vibDepth[k] * Math.Clamp((sung - _vibOnset[k]) * 2.5f, 0f, 1f);
                    _vibPh[k] += _vibRate[k] * Ctl; if (_vibPh[k] > Dsp.TwoPi) _vibPh[k] -= Dsp.TwoPi;
                    float cents = vib + _scoop[k] + _drift[k].Next(ref Rng, 9f);
                    _scoop[k] *= sd;
                    _amp[k] += (_ampTarget[k] - _amp[k]) * ad;
                    _step[k] = _inc[k] * (1f + cents / 1200f * 0.693f);
                    _gain[k] = _amp[k] * Math.Clamp(sung * 30f, 0f, 1f);
                }
                if (_count % (Ctl * 8) == 0)
                {
                    _vowelPh += _vowelRate * 4f; if (_vowelPh > Dsp.TwoPi) _vowelPh -= Dsp.TwoPi;
                    SetTracts();
                    // Each singer's loudness wanders: nobody holds a note perfectly even.
                    int k0 = (int)(Rng.Unit() * Singers) % Singers;
                    if (Rng.Unit() < 0.06f) _ampTarget[k0] = 0.72f + 0.28f * Rng.Unit();
                }
            }
            _count++;
            float e = Env.Next();
            float a = 0, b = 0;
            for (int k = 0; k < Singers; k++)
            {
                float g = Wavetable.Read(_t, _ph[k]) * _gain[k];
                _ph[k] += _step[k]; if (_ph[k] >= 1f) _ph[k] -= 1f;
                if (k < Singers / 2) a += g; else b += g;
            }
            // Breath, opening and closing with the first singer's glottis, strongest as the note begins.
            float puff = 1f - MathF.Abs(2f * _ph[0] - 1f);
            float breath = Rng.Next() * puff * (0.06f + 0.3f * _breath);
            _breath *= bd;
            a = a * (2f / Singers) + breath;
            b = b * (2f / Singers) + breath * 0.8f;
            float v = 0;
            for (int f = 0; f < 4; f++) v += (_ta[f].BandNorm(a) + _tb[f].BandNorm(b)) * _g[f];
            v += _chest.Low(a + b) * _chestGain + _air.High(breath) * 0.5f;
            buf[i] += v * 0.9f * e * Amp;
        }
    }
}

internal sealed class StringsVoice : Voice
{
    private float[] _t = Array.Empty<float>();
    private readonly float[] _ph = new float[3];
    private readonly float[] _inc = new float[3];
    private Svf _lp;
    private float _vib;
    private float _cut;
    private Drift _drift;

    protected override void Start(float hz, float velocity)
    {
        _t = Wavetable.SoftSaw.For(hz);
        float[] det = { -7f, 0.5f, 6f };
        for (int k = 0; k < 3; k++)
        {
            _inc[k] = hz * MathF.Pow(2f, det[k] / 1200f) / Dsp.SampleRate;
            _ph[k] = Rng.Unit();
        }
        _cut = Math.Min(700f + 2000f * velocity, hz * 9f + 300f);
        _lp.Set(_cut, 0.75f);
        Env = new Envelope(0.7f, 1.2f, 0.85f, 1.4f);
        _vib = Rng.Unit() * Dsp.TwoPi;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public override void Render(float[] buf, int n)
    {
        float vr = 5.3f * Dsp.TwoPi / Dsp.SampleRate;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            _vib += vr; if (_vib > Dsp.TwoPi) _vib -= Dsp.TwoPi;
            float m = 1f + (MathF.Sin(_vib) * 5f + _drift.Next(ref Rng, 4f)) / 1200f * 0.693f;
            float s = 0;
            for (int k = 0; k < 3; k++)
            {
                s += Wavetable.Read(_t, _ph[k]);
                _ph[k] += _inc[k] * m;
                if (_ph[k] >= 1f) _ph[k] -= 1f;
            }
            buf[i] += _lp.Low(s * 0.4f) * e * Amp;
        }
    }
}

internal sealed class DarkPadVoice : Voice
{
    private float[] _saw = Array.Empty<float>(), _sq = Array.Empty<float>();
    private float _p1, _p2, _p3, _i1, _i2, _i3, _lfo, _lfoRate, _base;
    private Svf _lp;
    private int _ctr;

    protected override void Start(float hz, float velocity)
    {
        _saw = Wavetable.Saw.For(hz);
        _sq = Wavetable.Square.For(hz * 0.5f);
        _i1 = hz * MathF.Pow(2f, 10f / 1200f) / Dsp.SampleRate;
        _i2 = hz * MathF.Pow(2f, -10f / 1200f) / Dsp.SampleRate;
        _i3 = hz * 0.5f / Dsp.SampleRate;
        _p2 = Rng.Unit();
        _lfo = Rng.Unit() * Dsp.TwoPi;
        _lfoRate = (0.05f + Rng.Unit() * 0.04f) * Dsp.TwoPi / Dsp.SampleRate;
        _base = Math.Max(250f, hz * 1.5f);
        Env = new Envelope(2.4f, 2f, 0.9f, 3.2f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public override void Render(float[] buf, int n)
    {
        for (int i = 0; i < n; i++)
        {
            if ((_ctr++ & 31) == 0)
                _lp.Set(_base + 700f * (0.5f + 0.5f * MathF.Sin(_lfo)), 1.6f);
            _lfo += _lfoRate;
            float e = Env.Next();
            float s = Wavetable.Read(_saw, _p1) + Wavetable.Read(_saw, _p2) + 0.5f * Wavetable.Read(_sq, _p3);
            _p1 += _i1; if (_p1 >= 1f) _p1 -= 1f;
            _p2 += _i2; if (_p2 >= 1f) _p2 -= 1f;
            _p3 += _i3; if (_p3 >= 1f) _p3 -= 1f;
            buf[i] += _lp.Low(s * 0.3f) * e * Amp;
        }
    }
}

internal sealed class RecorderVoice : Voice
{
    private static readonly Wavetable Table = new(h => h switch { 1 => 1f, 2 => 0.16f, 3 => 0.2f, 4 => 0.03f, 5 => 0.05f, _ => 0f }, 8);
    private float[] _t = Array.Empty<float>();
    private float _p, _inc, _age, _vib;
    private Svf _breath;
    private float _chiff = 1f;

    protected override void Start(float hz, float velocity)
    {
        _t = Table.For(hz);
        _inc = hz / Dsp.SampleRate;
        _breath.Set(Math.Min(hz * 2f, 9000f), 3f);
        Env = new Envelope(0.05f, 0.2f, 0.82f, 0.09f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public override void Render(float[] buf, int n)
    {
        float vr = 5.2f * Dsp.TwoPi / Dsp.SampleRate;
        float dt = 1f / Dsp.SampleRate;
        float chiffDecay = Dsp.Coef(0.05f);
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            _age += dt;
            _vib += vr; if (_vib > Dsp.TwoPi) _vib -= Dsp.TwoPi;
            float depth = 11f * Math.Clamp((_age - 0.35f) * 2f, 0f, 1f);
            float scoop = -14f * MathF.Max(0f, 1f - _age / 0.045f);
            float m = 1f + (MathF.Sin(_vib) * depth + scoop) / 1200f * 0.693f;
            float s = Wavetable.Read(_t, _p);
            _p += _inc * m; if (_p >= 1f) _p -= 1f;
            float br = _breath.Band(Rng.Next()) * (0.05f + 0.25f * _chiff);
            _chiff *= chiffDecay;
            buf[i] += (s * 0.8f + br) * e * Amp;
        }
    }
}

/// <summary>Karplus-Strong strings: harpsichord (bright, three strings), harp (soft, long), lute (warm, short).</summary>
internal sealed class PluckVoice : Voice
{
    public enum Kind { Harpsichord, Harp, Lute }

    private readonly Kind _kind;
    private DelayLine[] _lines = Array.Empty<DelayLine>();
    private float[] _len = Array.Empty<float>();
    private float[] _g = Array.Empty<float>();
    private float[] _mix = Array.Empty<float>();
    private float[] _prev = Array.Empty<float>();
    private float _bright;
    private float _damp = 1f;
    private bool _released;
    private OnePole _tone;

    public PluckVoice(Kind kind) => _kind = kind;

    protected override void Start(float hz, float velocity)
    {
        // String set: harpsichord is two 8' strings a few cents apart and a 4' an octave up.
        (float ratio, float cents, float level)[] strings = _kind == Kind.Harpsichord
            ? new[] { (1f, -2.5f, 1f), (1f, 2.5f, 0.8f), (2f, 0f, 0.35f) }
            : new[] { (1f, 0f, 1f) };
        int ns = strings.Length;
        _lines = new DelayLine[ns];
        _len = new float[ns];
        _g = new float[ns];
        _mix = new float[ns];
        _prev = new float[ns];
        // Ring time in seconds, shorter as the pitch rises.
        float t60 = _kind switch
        {
            Kind.Harpsichord => 5.5f,
            Kind.Harp => 7.0f,
            _ => 3.2f,
        } * MathF.Pow(2f, -(Midi - 48f) / 12f * 0.55f);
        _bright = _kind switch { Kind.Harpsichord => 0.18f, Kind.Harp => 0.5f, _ => 0.4f };
        _tone.Set(_kind switch { Kind.Harpsichord => 6500f, Kind.Harp => 1500f, _ => 2600f } * (0.6f + 0.5f * velocity));
        for (int s = 0; s < ns; s++)
        {
            float f = hz * strings[s].ratio * MathF.Pow(2f, strings[s].cents / 1200f);
            // The averaging loop filter adds half a sample of delay; take it off the line.
            float len = Dsp.SampleRate / f - 0.5f - _bright;
            _len[s] = Math.Max(2f, len);
            _lines[s] = new DelayLine((int)len + 4);
            _g[s] = MathF.Pow(10f, -3f / (t60 * f));
            _mix[s] = strings[s].level;
            // Excitation: a noise burst one period long, low-passed by how soft the pluck is,
            // and combed for the plucking point (near the end for a harpsichord's quill).
            int n = (int)len;
            var exc = new float[n];
            OnePole lp = default;
            lp.Set(_kind switch { Kind.Harpsichord => 9000f, Kind.Harp => 1200f, _ => 2400f } * (0.5f + 0.7f * velocity));
            for (int i = 0; i < n; i++) exc[i] = lp.Low(Rng.Next());
            int pos = Math.Max(1, (int)(n * (_kind == Kind.Harpsichord ? 0.12f : 0.3f)));
            float mean = 0;
            for (int i = n - 1; i >= pos; i--) exc[i] -= exc[i - pos];
            for (int i = 0; i < n; i++) mean += exc[i];
            mean /= n;
            for (int i = 0; i < n; i++) _lines[s].Write(exc[i] - mean);
        }
        // The strings carry their own decay; the envelope only says when the voice is finished.
        Env = new Envelope(0.001f, 0.01f, 1f, 0.01f);
    }

    public override void NoteOff()
    {
        // A harp is let ring; harpsichord and lute dampers fall.
        if (_kind == Kind.Harp) return;
        _released = true;
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public override void Render(float[] buf, int n)
    {
        float dampTarget = _released ? (_kind == Kind.Harpsichord ? 0.9965f : 0.997f) : 1f;
        float energy = 0;
        for (int i = 0; i < n; i++)
        {
            _damp += (dampTarget - _damp) * 0.01f;
            float sum = 0;
            for (int s = 0; s < _lines.Length; s++)
            {
                float y = _lines[s].Read(_len[s]);
                float f = (y * (1f - _bright) + _prev[s] * _bright);
                _prev[s] = y;
                _lines[s].Write(f * _g[s] * _damp);
                sum += y * _mix[s];
            }
            energy += sum * sum;
            buf[i] += _tone.Low(sum) * 0.9f * Amp;
        }
        // The voice is over when the strings have died away.
        if (energy / Math.Max(1, n) < 1e-9f) Env.Kill(0.001f);
        Env.Next();
    }
}

internal sealed class BellVoice : Voice
{
    // Church-bell partials relative to the nominal's octave below: hum, prime, tierce (a minor
    // third: why bells sound sad), quint, nominal, and the upper partials.
    private static readonly float[] Ratios = { 0.5f, 1f, 1.19f, 1.5f, 2f, 2.51f, 2.66f, 3.01f, 4.17f, 5.43f, 6.8f };
    private static readonly float[] Amps   = { 0.3f, 0.7f, 0.5f, 0.3f, 0.9f, 0.45f, 0.4f, 0.35f, 0.25f, 0.15f, 0.1f };
    private static readonly float[] Decays = { 1.7f, 1.1f, 0.9f, 0.7f, 0.6f, 0.4f, 0.35f, 0.3f, 0.2f, 0.15f, 0.1f };
    private readonly float[] _re = new float[11], _im = new float[11], _c = new float[11], _s = new float[11], _g = new float[11], _a = new float[11];
    private int _np;
    private float _strike;
    private Svf _strikeBp;

    protected override void Start(float hz, float velocity)
    {
        float t = 5.5f * MathF.Pow(2f, -(Midi - 72f) / 12f * 0.7f);
        _np = 0;
        for (int k = 0; k < Ratios.Length; k++)
        {
            float f = hz * 0.5f * Ratios[k] * 2f * (1f + (Rng.Next() * 0.002f));
            if (f > Dsp.Nyquist * 0.8f) break;
            float w = Dsp.TwoPi * f / Dsp.SampleRate;
            _c[k] = MathF.Cos(w); _s[k] = MathF.Sin(w);
            _re[k] = 0; _im[k] = 1;
            _g[k] = MathF.Pow(10f, -3f / (t * Decays[k] * Dsp.SampleRate));
            _a[k] = Amps[k];
            _np = k + 1;
        }
        _strike = 0.4f * velocity;
        _strikeBp.Set(Math.Min(hz * 4f, 8000f), 1.5f);
        Env = new Envelope(0.002f, 0.01f, 1f, 2.5f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public override void Render(float[] buf, int n)
    {
        float sd = Dsp.Coef(0.01f);
        float energy = 0;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            float sum = 0;
            for (int k = 0; k < _np; k++)
            {
                float re = _re[k] * _c[k] - _im[k] * _s[k];
                float im = _re[k] * _s[k] + _im[k] * _c[k];
                _re[k] = re * _g[k]; _im[k] = im * _g[k];
                sum += _im[k] * _a[k];
            }
            sum += _strikeBp.Band(Rng.Next()) * _strike;
            _strike *= sd;
            energy += sum * sum;
            buf[i] += sum * 0.35f * e * Amp;
        }
        if (energy / Math.Max(1, n) < 1e-10f) Env.Kill(0.001f);
    }
}

internal sealed class DrumVoice : Voice
{
    private static readonly float[] Ratios = { 1f, 1.5f, 1.98f, 2.44f };
    private readonly bool _hand;
    private readonly float[] _ph = new float[4], _inc = new float[4], _g = new float[4], _amp = new float[4];
    private float _glide, _thump;
    private OnePole _thumpLp;
    private Svf _skin;

    public DrumVoice(bool hand) => _hand = hand;

    protected override void Start(float hz, float velocity)
    {
        float t = _hand ? 0.35f : 2.2f;
        float[] amps = _hand ? new[] { 1f, 0.3f, 0.12f, 0.05f } : new[] { 1f, 0.7f, 0.5f, 0.3f };
        float[] dec = { 1f, 0.6f, 0.4f, 0.3f };
        for (int k = 0; k < 4; k++)
        {
            _inc[k] = hz * Ratios[k] / Dsp.SampleRate;
            _g[k] = MathF.Pow(10f, -3f / (t * dec[k] * Dsp.SampleRate));
            _amp[k] = amps[k];
            _ph[k] = 0;
        }
        _glide = _hand ? 0.06f : 0.03f;
        _thump = _hand ? 0.9f : 0.6f;
        _thumpLp.Set(_hand ? 500f : 260f);
        _skin.Set(_hand ? 1100f : 700f, 0.8f);
        Env = new Envelope(0.001f, 0.01f, 1f, _hand ? 0.15f : 0.8f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public override void Render(float[] buf, int n)
    {
        float gd = Dsp.Coef(0.05f), td = Dsp.Coef(_hand ? 0.03f : 0.06f);
        float energy = 0;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            float m = 1f + _glide;
            _glide *= gd;
            float s = 0;
            for (int k = 0; k < 4; k++)
            {
                s += MathF.Sin(Dsp.TwoPi * _ph[k]) * _amp[k];
                _ph[k] += _inc[k] * m; if (_ph[k] >= 1f) _ph[k] -= 1f;
                _amp[k] *= _g[k];
            }
            float nz = Rng.Next();
            s += (_thumpLp.Low(nz) * 2.0f + _skin.Band(nz) * 0.9f) * _thump;
            _thump *= td;
            energy += s * s;
            buf[i] += s * 0.6f * e * Amp;
        }
        if (energy / Math.Max(1, n) < 1e-10f) Env.Kill(0.001f);
    }
}

internal sealed class BourdonVoice : Voice
{
    private float[] _t = Array.Empty<float>();
    private float _p1, _p2, _ps, _i1, _i2, _is, _lfo, _lfoRate;
    private Svf _lp;

    protected override void Start(float hz, float velocity)
    {
        _t = Wavetable.SoftSaw.For(hz);
        _i1 = hz * MathF.Pow(2f, 4f / 1200f) / Dsp.SampleRate;
        _i2 = hz * MathF.Pow(2f, -4f / 1200f) / Dsp.SampleRate;
        _is = hz / Dsp.SampleRate;
        _p2 = Rng.Unit();
        _lp.Set(Math.Clamp(hz * 7f, 350f, 1400f), 0.8f);
        _lfo = Rng.Unit() * Dsp.TwoPi;
        _lfoRate = 0.13f * Dsp.TwoPi / Dsp.SampleRate;
        Env = new Envelope(3.0f, 2f, 0.9f, 4f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public override void Render(float[] buf, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            _lfo += _lfoRate;
            float s = Wavetable.Read(_t, _p1) + Wavetable.Read(_t, _p2);
            float sub = MathF.Sin(Dsp.TwoPi * _ps);
            _p1 += _i1; if (_p1 >= 1f) _p1 -= 1f;
            _p2 += _i2; if (_p2 >= 1f) _p2 -= 1f;
            _ps += _is; if (_ps >= 1f) _ps -= 1f;
            float v = _lp.Low(s * 0.4f) + sub * 0.25f;
            buf[i] += v * (0.9f + 0.1f * MathF.Sin(_lfo)) * e * Amp;
        }
    }
}

internal sealed class HornVoice : Voice
{
    private float[] _t = Array.Empty<float>();
    private float _p1, _p2, _i1, _i2, _hz, _age, _vib, _vel;
    private Svf _lp;
    private int _ctr;

    protected override void Start(float hz, float velocity)
    {
        _hz = hz;
        _vel = velocity;
        _t = Wavetable.Saw.For(hz);
        _i1 = hz * MathF.Pow(2f, 5f / 1200f) / Dsp.SampleRate;
        _i2 = hz * MathF.Pow(2f, -5f / 1200f) / Dsp.SampleRate;
        _p2 = Rng.Unit();
        Env = new Envelope(0.09f, 0.5f, 0.75f, 0.35f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public override void Render(float[] buf, int n)
    {
        float dt = 1f / Dsp.SampleRate;
        float vr = 4.9f * Dsp.TwoPi / Dsp.SampleRate;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            _age += dt;
            if ((_ctr++ & 15) == 0)
            {
                // The brass "blat": the filter opens with the attack and settles a little lower.
                float open = _age < 0.12f ? _age / 0.12f : 1f - 0.35f * Math.Min(1f, (_age - 0.12f) / 0.6f);
                _lp.Set(_hz * (1.2f + (2.5f + 4f * _vel) * open), 0.9f);
            }
            _vib += vr;
            float depth = 9f * Math.Clamp((_age - 0.4f) * 2f, 0f, 1f);
            float scoop = -28f * MathF.Max(0f, 1f - _age / 0.07f);
            float m = 1f + (MathF.Sin(_vib) * depth + scoop) / 1200f * 0.693f;
            float s = Wavetable.Read(_t, _p1) + Wavetable.Read(_t, _p2);
            _p1 += _i1 * m; if (_p1 >= 1f) _p1 -= 1f;
            _p2 += _i2 * m; if (_p2 >= 1f) _p2 -= 1f;
            buf[i] += _lp.Low(s * 0.4f) * e * Amp;
        }
    }
}

internal sealed class SquareVoice : Voice
{
    private float[] _t = Array.Empty<float>(), _q = Array.Empty<float>();
    private float _p, _inc;
    private OnePole _lp;

    protected override void Start(float hz, float velocity)
    {
        _t = Wavetable.Pulse25.For(hz);
        _q = Wavetable.Square.For(hz);
        _inc = hz / Dsp.SampleRate;
        _lp.Set(3500f);
        Env = new Envelope(0.004f, 0.15f, 0.7f, 0.08f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public override void Render(float[] buf, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            float s = Wavetable.Read(_t, _p) * 0.6f + Wavetable.Read(_q, _p) * 0.4f;
            _p += _inc; if (_p >= 1f) _p -= 1f;
            buf[i] += _lp.Low(s) * 0.5f * e * Amp;
        }
    }
}

internal sealed class WindVoice : Voice
{
    private Svf _bp, _bp2;
    private float _hz, _lfo, _lfoRate;
    private int _ctr;

    protected override void Start(float hz, float velocity)
    {
        _hz = hz;
        _lfo = Rng.Unit() * Dsp.TwoPi;
        _lfoRate = (0.07f + Rng.Unit() * 0.08f) * Dsp.TwoPi / Dsp.SampleRate;
        Env = new Envelope(3.5f, 2f, 0.9f, 4f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]

    public override void Render(float[] buf, int n)
    {
        for (int i = 0; i < n; i++)
        {
            if ((_ctr++ & 31) == 0)
            {
                float c = _hz * MathF.Pow(2f, 0.6f * MathF.Sin(_lfo));
                _bp.Set(c, 9f);
                _bp2.Set(c * 1.5f, 7f);
            }
            _lfo += _lfoRate;
            float e = Env.Next();
            float nz = Rng.Next();
            buf[i] += (_bp.BandNorm(nz) + 0.5f * _bp2.BandNorm(nz)) * 1.4f * e * Amp;
        }
    }
}

internal sealed class FiddleVoice : Voice
{
    private float[] _t = Array.Empty<float>();
    private float _p1, _p2, _inc, _age, _vib, _scratch;
    private Svf _b1, _b2, _b3, _lp, _bow, _scr;
    private Drift _drift;

    protected override void Start(float hz, float velocity)
    {
        _t = Wavetable.Saw.For(hz);
        _inc = hz / Dsp.SampleRate;
        _p2 = Rng.Unit();
        // The body: the air resonance, the main wood resonance and the bridge hill.
        _b1.Set(290f, 3f); _b2.Set(1050f, 2.5f); _b3.Set(2700f, 2f);
        _lp.Set(7500f, 0.7f);
        _bow.Set(3500f, 1.5f);
        _scr.Set(2200f, 2f);
        _scratch = 0.5f + 0.5f * velocity;
        _vib = Rng.Unit() * Dsp.TwoPi;
        Env = new Envelope(0.035f, 0.2f, 0.85f, 0.12f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        float dt = 1f / Dsp.SampleRate, vr = 5.8f * Dsp.TwoPi / Dsp.SampleRate, sd = Dsp.Coef(0.04f);
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            _age += dt;
            _vib += vr; if (_vib > Dsp.TwoPi) _vib -= Dsp.TwoPi;
            float depth = 14f * Math.Clamp((_age - 0.18f) * 3f, 0f, 1f);
            float scoop = -10f * MathF.Max(0f, 1f - _age / 0.03f);
            float m = 1f + (MathF.Sin(_vib) * depth + scoop + _drift.Next(ref Rng, 3f)) / 1200f * 0.693f;
            float src = Wavetable.Read(_t, _p1) + 0.3f * Wavetable.Read(_t, _p2);
            _p1 += _inc * m; if (_p1 >= 1f) _p1 -= 1f;
            _p2 += _inc * m * 1.0023f; if (_p2 >= 1f) _p2 -= 1f;
            float nz = Rng.Next();
            float body = _b1.BandNorm(src) * 0.3f + _b2.BandNorm(src) * 0.9f + _b3.BandNorm(src) * 1.0f + src * 0.35f;
            body += _bow.Band(nz) * 0.015f + _scr.BandNorm(nz) * _scratch * 0.3f;
            _scratch *= sd;
            buf[i] += _lp.Low(body) * 0.5f * e * Amp;
        }
    }
}

internal sealed class WhistleVoice : Voice
{
    private static readonly Wavetable Table = new(h => h switch { 1 => 1f, 2 => 0.08f, 3 => 0.05f, _ => 0f }, 4);
    private float[] _t = Array.Empty<float>();
    private float _p, _inc, _age, _vib, _chiff;
    private Svf _breath, _chiffBp;

    protected override void Start(float hz, float velocity)
    {
        _t = Table.For(hz);
        _inc = hz / Dsp.SampleRate;
        _breath.Set(Math.Min(hz * 2f, 12000f), 2.5f);
        _chiffBp.Set(Math.Min(hz * 3f, 12000f), 3f);
        _chiff = 0.5f;
        Env = new Envelope(0.015f, 0.1f, 0.9f, 0.04f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        float dt = 1f / Dsp.SampleRate, vr = 5.1f * Dsp.TwoPi / Dsp.SampleRate, cd = Dsp.Coef(0.025f);
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            _age += dt;
            _vib += vr; if (_vib > Dsp.TwoPi) _vib -= Dsp.TwoPi;
            float m = 1f + MathF.Sin(_vib) * 6f * Math.Clamp((_age - 0.4f) * 2f, 0f, 1f) / 1200f * 0.693f;
            float s = Wavetable.Read(_t, _p);
            _p += _inc * m; if (_p >= 1f) _p -= 1f;
            float nz = Rng.Next();
            float v = s * 0.8f + _breath.BandNorm(nz) * 0.03f + _chiffBp.BandNorm(nz) * _chiff * 0.6f;
            _chiff *= cd;
            buf[i] += v * e * Amp;
        }
    }
}

internal sealed class PipesVoice : Voice
{
    private static readonly Wavetable Reed = new(h => MathF.Sin(MathF.PI * h * 0.33f) / h * (h <= 2 ? 0.7f : 1f));
    private float[] _t = Array.Empty<float>();
    private float _p, _inc;
    private Svf _f1, _f2, _lp;
    private Drift _drift;

    protected override void Start(float hz, float velocity)
    {
        _t = Reed.For(hz);
        _inc = hz / Dsp.SampleRate;
        _f1.Set(1400f, 2f); _f2.Set(2600f, 3f);
        _lp.Set(4200f, 0.7f);
        Env = new Envelope(0.015f, 0.05f, 0.95f, 0.06f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            float m = 1f + _drift.Next(ref Rng, 3f) / 1200f * 0.693f;
            float s = Wavetable.Read(_t, _p);
            _p += _inc * m; if (_p >= 1f) _p -= 1f;
            float v = s * 0.45f + _f1.BandNorm(s) * 0.7f + _f2.BandNorm(s) * 0.4f;
            buf[i] += _lp.Low(v) * 0.5f * e * Amp;
        }
    }
}

internal sealed class BodhranVoice : Voice
{
    private static readonly float[] Ratios = { 1f, 1.59f, 2.14f };
    private readonly float[] _ph = new float[3], _inc = new float[3], _g = new float[3], _amp = new float[3];
    private float _thump, _click;
    private OnePole _thumpLp;
    private Svf _tip;

    protected override void Start(float hz, float velocity)
    {
        float[] amps = { 1f, 0.35f, 0.15f };
        float[] dec = { 0.28f, 0.16f, 0.1f };
        for (int k = 0; k < 3; k++)
        {
            _inc[k] = hz * Ratios[k] / Dsp.SampleRate;
            _g[k] = MathF.Pow(10f, -3f / (dec[k] * Dsp.SampleRate));
            _amp[k] = amps[k];
        }
        _thump = 1f;
        _click = 0.25f + 0.35f * velocity;
        _thumpLp.Set(190f);
        _tip.Set(2300f, 1.5f);
        Env = new Envelope(0.001f, 0.01f, 1f, 0.12f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        float td = Dsp.Coef(0.035f), cd = Dsp.Coef(0.006f);
        float energy = 0;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            float s = 0;
            for (int k = 0; k < 3; k++)
            {
                s += MathF.Sin(Dsp.TwoPi * _ph[k]) * _amp[k];
                _ph[k] += _inc[k]; if (_ph[k] >= 1f) _ph[k] -= 1f;
                _amp[k] *= _g[k];
            }
            float nz = Rng.Next();
            s += _thumpLp.Low(nz) * 3f * _thump + _tip.BandNorm(nz) * _click;
            _thump *= td;
            _click *= cd;
            energy += s * s;
            buf[i] += s * 0.6f * e * Amp;
        }
        if (energy / Math.Max(1, n) < 1e-10f) Env.Kill(0.001f);
    }
}

internal sealed class DistortionVoice : Voice
{
    private readonly bool _power;
    private readonly float[] _p = new float[6], _inc = new float[6];
    private float[] _t = Array.Empty<float>();
    private int _n;
    private float _drive, _pick;
    private bool _mute;
    private Svf _pre, _cab1, _cab2, _mid, _pickBand;
    private OnePole _dc;

    public DistortionVoice(bool power) => _power = power;

    protected override void Start(float hz, float velocity)
    {
        _t = Wavetable.Saw.For(_power ? hz * 2f : hz);
        // Each pitch twice, a few cents apart, as a part tracked twice and panned together.
        float[] ratios = _power ? new[] { 1f, 1.4983f, 2f } : new[] { 1f };
        _n = 0;
        foreach (float r in ratios)
            foreach (float cents in new[] { -6f, 6f })
            {
                _inc[_n] = hz * r * MathF.Pow(2f, cents / 1200f) / Dsp.SampleRate;
                _p[_n] = Rng.Unit();
                _n++;
            }
        _mute = velocity < 0.55f;
        _drive = _power ? 7f : 11f;
        _pre.Set(110f, 0.7f);
        // The cabinet: a speaker rolls off above about 4 kHz; a palm on the strings takes most of the top.
        _cab1.Set(_mute ? 1500f : 4200f, 0.9f);
        _cab2.Set(_mute ? 2500f : 5600f, 0.6f);
        _mid.Set(750f, 0.8f);
        _pickBand.Set(2600f, 1.2f);
        _pick = 0.4f + 0.4f * velocity;
        _dc.Set(20f);
        Env = _mute ? new Envelope(0.002f, 0.14f, 0.2f, 0.04f) : new Envelope(0.003f, 0.5f, 0.75f, 0.08f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        float pd = Dsp.Coef(0.004f), norm = 1f / _n;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            float src = 0;
            for (int k = 0; k < _n; k++)
            {
                // One table, band-limited for the highest pitch sounding (the octave): the root loses only
                // harmonics the cabinet would take anyway.
                src += Wavetable.Read(_t, _p[k]);
                _p[k] += _inc[k]; if (_p[k] >= 1f) _p[k] -= 1f;
            }
            src = src * norm + _pickBand.BandNorm(Rng.Next()) * _pick;
            _pick *= pd;
            // The envelope goes in before the clipper, as a string's decay does into an amp: the note
            // stays loud and gritty, then thins and breaks up as it dies.
            float x = _pre.High(src) * e * _drive;
            float y = MathF.Tanh(x) + 0.12f * MathF.Tanh(x * 0.5f) * MathF.Tanh(x * 0.5f);
            y = _dc.High(y);
            y = _cab2.Low(_cab1.Low(y));
            y -= 0.35f * _mid.BandNorm(y);
            buf[i] += y * 0.3f * MathF.Min(1f, e * 4f) * Amp;
        }
    }
}

internal sealed class KickVoice : Voice
{
    private float _ph, _f, _fEnd, _amp, _ampCoef, _sweep, _click;
    private Svf _clickBand;

    protected override void Start(float hz, float velocity)
    {
        _fEnd = Math.Clamp(hz, 38f, 75f);
        _f = _fEnd * 3.4f;
        _sweep = Dsp.Coef(0.028f);
        _amp = 1f;
        _ampCoef = Dsp.Coef(0.32f);
        _click = 0.35f + 0.4f * velocity;
        _clickBand.Set(3200f, 1f);
        Env = new Envelope(0.0005f, 0.01f, 1f, 0.06f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        float cd = Dsp.Coef(0.003f), energy = 0;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            _f = _fEnd + (_f - _fEnd) * _sweep;
            _ph += _f / Dsp.SampleRate; if (_ph >= 1f) _ph -= 1f;
            float s = MathF.Sin(Dsp.TwoPi * _ph) * _amp;
            s = MathF.Tanh(s * 1.6f);
            s += _clickBand.BandNorm(Rng.Next()) * _click;
            _amp *= _ampCoef;
            _click *= cd;
            energy += s * s;
            buf[i] += s * 0.7f * e * Amp;
        }
        if (energy / Math.Max(1, n) < 1e-10f) Env.Kill(0.001f);
    }
}

internal sealed class SnareVoice : Voice
{
    private float _p1, _p2, _a1, _a2, _nz, _crack, _c1, _c2, _cn;
    private Svf _wires, _air;

    protected override void Start(float hz, float velocity)
    {
        _a1 = 1f; _a2 = 0.6f;
        _nz = 0.9f + 0.4f * velocity;
        _crack = 0.6f;
        _c1 = Dsp.Coef(0.09f); _c2 = Dsp.Coef(0.06f);
        _cn = Dsp.Coef(0.12f + 0.08f * velocity);
        _wires.Set(1900f, 0.7f);
        _air.Set(5200f, 0.7f);
        Env = new Envelope(0.0005f, 0.01f, 1f, 0.08f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        float i1 = 185f / Dsp.SampleRate, i2 = 330f / Dsp.SampleRate, ck = Dsp.Coef(0.008f), energy = 0;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            float body = MathF.Sin(Dsp.TwoPi * _p1) * _a1 + MathF.Sin(Dsp.TwoPi * _p2) * _a2;
            _p1 += i1; if (_p1 >= 1f) _p1 -= 1f;
            _p2 += i2; if (_p2 >= 1f) _p2 -= 1f;
            float w = Rng.Next();
            float s = body * 0.6f + _wires.BandNorm(w) * _nz + _air.High(w) * _crack;
            _a1 *= _c1; _a2 *= _c2; _nz *= _cn; _crack *= ck;
            energy += s * s;
            buf[i] += s * 0.55f * e * Amp;
        }
        if (energy / Math.Max(1, n) < 1e-10f) Env.Kill(0.001f);
    }
}

internal sealed class CymbalVoice : Voice
{
    // The 808's six square oscillators: inharmonic enough to read as metal.
    private static readonly float[] Freqs = { 205.3f, 304.4f, 369.6f, 522.7f, 540.0f, 800.0f };
    private readonly float[] _ph = new float[6];
    private float _amp, _coef;
    private Svf _hp1, _hp2, _band;

    protected override void Start(float hz, float velocity)
    {
        bool crash = velocity >= 0.5f;
        _amp = 1f;
        _coef = Dsp.Coef(crash ? 1.4f : 0.045f);
        // Darker than a real cymbal: the genre's records are cassettes, and the highs are the first to go.
        _hp1.Set(crash ? 3200f : 5000f, 0.7f);
        _hp2.Set(crash ? 2800f : 4500f, 0.7f);
        _band.Set(6000f, 0.9f);
        for (int k = 0; k < 6; k++) _ph[k] = Rng.Unit();
        Env = new Envelope(0.001f, 0.01f, 1f, crash ? 0.5f : 0.03f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        float energy = 0;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            float s = 0;
            for (int k = 0; k < 6; k++)
            {
                s += _ph[k] < 0.5f ? 1f : -1f;
                _ph[k] += Freqs[k] * 1.7f / Dsp.SampleRate; if (_ph[k] >= 1f) _ph[k] -= 1f;
            }
            s = s / 6f + Rng.Next() * 0.5f;
            s = _hp2.High(_hp1.High(s)) + 0.5f * _band.BandNorm(s);
            s *= _amp;
            _amp *= _coef;
            energy += s * s;
            buf[i] += s * 0.4f * e * Amp;
        }
        if (energy / Math.Max(1, n) < 1e-10f) Env.Kill(0.001f);
    }
}

internal sealed class StaccatoVoice : Voice
{
    private readonly float[] _p = new float[4], _inc = new float[4];
    private float[] _t = Array.Empty<float>(), _sub = Array.Empty<float>();
    private float _cut, _bite;
    private Svf _lp, _body;

    protected override void Start(float hz, float velocity)
    {
        _t = Wavetable.Saw.For(hz);
        _sub = Wavetable.SoftSaw.For(hz * 0.5f);
        float[] cents = { -9f, -2f, 7f };
        for (int k = 0; k < 3; k++) { _inc[k] = hz * MathF.Pow(2f, cents[k] / 1200f) / Dsp.SampleRate; _p[k] = Rng.Unit(); }
        _inc[3] = hz * 0.5f / Dsp.SampleRate;
        _bite = 2600f + 1800f * velocity;
        _cut = _bite;
        _body.Set(420f, 1.2f);
        Env = new Envelope(0.008f, 0.16f, 0.3f, 0.12f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        float cd = Dsp.Coef(0.09f);
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            if ((i & 15) == 0) { _cut = 750f + (_cut - 750f) * MathF.Pow(cd, 16); _lp.Set(_cut, 0.8f); }
            float src = 0;
            for (int k = 0; k < 3; k++) { src += Wavetable.Read(_t, _p[k]); _p[k] += _inc[k]; if (_p[k] >= 1f) _p[k] -= 1f; }
            src = src / 3f + 0.35f * Wavetable.Read(_sub, _p[3]);
            _p[3] += _inc[3]; if (_p[3] >= 1f) _p[3] -= 1f;
            float y = _lp.Low(src) + 0.25f * _body.BandNorm(src);
            buf[i] += y * 0.45f * e * Amp;
        }
    }
}

internal sealed class WarDrumVoice : Voice
{
    private static readonly float[] Ratios = { 1f, 1.52f, 1.98f, 2.44f };
    private readonly float[] _ph = new float[4], _amp = new float[4], _g = new float[4];
    private float _hz, _bend, _bendCoef, _slap, _thump;
    private OnePole _slapLp, _thumpLp;

    protected override void Start(float hz, float velocity)
    {
        _hz = Math.Clamp(hz, 45f, 140f);
        // Struck hard, a big skin starts sharp and settles: a third of an octave in forty milliseconds.
        _bend = 0.26f + 0.12f * velocity;
        _bendCoef = Dsp.Coef(0.04f);
        float[] amps = { 1f, 0.45f, 0.25f, 0.12f };
        float[] dec = { 1.1f, 0.55f, 0.35f, 0.22f };
        float length = 0.7f + 0.6f * velocity;
        for (int k = 0; k < 4; k++) { _amp[k] = amps[k]; _g[k] = MathF.Pow(10f, -3f / (length * dec[k] * Dsp.SampleRate)); _ph[k] = 0f; }
        _slap = 0.5f + 0.5f * velocity;
        _thump = 1f;
        _slapLp.Set(1100f);
        _thumpLp.Set(140f);
        Env = new Envelope(0.0008f, 0.01f, 1f, 0.15f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        float sd = Dsp.Coef(0.014f), td = Dsp.Coef(0.05f), energy = 0;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            float f = _hz * (1f + _bend);
            _bend *= _bendCoef;
            float s = 0;
            for (int k = 0; k < 4; k++)
            {
                s += MathF.Sin(Dsp.TwoPi * _ph[k]) * _amp[k];
                _ph[k] += f * Ratios[k] / Dsp.SampleRate; if (_ph[k] >= 1f) _ph[k] -= 1f;
                _amp[k] *= _g[k];
            }
            float nz = Rng.Next();
            s += _slapLp.Low(nz) * 1.6f * _slap + _thumpLp.Low(nz) * 4f * _thump;
            _slap *= sd;
            _thump *= td;
            s = MathF.Tanh(s * 1.2f);
            energy += s * s;
            buf[i] += s * 0.6f * e * Amp;
        }
        if (energy / Math.Max(1, n) < 1e-10f) Env.Kill(0.001f);
    }
}

internal sealed class MetallophoneVoice : Voice
{
    // A free metal bar's modes, not a string's harmonics.
    private static readonly float[] Ratios = { 1f, 2.76f, 5.40f, 8.93f };
    private readonly float[] _ph = new float[8], _inc = new float[8], _amp = new float[8], _g = new float[8];
    private float _click;
    private OnePole _clickLp;

    protected override void Start(float hz, float velocity)
    {
        float[] amps = { 1f, 0.42f, 0.18f, 0.07f };
        float[] dec = { 1f, 0.42f, 0.22f, 0.12f };
        float ring = (2.6f + velocity) * MathF.Pow(2f, -(Midi - 60f) / 12f * 0.4f);
        // Two bars a few hertz apart: the pair beats, a shimmer that never settles.
        float beat = 3.5f + Rng.Unit() * 2f;
        for (int k = 0; k < 8; k++)
        {
            int m = k % 4;
            float f = hz * Ratios[m] + (k >= 4 ? beat : 0f);
            _inc[k] = f / Dsp.SampleRate;
            _amp[k] = amps[m] * (k >= 4 ? 0.85f : 1f) * (f < Dsp.Nyquist * 0.9f ? 1f : 0f);
            _g[k] = MathF.Pow(10f, -3f / (ring * dec[m] * Dsp.SampleRate));
            _ph[k] = 0f;
        }
        _click = 0.3f + 0.4f * velocity;
        _clickLp.Set(3500f);
        Env = new Envelope(0.0008f, 0.01f, 1f, 0.6f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        float cd = Dsp.Coef(0.003f), energy = 0;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            float s = 0;
            for (int k = 0; k < 8; k++)
            {
                s += MathF.Sin(Dsp.TwoPi * _ph[k]) * _amp[k];
                _ph[k] += _inc[k]; if (_ph[k] >= 1f) _ph[k] -= 1f;
                _amp[k] *= _g[k];
            }
            s += _clickLp.Low(Rng.Next()) * _click;
            _click *= cd;
            energy += s * s;
            buf[i] += s * 0.4f * e * Amp;
        }
        if (energy / Math.Max(1, n) < 1e-10f) Env.Kill(0.001f);
    }
}

internal sealed class BuzzStringVoice : Voice
{
    private DelayLine _line = null!;
    private float _len, _g, _prev, _bend, _bendCoef, _buzz;
    private OnePole _tone;
    private Svf _nasal;

    protected override void Start(float hz, float velocity)
    {
        float t60 = 4.5f * MathF.Pow(2f, -(Midi - 48f) / 12f * 0.5f);
        _len = Math.Max(2f, Dsp.SampleRate / hz - 0.5f);
        _line = new DelayLine((int)(_len * 1.3f) + 8);
        _g = MathF.Pow(10f, -3f / (t60 * hz));
        _bend = Glide;
        _bendCoef = Dsp.Coef(0.09f);
        _buzz = 0.35f;
        int n = (int)_len;
        OnePole lp = default;
        lp.Set(3000f * (0.6f + 0.6f * velocity));
        float mean = 0;
        var exc = new float[n];
        for (int i = 0; i < n; i++) { exc[i] = lp.Low(Rng.Next()); mean += exc[i]; }
        mean /= n;
        for (int i = 0; i < n; i++) _line.Write(exc[i] - mean);
        _tone.Set(4200f);
        _nasal.Set(1600f, 2.5f);
        Env = new Envelope(0.001f, 0.01f, 1f, 0.12f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        float energy = 0;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            // A bend shortens or lengthens the string as it rings.
            float len = _len * (1f - _bend * 0.0578f);
            _bend *= _bendCoef;
            float y = _line.Read(len);
            // The bridge: the string's swing is cut short on one side, which is the buzz.
            float th = 0.02f;
            if (y > th) y = th + (y - th) * (1f - _buzz);
            float f = (y + _prev) * 0.5f;
            _prev = y;
            _line.Write(f * _g);
            float o = _tone.Low(y) + 0.6f * _nasal.BandNorm(y);
            energy += o * o;
            buf[i] += o * 1.2f * e * Amp;
        }
        if (energy / Math.Max(1, n) < 1e-9f) Env.Kill(0.001f);
    }
}

internal sealed class ReedVoice : Voice
{
    private float[] _t = Array.Empty<float>();
    private float _p, _inc, _vib, _age, _bend, _bendCoef;
    private Svf _f1, _f2, _f3, _breathBand;
    private Drift _drift;

    protected override void Start(float hz, float velocity)
    {
        _t = Wavetable.Pulse25.For(hz);
        _inc = hz / Dsp.SampleRate;
        _p = Rng.Unit();
        _vib = Rng.Unit() * Dsp.TwoPi;
        _bend = Glide;
        _bendCoef = Dsp.Coef(0.1f);
        _f1.Set(650f, 2.2f); _f2.Set(1350f, 3f); _f3.Set(2700f, 4f);
        _breathBand.Set(1800f, 1.2f);
        Env = new Envelope(0.06f, 0.3f, 0.82f, 0.18f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        float dt = 1f / Dsp.SampleRate, vr = 5.2f * Dsp.TwoPi / Dsp.SampleRate;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            _age += dt;
            _vib += vr; if (_vib > Dsp.TwoPi) _vib -= Dsp.TwoPi;
            float depth = 18f * Math.Clamp((_age - 0.35f) * 2f, 0f, 1f);
            float cents = MathF.Sin(_vib) * depth + _drift.Next(ref Rng, 8f) + _bend * 100f;
            _bend *= _bendCoef;
            float src = Wavetable.Read(_t, _p);
            _p += _inc * (1f + cents / 1200f * 0.693f); if (_p >= 1f) _p -= 1f;
            float v = _f1.BandNorm(src) + 0.7f * _f2.BandNorm(src) + 0.3f * _f3.BandNorm(src) + 0.25f * src;
            v += _breathBand.BandNorm(Rng.Next()) * 0.06f;
            buf[i] += v * 0.4f * e * Amp;
        }
    }
}

internal sealed class GlassVoice : Voice
{
    private static readonly float[] Ratios = { 1f, 1.0035f, 2.002f, 3.007f };
    private static readonly float[] Amps = { 1f, 0.8f, 0.3f, 0.12f };
    private readonly float[] _ph = new float[4], _inc = new float[4];
    private float _bend, _bendCoef;
    private Svf _bow;

    protected override void Start(float hz, float velocity)
    {
        for (int k = 0; k < 4; k++) { _inc[k] = hz * Ratios[k] / Dsp.SampleRate; _ph[k] = Rng.Unit(); }
        _bend = Glide;
        _bendCoef = Dsp.Coef(0.25f);
        _bow.Set(Math.Min(hz * 4f, 6000f), 3f);
        Env = new Envelope(0.7f, 1.0f, 0.85f, 1.5f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            float m = 1f + _bend * 0.0578f;
            _bend *= _bendCoef;
            float s = 0;
            for (int k = 0; k < 4; k++)
            {
                s += MathF.Sin(Dsp.TwoPi * _ph[k]) * Amps[k];
                _ph[k] += _inc[k] * m; if (_ph[k] >= 1f) _ph[k] -= 1f;
            }
            s += _bow.BandNorm(Rng.Next()) * 0.03f;
            buf[i] += s * 0.3f * e * Amp;
        }
    }
}

internal sealed class BentDrumVoice : Voice
{
    private static readonly float[] Ratios = { 1f, 1.5f, 1.99f };
    private readonly float[] _ph = new float[3], _amp = new float[3], _g = new float[3];
    private float _hz, _bend, _bendCoef, _slap;
    private OnePole _slapLp;

    protected override void Start(float hz, float velocity)
    {
        _hz = Math.Clamp(hz, 70f, 300f);
        // A hand pressing the skin as it rings: the pitch rises from below unless told otherwise.
        _bend = Glide != 0f ? Glide : -3f;
        _bendCoef = Dsp.Coef(0.12f);
        float[] amps = { 1f, 0.4f, 0.2f };
        float[] dec = { 0.45f, 0.25f, 0.15f };
        for (int k = 0; k < 3; k++) { _amp[k] = amps[k]; _g[k] = MathF.Pow(10f, -3f / (dec[k] * (0.7f + 0.5f * velocity) * Dsp.SampleRate)); }
        _slap = 0.4f + 0.4f * velocity;
        _slapLp.Set(2200f);
        Env = new Envelope(0.0008f, 0.01f, 1f, 0.1f);
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveOptimization)]
    public override void Render(float[] buf, int n)
    {
        float sd = Dsp.Coef(0.006f), energy = 0;
        for (int i = 0; i < n; i++)
        {
            float e = Env.Next();
            // The bend runs the other way to a glide: from the offset back to the drum's own pitch,
            // and on past it a little as the skin is pressed.
            float f = _hz * (1f + (_bend + 1.5f * (1f - MathF.Min(1f, MathF.Abs(_bend) / 3f))) * 0.0578f);
            _bend *= _bendCoef;
            float s = 0;
            for (int k = 0; k < 3; k++)
            {
                s += MathF.Sin(Dsp.TwoPi * _ph[k]) * _amp[k];
                _ph[k] += f * Ratios[k] / Dsp.SampleRate; if (_ph[k] >= 1f) _ph[k] -= 1f;
                _amp[k] *= _g[k];
            }
            s += _slapLp.Low(Rng.Next()) * _slap;
            _slap *= sd;
            energy += s * s;
            buf[i] += s * 0.6f * e * Amp;
        }
        if (energy / Math.Max(1, n) < 1e-10f) Env.Kill(0.001f);
    }
}
