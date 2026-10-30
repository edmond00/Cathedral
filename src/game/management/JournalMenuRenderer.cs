using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Management;

/// <summary>
/// Renders the Journal management tab: the run's story as one long scrolling list, opening at the
/// newest entry. Protagonist only.
///
/// <para>A visit the chronicler has written reads as its paragraph; one it has not reads as what was
/// recorded — the narration, the lines spoken, the chips — so the journal is never empty while the
/// chronicler catches up. Milestones always show, as short lines of their own, and a header marks every
/// change of day or place. A passage a companion carried is marked down its left edge.</para>
///
/// Layout (cols 17-98): title rows 1-2, the list rows 4-95, a footer at 97.
/// </summary>
public sealed class JournalMenuRenderer
{
    private readonly TerminalHUD _terminal;

    private const int AreaLeft = 17;
    private const int TextX    = 21;
    private const int TextW    = 76;
    private const int ListTop  = 4;
    private const int ListBottom = 95;
    private const int FooterRow  = 97;
    private int PageRows => ListBottom - ListTop + 1;

    private static readonly Vector4 Bg            = Config.Colors.Black;
    private static readonly Vector4 TitleColor    = Config.Colors.DarkYellowGrey;
    private static readonly Vector4 SepColor      = Config.Colors.DarkGray35;
    private static readonly Vector4 HeaderColor   = Config.Colors.DarkYellow;
    private static readonly Vector4 WrittenColor  = Config.Colors.LightGray85;
    private static readonly Vector4 RawColor      = Config.Colors.MediumGray60;
    private static readonly Vector4 SpeechColor   = Config.Colors.LightGray75;
    private static readonly Vector4 FactColor     = Config.Colors.MediumGray50;
    private static readonly Vector4 MilestoneColor = Config.Colors.MediumYellow;
    private static readonly Vector4 CompanionColor = Config.Colors.LightPurple;
    private static readonly Vector4 NoteColor     = Config.Colors.MediumGray50;

    private readonly record struct Line(string Text, Vector4 Color, bool Companion, bool Header = false);

    /// <summary>Lines scrolled up from the bottom; 0 shows the newest entry.</summary>
    private int _fromBottom;
    private int _lineCount;

    public JournalMenuRenderer(TerminalHUD terminal) => _terminal = terminal;

    /// <summary>Back to the newest entry — the tab opens there.</summary>
    public void ScrollToNewest() => _fromBottom = 0;

    /// <summary>Scrolls by <paramref name="lines"/> (positive is towards older entries). True when it moved.</summary>
    public bool Scroll(int lines)
    {
        int max = Math.Max(0, _lineCount - PageRows);
        int next = Math.Clamp(_fromBottom + lines, 0, max);
        if (next == _fromBottom) return false;
        _fromBottom = next;
        return true;
    }

    public bool PageUp()   => Scroll(PageRows - 2);
    public bool PageDown() => Scroll(-(PageRows - 2));

