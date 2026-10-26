using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Cathedral.Audio.Music;

/// <summary>
/// The score behind a render, in the format tools/music reads (<c>musickit/score.py</c>): every
/// note with its part and instrument, and what a MIDI file cannot carry: pieces (mode, tonic, tempo,
/// metre, form), sections (label, playing layers) and chords. Written beside the WAV by
/// <c>--render-music</c> as <c>&lt;out&gt;.score.json</c>.
/// </summary>
internal sealed class ScoreLog
{
    private readonly StringBuilder _notes = new(), _sections = new(), _chords = new(), _pieces = new(), _marks = new();
    private readonly Dictionary<string, object> _meta = new();
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public void Meta(string key, object value) => _meta[key] = value;

    public void Note(double t, ScoreEvent e) => Append(_notes,
        $"{{\"t\":{e.T.ToString("F4", Inv)},\"d\":{(e.LogDur > 0 ? e.LogDur : e.Dur).ToString("F4", Inv)},{(e.Ornament ? "\"o\":1," : "")}\"p\":{MathF.Round(e.Midi).ToString(Inv)},\"v\":{e.Vel.ToString("F3", Inv)},\"part\":\"{e.Part.ToString().ToLowerInvariant()}\",\"inst\":\"{e.Inst}\"}}");

    public void Piece(double t, Piece p) => Append(_pieces,
        $"{{\"t\":{t.ToString("F4", Inv)},\"index\":{p.Index},\"mode\":\"{p.Key.Mode.Name}\",\"tonic\":{p.Key.Tonic},\"bpm\":{p.Bpm.ToString("F2", Inv)},\"meter\":{p.Meter},\"form\":\"{p.Form}\",\"pole\":\"{p.Orch.Pole}\",\"theme\":{(p.UsesTheme ? "true" : "false")},\"dance\":\"{p.Dance}\"," +
        $"\"melody\":\"{p.Orch.Melody}\",\"pad\":\"{p.Orch.Pad}\",\"ostinato\":\"{p.Orch.Ostinato}\",\"mood\":[{p.Mood.Coldness.ToString("F2", Inv)},{p.Mood.Fear.ToString("F2", Inv)},{p.Mood.Mystery.ToString("F2", Inv)}]}}");

    public void Section(double t, Section s, Piece p) => Append(_sections,
        $"{{\"t\":{t.ToString("F4", Inv)},\"label\":\"{s.Label}\",\"piece\":{p.Index},\"bars\":{s.Bars},\"counter\":\"{s.Counter}\",\"layers\":[{string.Join(",", s.Layers.Select(l => $"\"{l.ToString().ToLowerInvariant()}\""))}]}}");

    /// <summary>Something the game did at <paramref name="t"/> (a journey's mood change, event, filter).</summary>
    public void Mark(double t, string what) => Append(_marks, $"{{\"t\":{t.ToString("F4", Inv)},\"what\":\"{what}\"}}");

    public void Chord(double t, double dur, int root, int[] pcs) => Append(_chords,
        $"{{\"t\":{t.ToString("F4", Inv)},\"d\":{dur.ToString("F4", Inv)},\"root\":{root},\"pcs\":[{string.Join(",", pcs)}]}}");

    private static void Append(StringBuilder sb, string item)
    {
        if (sb.Length > 0) sb.Append(",\n");
        sb.Append(item);
    }

    private string _stems = "";

    public void Stems(IEnumerable<(Part part, double db, double seconds)> stems) =>
        _stems = string.Join(",", stems.Select(x => $"\"{x.part.ToString().ToLowerInvariant()}\":{{\"db\":{x.db.ToString("F2", Inv)},\"seconds\":{x.seconds.ToString("F1", Inv)}}}"));

    public void Write(string path)
    {
        string meta = string.Join(",", _meta.Select(kv => kv.Value switch
        {
            string s => $"\"{kv.Key}\":\"{s}\"",
            double d => $"\"{kv.Key}\":{d.ToString("F3", Inv)}",
            float f => $"\"{kv.Key}\":{f.ToString("F3", Inv)}",
            _ => $"\"{kv.Key}\":{Convert.ToString(kv.Value, Inv)}",
        }));
        File.WriteAllText(path,
            $"{{\"meta\":{{{meta}}},\n\"stems\":{{{_stems}}},\n\"marks\":[\n{_marks}],\n\"pieces\":[\n{_pieces}],\n\"sections\":[\n{_sections}],\n\"chords\":[\n{_chords}],\n\"notes\":[\n{_notes}]}}\n");
    }
}
