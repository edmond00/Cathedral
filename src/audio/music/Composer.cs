using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Audio.Music;

/// <summary>Something the composer has decided will happen at a given moment.</summary>
/// <para><c>Ornament</c> marks a grace note (a cut, a roll's taps); <c>LogDur</c>, when set, is the length
/// the score log records for a note sounded in pieces (a rolled note's first segment carries the
/// whole note), so the harness reads the tune under the ornaments.</para>
internal readonly record struct ScoreEvent(double T, double Dur, Part Part, Instrument Inst, float Midi, float Vel, float Pan,
                                           bool IsGain = false, float FadeSeconds = 0f, bool Ornament = false, double LogDur = 0);

/// <summary>What the composer tells whoever is listening about structure: for the score log and the harness.</summary>
internal interface IScoreObserver
{
    void Piece(double t, Piece p);
    void Section(double t, Section s, Piece p);
    void Chord(double t, double dur, int root, int[] pcs);
}

internal enum SectionKind { Intro, A, AVar, B, BVar, C, Outro, Bridge }

internal sealed class Section
{
    public SectionKind Kind;
    public string Label = "";
    public int Bars;
    public int[] Chords = Array.Empty<int>();           // root degree per bar
    public HashSet<Part> Layers = new();
    public Instrument Melody;
    public Instrument Pad;
    public CounterStyle Counter;
    // Planned lines, filled when the section starts.
    public List<LineNote> MelodyLine = new();
    public List<(double beat, double dur, int midi, float vel)> CounterLine = new();
    public int OctaveShift;                              // melody up an octave (in degrees) for a variation
}

internal sealed class Piece
{
    public int Index;
    public MusicMoodState Mood;
    public Key Key;
    public double Bpm;
    public int Meter;
    public Orchestration Orch = new();
    public List<Section> Sections = new();
    public Motif M = null!, N = null!, MB = null!, NB = null!;
    public string Form = "";
    public double Start;
    public int Center;                                   // melody's central degree
    public bool Fight;
    public bool UsesTheme;
    public Dance Dance;                                  // a festive piece's dance; None for the rest
    public List<LineNote>? ALine;                       // the first A's melody, for the variations
    public List<LineNote>? BLine;                       // a dance tune's B part, played twice
    public double BarSeconds => Meter * 60.0 / Bpm;
    public double BeatSeconds => 60.0 / Bpm;
}

/// <summary>
/// The composer. It works top down, which is the point of it: a piece has a form (intro, A, A', B,
/// A', outro), each section a chord plan and a set of playing layers, each A a period built from a
/// motif (stated, repeated, moved along the harmony, answered with an open and then a closed
/// cadence), each later section a variation of what came before. Notes are the last thing decided.
///
/// <para>It is pulled, never pushed: <see cref="Fill"/> composes bar by bar until it is a little
/// ahead of the clock, so a mood change or a game event lands at the next bar line. Pieces follow
/// each other forever, each from its own seed, so the music never loops; a big enough change of
/// mood ends the current piece early through a short bridge.</para>
/// </summary>
internal sealed class Composer
{
    private readonly Func<int, Random> _pieceRng;
    private readonly IScoreObserver? _observer;
    private Piece? _piece;
    private Random _rng = new(0);   // replaced per piece from _pieceRng; never drawn from before that
    private int _pieceCount;
    private Theme? _theme;

    /// <summary>The theme in both metres: one contour, a rhythm for four and one for three with as many notes.</summary>
    private sealed class Theme
    {
        public Motif Four = null!, Three = null!;
        public Motif For(int meter) => meter == 3 ? Three : Four;

        /// <summary>The theme as a dance figure: its contour on a jig or reel bar with as many notes, if there is one.</summary>
        public Motif? AsDance(Dance d, Random rng)
        {
            for (int i = 0; i < 24; i++)
            {
                var r = Rhythms.DanceBar(d, rng);
                if (r.Length == Four.Count) return new Motif(r, Four.Contour);
            }
            return null;
        }
    }

    private Theme MakeTheme()
    {
        var rng = _pieceRng(-1);
        var four = Melodist.BestMotif(16, 4, 0.45f, 0.35f, 0.4f, rng);
        // A triple-time rhythm with the same number of notes, or the contour cut to fit one.
        double[]? three = null;
        for (int i = 0; i < 24 && three == null; i++)
        {
            var r = Rhythms.Pick(3, 0.45f, rng);
            if (r.Length == four.Count) three = r;
        }
        three ??= Rhythms.Pick(3, 0.6f, rng);
        var contour = four.Contour.Take(three.Length).ToList();
        while (contour.Count < three.Length) contour.Add(contour[^1] - 1);
        return new Theme { Four = four, Three = new Motif(three, contour.ToArray()) };
    }
    private int _section, _bar;
    private double _cursor;          // seconds: start of the next bar to compose
    private int _lastTonic = 50;
    private int[]? _lastPadVoicing;
    private int _lastChordRoot = int.MinValue;
    private double _lastChordStart;

    // Game-event requests, consumed at the next bar or beat.
    private bool _positive, _negative, _neutral;
    private float _melodyBoost = 1f;
    // The last piece was cut short by a new place: the next one starts on its tune, not an intro.
    private bool _cutShort;
    // The tune's last note and when it was sung: a bridge comes to rest on the tonic nearest it.
    private int _lastSung;
    private double _lastSungAt = double.NegativeInfinity;

    public MusicMoodState Mood;                       // the smoothed mood; set by the owner every block
    public bool Fighting;                             // the fight overlay asks for battle music
    public bool Traveling;                            // footfalls under the music
    public string ModeName => _piece?.Key.Mode.DisplayName ?? "—";
    public double Bpm => _piece?.Bpm ?? 0;
    public Piece? Current => _piece;

    public Composer(Func<int, Random> pieceRng, IScoreObserver? observer = null)
    {
        _pieceRng = pieceRng;
        _observer = observer;
    }

    public void Positive() => _positive = true;
    public void Negative() => _negative = true;
    public void Neutral() => _neutral = true;

    /// <summary>
    /// Ends the music here, mid-bar if need be: nothing more of this piece, and the next one begins
    /// at <paramref name="resumeAt"/>, from its own beginning. What was already planned is the
    /// owner's to drop.
    /// </summary>
    public void Cut(double resumeAt)
    {
        if (_lastChordRoot != int.MinValue && _piece != null)
            _observer?.Chord(_lastChordStart, Math.Max(0, resumeAt - _lastChordStart), _lastChordRoot,
                             Chord.On(_piece.Key, _lastChordRoot).PitchClasses(_piece.Key));
        _lastChordRoot = int.MinValue;
        _piece = null;
        _section = 0;
        _bar = 0;
        _cursor = resumeAt;
        _cutShort = false;
        _melodyBoost = 1f;
        _positive = _negative = _neutral = false;
        _lastSungAt = double.NegativeInfinity;
    }

    /// <summary>Composes until the music is planned up to <paramref name="until"/> seconds.</summary>
    public void Fill(double until, List<ScoreEvent> output)
    {
        while (_cursor < until)
            EmitBar(output);
    }

    /// <summary>The time of the next beat at or after <paramref name="t"/>, on the current piece's grid.</summary>
    private double NextBeat(double t)
    {
        if (_piece == null) return t;
        double b = _piece.BeatSeconds;
        double k = Math.Ceiling((t - _piece.Start) / b - 1e-6);
        return _piece.Start + k * b;
    }

    /// <summary>
    /// The stingers that should not wait for a bar line: a timpani stroke for a failure, a bell for a
    /// success, on the next beat.
    /// </summary>
    public void Immediate(double now, List<ScoreEvent> output)
    {
        if (_piece == null) return;
        var key = _piece.Key;
        if (_negative)
        {
            _negative = false;
            double t = NextBeat(now + 0.02);
            int low = key.Tonic - 12;
            output.Add(new(t, 1.5, Part.Perc, Instrument.Timpani, low, 0.95f, 0f));
            // The tune steps back under the stroke and comes up again; it never stops.
            _melodyBoost = 0.6f;
        }
        if (_positive)
        {
            _positive = false;
            double t = NextBeat(now + 0.02);
            var chord = Chord.On(key, 0);
            double step = _piece.BeatSeconds * 0.5;
            int i = 0;
            foreach (int d in new[] { chord.Tones[0] + key.Mode.Size, chord.Tones[1] + key.Mode.Size, chord.Tones[2] + key.Mode.Size, chord.Tones[0] + 2 * key.Mode.Size })
                output.Add(new(t + step * i++, 2.5, Part.Bell, Instrument.Bell, key.Pitch(d), 0.55f, i % 2 == 0 ? -0.6f : 0.6f));
            _melodyBoost = 1.2f;
        }
        if (_neutral)
        {
            _neutral = false;
            _melodyBoost = 0.75f;
        }
    }