    public void Render(Protagonist protagonist)
    {
        _terminal.FillRect(AreaLeft, 0, Config.Terminal.MainWidth - AreaLeft, Config.Terminal.MainHeight, ' ', Bg, Bg);

        const string title = "J O U R N A L";
        int availW = Config.Terminal.MainWidth - AreaLeft;
        _terminal.Text(AreaLeft + (availW - title.Length) / 2, 1, title, TitleColor, Bg);
        for (int x = AreaLeft; x < Config.Terminal.MainWidth; x++)
            _terminal.SetCell(x, 2, '─', SepColor, Bg);

        var journal = protagonist.Journal;
        var lines = Layout(journal);
        _lineCount = lines.Count;
        _fromBottom = Math.Clamp(_fromBottom, 0, Math.Max(0, _lineCount - PageRows));

        if (lines.Count == 0)
        {
            _terminal.Text(TextX, ListTop, "Nothing written yet. What you see and do will be set down here.", NoteColor, Bg);
            return;
        }

        int first = Math.Max(0, lines.Count - PageRows - _fromBottom);
        int row = ListTop;
        for (int i = first; i < lines.Count && row <= ListBottom; i++, row++)
        {
            var l = lines[i];
            if (l.Companion)
                _terminal.SetCell(TextX - 2, row, '┃', CompanionColor, Bg);
            _terminal.Text(l.Header ? TextX - 2 : TextX, row, l.Text, l.Color, Bg);
        }

        for (int x = AreaLeft + 1; x < Config.Terminal.MainWidth - 1; x++)
            _terminal.SetCell(x, FooterRow - 1, '─', SepColor, Bg);
        int pending = journal.PendingGroupCount;
        string where = _fromBottom == 0 ? "newest" : $"{_fromBottom} lines up";
        string footer = $"wheel or PgUp/PgDn · {where}"
                      + (pending > 0 ? $" · {pending} visit{(pending == 1 ? "" : "s")} not yet written up" : "");
        _terminal.Text(TextX - 2, FooterRow, Clip(footer, TextW + 2), NoteColor, Bg);
    }

    /// <summary>The whole journal as screen lines, oldest first.</summary>
    private static List<Line> Layout(Journal journal)
    {
        var lines = new List<Line>();
        int? day = null;
        string? place = null;
        string? lastActor = null;

        foreach (var e in journal.Entries)
        {
            bool written = e.State == JournalEntryState.Written;
            // A written group reads as its paragraph alone, carried by its first entry; the rest of
            // the group is inside it.
            if (written && e.Written.Length == 0) continue;
            var kind = written ? JournalEntryKind.Narration : e.Kind;

            if (e.Day != day || e.Place != place)
            {
                if (lines.Count > 0) lines.Add(new Line("", Bg, false));
                string where = e.Place.Length > 0 ? e.Place.ToUpperInvariant() : "ON THE ROAD";
                lines.Add(new Line($"◆ DAY {e.Day + 1}  ·  {where}", HeaderColor, false, Header: true));
                lines.Add(new Line("", Bg, false));
                day = e.Day; place = e.Place; lastActor = null;
            }

            bool comp = e.ByCompanion;
            switch (kind)
            {
                case JournalEntryKind.Milestone:
                    foreach (var w in Wrap($"✦ {e.Source}", TextW))
                        lines.Add(new Line(w, MilestoneColor, false));
                    break;

                case JournalEntryKind.Dialogue:
                    foreach (var w in Wrap($"{e.Actor}: “{e.Source.Trim('"', '“', '”')}”", TextW))
                        lines.Add(new Line(w, SpeechColor, comp));
                    break;

                case JournalEntryKind.Fact:
                    foreach (var w in Wrap($"· {e.Source}", TextW))
                        lines.Add(new Line(w, FactColor, comp));
                    break;

                default:
                    string text = written ? e.Written : e.Source;
                    if (comp && e.Actor != lastActor) text = $"{e.Actor} — {text}";
                    foreach (var w in Wrap(text, TextW))
                        lines.Add(new Line(w, written ? WrittenColor : RawColor, comp && !written));
                    lines.Add(new Line("", Bg, false));
                    break;
            }
            lastActor = e.Actor;
        }
        while (lines.Count > 0 && lines[^1].Text.Length == 0) lines.RemoveAt(lines.Count - 1);
        return lines;
    }

    private static string Clip(string s, int w) => s.Length <= w ? s : s[..Math.Max(0, w - 1)] + "…";

    private static IEnumerable<string> Wrap(string text, int width)
    {
        foreach (var para in text.Replace("\r", "").Split('\n'))
        {
            var line = "";
            foreach (var word in para.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length > 0 && line.Length + 1 + word.Length > width)
                {
                    yield return line;
                    line = "";
                }
                line = line.Length == 0 ? word : line + " " + word;
            }
            if (line.Length > 0) yield return line;
        }
    }
}
