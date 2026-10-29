using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Management;

/// <summary>
/// Renders the Bellic management tab: one party member's fighting profile, everything a fight reads
/// off them that no other tab shows (see <see cref="BellicProfile"/> for what is in and what is
/// left out). Information only — nothing here changes state; a click only expands a skill.
///
/// Layout (cols 17-98, the left menu panel owns 0-15):
///   Row 1-2    : title "B E L L I C" and a rule, as the Humors tab draws its own
///   Row 4-5    : who, and the wound legend when wounds weigh on the fight
///   Row 7-     : two columns of stats — TURN and ESCAPE left, STRIKING and DEFENCE right —
///                each line label / value / source organ, notes beneath in grey
///   then       : SKILLS, one block per medium (a hand, the feet, a weapon…), two columns
///   Row 76-98  : the selected skill, expanded: what it does and every term behind its number
/// </summary>
public sealed class BellicMenuRenderer
{
    private readonly TerminalHUD _terminal;

    private const int AreaLeft  = 17;
    private const int LeftX     = 19;
    private const int RightX    = 59;
    private const int ColW      = 38;
    private const int ValueOff  = 17;   // value column, relative to a column's x
    private const int SourceOff = 28;   // source column
    private const int StatsTop  = 7;
    private const int DetailSepRow = 74;
    private const int DetailTop    = 76;
    private const int DetailBottom = 98;
    private const int DetailW      = 79;

    // Skill table columns, relative to a column's x
    private const int RollOff  = 19;
    private const int CostOff  = 28;
    private const int ReachOff = 33;

    private static readonly Vector4 Bg          = Config.Colors.Black;
    private static readonly Vector4 TitleColor  = Config.Colors.DarkYellowGrey;
    private static readonly Vector4 SepColor    = Config.Colors.DarkGray35;
    private static readonly Vector4 HeadColor   = Config.Colors.DarkYellow;
    private static readonly Vector4 LabelColor  = Config.Colors.LightGray75;
    private static readonly Vector4 ValueColor  = Config.Colors.White;
    private static readonly Vector4 SourceColor = Config.Colors.MediumGray60;
    private static readonly Vector4 NoteColor   = Config.Colors.MediumGray50;
    private static readonly Vector4 WoundColor  = Config.Colors.BrightPurple;
    private static readonly Vector4 LearnColor  = Config.Colors.LightPurple;
    private static readonly Vector4 OffColor    = Config.Colors.DarkGray40;
    private static readonly Vector4 HoverBg     = new(0.07f, 0.06f, 0.01f, 1.0f);
    private static readonly Vector4 SelectBg    = new(0.10f, 0.09f, 0.02f, 1.0f);

    private BellicProfile? _profile;
    private readonly List<(int Row, int X, BellicProfile.SkillRow Skill)> _hitRows = new();
    private (string Group, string Skill)? _selected;
    private (string Group, string Skill)? _hovered;

    public BellicMenuRenderer(TerminalHUD terminal) => _terminal = terminal;

    public bool IsHovering => _hovered != null;

    public void Render(PartyMember member)
    {
        // Built fresh per draw: a few dozen property reads, and nothing to keep in step with an
        // equip, a wound or a modus mentis learned while the screen was closed.
        _profile = new BellicProfile(member);
        if (_selected is { } sel && Find(sel) == null) _selected = null;

        _terminal.FillRect(AreaLeft, 0, Config.Terminal.MainWidth - AreaLeft, Config.Terminal.MainHeight, ' ', Bg, Bg);
        RenderTitle();
        RenderIdentity(_profile);

        int left  = Section(LeftX,  StatsTop, "TURN", _profile.Turn);
        left      = Section(LeftX,  left + 1, "ESCAPE AND RECOVERY", _profile.Escape);
        int right = Section(RightX, StatsTop, "STRIKING", _profile.Striking);
        right     = Section(RightX, right + 1, "DEFENCE  where the blow lands", _profile.Defence);

        RenderSkills(_profile, Math.Max(left, right) + 1);
        RenderDetail(_profile);
    }

    public bool ProcessHover(int x, int y)
    {
        var hit = HitTest(x, y);
        var key = hit == null ? ((string, string)?)null : (hit.Owner.Key, hit.Skill.SkillId);
        if (key == _hovered) return false;
        _hovered = key;
        return true;
    }

    public bool ProcessClick(int x, int y)
    {
        var hit = HitTest(x, y);
        if (hit == null) return false;
        var key = (hit.Owner.Key, hit.Skill.SkillId);
        _selected = _selected == key ? null : key;
        return true;
    }

    public void ClearHover() => _hovered = null;