    // ── Pieces ────────────────────────────────────────────────────────────────

    private void NewPiece()
    {
        int index = _pieceCount++;
        _rng = _pieceRng(index);
        var mood = EffectiveMood();
        var p = new Piece { Index = index, Mood = mood, Start = _cursor };
        p.Dance = Fighting ? Dance.None : Orchestra.DanceFor(mood, _rng);
        // A dance stays in the two minor modes the old session tunes favour: Dorian above all, then Aeolian.
        var mode = p.Dance != Dance.None ? (_rng.NextDouble() < 0.6 ? Mode.Dorian : Mode.Aeolian) : Mode.ForMood(mood, _rng);
        // The tonic moves by a fourth or a fifth between pieces, or stays: the drone can pivot.
        int[] moves = { 0, 5, -5, 7, -7, 2, -2 };
        double[] mw = { 2, 2, 2, 1.5, 1.5, 0.7, 0.7 };
        int tonic = _lastTonic + moves[WeightedIndex(mw)];
        // D3..C#4: the melody, an octave up, then sits round D4..C#5, a singing register.
        while (tonic > 61) tonic -= 12;
        while (tonic < 50) tonic += 12;
        _lastTonic = tonic;
        p.Key = new Key(tonic, mode);
        p.Bpm = p.Dance != Dance.None ? Orchestra.DanceTempo(p.Dance, mood, _rng) : Orchestra.Tempo(mood, _rng);
        p.Meter = p.Dance switch { Dance.Jig => 2, Dance.Reel => 4, _ => Orchestra.Meter(mood, _rng) };
        if (Fighting) p.Meter = 4;
        p.Fight = Fighting;
        p.Orch = Orchestra.Choose(mood, p.Meter, p.Bpm, _rng);
        p.Orch.Dance = p.Dance;
        p.Center = mode.Size + 1;   // a step above the octave over the tonic: melodies hover round it

        var o = p.Orch;
        if (p.Dance != Dance.None)
        {
            ComposeDanceMotifs(p, index);
            PlanForm(p);
            _piece = p;
            _section = 0;
            _bar = 0;
            _lastPadVoicing = null;
            _observer?.Piece(_cursor, p);
            return;
        }
        p.M = Melodist.BestMotif(4, p.Meter, o.Liveliness, o.Chant, o.Fall, _rng, mode.Size);
        // The world's theme: one motif drawn once per seed, taken up by about a third of the pieces
        // (and always by the first), in their own key and metre. Endless music still has a tune that
        // comes back, which is what makes a soundtrack recognisably one game's.
        _theme ??= MakeTheme();
        if (!Fighting && (index == 0 || _rng.NextDouble() < 0.33))
        {
            p.M = _theme.For(p.Meter).ForModeSize(mode.Size);
            p.UsesTheme = true;
        }
        var orn = p.M.Ornamented(_rng);
        p.N = _rng.NextDouble() < 0.4 && orn.Count != p.M.Count ? orn.WithEnding(_rng.NextDouble() < 0.5 ? -1 : 1)
                                                               : Melodist.NewMotif(p.Meter, o.Liveliness, o.Chant, o.Fall, _rng, mode.Size);
        // The B section's motif is the A motif turned over, or a new one with the A motif's rhythm:
        // contrast that still sounds like the same piece.
        p.MB = _rng.NextDouble() < 0.5 ? p.M.Inverted()
             : new Motif(p.M.Rhythm, Melodist.Contour(p.M.Count, _rng, o.Chant, o.Fall, mode.Size));
        p.NB = Melodist.NewMotif(p.Meter, o.Liveliness, o.Chant, o.Fall, _rng, mode.Size);

        PlanForm(p);
        _piece = p;
        _section = 0;
        _bar = 0;
        _lastPadVoicing = null;
        _observer?.Piece(_cursor, p);
    }

    /// <summary>
    /// A dance tune's material: the A part's motif and its answer, and a B part with its own, as
    /// session tunes have (the B part is a new strain, usually higher, not the A turned over). The
    /// world's theme is taken up as a jig or a reel figure when its note count fits one.
    /// </summary>
    private void ComposeDanceMotifs(Piece p, int index)
    {
        var o = p.Orch;
        int size = p.Key.Mode.Size;
        _theme ??= MakeTheme();
        p.M = Melodist.DanceMotif(p.Dance, o.Fall, _rng, size);
        if (index == 0 || _rng.NextDouble() < 0.33)
        {
            if (_theme.AsDance(p.Dance, _rng) is { } t) { p.M = t.ForModeSize(size); p.UsesTheme = true; }
        }
        p.N = Melodist.DanceMotif(p.Dance, o.Fall, _rng, size);
        p.MB = Melodist.DanceMotif(p.Dance, o.Fall, _rng, size);
        p.NB = _rng.NextDouble() < 0.5 ? p.N : Melodist.DanceMotif(p.Dance, o.Fall, _rng, size);
    }

    private int WeightedIndex(double[] w)
    {
        double r = _rng.NextDouble() * w.Sum();
        for (int i = 0; i < w.Length; i++) { r -= w[i]; if (r <= 0) return i; }
        return w.Length - 1;
    }

    private static readonly string[] Forms =
    {
        "I A A' B A' O", "I A B A' C A' O", "A A' B A' B A O", "I A A' C A' O", "I A A' B B A' O", "A B A' C A' B A O",
    };

    private void PlanForm(Piece p)
    {
        bool cutShort = _cutShort;
        _cutShort = false;
        if (p.Dance != Dance.None) { PlanDanceForm(p, cutShort); return; }
        var o = p.Orch;
        string form = Forms[_rng.Next(Forms.Length)];
        if (Fighting) form = "A A' B A' B A'";
        // After a bridge the drone and pad have already been holding for a bar or two: an intro of
        // two to four more bars without the tune is heard as the music dropping out.
        if (cutShort && form.StartsWith("I ")) form = form[2..];
        p.Form = form;
        var tokens = form.Split(' ');
        int[] progA = Progressions.For(p.Key.Mode, false, _rng);
        int[] progB = Progressions.For(p.Key.Mode, true, _rng);
        int[] progC = Progressions.For(p.Key.Mode, _rng.NextDouble() < 0.5, _rng);

        // Layers enter one at a time across the piece: the extra layers in a shuffled order.
        var extras = new List<Part> { Part.Ostinato, Part.Counter };
        if (o.Percussion) extras.Add(Part.Perc);
        if (_rng.NextDouble() < 0.5) extras.Add(Part.Bass);
        Shuffle(extras);
        int added = 0;
        int aCount = 0;
        for (int k = 0; k < tokens.Length; k++)
        {
            var s = new Section { Label = tokens[k], Melody = o.Melody, Pad = o.Pad };
            switch (tokens[k])
            {
                case "I":
                    s.Kind = SectionKind.Intro;
                    s.Bars = _rng.NextDouble() < 0.5 ? 2 : 4;
                    s.Chords = Expand(new[] { 0, 0, progA[^1], 0 }, s.Bars);
                    s.Layers = new() { Part.Drone, Part.Pad };
                    if (_rng.NextDouble() < 0.5) s.Layers.Add(Part.Ostinato);
                    break;
                case "A":
                case "A'":
                    s.Kind = tokens[k] == "A" && aCount == 0 ? SectionKind.A : SectionKind.AVar;
                    aCount++;
                    s.Bars = 8;
                    s.Chords = Period(progA, close: true);
                    s.Layers = new() { Part.Drone, Part.Pad, Part.Melody };
                    // Each further A carries one more layer.
                    int want = Math.Min(extras.Count, aCount - 1 + (k > 2 ? 1 : 0));
                    added = Math.Max(added, want);
                    foreach (var e in extras.Take(added)) s.Layers.Add(e);
                    if (s.Kind == SectionKind.AVar && _rng.NextDouble() < 0.35) s.Melody = o.MelodyAlt;
                    if (s.Kind == SectionKind.AVar && _rng.NextDouble() < 0.3) s.OctaveShift = p.Key.Mode.Size;
                    break;
                case "B":
                    s.Kind = SectionKind.B;
                    s.Bars = 8;
                    s.Chords = Period(progB, close: false);
                    s.Layers = new() { Part.Pad, Part.Melody, Part.Counter };
                    if (_rng.NextDouble() < 0.6) s.Layers.Add(Part.Ostinato);
                    if (_rng.NextDouble() < 0.5) s.Layers.Add(Part.Bass); else s.Layers.Add(Part.Drone);
                    if (o.Percussion && _rng.NextDouble() < 0.5) s.Layers.Add(Part.Perc);
                    s.Pad = o.PadAlt;
                    s.Melody = _rng.NextDouble() < 0.5 ? o.MelodyAlt : o.Melody;
                    break;
                case "C":
                    // The breakdown: the tune leaves; ostinato, drone and bells, or a lone chant.
                    s.Kind = SectionKind.C;
                    // Without the tune it lasts four bars: eight is half a minute of no melody.
                    bool tuneless = _rng.NextDouble() < 0.5;
                    s.Bars = tuneless ? 4 : 4 + 4 * _rng.Next(2);
                    s.Chords = Expand(progC, s.Bars);
                    s.Layers = tuneless
                        ? new() { Part.Drone, Part.Ostinato, Part.Pad }
                        : new() { Part.Drone, Part.Melody };
                    s.Melody = o.MelodyAlt;
                    break;
                case "O":
                    s.Kind = SectionKind.Outro;
                    s.Bars = 4;
                    s.Chords = new[] { 0, progA[1], progA[^1] == 4 ? 4 : 6, 0 };
                    if (p.Key.Mode.Size == 5) s.Chords = new[] { 0, 3, 4, 0 };
                    s.Layers = new() { Part.Drone, Part.Pad, Part.Melody };
                    break;
            }
            // A tonic pedal under a chord holding a tone a semitone from it (the leading tone of a
            // harmonic-minor V, the bII of Phrygian) grinds for bars on end: such a section takes a
            // moving bass instead.
            if (s.Layers.Contains(Part.Drone) && !TonicFits(p.Key, s))
            {
                s.Layers.Remove(Part.Drone);
                s.Layers.Add(Part.Bass);
            }
            if (o.Bell && (s.Kind is SectionKind.Intro or SectionKind.C or SectionKind.B || _rng.NextDouble() < 0.25))
                s.Layers.Add(Part.Bell);
            if (o.Ambience) s.Layers.Add(Part.Ambience);
            s.Counter = (CounterStyle)_rng.Next(3);
            if (s.Kind == SectionKind.B && s.Counter == CounterStyle.Organum) s.Counter = CounterStyle.Tenor;
            p.Sections.Add(s);
        }
    }

    /// <summary>
    /// A dance tune as a session plays it: each part twice (A A B B), the whole tune twice round, then
    /// straight on into the next tune of the set with no outro. Two bars of the strummed chords and the
    /// drum lead in. The band grows as the tune goes round: lute and tune first, the drum on the
    /// repeat, the second instrument in the B part, everyone the second time through.
    /// </summary>
    private void PlanDanceForm(Piece p, bool cutShort)
    {
        var o = p.Orch;
        // After a cut the bridge has already led in.
        bool leadIn = _rng.NextDouble() < 0.6 && !cutShort;
        p.Form = (leadIn ? "I " : "") + "A A' B B' A' A' B' B'";
        int[] progA = Progressions.Dance(p.Key.Mode, false, _rng);
        int[] progB = Progressions.Dance(p.Key.Mode, true, _rng);
        bool pipesDrone = o.Drone == Instrument.Pipes;
        int round = 0, part = 0;
        foreach (var tok in p.Form.Split(' '))
        {
            var s = new Section { Label = tok, Melody = o.Melody, Pad = o.Pad, Counter = CounterStyle.Heterophony };
            if (tok == "I")
            {
                s.Kind = SectionKind.Intro;
                s.Bars = 2;
                s.Chords = new[] { 0, progA[^1] == 0 ? progA[1] : progA[^1] };
                s.Layers = new() { Part.Ostinato, Part.Perc };
                p.Sections.Add(s);
                continue;
            }
            bool isA = tok.StartsWith('A');
            if (part == 4) { round = 1; }
            s.Kind = part switch { 0 => SectionKind.A, 2 => SectionKind.B, _ => isA ? SectionKind.AVar : SectionKind.BVar };
            s.Bars = 8;
            s.Chords = Period(isA ? progA : progB, close: true);
            s.Layers = new() { Part.Melody, Part.Ostinato };
            if (part >= 1 || round == 1) s.Layers.Add(Part.Perc);
            if (!isA || round == 1) s.Layers.Add(Part.Counter);
            if (round == 1 && !isA) s.Layers.Add(Part.Bass);
            if (pipesDrone && isA && TonicFits(p.Key, s)) s.Layers.Add(Part.Drone);
            // The second time round, the instruments swap the lead now and then.
            if (round == 1 && _rng.NextDouble() < 0.5) s.Melody = o.MelodyAlt is Instrument.Fiddle or Instrument.Whistle or Instrument.Pipes ? o.MelodyAlt : o.Counter;
            p.Sections.Add(s);
            part++;
        }
    }

    private void Shuffle<T>(List<T> l)
    {
        for (int i = l.Count - 1; i > 0; i--) { int j = _rng.Next(i + 1); (l[i], l[j]) = (l[j], l[i]); }
    }

    private static int[] Expand(int[] prog, int bars)
    {
        var r = new int[bars];
        for (int i = 0; i < bars; i++) r[i] = prog[i * prog.Length / bars];
        return r;
    }

    /// <summary>Eight bars: the progression twice; a closing period comes home to the tonic.</summary>
    private static int[] Period(int[] prog, bool close)
    {
        var r = new int[8];
        for (int i = 0; i < 8; i++) r[i] = prog[i % 4];
        if (close) { r[7] = 0; if (r[6] == 0) r[6] = prog[^1] == 0 ? prog[1] : prog[^1]; }
        return r;
    }

    // ── Bars ──────────────────────────────────────────────────────────────────

    private void EmitBar(List<ScoreEvent> output)
    {
        if (_piece == null || _section >= _piece.Sections.Count) NewPiece();
        var p = _piece!;

        // A changed mood (a new place, a fight) cuts the piece short through a bridge.
        CheckDrift(p);
        var s = p.Sections[_section];
        if (_bar == 0) StartSection(p, s, output);

        double barStart = _cursor;
        double barLen = p.BarSeconds;
        int root = s.Chords[_bar];
        var key = p.Key;
        var chord = Chord.On(key, root);
        if (root != _lastChordRoot)
        {
            if (_lastChordRoot != int.MinValue)
                _observer?.Chord(_lastChordStart, barStart - _lastChordStart, _lastChordRoot, Chord.On(key, _lastChordRoot).PitchClasses(key));
            _lastChordRoot = root;
            _lastChordStart = barStart;
        }
        // Bars the chord lasts from here.
        int span = 1;
        while (_bar + span < s.Bars && s.Chords[_bar + span] == root) span++;
        bool chordStarts = _bar == 0 || s.Chords[_bar - 1] != root;

        float arc = SectionArc(p, s);

        foreach (var part in s.Layers)
        {
            switch (part)
            {
                case Part.Melody:
                    EmitLine(output, p, s, barStart, arc * _melodyBoost);
                    break;
                case Part.Counter:
                    EmitCounter(output, p, s, barStart, arc);
                    break;
                case Part.Pad when chordStarts:
                    EmitPad(output, p, s, chord, barStart, span * barLen, arc);
                    break;
                case Part.Bass when chordStarts:
                    output.Add(new(barStart, span * barLen + 0.15, Part.Bass, p.Orch.Bass, LowPitch(key, root, key.Tonic - 12), 0.6f * arc, 0f));
                    break;
                case Part.Ostinato:
                    EmitOstinato(output, p, chord, barStart, arc);
                    break;
                case Part.Perc:
                    EmitPerc(output, p, s, barStart, arc);
                    break;
            }
        }
        if (Traveling) EmitFootfalls(output, p, barStart);
        _melodyBoost = 1f + (_melodyBoost - 1f) * 0.5f;

        _cursor += barLen;
        if (++_bar >= s.Bars)
        {
            _bar = 0;
            _section++;
            if (_section >= p.Sections.Count && _lastChordRoot != int.MinValue)
            {
                _observer?.Chord(_lastChordStart, _cursor - _lastChordStart, _lastChordRoot, Chord.On(key, _lastChordRoot).PitchClasses(key));
                _lastChordRoot = int.MinValue;
            }
        }
    }