    /// <summary>Expands the first skill row with this name, for <c>--cli</c>. False when there is none.</summary>
    public bool CliSelectSkill(PartyMember member, string name)
    {
        var row = new BellicProfile(member).Groups.SelectMany(g => g.Skills)
            .FirstOrDefault(r => string.Equals(r.Skill.DisplayName, name, StringComparison.OrdinalIgnoreCase)
                              || string.Equals(r.Skill.SkillId, name, StringComparison.OrdinalIgnoreCase));
        if (row == null) return false;
        _selected = (row.Owner.Key, row.Skill.SkillId);
        return true;
    }

    // ── Drawing ──────────────────────────────────────────────────

    private void RenderTitle()
    {
        const string title = "B E L L I C";
        int availW = Config.Terminal.MainWidth - AreaLeft;
        _terminal.Text(AreaLeft + (availW - title.Length) / 2, 1, title, TitleColor, Bg);
        for (int x = AreaLeft; x < Config.Terminal.MainWidth; x++)
            _terminal.SetCell(x, 2, '─', SepColor, Bg);
    }

    private void RenderIdentity(BellicProfile p)
    {
        var m = p.Member;
        _terminal.Text(LeftX, 4, m.DisplayName, Config.Colors.BrightYellow, Bg);
        string species = m.Species.DisplayName.ToLowerInvariant(), anatomy = m.AnatomyType.ToString().ToLowerInvariant();
        _terminal.Text(LeftX + m.DisplayName.Length + 2, 4,
                       species == anatomy ? $"{anatomy} anatomy" : $"{species}, {anatomy} anatomy", SourceColor, Bg);

        bool wounded = p.Turn.Concat(p.Escape).Concat(p.Striking).Concat(p.Defence).Any(s => s.Wounded)
                    || p.Groups.SelectMany(g => g.Skills).Any(r => r.State is BellicProfile.SkillState.Broken
                                                                         or BellicProfile.SkillState.LimbDisabled);
        if (wounded)
            _terminal.Text(LeftX, 5, "Wounds weigh on this body in a fight: what they cost is marked ↓ and ×.",
                           WoundColor, Bg);
    }

    /// <summary>A titled block of stat lines. Returns the row after it.</summary>
    private int Section(int x, int row, string title, List<BellicProfile.Stat> stats)
    {
        _terminal.Text(x, row, $"▸ {title}", HeadColor, Bg);
        if (stats.Any(s => s.Source.Length > 0) && !title.StartsWith("DEFENCE"))
            _terminal.Text(x + SourceOff, row, "from", NoteColor, Bg);
        row += 2;

        foreach (var s in stats)
        {
            if (s.Label.Length > 0 || s.Value.Length > 0)
            {
                _terminal.Text(x, row, Clip(s.Label, ValueOff - 1), LabelColor, Bg);
                string value = s.Wounded ? s.Value + " ↓" : s.Value;
                _terminal.Text(x + ValueOff, row, Clip(value, SourceOff - ValueOff - 1), s.Wounded ? WoundColor : ValueColor, Bg);
                if (s.Source.Length > 0)
                    _terminal.Text(x + SourceOff, row, Clip(s.Source, ColW - SourceOff), s.Wounded ? WoundColor : SourceColor, Bg);
                row++;
            }
            if (s.Note != null)
                foreach (var line in Wrap(s.Note, ColW - 4))
                    _terminal.Text(x + 2, row++, line, NoteColor, Bg);
        }
        return row;
    }

    private void RenderSkills(BellicProfile p, int top)
    {
        _hitRows.Clear();
        for (int c = AreaLeft + 1; c < Config.Terminal.MainWidth - 1; c++)
            _terminal.SetCell(c, top, '─', SepColor, Bg);
        top += 2;
        _terminal.Text(LeftX, top, "▸ SKILLS", HeadColor, Bg);
        _terminal.Text(LeftX + 10, top, "× broken by wounds   – limb out of use   ? next to learn",
                       NoteColor, Bg);
        top += 1;

        if (p.Groups.Count == 0)
        {
            _terminal.Text(LeftX, top, "Nothing to fight with.", SourceColor, Bg);
            return;
        }

        // One header per column for the three numbers, rather than one per block: a block then costs
        // its title, its rows and a blank, which is what lets an armed human fit.
        foreach (int cx in new[] { LeftX, RightX })
        {
            _terminal.Text(cx + RollOff, top, "roll", NoteColor, Bg);
            _terminal.Text(cx + CostOff, top, "cost", NoteColor, Bg);
            _terminal.Text(cx + ReachOff, top, "reach", NoteColor, Bg);
        }
        top++;

        // Whole blocks, never split: the first k down the left column and the rest down the right,
        // k chosen so the taller column is as short as it can be.
        int Height(BellicProfile.Group g) => 1 + g.Skills.Count + 1;
        var heights = p.Groups.Select(Height).ToList();
        int split = Enumerable.Range(0, p.Groups.Count + 1)
            .OrderBy(k => Math.Max(heights.Take(k).Sum(), heights.Skip(k).Sum())).First();

        int bottom = DetailSepRow - 1;
        int row = top, x = LeftX;
        for (int i = 0; i < p.Groups.Count; i++)
        {
            var g = p.Groups[i];
            if (i == split) { x = RightX; row = top; }
            if (row > bottom) continue;

            _terminal.Text(x, row, Clip(g.Title, 20), LabelColor, Bg);
            string sub = (g.Subtitle.Length > 0 ? g.Subtitle + ", " : "") + $"lv {g.Level}";
            int subX = Math.Min(20, g.Title.Length) + 2;
            _terminal.Text(x + subX, row, Clip(sub, ColW - subX), NoteColor, Bg);
            row++;

            foreach (var r in g.Skills)
            {
                if (row > bottom) break;
                DrawSkillRow(x, row, r);
                _hitRows.Add((row, x, r));
                row++;
            }
            row++;
        }
    }