    /// <summary>The mood a new piece is written for: a fight hardens the place's own mood into battle.</summary>
    private MusicMoodState EffectiveMood() => Fighting
        ? new MusicMoodState(Math.Max(Mood.Coldness, 0.4f), Math.Max(Mood.Fear, 0.85f), Mood.Mystery * 0.5f, 1f, 0f)
        : Mood;

    private void CheckDrift(Piece p)
    {
        var m = EffectiveMood();
        float d = MathF.Sqrt(Sq(m.Coldness - p.Mood.Coldness) + Sq(m.Fear - p.Mood.Fear) + Sq(m.Mystery - p.Mood.Mystery)
                             + Sq(m.Festivity - p.Mood.Festivity));
        bool fightChanged = Fighting != p.Fight;
        // A new place is a new mood, and a new mood is a new piece: anything past a small change ends
        // this one, through a short bridge.
        if (d < 0.12f && !fightChanged) return;
        // A small change waits for the half phrase to end; a large one (a fight, the world map, a
        // dungeon after a tavern) cuts at the bar line.
        bool large = d > 0.35f || fightChanged;
        if (!large && _bar % 2 != 0) return;
        var cur = p.Sections[_section];
        if (cur.Kind == SectionKind.Bridge) return;
        // Finish here: drop the rest, add a bridge of held tonic, and let the next piece begin.
        _cutShort = true;
        if (_bar > 0) { cur.Bars = _bar; cur.Chords = cur.Chords[.._bar]; _section++; }
        p.Sections.RemoveRange(_section, p.Sections.Count - _section);
        // Two bars only when bars are short: at a slow tempo two are eight seconds of waiting.
        int bars = large || p.BarSeconds > 3 ? 1 : 2;
        var bridge = new Section
        {
            Kind = SectionKind.Bridge, Label = "~", Bars = bars, Chords = Enumerable.Repeat(0, bars).ToArray(),
            Layers = new() { Part.Drone, Part.Pad }, Pad = p.Orch.Pad, Melody = cur.Melody,
        };
        // If the tune was singing, it does not stop dead: it comes to rest on the nearest tonic and
        // holds it across the bridge, so the change is heard as an ending, not a dropout.
        if (_cursor - _lastSungAt < 2 * p.BarSeconds)
        {
            bridge.Layers.Add(Part.Melody);
            int home = NearestTonic(p.Key, _lastSung, _lastSung - 4, _lastSung + 4);
            bridge.MelodyLine = new() { new LineNote(0, bars * p.Meter - 0.25, home, 0.7f) };
        }
        p.Sections.Add(bridge);
        _bar = 0;
    }

    private static float Sq(float x) => x * x;

    /// <summary>The piece's arc: quieter at the edges, fullest at the last A.</summary>
    private float SectionArc(Piece p, Section s)
    {
        int i = p.Sections.IndexOf(s), n = p.Sections.Count;
        float pos = n <= 1 ? 1f : i / (float)(n - 1);
        float arc = 0.8f + 0.25f * MathF.Sin(MathF.PI * MathF.Min(1f, pos * 1.15f));
        // Within the section: a phrase swells to its third bar and settles.
        float inPhrase = (_bar % 4) switch { 0 => 0.92f, 1 => 0.97f, 2 => 1.04f, _ => 0.95f };
        return arc * inPhrase;
    }

    private void StartSection(Piece p, Section s, List<ScoreEvent> output)
    {
        _observer?.Section(_cursor, s, p);
        double t = _cursor;
        double len = s.Bars * p.BarSeconds;
        var key = p.Key;

        // Layers in and out as swells, never cuts.
        foreach (Part part in new[] { Part.Drone, Part.Pad, Part.Melody, Part.Counter, Part.Ostinato, Part.Bass, Part.Perc, Part.Bell, Part.Ambience })
        {
            bool on = s.Layers.Contains(part);
            output.Add(new(t, 0, part, default, 0, on ? Orchestra.PartLevel(part) : 0f, 0, IsGain: true,
                           FadeSeconds: (float)(on ? p.BarSeconds * 0.5 : p.BarSeconds * 1.5)));
        }

        // The drone: tonic and fifth, held across the whole section.
        if (s.Layers.Contains(Part.Drone))
        {
            int low = LowPitch(key, 0, key.Tonic - 12);
            output.Add(new(t, len + 0.6, Part.Drone, p.Orch.Drone, low, 0.7f, -0.2f));
            if (FifthFits(key, s))
                output.Add(new(t, len + 0.6, Part.Drone, p.Orch.Drone, low + 7, 0.45f, 0.2f));
        }
        if (s.Layers.Contains(Part.Bell))
        {
            int deg = _rng.NextDouble() < 0.6 ? 0 : 4;
            output.Add(new(t, 4, Part.Bell, Instrument.Bell, key.Pitch(deg + 2 * key.Mode.Size), 0.5f, 0.5f));
            if (_rng.NextDouble() < 0.5)
                output.Add(new(t + p.BarSeconds * 2, 4, Part.Bell, Instrument.Bell, key.Pitch(4 + key.Mode.Size), 0.4f, -0.5f));
        }
        if (s.Layers.Contains(Part.Ambience))
        {
            output.Add(new(t, len, Part.Ambience, Instrument.Wind, key.Tonic + 24, 0.6f, -0.7f));
            output.Add(new(t + p.BarSeconds, len, Part.Ambience, Instrument.Wind, key.Tonic + 31, 0.5f, 0.7f));
        }

        PlanLines(p, s);
    }

    /// <summary>
    /// Whether the drone's fifth can sound under the whole section: not when a chord there has a tone
    /// a semitone from it (the bVI over a fifth pedal, the diminished tonic of Locrian).
    /// </summary>
    private static bool FifthFits(Key key, Section s)
    {
        int fifth = Key.Mod(key.Tonic + 7, 12);
        foreach (int root in s.Chords.Distinct())
            foreach (int pc in Chord.On(key, root).PitchClasses(key))
            {
                int d = Key.Mod(pc - fifth, 12);
                if (d == 1 || d == 11 || d == 6) return false;
            }
        return true;
    }

    private static bool TonicFits(Key key, Section s)
    {
        int tonic = Key.Mod(key.Tonic, 12);
        foreach (int root in s.Chords.Distinct())
            foreach (int pc in Chord.On(key, root).PitchClasses(key))
            {
                int d = Key.Mod(pc - tonic, 12);
                if (d == 1 || d == 11) return false;
            }
        return true;
    }

    // ── Lines ─────────────────────────────────────────────────────────────────

    private void PlanLines(Piece p, Section s)
    {
        var key = p.Key;
        int n = key.Mode.Size;
        int lo = p.Center - n + 2, hi = p.Center + n + 2;   // a little under the octave below to a ninth above
        // A dance tune's B part is a strain higher: its range moves up a third.
        int lift = p.Dance != Dance.None ? 2 : 0;
        s.MelodyLine = s.Kind switch
        {
            SectionKind.A => p.ALine = BuildPeriod(p, s, p.M, p.N, lo, hi, close: true),
            SectionKind.AVar => VaryA(p, s, lo, hi),
            SectionKind.B when p.Dance != Dance.None => p.BLine = BuildPeriod(p, s, p.MB, p.NB, lo + lift, hi + lift, close: true, lift: lift),
            SectionKind.BVar => p.BLine != null ? new List<LineNote>(p.BLine) : BuildPeriod(p, s, p.MB, p.NB, lo + lift, hi + lift, close: true, lift: lift),
            SectionKind.B => BuildPeriod(p, s, p.MB, p.NB, lo, hi, close: false),
            SectionKind.C => Chant(p, s, lo, hi),
            SectionKind.Outro => Outro(p, s, lo, hi),
            SectionKind.Bridge => s.MelodyLine,
            _ => new List<LineNote>(),
        };
        if (s.OctaveShift != 0)
        {
            var shifted = s.MelodyLine.Select(x => { x.Degree += s.OctaveShift; return x; }).ToList();
            // Only if it still sings: the top must stay under about A5.
            if (shifted.Count == 0 || key.Pitch(shifted.Max(x => x.Degree)) <= 81) s.MelodyLine = shifted;
        }
        s.CounterLine = s.Layers.Contains(Part.Counter) ? BuildCounter(p, s) : new();
    }

    private List<LineNote> BuildPeriod(Piece p, Section s, Motif m, Motif n2, int lo, int hi, bool close, int lift = 0)
    {
        double[] Cadence() => p.Dance != Dance.None ? Rhythms.DanceCadence(p.Dance, _rng) : Rhythms.Cadence(p.Meter, _rng);
        var key = p.Key;
        var line = new List<LineNote>();
        int meter = p.Meter;
        Chord C(int bar) => Chord.On(key, s.Chords[bar]);
        // Where a motif may start so that all of it stays in range, on a tone of the bar's chord,
        // as near as possible to where the line wants to go.
        int Anchor(int bar, Motif mo, int near, bool stable = false) =>
            Melodist.NearestChordTone(key, C(bar), near, lo - mo.Low, hi - mo.High, _rng, stable);
        int Last() => line[^1].Degree;
        int StepNear() => _rng.NextDouble() < 0.55 ? -1 : 1;

        // Phrase one. A B section starts higher: contrast by register as well as by harmony.
        int a0 = Anchor(0, m, p.Center + lift + (close ? (_rng.NextDouble() < 0.5 ? 0 : -2) : 2), stable: true);
        Melodist.PlaceBar(line, m, 0, a0, key, C(0), meter, lo, hi, 0.9f);
        // Bar two: the motif again a step away (a sequence) or in place with its ending turned.
        var m2 = _rng.NextDouble() < 0.5 ? m : m.WithEnding(_rng.NextDouble() < 0.6 ? -1 : 1);
        int[] seqSteps = { -1, 1, -2, 0, 2 };
        int a1 = Anchor(1, m2, a0 + seqSteps[_rng.Next(seqSteps.Length)]);
        Melodist.PlaceBar(line, m2, meter, a1, key, C(1), meter, lo, hi, 0.95f);
        // Bar three: the second idea, starting a step from where the line is.
        int a2 = Anchor(2, n2, Last() + StepNear());
        Melodist.PlaceBar(line, n2, 2 * meter, a2, key, C(2), meter, lo, hi, 1f);
        // Bar four: an open cadence on a tone of its chord, not the tonic, then a breath.
        int openTarget = Melodist.NearestChordTone(key, C(3), Last() - 1, lo + 1, hi, _rng);
        if (Key.Mod(openTarget, key.Mode.Size) == 0)
            openTarget = Melodist.NearestChordTone(key, C(3), openTarget + 1, openTarget + 1, hi, _rng);
        Melodist.PlaceCadence(line, Cadence(), 3 * meter, openTarget, lo, hi, _rng, 0.9f);
        if (p.Dance == Dance.None) Breathe(line, meter);   // a dance tune does not stop for breath

        // Phrase two. When the harmony allows, the whole phrase again with only the ending changed:
        // the estampie's punctum, open ending then closed, and the commonest form in folk tunes.
        // Otherwise the same opening, so it is heard again, and a new turn.
        bool sameOpening = s.Chords[4] == s.Chords[0] && s.Chords[5] == s.Chords[1];
        bool sameThree = sameOpening && s.Chords[6] == s.Chords[2] && _rng.NextDouble() < 0.85;
        int copyBars = sameThree ? 3 : sameOpening ? 2 : 0;
        foreach (var x in line.Where(x => x.Beat < copyBars * meter - 1e-6).ToList())
            line.Add(new LineNote(x.Beat + 4 * meter, x.Dur, x.Degree, x.Vel));
        if (copyBars == 0)
        {
            Melodist.PlaceBar(line, m, 4 * meter, Anchor(4, m, a0), key, C(4), meter, lo, hi, 0.9f);
            Melodist.PlaceBar(line, m2, 5 * meter, Anchor(5, m2, a1), key, C(5), meter, lo, hi, 0.95f);
        }
        if (copyBars < 3)
        {
            var n3 = _rng.NextDouble() < 0.5 ? n2.Ornamented(_rng) : (_rng.NextDouble() < 0.5 ? m.Inverted() : n2);
            Melodist.PlaceBar(line, n3, 6 * meter, Anchor(6, n3, Last() + StepNear()), key, C(6), meter, lo, hi, 1.05f);
        }
        // The close comes down to the tonic nearest the middle of the tune; an open end to its chord.
        int closeTarget = close
            ? NearestTonic(key, Math.Min(Last(), p.Center + 1 + lift), lo, hi)
            : Melodist.NearestChordTone(key, C(7), Last() - 1, lo, hi, _rng, preferStable: true);
        Melodist.PlaceCadence(line, Cadence(), 7 * meter, closeTarget, lo, hi, _rng, 0.85f);
        if (p.Dance == Dance.None) Breathe(line, meter);
        return line;
    }

    /// <summary>The phrase's last note gives back a beat (half in triple time): the breath between phrases.</summary>
    private static void Breathe(List<LineNote> line, int meter)
    {
        var x = line[^1];
        double give = meter == 3 ? 1.0 : 1.0;
        if (x.Dur >= give + 1.0) { x.Dur -= give; line[^1] = x; }
        else if (x.Dur >= 1.0) { x.Dur *= 0.6; line[^1] = x; }
    }

    private static int NearestTonic(Key key, int near, int lo, int hi)
    {
        int n = key.Mode.Size, best = near, bd = int.MaxValue;
        for (int d = lo; d <= hi; d++)
            if (Key.Mod(d, n) == 0 && Math.Abs(d - near) < bd) { bd = Math.Abs(d - near); best = d; }
        return best;
    }

    /// <summary>A' : the A melody again, varied: ornamented here and there, an ending turned, or plain.</summary>
    private List<LineNote> VaryA(Piece p, Section s, int lo, int hi)
    {
        if (p.ALine == null) return p.ALine = BuildPeriod(p, s, p.M, p.N, lo, hi, close: true);
        var src = p.ALine;
        double r = _rng.NextDouble();
        if (r < 0.6) return new List<LineNote>(src);    // a plain restatement: the recognisable return
        var key = p.Key;
        var outLine = new List<LineNote>();
        int meter = p.Meter;
        for (int i = 0; i < src.Count; i++)
        {
            var x = src[i];
            int bar = (int)(x.Beat / meter);
            // Ornament: a long note in bars 2, 3, 6 or 7 becomes two, the second a neighbour or passing tone.
            bool canOrnament = (bar % 4 == 1 || bar % 4 == 2) && x.Dur >= 1.0 && i + 1 < src.Count;
            if (canOrnament && _rng.NextDouble() < 0.25)
            {
                double half = x.Dur >= 2 ? 1.0 : x.Dur / 2;
                int next = src[i + 1].Degree;
                int orn = next == x.Degree ? x.Degree + (_rng.NextDouble() < 0.6 ? -1 : 1) : x.Degree + Math.Sign(next - x.Degree);
                if (Math.Abs(next - x.Degree) > 2) orn = x.Degree + (_rng.NextDouble() < 0.5 ? 1 : -1);
                outLine.Add(new LineNote(x.Beat, x.Dur - half, x.Degree, x.Vel));
                outLine.Add(new LineNote(x.Beat + x.Dur - half, half, Math.Clamp(orn, lo, hi), x.Vel * 0.9f));
            }
            else outLine.Add(x);
        }
        // Sometimes the second phrase's third bar takes a new turn.
        if (r > 0.85)
        {
            var c6 = Chord.On(key, s.Chords[6]);
            outLine.RemoveAll(x => x.Beat >= 6 * meter && x.Beat < 7 * meter);
            var tmp = new List<LineNote>();
            int prev = outLine.Where(x => x.Beat < 6 * meter).Select(x => x.Degree).DefaultIfEmpty(p.Center).Last();
            Melodist.PlaceBar(tmp, p.M.Retrograde(), 6 * meter, Melodist.NearestChordTone(key, c6, prev, lo, hi - 3, _rng), key, c6, meter, lo, hi, 1.05f);
            outLine.AddRange(tmp);
            outLine.Sort((a, b) => a.Beat.CompareTo(b.Beat));
        }
        return outLine;
    }