    private void DrawSkillRow(int x, int row, BellicProfile.SkillRow r)
    {
        var key = (r.Owner.Key, r.Skill.SkillId);
        bool sel = _selected == key, hov = _hovered == key;
        Vector4 bg = sel ? SelectBg : hov ? HoverBg : Bg;

        (char mark, Vector4 nameC, Vector4 numC) = r.State switch
        {
            BellicProfile.SkillState.Broken       => ('×', WoundColor, OffColor),
            BellicProfile.SkillState.LimbDisabled => ('–', WoundColor, OffColor),
            BellicProfile.SkillState.NextToLearn  => ('?', LearnColor, OffColor),
            _                                     => ('·', LabelColor, ValueColor),
        };
        if (sel) nameC = Config.Colors.BrightYellow;
        else if (hov) nameC = Config.Colors.MediumYellow;

        for (int i = 0; i < ColW; i++) _terminal.SetCell(x + i, row, ' ', nameC, bg);
        _terminal.SetCell(x, row, mark, r.State == BellicProfile.SkillState.Ready ? SourceColor : nameC, bg);
        _terminal.Text(x + 2, row, Clip(r.Skill.DisplayName, RollOff - 3), nameC, bg);
        _terminal.Text(x + RollOff, row, r.Roll, numC, bg);
        _terminal.Text(x + CostOff, row, r.Cost, r.State == BellicProfile.SkillState.Ready ? SourceColor : OffColor, bg);
        _terminal.Text(x + ReachOff, row, Clip(r.Reach, ColW - ReachOff), r.State == BellicProfile.SkillState.Ready ? SourceColor : OffColor, bg);
    }

    private void RenderDetail(BellicProfile p)
    {
        for (int x = AreaLeft + 1; x < Config.Terminal.MainWidth - 1; x++)
            _terminal.SetCell(x, DetailSepRow, '─', SepColor, Bg);

        var row = _selected is { } sel ? Find(sel) : null;
        if (row == null)
        {
            _terminal.Text(LeftX, DetailTop, "Select a skill to see what it does and where its number comes from.",
                           NoteColor, Bg);
            return;
        }

        int y = DetailTop;
        foreach (var (text, tone) in p.Detail(row))
        {
            Vector4 c = tone switch
            {
                BellicProfile.Tone.Title  => Config.Colors.BrightYellow,
                BellicProfile.Tone.Text   => LabelColor,
                BellicProfile.Tone.Dim    => SourceColor,
                BellicProfile.Tone.Number => ValueColor,
                BellicProfile.Tone.Warn   => WoundColor,
                BellicProfile.Tone.Learn  => LearnColor,
                _                         => LabelColor,
            };
            foreach (var line in text.Length == 0 ? new[] { "" } : Wrap(text, DetailW))
            {
                if (y > DetailBottom) return;
                _terminal.Text(LeftX, y++, line, c, Bg);
            }
        }
    }

    // ── Helpers ──────────────────────────────────────────────────

    private BellicProfile.SkillRow? Find((string Group, string Skill) key) =>
        _profile?.Groups.FirstOrDefault(g => g.Key == key.Group)?.Skills.FirstOrDefault(r => r.Skill.SkillId == key.Skill);

    private BellicProfile.SkillRow? HitTest(int x, int y)
    {
        foreach (var (row, rx, skill) in _hitRows)
            if (y == row && x >= rx && x < rx + ColW) return skill;
        return null;
    }

    private static string Clip(string s, int w) => w <= 0 ? "" : s.Length <= w ? s : s[..Math.Max(0, w - 1)] + "…";

    private static IEnumerable<string> Wrap(string text, int width)
    {
        // A line that fits is kept verbatim: the breakdown terms are padded into columns.
        if (text.Length <= width) { yield return text; yield break; }
        var line = "";
        foreach (var word in text.Split(' '))
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