    /// <summary>C: the motif at half speed, chant-like, over the drone.</summary>
    private List<LineNote> Chant(Piece p, Section s, int lo, int hi)
    {
        if (!s.Layers.Contains(Part.Melody)) return new();
        var line = new List<LineNote>();
        var key = p.Key;
        double beat = 0;
        int anchor = Melodist.NearestChordTone(key, Chord.On(key, s.Chords[0]), p.Center - 1, lo, hi - 4, _rng, true);
        for (int rep = 0; beat < s.Bars * p.Meter - 1e-6; rep++)
        {
            var m = rep % 2 == 0 ? p.M : p.M.WithEnding(-1);
            for (int i = 0; i < m.Count && beat < s.Bars * p.Meter - 1e-6; i++)
            {
                double d = Math.Min(m.Rhythm[i] * 2, s.Bars * p.Meter - beat);
                line.Add(new LineNote(beat, d, Math.Clamp(anchor + m.Contour[i], lo, hi), 0.85f));
                beat += d;
            }
        }
        // Come to rest on the tonic.
        var lastN = line[^1];
        lastN.Degree = NearestTonic(key, lastN.Degree, lo, hi);
        line[^1] = lastN;
        return line;
    }

    private List<LineNote> Outro(Piece p, Section s, int lo, int hi)
    {
        var key = p.Key;
        var line = new List<LineNote>();
        int anchor = p.ALine != null && p.ALine.Count > 0 ? p.ALine[0].Degree : p.Center;
        // The motif once more, slowly, then the long tonic.
        double beat = 0;
        foreach (var (d, c) in p.M.Rhythm.Zip(p.M.Contour))
        {
            line.Add(new LineNote(beat, d * 2, Math.Clamp(anchor + c, lo, hi), 0.75f));
            beat += d * 2;
        }
        int tonic = NearestTonic(key, line[^1].Degree, lo, hi);
        line.Add(new LineNote(beat, s.Bars * p.Meter - beat, tonic, 0.7f));
        return line;
    }

    private List<(double beat, double dur, int midi, float vel)> BuildCounter(Piece p, Section s)
    {
        var key = p.Key;
        var outLine = new List<(double, double, int, float)>();
        var mel = s.MelodyLine;
        int meter = p.Meter;
        switch (s.Counter)
        {
            case CounterStyle.Heterophony when mel.Count > 0:
                // The same tune on the second instrument: a run's quick notes now and then left out
                // (the note before is held through them), the rest note for note.
                for (int i = 0; i < mel.Count; i++)
                {
                    var x = mel[i];
                    double dur = x.Dur;
                    while (i + 1 < mel.Count && mel[i + 1].Dur < 0.5 && _rng.NextDouble() < 0.18)
                        dur += mel[++i].Dur;
                    outLine.Add((x.Beat, dur, key.Pitch(x.Degree), x.Vel * 0.75f));
                }
                break;
            case CounterStyle.Organum when mel.Count > 0:
                foreach (var x in mel)
                {
                    int m = key.Pitch(x.Degree);
                    int c = m - 7;
                    if (!key.Contains(c)) c = m - 5;
                    if (!key.Contains(c)) c = key.Pitch(x.Degree - 3);
                    outLine.Add((x.Beat, x.Dur, c, x.Vel * 0.8f));
                }
                break;
            case CounterStyle.Echo when mel.Count > 0:
                // Answer in the bars where the melody holds: a fragment of the motif an octave down.
                for (int bar = 0; bar < s.Bars; bar++)
                {
                    double b0 = bar * meter, b1 = b0 + meter;
                    var inBar = mel.Where(x => x.Beat >= b0 && x.Beat < b1).ToList();
                    var held = inBar.LastOrDefault();
                    if (inBar.Count == 0 || held.Dur < 2) continue;
                    var chord = Chord.On(key, s.Chords[bar]);
                    int start = Melodist.NearestChordTone(key, chord, held.Degree - key.Mode.Size, held.Degree - key.Mode.Size - 3, held.Degree - 2, _rng);
                    double t = held.Beat + 0.5;
                    for (int i = 0; i < Math.Min(3, p.M.Count) && t < b1 + meter * 0.5; i++)
                    {
                        double d = Math.Min(p.M.Rhythm[i], 1.0);
                        outLine.Add((t, d * 0.95, key.Pitch(start + p.M.Contour[i]), 0.75f));
                        t += d;
                    }
                }
                break;
            default:
                // Tenor: one chord tone per half bar (per bar in triple time), moving against the melody.
                double step = meter == 3 ? 3 : 2;
                int prev = key.DegreeAtOrBelow(key.Tonic + 4);
                int prevMel = p.Center;
                for (double b = 0; b < s.Bars * meter - 1e-6; b += step)
                {
                    int bar = (int)(b / meter);
                    var chord = Chord.On(key, s.Chords[bar]);
                    var melHere = mel.Where(x => x.Beat >= b && x.Beat < b + step).Select(x => x.Degree).DefaultIfEmpty(prevMel).Average();
                    int dir = melHere > prevMel ? -1 : melHere < prevMel ? 1 : 0;
                    int target = prev + (dir == 0 ? (_rng.NextDouble() < 0.5 ? 1 : -1) : dir);
                    int lo = key.DegreeAtOrBelow(key.Tonic - 5), hi = key.DegreeAtOrBelow(key.Tonic + 7);
                    int d = Melodist.NearestChordTone(key, chord, target, lo, hi, _rng);
                    // Never on the melody's pitch class at the downbeat: that is a doubling, not a voice.
                    if (Key.Mod(d - (int)Math.Round(melHere), key.Mode.Size) == 0)
                        d = Melodist.NearestChordTone(key, chord, d + 2, lo, hi, _rng);
                    // A tone held over from the half bar before is tied, not struck again.
                    if (outLine.Count > 0 && outLine[^1].Item3 == key.Pitch(d) && outLine[^1].Item1 + outLine[^1].Item2 >= b - 0.1)
                    {
                        var held = outLine[^1];
                        outLine[^1] = (held.Item1, held.Item2 + step, held.Item3, held.Item4);
                    }
                    else outLine.Add((b, step * 0.97, key.Pitch(d), 0.7f));
                    prev = d;
                    prevMel = (int)Math.Round(melHere);
                }
                break;
        }
        return outLine;
    }

    // ── Emission per part ────────────────────────────────────────────────────

    private void EmitLine(List<ScoreEvent> output, Piece p, Section s, double barStart, float arc)
    {
        double b0 = _bar * p.Meter, b1 = b0 + p.Meter;
        var key = p.Key;
        float pan = -0.25f;
        int up = Transposition(s.Melody);
        foreach (var x in s.MelodyLine)
        {
            if (x.Beat < b0 - 1e-6 || x.Beat >= b1 - 1e-6) continue;
            double t = barStart + (x.Beat - b0 + Lilt(p, x.Beat - b0)) * p.BeatSeconds + Humanise();
            _lastSung = x.Degree;
            _lastSungAt = t + x.Dur * p.BeatSeconds;
            if (p.Dance != Dance.None && s.Kind != SectionKind.Bridge)
            {
                EmitDanceNote(output, p, s.Melody, Part.Melody, t, x, b0, arc, pan, up);
                continue;
            }
            // Legato: a note lasts to the next one, a breath shorter on wind and voice.
            double dur = x.Dur * p.BeatSeconds * (IsPlucked(s.Melody) ? 1.0 : 0.95);
            bool downbeat = Math.Abs(x.Beat - b0) < 1e-6;
            float v = x.Vel * arc * (downbeat ? 1.05f : 0.95f) * Orchestra.Trim(s.Melody) * Jitter(0.06f);
            // A mordent now and then on a long note of a recorder, harpsichord or lute in the lighter
            // palettes: the note, the step above, the note, all inside the first eighth of a beat each.
            if (x.Dur >= 1.0 && OrnamentsSuit(p, s.Melody) && _rng.NextDouble() < 0.18)
            {
                double g = p.BeatSeconds * 0.125;
                output.Add(new(t, g, Part.Melody, s.Melody, key.Pitch(x.Degree), Math.Clamp(v * 0.9f, 0.05f, 1f), pan));
                output.Add(new(t + g, g, Part.Melody, s.Melody, key.Pitch(x.Degree + 1), Math.Clamp(v * 0.8f, 0.05f, 1f), pan));
                t += 2 * g;
                dur -= 2 * g;
            }
            output.Add(new(t, dur, Part.Melody, s.Melody, key.Pitch(x.Degree) + up, Math.Clamp(v, 0.05f, 1f), pan));
        }
    }

    private void EmitCounter(List<ScoreEvent> output, Piece p, Section s, double barStart, float arc)
    {
        double b0 = _bar * p.Meter, b1 = b0 + p.Meter;
        var inst = p.Orch.Counter == s.Melody ? p.Orch.MelodyAlt : p.Orch.Counter;
        if (s.Counter == CounterStyle.Organum) inst = s.Melody == Instrument.Choir ? Instrument.Choir : p.Orch.Counter;
        if (s.Counter == CounterStyle.Heterophony && inst == s.Melody)
            inst = s.Melody == Instrument.Fiddle ? Instrument.Whistle : Instrument.Fiddle;
        int up = Transposition(inst);
        foreach (var (beat, dur, midi, vel) in s.CounterLine)
        {
            if (beat < b0 - 1e-6 || beat >= b1 - 1e-6) continue;
            double t = barStart + (beat - b0 + Lilt(p, beat - b0)) * p.BeatSeconds + Humanise();
            output.Add(new(t, dur * p.BeatSeconds * (p.Dance != Dance.None ? 0.92 : 1.0), Part.Counter, inst, midi + up,
                           Math.Clamp(vel * arc * Orchestra.Trim(inst) * Jitter(0.05f), 0.05f, 1f), 0.35f));
        }
    }

    private void EmitPad(List<ScoreEvent> output, Piece p, Section s, Chord chord, double t, double len, float arc)
    {
        var key = p.Key;
        int[] voicing;
        int root = LowPitch(key, chord.Root, key.Tonic - 10);   // under the melody
        if (p.Orch.OpenVoicing)
        {
            // Root, fifth, octave: hollow, the organum chord. The fifth is the mode's own.
            int fifth = key.Pitch(key.DegreeAtOrBelow(root) + (key.Mode.Size == 5 ? 3 : 4));
            voicing = _rng.NextDouble() < 0.3 ? new[] { root, fifth, root + 12, key.Pitch(key.DegreeAtOrBelow(root) + key.Mode.Size + 2) }
                                              : new[] { root, fifth, root + 12 };
        }
        else
        {
            // A triad near the last one: each voice moves as little as it can.
            var pcs = chord.Tones.Select(d => key.Pitch(d)).ToArray();
            var options = new List<int[]>();
            for (int inv = 0; inv < 3; inv++)
            {
                var v = new int[3];
                for (int k = 0; k < 3; k++) v[k] = pcs[(k + inv) % 3] + ((k + inv) >= 3 ? 12 : 0);
                foreach (int shift in new[] { -24, -12, 0, 12 })
                {
                    var w = v.Select(x => x + shift).ToArray();
                    if (w.Min() >= key.Tonic - 8 && w.Max() <= key.Tonic + 8) options.Add(w);
                }
            }
            if (options.Count == 0) options.Add(pcs);
            voicing = _lastPadVoicing == null
                ? options[_rng.Next(options.Count)]
                : options.OrderBy(o => o.Zip(_lastPadVoicing, (a, b) => Math.Abs(a - b)).Sum() + _rng.NextDouble()).First();
        }
        _lastPadVoicing = voicing.Length == 3 ? voicing : _lastPadVoicing;
        float vel = 0.55f * arc * Orchestra.Trim(s.Pad);
        for (int i = 0; i < voicing.Length; i++)
            output.Add(new(t, len + 0.25, Part.Pad, s.Pad, voicing[i], vel * (i == 0 ? 1f : 0.85f), (i - 1) * 0.6f));
    }

    private void EmitOstinato(List<ScoreEvent> output, Piece p, Chord chord, double barStart, float arc)
    {
        var o = p.Orch;
        var key = p.Key;
        if (o.OstinatoStyle == OstinatoStyle.Comp) { EmitComp(output, p, chord, barStart, arc); return; }
        int n = key.Mode.Size;
        // Chord tones as degrees, the root in the octave of the tonic (low for a pulse).
        int rootDeg = chord.Root;
        while (key.Pitch(rootDeg) > key.Tonic + 4) rootDeg -= n;
        while (key.Pitch(rootDeg) < key.Tonic - 8) rootDeg += n;
        if (o.OstinatoStyle == OstinatoStyle.Pulse) rootDeg -= n;
        int[] tones = { rootDeg, chord.Tones[1] - chord.Root + rootDeg, chord.Tones[2] - chord.Root + rootDeg, rootDeg + n };
        var inst = o.Ostinato;
        double step = o.OstinatoStep;
        int count = (int)Math.Round(p.Meter / step);
        for (int i = 0; i < count; i++)
        {
            int idx = o.OstinatoPattern[i % o.OstinatoPattern.Length];
            double t = barStart + i * step * p.BeatSeconds + Humanise() * 0.5;
            bool down = i == 0, mid = Math.Abs(i * step - p.Meter / 2.0) < 1e-6;
            float v = (down ? 0.75f : mid ? 0.62f : 0.5f) * arc * Orchestra.Trim(inst) * Jitter(0.08f);
            double dur = inst is Instrument.Strings ? step * p.BeatSeconds * 0.8 : step * p.BeatSeconds * 1.6;
            output.Add(new(t, dur, Part.Ostinato, inst, key.Pitch(tones[idx]), v, (i % 2 == 0 ? -0.5f : 0.5f)));
        }
    }

    private void EmitPerc(List<ScoreEvent> output, Piece p, Section s, double barStart, float arc)
    {
        var key = p.Key;
        double beat = p.BeatSeconds;
        if (p.Orch.Perc == Instrument.Bodhran) { EmitBodhran(output, p, barStart, arc); return; }
        if (p.Orch.Perc == Instrument.HandDrum)
        {
            // The tavern's frame drum: strong one, lighter offbeats, a little skip before the bar line.
            (double at, float v)[] pat = p.Meter == 3
                ? new[] { (0.0, 0.8f), (1.0, 0.35f), (2.0, 0.5f), (2.5, 0.3f) }
                : new[] { (0.0, 0.8f), (1.0, 0.3f), (1.5, 0.35f), (2.0, 0.6f), (3.0, 0.3f), (3.5, 0.4f) };
            foreach (var (at, v) in pat)
                output.Add(new(barStart + at * beat + Humanise() * 0.5, 0.3, Part.Perc, Instrument.HandDrum, key.Tonic - 10, v * arc * Jitter(0.1f), 0.2f));
            return;
        }
        // Timpani: on the section's first bar and on cadences; in a fight, a processional beat.
        int low = key.Tonic - 12, fifth = low + 7;
        if (Fighting || p.Mood.Fear > 0.8f)
        {
            for (int i = 0; i < p.Meter; i++)
                output.Add(new(barStart + i * beat, 1.0, Part.Perc, Instrument.Timpani, i % 2 == 0 ? low : fifth, (i == 0 ? 0.8f : 0.45f) * arc, 0f));
            return;
        }
        if (_bar == 0)
            output.Add(new(barStart, 2.0, Part.Perc, Instrument.Timpani, low, 0.75f * arc, 0f));
        else if (_bar % 4 == 3)
        {
            output.Add(new(barStart + (p.Meter - 1) * beat, 1.0, Part.Perc, Instrument.Timpani, fifth, 0.5f * arc, 0f));
        }
    }

    private void EmitFootfalls(List<ScoreEvent> output, Piece p, double barStart)
    {
        // Uneven ground: two to four soft steps a bar, off the grid.
        int steps = 2 + _rng.Next(3);
        for (int i = 0; i < steps; i++)
        {
            double t = barStart + p.BarSeconds * (i + _rng.NextDouble() * 0.6) / steps;
            output.Add(new(t, 0.2, Part.Perc, Instrument.HandDrum, 33 + _rng.Next(6), 0.25f + (float)_rng.NextDouble() * 0.15f, 0f));
        }
    }

    // ── The session ──────────────────────────────────────────────────────────

    /// <summary>
    /// The lilt, in beats: a reel's off-beat eighths a touch late, a jig's first eighth of each three
    /// held a touch long. Played straight, either sounds like a machine.
    /// </summary>
    private static double Lilt(Piece p, double beatInBar)
    {
        if (p.Dance == Dance.None) return 0;
        double f = beatInBar - Math.Floor(beatInBar + 1e-6);
        if (p.Dance == Dance.Reel) return Math.Abs(f - 0.5) < 1e-3 ? 0.06 : 0;
        if (Math.Abs(f - 1.0 / 3) < 1e-3) return 0.04;
        if (Math.Abs(f - 2.0 / 3) < 1e-3) return 0.02;
        return 0;
    }

    /// <summary>The whistle sounds an octave above the line, where a tin whistle lives.</summary>
    private static int Transposition(Instrument i) => i == Instrument.Whistle ? 12 : 0;

    /// <summary>
    /// One note of a dance tune, with the session's ornaments: a cut (a grace note above, a flick of a
    /// finger) on a beat, or a roll (cut, note, tap below, note) on a held note. Accents on the jig's
    /// two beats and the reel's first and third.
    /// </summary>
    private void EmitDanceNote(List<ScoreEvent> output, Piece p, Instrument inst, Part part, double t, LineNote x, double b0,
                               float arc, float pan, int up)
    {
        var key = p.Key;
        double inBar = x.Beat - b0;
        bool onBeat = Math.Abs(inBar - Math.Round(inBar)) < 1e-3 && (p.Dance == Dance.Jig || Math.Round(inBar) % 2 == 0);
        double dur = x.Dur * p.BeatSeconds * (IsPlucked(inst) ? 1.0 : 0.9);
        float v = Math.Clamp(x.Vel * arc * (onBeat ? 1.08f : 0.9f) * Orchestra.Trim(inst) * Jitter(0.05f), 0.05f, 1f);
        int midi = key.Pitch(x.Degree) + up;
        bool ornaments = inst is Instrument.Whistle or Instrument.Fiddle or Instrument.Pipes;
        const double grace = 0.03;
        if (ornaments && x.Dur >= 0.99 && _rng.NextDouble() < 0.35 && dur > 0.3)
        {
            // A roll: note, cut, note, tap, note.
            double a = dur * 0.28;
            output.Add(new(t, a, part, inst, midi, v, pan, LogDur: dur));
            output.Add(new(t + a, grace, part, inst, key.Pitch(x.Degree + 1) + up, v * 0.7f, pan, Ornament: true));
            output.Add(new(t + a + grace, a, part, inst, midi, v * 0.9f, pan, Ornament: true));
            output.Add(new(t + 2 * a + grace, grace, part, inst, key.Pitch(x.Degree - 1) + up, v * 0.7f, pan, Ornament: true));
            output.Add(new(t + 2 * a + 2 * grace, dur - 2 * a - 2 * grace, part, inst, midi, v * 0.9f, pan, Ornament: true));
            return;
        }
        if (ornaments && onBeat && _rng.NextDouble() < 0.25)
        {
            output.Add(new(t, grace, part, inst, key.Pitch(x.Degree + 1) + up, v * 0.7f, pan, Ornament: true));
            t += grace;
            dur -= grace;
        }
        output.Add(new(t, dur, part, inst, midi, v, pan));
    }

    /// <summary>
    /// The strummed accompaniment: the bass note on the beat and the chord off it, boom-chick. A jig
    /// strums on the third eighth of each beat; a reel on the second and fourth beats, with a light
    /// stroke before the bar line. The chord's notes are spread a few milliseconds, as a strum is.
    /// </summary>
    private void EmitComp(List<ScoreEvent> output, Piece p, Chord chord, double barStart, float arc)
    {
        var key = p.Key;
        var inst = p.Orch.Ostinato;
        // The bass strings of a bouzouki or a guitar: low enough to be the floor of the room.
        int bass = LowPitch(key, chord.Root, key.Tonic - 17);
        int fifth = LowPitch(key, chord.Tones[2], key.Tonic - 17);
        var tones = chord.Tones.Select(d => LowPitch(key, d, key.Tonic - 3)).OrderBy(m => m).ToArray();
        (double at, bool isBass, int low, float v)[] pat = p.Dance == Dance.Jig
            ? new[] { (0.0, true, bass, 0.8f), (2.0 / 3, false, 0, 0.55f), (1.0, true, fifth, 0.7f), (5.0 / 3, false, 0, 0.55f) }
            : new[] { (0.0, true, bass, 0.8f), (1.0, false, 0, 0.55f), (2.0, true, fifth, 0.7f), (3.0, false, 0, 0.55f), (3.5, false, 0, 0.35f) };
        foreach (var (at, isBass, low, v) in pat)
        {
            double t = barStart + (at + Lilt(p, at)) * p.BeatSeconds + Humanise() * 0.5;
            float vel = v * arc * Orchestra.Trim(inst) * Jitter(0.08f);
            if (isBass)
            {
                output.Add(new(t, p.BeatSeconds * 0.7, Part.Ostinato, inst, low, vel, -0.3f));
                continue;
            }
            for (int k = 0; k < tones.Length; k++)
                output.Add(new(t + k * 0.012, p.BeatSeconds * 0.4, Part.Ostinato, inst, tones[k], vel * 0.8f, 0.2f + k * 0.15f));
        }
    }

    /// <summary>
    /// The bodhrán: every eighth, the beats struck low with the hand's weight behind them, the rest
    /// lighter and higher (the other hand pressing the skin), and a triplet run into the last bar of
    /// each phrase.
    /// </summary>
    private void EmitBodhran(List<ScoreEvent> output, Piece p, double barStart, float arc)
    {
        int low = p.Key.Tonic - 14;
        while (low > 45) low -= 12;
        while (low < 36) low += 12;
        bool fill = _bar % 4 == 3;
        List<(double at, float v, int pitch)> hits = new();
        if (p.Dance == Dance.Jig)
        {
            double t3 = 1.0 / 3;
            foreach (var (at, v) in new[] { (0.0, 0.9f), (t3, 0.35f), (2 * t3, 0.5f), (1.0, 0.75f), (1 + t3, 0.35f), (1 + 2 * t3, 0.55f) })
                hits.Add((at, v, v > 0.6f ? low : low + 4));
        }
        else
        {
            for (int i = 0; i < 8; i++)
            {
                float v = i % 4 == 0 ? 0.9f : i % 2 == 0 ? 0.6f : 0.35f;
                hits.Add((i * 0.5, v, v > 0.6f ? low : low + 4));
            }
            if (fill)
                foreach (double at in new[] { 3.0 + 1.0 / 3, 3.0 + 2.0 / 3 }) hits.Add((at, 0.45f, low + 3));
        }
        if (fill && p.Dance == Dance.Jig)
            hits.Add((1 + 1.0 / 6, 0.3f, low + 5));
        foreach (var (at, v, pitch) in hits)
        {
            double t = barStart + (at + Lilt(p, at)) * p.BeatSeconds + Humanise() * 0.4;
            output.Add(new(t, 0.25, Part.Perc, Instrument.Bodhran, pitch, v * arc * Jitter(0.1f), 0.1f));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>The pitch of a degree's pitch class at or above <paramref name="floor"/>, within an octave.</summary>
    private static int LowPitch(Key key, int degree, int floor)
    {
        int m = key.Pitch(degree);
        while (m >= floor + 12) m -= 12;
        while (m < floor) m += 12;
        return m;
    }

    private static bool OrnamentsSuit(Piece p, Instrument i) =>
        (p.Orch.Pole == "comfy" || p.Orch.Pole == "melancholy") &&
        i is Instrument.Recorder or Instrument.Harpsichord or Instrument.Lute or Instrument.OrganFlute;

    private static bool IsPlucked(Instrument i) => i is Instrument.Harp or Instrument.Lute or Instrument.Harpsichord;

    /// <summary>A few milliseconds early or late: players are never exactly on the grid.</summary>
    private double Humanise() => (_rng.NextDouble() - 0.5) * 0.016;

    private float Jitter(float amount) => 1f + (float)(_rng.NextDouble() - 0.5) * 2 * amount;
}
