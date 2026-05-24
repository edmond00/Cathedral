using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;
using Cathedral.Game;
using Cathedral.Game.Narrative;

namespace Cathedral.Fight;

/// <summary>Kind of clickable row in the left action panel.</summary>
public enum LeftPanelRowKind
{
    Medium,
    UnlockedSkill,
    LearnableSkill,
}

/// <summary>
/// One clickable row in the left panel's action area (medium header or skill entry).
/// Returned by <see cref="FightModeUI.RenderLeftPanel"/> so the adapter can dispatch clicks/hovers.
/// </summary>
public readonly record struct LeftPanelRow(
    int Y,
    LeftPanelRowKind Kind,
    string MediumKey,
    int SkillIndex);

/// <summary>
/// Pure renderer: draws the five-panel fight UI onto a <see cref="TerminalHUD"/>.
/// No state is stored here — callers pass in all necessary data.
///
/// Panel layout (100×100 terminal):
///   Top:    rows  0-19, cols  0-99  (initiative bar)
///   Left:   rows 20-79, cols  0-19  (active fighter detail + skills)
///   Center: rows 20-79, cols 20-79  (60×60 arena — matches FightAreaRenderer constants)
///   Right:  rows 20-79, cols 80-99  (action log)
///   Bottom: rows 80-99, cols  0-99  (terrain legend)
/// </summary>
public static class FightModeUI
{
    // ── Panel boundaries ─────────────────────────────────────────────
    private const int TopRows    = 20;
    private const int BotStart   = 80;
    private const int LeftEnd    = 20;
    private const int RightStart = 80;
    private const int CenterX    = 20;  // Matches FightAreaRenderer.OffsetX
    private const int CenterY    = 20;  // Matches FightAreaRenderer.OffsetY

    // ── Left-panel split: action menu on top, action info on bottom ──
    public const int LeftSplitRow      = 50;            // boundary between the two left-panel boxes
    public const int MoveButtonRow     = TopRows + 3;   // row 23
    public const int SkillButtonsStart = TopRows + 4;   // row 24, each skill +1 below
    public const int EndTurnButtonRow  = LeftSplitRow - 3;  // row 47
    public const int RunButtonRow      = LeftSplitRow - 2;  // row 48

    // ── Highlight \u2014 dim color for out-of-range tiles ─────────────────────
    private static readonly Vector4 ActiveFighterBg = new(0.35f, 0.22f, 0f, 1f); // dark amber

    // ── Top panel — detailed fighter info ─────────────────────────────

    /// <summary>
    /// Draw a detailed info card for <paramref name="fighter"/> across the full top panel
    /// (rows 0-19). Used both for the currently active fighter (default) and for whichever
    /// fighter the mouse is currently hovering over.
    /// </summary>
    public static void RenderDetailPanel(TerminalHUD terminal, Fighter? fighter, bool isHoverOverride)
    {
        terminal.FillRect(0, 0, 100, TopRows, ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(0, 0, 100, TopRows, BoxStyle.Single, Config.Colors.DarkGray, Config.Colors.Black);

        if (fighter == null)
        {
            terminal.Text(2, 1, "DETAIL", Config.Colors.DarkYellowGrey, Config.Colors.Black);
            return;
        }

        Vector4 nameColor = fighter.Faction == FighterFaction.Party
            ? Config.Colors.Yellow : Config.Colors.BrightPurple;
        string factionLabel = fighter.Faction == FighterFaction.Party ? "PARTY" : "ENEMY";

        // Row 1: header
        terminal.Text(2, 1, $"{factionLabel} — {fighter.DisplayName}",
            nameColor, Config.Colors.Black);
        if (isHoverOverride)
            terminal.Text(2 + factionLabel.Length + 3 + fighter.DisplayName.Length + 1, 1,
                "(hovered)", Config.Colors.DarkYellowGrey, Config.Colors.Black);
        if (!fighter.IsAlive)
            terminal.Text(90, 1, "[DEAD]", Config.Colors.DarkGray, Config.Colors.Black);

        const int leftCol  = 2;
        const int rightCol = 50;
        int row = 3;

        bool isEnemy = fighter.Faction == FighterFaction.Enemy;
        Vector4 barFull = isEnemy ? Config.Colors.BrightPurple : Config.Colors.Yellow;
        Vector4 barLow  = isEnemy ? Config.Colors.DarkPurple   : Config.Colors.DarkPurple;
        Vector4 dotFull = isEnemy ? Config.Colors.BrightPurple : Config.Colors.Yellow;

        // ── Left column: bars, FX, combat stats ──
        DrawBar(terminal, leftCol, row, 40, fighter.CurrentHp, fighter.MaxHp,
            "HP", barFull, barLow);
        terminal.Text(leftCol + 41, row, $"{fighter.CurrentHp}/{fighter.MaxHp}",
            Config.Colors.White, Config.Colors.Black);
        row++;

        terminal.Text(leftCol, row, "CP :", Config.Colors.DarkGray, Config.Colors.Black);
        int dotX = leftCol + 5;
        int cpMax = Math.Max(1, fighter.MaxCineticPoints);
        for (int i = 0; i < cpMax && dotX < leftCol + 40; i++)
        {
            Vector4 col = i < fighter.CurrentCineticPoints
                ? dotFull : Config.Colors.DarkGray35;
            terminal.SetCell(dotX, row, Config.Symbols.NoeticPointMarker, col, Config.Colors.Black);
            dotX++;
        }
        terminal.Text(leftCol + 41, row, $"{fighter.CurrentCineticPoints}/{fighter.MaxCineticPoints}",
            Config.Colors.White, Config.Colors.Black);
        row++;

        if (fighter.ActiveEffects.Count > 0)
        {
            var sb = new System.Text.StringBuilder("FX :");
            foreach (var fx in fighter.ActiveEffects) sb.Append(' ').Append(fx.DisplayLabel);
            string fxLine = sb.ToString();
            if (fxLine.Length > 46) fxLine = fxLine[..46];
            terminal.Text(leftCol, row, fxLine, Config.Colors.BrightRed, Config.Colors.Black);
            row++;
        }

        row++; // spacer

        // Combat stats — 3 per row
        int damageRes = fighter.Member.DerivedStats
            .FirstOrDefault(s => s.Name == "damage_resistance")?.GetValue(fighter.Member) ?? 0;
        terminal.Text(leftCol, row,
            $"INIT:{fighter.InitiativeValue,-3} DEF:{fighter.NaturalDefense,-3} MOV:{fighter.MoveSpeed,-3}",
            Config.Colors.LightGray, Config.Colors.Black);
        row++;
        terminal.Text(leftCol, row,
            $"DR  :{damageRes,-3} RUN:{fighter.RunawayChancePercent,-2}% MCP:{fighter.MaxCineticPoints,-3}",
            Config.Colors.LightGray, Config.Colors.Black);
        row++;

        // ── Right column: wounds ──
        int rRow = 3;
        terminal.Text(rightCol, rRow, "WOUNDS",
            Config.Colors.DarkYellowGrey, Config.Colors.Black);
        rRow++;
        var wounds = fighter.Member.Wounds;
        if (wounds.Count == 0)
        {
            terminal.Text(rightCol, rRow, "(none)", Config.Colors.DarkGray, Config.Colors.Black);
        }
        else
        {
            int slotMax = TopRows - rRow - 1; // up to bottom border
            int shown = Math.Min(wounds.Count, slotMax);
            for (int i = 0; i < shown; i++)
            {
                var w = wounds[i];
                string tag = w.Handicap switch
                {
                    WoundHandicap.High   => "[H]",
                    WoundHandicap.Medium => "[M]",
                    _                    => "[L]",
                };
                Vector4 tagColor = w.Handicap switch
                {
                    WoundHandicap.High   => Config.Colors.BrightRed,
                    WoundHandicap.Medium => Config.Colors.Orange,
                    _                    => Config.Colors.DarkYellowGrey,
                };
                terminal.Text(rightCol, rRow + i, tag, tagColor, Config.Colors.Black);
                string lbl = $" {w.WoundName} ({w.TargetId})";
                if (lbl.Length > 100 - rightCol - 4) lbl = lbl[..(100 - rightCol - 4)];
                terminal.Text(rightCol + 3, rRow + i, lbl,
                    Config.Colors.Purple, Config.Colors.Black);
            }
            if (wounds.Count > shown)
                terminal.Text(rightCol, rRow + shown,
                    $"+{wounds.Count - shown} more",
                    Config.Colors.DarkGray, Config.Colors.Black);
        }
    }

    // ── Left panel ────────────────────────────────────────────────────

    /// <summary>
    /// Draw detail for <paramref name="fighter"/>: HP/CP bars, status effects, plus the
    /// grouped action list (one medium header per row; expanded medium reveals its skills).
    /// Returns the row layout so the adapter can dispatch clicks / hovers.
    /// </summary>
    public static IReadOnlyList<LeftPanelRow> RenderLeftPanel(
        TerminalHUD terminal, Fighter fighter,
        IReadOnlyList<FightingSkill> unlockedSkills,
        IReadOnlyList<FightingSkill> learnableSkills,
        bool isMoveMode, int selectedSkillIndex, int selectedLearnableSkillIndex,
        string? expandedMediumKey,
        int hoveredButtonRow = -1)
    {
        var layout = new List<LeftPanelRow>();

        // Top half = action menu (rows TopRows..LeftSplitRow-1)
        int topH = LeftSplitRow - TopRows;
        terminal.FillRect(0, TopRows, LeftEnd, topH, ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(0, TopRows, LeftEnd, topH, BoxStyle.Single, Config.Colors.DarkGray, Config.Colors.Black);

        const int x = 1;
        int y = TopRows + 1;

        // "ACTIONS:" header + divider
        terminal.Text(x, y++, "ACTIONS:", Config.Colors.DarkYellowGrey, Config.Colors.Black);
        terminal.Text(x, y++, new string('─', LeftEnd - 2), Config.Colors.DarkGray, Config.Colors.Black);

        // ── Enemy turn: skip action buttons, show a status message instead ──
        if (!fighter.IsPlayerControlled)
        {
            int midRow = (y + (EndTurnButtonRow - 1)) / 2;
            string msg = "── ENEMY TURN ──";
            int mx = 1 + Math.Max(0, (LeftEnd - 2 - msg.Length) / 2);
            terminal.Text(mx, midRow, msg, Config.Colors.Purple, Config.Colors.Black);
            return layout;
        }

        // y is now at MoveButtonRow (23)
        // MOVE button — no grey background, yellow when selected
        {
            bool sel = isMoveMode;
            bool hov = !sel && hoveredButtonRow == MoveButtonRow;
            Vector4 fg = sel ? Config.Colors.Black
                       : hov ? Config.Colors.GoldYellow
                       : Config.Colors.White;
            Vector4 bg = sel ? Config.Colors.Yellow : Config.Colors.Black;
            string label = (sel ? "* MOVE          " : "  MOVE          ")[..Math.Min(16, LeftEnd - 2)];
            terminal.Text(x, y++, label, fg, bg);
        }

        // ── Grouped skill list ──
        // Build groups: key → (label, ordered list of (index, skill, learnable))
        var groups = new List<(string Key, string Label, List<(int Idx, FightingSkill Skill, bool Learnable)> Skills)>();

        void AddSkill(FightingSkill skill, int idx, bool learnable)
        {
            string key = MediumKeyFor(skill);
            var entry = groups.FirstOrDefault(g => g.Key == key);
            if (entry.Key == null)
            {
                entry = (key, MediumLabelFor(skill), new List<(int, FightingSkill, bool)>());
                groups.Add(entry);
            }
            entry.Skills.Add((idx, skill, learnable));
        }
        for (int i = 0; i < unlockedSkills.Count; i++) AddSkill(unlockedSkills[i], i, false);
        for (int i = 0; i < learnableSkills.Count; i++) AddSkill(learnableSkills[i], i, true);

        // Stable ordering: organ mediums (alphabetical) first, then weapon mediums (alphabetical)
        groups = groups
            .OrderBy(g => g.Skills[0].Skill.Medium.Type == MediumType.OrganMedium ? 0 : 1)
            .ThenBy(g => g.Label)
            .ToList();

        foreach (var grp in groups)
        {
            if (y >= EndTurnButtonRow - 1) break;

            bool isExpanded = grp.Key == expandedMediumKey;
            bool hovHeader  = hoveredButtonRow == y;
            Vector4 headFg  = hovHeader ? Config.Colors.GoldYellow : Config.Colors.DarkYellowGrey;
            string marker   = isExpanded ? "▼" : "▶";
            string headLine = $"{marker} {grp.Label}";
            if (headLine.Length > LeftEnd - 2) headLine = headLine[..(LeftEnd - 2)];
            terminal.Text(x, y, headLine.PadRight(LeftEnd - 2), headFg, Config.Colors.Black);
            layout.Add(new LeftPanelRow(y, LeftPanelRowKind.Medium, grp.Key, -1));
            y++;

            if (!isExpanded) continue;

            foreach (var (idx, skill, learnable) in grp.Skills)
            {
                if (y >= EndTurnButtonRow - 1) break;
                bool sel = !isMoveMode
                    && (learnable ? idx == selectedLearnableSkillIndex : idx == selectedSkillIndex);
                bool hov = !sel && hoveredButtonRow == y;

                Vector4 fg, bg;
                if (learnable)
                {
                    fg = sel ? Config.Colors.Black
                       : hov ? Config.Colors.GoldYellow
                       : new Vector4(0.6f, 0.6f, 0.9f, 1f);
                    bg = sel ? new Vector4(0.4f, 0.4f, 0.9f, 1f) : Config.Colors.Black;
                }
                else
                {
                    fg = sel ? Config.Colors.Black
                       : hov ? Config.Colors.GoldYellow
                       : Config.Colors.White;
                    bg = sel ? Config.Colors.GoldYellow : Config.Colors.Black;
                }

                string prefix = learnable ? "  ? " : "    ";
                string line = $"{prefix}{skill.DisplayName} {skill.CineticPointsCost}CP";
                if (line.Length > LeftEnd - 2) line = line[..(LeftEnd - 2)];
                terminal.Text(x, y, line.PadRight(LeftEnd - 2), fg, bg);
                layout.Add(new LeftPanelRow(y,
                    learnable ? LeftPanelRowKind.LearnableSkill : LeftPanelRowKind.UnlockedSkill,
                    grp.Key, idx));
                y++;
            }
        }

        // Divider before end/run
        int divY = EndTurnButtonRow - 1;
        terminal.Text(x, divY, new string('─', LeftEnd - 2), Config.Colors.DarkGray, Config.Colors.Black);

        // END TURN button — no grey background
        {
            bool hov = hoveredButtonRow == EndTurnButtonRow;
            terminal.Text(x, EndTurnButtonRow,
                "END TURN        "[..Math.Min(16, LeftEnd - 2)],
                hov ? Config.Colors.GoldYellow : Config.Colors.White,
                Config.Colors.Black);
        }

        // RUN button — no grey background; visibly dim (not invisible) when disabled
        {
            bool onExit = fighter.X == FightArea.ExitCol && fighter.Y == FightArea.ExitRow;
            bool hov    = hoveredButtonRow == RunButtonRow;
            Vector4 runFg = !onExit    ? Config.Colors.DarkGray35
                          : hov        ? Config.Colors.Yellow
                          : Config.Colors.Orange;
            terminal.Text(x, RunButtonRow,
                "RUN             "[..Math.Min(16, LeftEnd - 2)],
                runFg, Config.Colors.Black);
        }

        return layout;
    }

    // ── Left-panel info box (bottom half) ─────────────────────────────

    /// <summary>What's being described in the left-panel info box.</summary>
    public enum LeftInfoKind { None, Move, EndTurn, Run, Skill, LearnableSkill }

    /// <summary>
    /// Draw the bottom-half action-info box (rows <see cref="LeftSplitRow"/>..<see cref="BotStart"/>).
    /// Shows details for the hovered/selected action.
    /// </summary>
    public static void RenderLeftInfoPanel(TerminalHUD terminal,
        LeftInfoKind kind, FightingSkill? skill, Fighter? fighter)
    {
        int boxY = LeftSplitRow;
        int boxH = BotStart - LeftSplitRow;
        terminal.FillRect(0, boxY, LeftEnd, boxH, ' ', Config.Colors.White, Config.Colors.Black);

        bool isLearn = kind == LeftInfoKind.LearnableSkill;
        Vector4 border = isLearn ? new Vector4(0.4f, 0.4f, 0.9f, 1f) : Config.Colors.DarkGray;
        terminal.DrawBox(0, boxY, LeftEnd, boxH, BoxStyle.Single, border, Config.Colors.Black);

        const int x = 1;
        int y = boxY + 1;
        int innerW = LeftEnd - 2;

        terminal.Text(x, y++, "INFO:", Config.Colors.DarkYellowGrey, Config.Colors.Black);
        terminal.Text(x, y++, new string('─', innerW), Config.Colors.DarkGray, Config.Colors.Black);

        void TextLine(string s, Vector4 col)
        {
            if (s.Length > innerW) s = s[..innerW];
            terminal.Text(x, y++, s, col, Config.Colors.Black);
        }
        void WrapText(string s, Vector4 col)
        {
            for (int i = 0; i < s.Length && y < boxY + boxH - 1; i += innerW)
                TextLine(s.Substring(i, Math.Min(innerW, s.Length - i)), col);
        }

        switch (kind)
        {
            case LeftInfoKind.Move:
                TextLine("MOVE", Config.Colors.Yellow);
                y++;
                WrapText("Walk across the arena tile by tile.", Config.Colors.White);
                if (fighter != null)
                {
                    y++;
                    int maxSteps = fighter.CurrentCineticPoints * Math.Max(1, fighter.MoveSpeed);
                    TextLine($"Range : {maxSteps} tiles", Config.Colors.LightGray);
                    TextLine($"Speed : {Math.Max(1, fighter.MoveSpeed)}/CP", Config.Colors.LightGray);
                }
                break;

            case LeftInfoKind.EndTurn:
                TextLine("END TURN", Config.Colors.Yellow);
                y++;
                WrapText("Pass to the next fighter.", Config.Colors.White);
                break;

            case LeftInfoKind.Run:
                TextLine("RUN", Config.Colors.Orange);
                y++;
                WrapText("Flee combat. Requires standing on the exit tile.", Config.Colors.White);
                if (fighter != null)
                {
                    y++;
                    TextLine($"Chance: {fighter.RunawayChancePercent}%", Config.Colors.LightGray);
                }
                break;

            case LeftInfoKind.Skill:
            case LeftInfoKind.LearnableSkill:
                if (skill == null || fighter == null) break;
                Vector4 titleC = isLearn ? new Vector4(0.6f, 0.6f, 0.9f, 1f) : Config.Colors.Yellow;
                TextLine(skill.DisplayName, titleC);
                TextLine($"Medium: {MediumLabelFor(skill)}", Config.Colors.DarkYellowGrey);
                if (isLearn) TextLine("(unknown — learn first)", titleC);
                y++;
                WrapText(skill.Description ?? "", Config.Colors.White);
                y++;
                TextLine($"Dice  : {skill.TotalDice(fighter)}", Config.Colors.LightGray);
                TextLine($"Cost  : {skill.CineticPointsCost} CP", Config.Colors.LightGray);
                TextLine($"Range : {skill.Range}", Config.Colors.LightGray);
                TextLine($"Effect: {skill.EffectType}", Config.Colors.LightGray);
                TextLine($"Wound : {skill.WoundTargetMode}", Config.Colors.LightGray);
                if (isLearn)
                {
                    int diff = Math.Max(0, skill.MediumPosition - 1);
                    int dice = Math.Max(1, fighter.FightLearningStat);
                    y++;
                    TextLine($"LEARN: {dice}d", titleC);
                    TextLine($"need {diff} sixes", titleC);
                }
                else if (skill.VitalHeatCost > 0)
                {
                    y++;
                    TextLine($"VH cost: {skill.VitalHeatCost}", Config.Colors.Orange);
                }
                break;

            default:
                TextLine("(hover or select", Config.Colors.DarkGray);
                TextLine(" an action)", Config.Colors.DarkGray);
                break;
        }
    }

    /// <summary>
    /// Draw a detailed info card for <paramref name="skill"/> across the full top panel.
    /// Used when the mouse is hovering a skill row in the left panel.
    /// </summary>
    public static void RenderSkillDetailPanel(TerminalHUD terminal, FightingSkill skill,
                                               Fighter fighter, bool isLearnable)
    {
        terminal.FillRect(0, 0, 100, TopRows, ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(0, 0, 100, TopRows, BoxStyle.Single,
            isLearnable ? new Vector4(0.4f, 0.4f, 0.9f, 1f) : Config.Colors.DarkGray,
            Config.Colors.Black);

        Vector4 titleColor = isLearnable ? new Vector4(0.6f, 0.6f, 0.9f, 1f) : Config.Colors.Yellow;
        string headerKind  = isLearnable ? "SKILL (learnable)" : "SKILL";
        terminal.Text(2, 1, $"{headerKind} — {skill.DisplayName}", titleColor, Config.Colors.Black);

        string mediumLabel = MediumLabelFor(skill);
        terminal.Text(2, 2, $"Medium: {mediumLabel}",
            Config.Colors.DarkYellowGrey, Config.Colors.Black);

        // ── Left column: description ──
        const int leftCol = 2;
        const int leftW   = 60;
        int row = 4;
        string desc = skill.Description ?? "";
        for (int s = 0; s < desc.Length && row < TopRows - 2; s += leftW)
        {
            terminal.Text(leftCol, row,
                desc.Substring(s, Math.Min(leftW, desc.Length - s)),
                Config.Colors.White, Config.Colors.Black);
            row++;
        }

        // ── Right column: stats ──
        const int rightCol = 66;
        int rRow = 3;
        terminal.Text(rightCol, rRow++, "STATS",
            Config.Colors.DarkYellowGrey, Config.Colors.Black);
        rRow++;
        terminal.Text(rightCol, rRow++, $"Dice  : {skill.TotalDice(fighter)}",
            Config.Colors.LightGray, Config.Colors.Black);
        terminal.Text(rightCol, rRow++, $"Cost  : {skill.CineticPointsCost} CP",
            Config.Colors.LightGray, Config.Colors.Black);
        terminal.Text(rightCol, rRow++, $"Range : {skill.Range}",
            Config.Colors.LightGray, Config.Colors.Black);
        terminal.Text(rightCol, rRow++, $"Effect: {skill.EffectType}",
            Config.Colors.LightGray, Config.Colors.Black);
        terminal.Text(rightCol, rRow++, $"Wound : {skill.WoundTargetMode}",
            Config.Colors.LightGray, Config.Colors.Black);

        if (isLearnable)
        {
            int diff = Math.Max(0, skill.MediumPosition - 1);
            int dice = Math.Max(1, fighter.FightLearningStat);
            terminal.Text(2, TopRows - 2,
                $"LEARN: roll {dice} cerebellum dice — need {diff} sixes to learn '{skill.RequiredModusMentisId}'",
                new Vector4(0.6f, 0.6f, 0.9f, 1f), Config.Colors.Black);
        }
        else if (skill.VitalHeatCost > 0)
        {
            terminal.Text(2, TopRows - 2,
                $"Vital heat cost: {skill.VitalHeatCost}",
                Config.Colors.Orange, Config.Colors.Black);
        }
    }

    /// <summary>Group key for a fighting skill — mirrors Fighter.GetLearnableSkills grouping.</summary>
    private static string MediumKeyFor(FightingSkill s) =>
        s.Medium.Type == MediumType.OrganMedium
            ? s.Medium.OrganId ?? s.SkillId
            : s.RequiredModusMentisId;

    /// <summary>Display label for a medium group header (e.g. "Hands", "Swordsmanship").</summary>
    private static string MediumLabelFor(FightingSkill s)
    {
        string raw = s.Medium.Type == MediumType.OrganMedium
            ? (s.Medium.OrganId ?? "?")
            : s.RequiredModusMentisId;
        raw = raw.Replace('_', ' ');
        return raw.Length == 0 ? raw : char.ToUpperInvariant(raw[0]) + raw[1..];
    }

    // ── Right panel — split: terrain legend (top) + initiative (bottom) ──

    /// <summary>Vertical split point inside the right panel (terrain above, initiative below).</summary>
    private const int RightSplitRow = 40;

    /// <summary>
    /// Draw the right panel: terrain legend on top, then the initiative list below.
    /// Returns the (Y, Fighter) pairs for the initiative entries so the adapter can
    /// route hover detection to the correct fighter.
    /// </summary>
    public static IReadOnlyList<(int Y, Fighter Fighter)> RenderRightPanel(
        TerminalHUD terminal, FightArea area, FightState state, int hoveredY)
    {
        int panelW = 100 - RightStart;

        // Clear the whole right column once
        terminal.FillRect(RightStart, TopRows, panelW, BotStart - TopRows,
            ' ', Config.Colors.White, Config.Colors.Black);

        // ── Top half: terrain legend ──
        int terrH = RightSplitRow - TopRows;
        terminal.DrawBox(RightStart, TopRows, panelW, terrH,
            BoxStyle.Single, Config.Colors.DarkGray, Config.Colors.Black);
        terminal.Text(RightStart + 1, TopRows + 1, "TERRAIN",
            Config.Colors.DarkYellowGrey, Config.Colors.Black);

        var entries = new (char Glyph, Vector4 Color, string Label)[]
        {
            ('⎆', Config.Colors.GoldYellow,      "Exit"),
            ('.', Config.Colors.DarkGray35,      "Free space"),
            ('·', Config.Colors.DarkYellowGrey,  "Soft (slow)"),
            ('~', Config.Colors.MediumYellow,    "Treacherous"),
            ('∴', Config.Colors.Purple,          "Dangerous"),
            ('#', Config.Colors.LightGray75,     "Hard obstacle"),
        };

        int maxLabel = panelW - 4;
        int y = TopRows + 3;
        foreach (var (glyph, color, label) in entries)
        {
            if (y >= RightSplitRow - 1) break;
            terminal.SetCell(RightStart + 1, y, glyph, color, Config.Colors.Black);
            string lbl = label.Length > maxLabel ? label[..maxLabel] : label;
            terminal.Text(RightStart + 3, y, lbl, Config.Colors.LightGray, Config.Colors.Black);
            y += 2;
        }

        // ── Bottom half: initiative list ──
        int initH = BotStart - RightSplitRow;
        terminal.DrawBox(RightStart, RightSplitRow, panelW, initH,
            BoxStyle.Single, Config.Colors.DarkGray, Config.Colors.Black);
        terminal.Text(RightStart + 1, RightSplitRow + 1, "INITIATIVE",
            Config.Colors.DarkYellowGrey, Config.Colors.Black);

        var rows = new List<(int Y, Fighter Fighter)>();
        int iy = RightSplitRow + 3;
        for (int i = 0; i < state.Fighters.Count && iy < BotStart - 1; i++)
        {
            var f = state.Fighters[i];
            bool isActive = i == state.ActiveFighterIndex;
            bool isDead   = !f.IsAlive;
            bool isHover  = hoveredY == iy;

            Vector4 fg = isDead   ? Config.Colors.DarkGray
                       : isActive ? Config.Colors.Yellow
                       : f.Faction == FighterFaction.Party
                            ? Config.Colors.White
                            : Config.Colors.Purple;
            Vector4 bg = isHover ? Config.Colors.DarkGray : Config.Colors.Black;

            string mark = isActive ? "▶" : " ";
            string suffix = isDead ? "DEAD"
                          : $"{f.CurrentHp}/{f.MaxHp}";
            string nameMax = f.DisplayName.Length > 9 ? f.DisplayName[..9] : f.DisplayName;
            string line = $"{mark}{f.DisplayChar} {nameMax,-9} {suffix}";
            if (line.Length > panelW - 2) line = line[..(panelW - 2)];
            terminal.Text(RightStart + 1, iy, line.PadRight(panelW - 2), fg, bg);
            rows.Add((iy, f));
            iy++;
        }

        return rows;
    }

    // ── Bottom panel — action log ─────────────────────────────────────

    // Color mapping for log entry types
    private static Vector4 LogEntryColor(LogEntryType type) => type switch
    {
        LogEntryType.Attack        => new Vector4(1.0f, 0.8f, 0.0f, 1.0f), // OrangeYellow
        LogEntryType.Miss          => Config.Colors.DarkGray,
        LogEntryType.Wound         => new Vector4(1.0f, 0.15f, 0.15f, 1.0f), // BrightRed
        LogEntryType.SpecialEffect => new Vector4(1.0f, 0.5f, 0.0f, 1.0f),  // Orange
        LogEntryType.Learning      => new Vector4(0.0f, 1.0f, 1.0f, 1.0f),  // Cyan
        LogEntryType.Defense       => new Vector4(0.7f, 0.9f, 1.0f, 1.0f),  // LightCyan
        _                          => Config.Colors.White,
    };

    /// <summary>Draw the action log in the bottom panel (full width, newest entries at bottom).</summary>
    public static void RenderBottomPanel(TerminalHUD terminal,
        IReadOnlyList<(string Text, LogEntryType Type)> actionLog, int scrollOffset)
    {
        int panelH = 100 - BotStart;
        terminal.FillRect(0, BotStart, 100, panelH, ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(0, BotStart, 100, panelH, BoxStyle.Single, Config.Colors.DarkGray, Config.Colors.Black);

        terminal.Text(1, BotStart + 1, "LOG", Config.Colors.DarkYellowGrey, Config.Colors.Black);

        int lineWidth = 98;
        int visibleLines = panelH - 3;

        var wrappedLines = new List<(string Text, LogEntryType Type)>();
        foreach (var (text, type) in actionLog)
        {
            if (text.Length <= lineWidth)
            {
                wrappedLines.Add((text, type));
            }
            else
            {
                for (int s = 0; s < text.Length; s += lineWidth)
                    wrappedLines.Add((text.Substring(s, Math.Min(lineWidth, text.Length - s)), type));
            }
        }

        int total = wrappedLines.Count;
        int firstVisible = Math.Max(0, total - visibleLines - scrollOffset);
        for (int i = 0; i < visibleLines && firstVisible + i < total; i++)
        {
            var (text, type) = wrappedLines[firstVisible + i];
            terminal.Text(1, BotStart + 3 + i, text,
                LogEntryColor(type), Config.Colors.Black);
        }
    }

    // ── Center panel ─────────────────────────────────────────────────

    /// <summary>
    /// Render arena terrain, optional tile highlights, and fighter glyphs.
    /// <paramref name="highlightCells"/> is in arena coords (0-59). Pass null for no overlay.
    /// When a highlight set is active, tiles OUTSIDE it are dimmed so the valid range stands out.
    /// Active fighter is rendered with a bright background so it stands out.
    /// </summary>
    public static void RenderCenterPanel(TerminalHUD terminal, FightArea area,
                                          IEnumerable<Fighter> fighters, Fighter? activeFighter,
                                          bool blinkOn,
                                          HashSet<(int X, int Y)>? highlightCells,
                                          bool isAttackHighlight = false,
                                          IReadOnlyList<(int X, int Y)>? previewPath = null,
                                          HashSet<(int X, int Y)>? hoverCells = null)
    {
        // Always render the full terrain so state changes (hover/selection ending) are
        // properly reflected — without this, dimmed cells would persist after hover ends.
        for (int ay = 0; ay < FightArea.Height; ay++)
        for (int ax = 0; ax < FightArea.Width; ax++)
        {
            var cell = area.GetCell(ax, ay);

            // Hover preview: blink by swapping fg/bg each blink cycle
            if (hoverCells != null && hoverCells.Contains((ax, ay)))
            {
                var hfg = new Vector4(
                    Math.Min(1f, cell.TextColor.X + 0.45f),
                    Math.Min(1f, cell.TextColor.Y + 0.45f),
                    Math.Min(1f, cell.TextColor.Z + 0.45f), 1f);
                if (blinkOn)
                    terminal.SetCell(CenterX + ax, CenterY + ay, cell.Glyph, Config.Colors.Black, hfg);
                else
                    terminal.SetCell(CenterX + ax, CenterY + ay, cell.Glyph, hfg, Config.Colors.Black);
                continue;
            }

            Vector4 fg;
            if (highlightCells != null)
            {
                // Boost toward white so in-range tiles are clearly readable
                fg = highlightCells.Contains((ax, ay))
                    ? new Vector4(
                        Math.Min(1f, cell.TextColor.X + 0.45f),
                        Math.Min(1f, cell.TextColor.Y + 0.45f),
                        Math.Min(1f, cell.TextColor.Z + 0.45f), 1f)
                    : new Vector4(cell.TextColor.X * 0.25f, cell.TextColor.Y * 0.25f,
                                  cell.TextColor.Z * 0.25f, 1f);
                terminal.SetCell(CenterX + ax, CenterY + ay, cell.Glyph, fg, Config.Colors.Black);
            }
            else if (hoverCells != null)
            {
                // Hover active but this cell is not a target — dim it
                fg = new Vector4(cell.TextColor.X * 0.25f, cell.TextColor.Y * 0.25f,
                                 cell.TextColor.Z * 0.25f, 1f);
                terminal.SetCell(CenterX + ax, CenterY + ay, cell.Glyph, fg, Config.Colors.Black);
            }
            else
            {
                // No highlight, no hover — restore full brightness
                terminal.SetCell(CenterX + ax, CenterY + ay, cell.Glyph, cell.TextColor, cell.BgColor);
            }
        }

        // Re-apply exit-tile animation after the terrain loop so it isn't overwritten
        FightAreaRenderer.UpdateBlink(terminal, blinkOn);

        // Path preview: dots on intermediate steps, circle on destination
        if (previewPath != null && previewPath.Count > 0)
        {
            var pathDotColor = new Vector4(0.95f, 0.95f, 0.55f, 1f); // warm white-yellow
            // Intermediate tiles (skip first = fighter start if path excludes origin;
            // skip last = destination shown as circle)
            for (int i = 0; i < previewPath.Count - 1; i++)
            {
                var (px, py) = previewPath[i];
                terminal.SetCell(CenterX + px, CenterY + py, '·', pathDotColor, Config.Colors.Black);
            }
            var (dx, dy) = previewPath[^1];
            terminal.SetCell(CenterX + dx, CenterY + dy, '○',
                new Vector4(1f, 1f, 0.5f, 1f), Config.Colors.Black);
        }

        // Overlay fighters
        var fList = fighters.Where(f => f.IsAlive);
        foreach (var f in fList)
        {
            int tx = CenterX + f.X;
            int ty = CenterY + f.Y;
            bool isActive = f == activeFighter;

            if (hoverCells != null && hoverCells.Contains((f.X, f.Y)))
            {
                var hfg = new Vector4(
                    Math.Min(1f, f.DisplayColor.X + 0.45f),
                    Math.Min(1f, f.DisplayColor.Y + 0.45f),
                    Math.Min(1f, f.DisplayColor.Z + 0.45f), 1f);
                if (blinkOn)
                    terminal.SetCell(tx, ty, f.DisplayChar, Config.Colors.Black, hfg);
                else
                    terminal.SetCell(tx, ty, f.DisplayChar, hfg, Config.Colors.Black);
            }
            else
            {
                Vector4 bg = isActive ? ActiveFighterBg : Config.Colors.Black;
                terminal.SetCell(tx, ty, f.DisplayChar, f.DisplayColor, bg);
            }
        }
    }

    // ── Dice overlay ─────────────────────────────────────────────────

    /// <summary>Draw the dice roll animation overlay, centered in the arena.</summary>
    public static bool RenderDiceOverlay(TerminalHUD terminal, DiceRollComponent dice, bool continueHovered)
    {
        int cx = CenterX + FightArea.Width / 2;
        int cy = CenterY + FightArea.Height / 2;
        return dice.Render(terminal, cx, cy, continueHovered);
    }

    // ── Body-part selection menu ──────────────────────────────────────

    /// <summary>
    /// Render a numbered menu over the left panel for PlayerChooses wound targeting.
    /// Returns the list of body-part ids in display order (index = key 1-9).
    /// </summary>
    // ── Body-part menu overlay geometry ────────────────────────────────
    private const int BodyMenuW  = 30;
    private const int BodyMenuH  = 14;
    private const int BodyMenuX  = CenterX + (FightArea.Width  - BodyMenuW)  / 2; // centered in arena
    private const int BodyMenuY  = CenterY + (FightArea.Height - BodyMenuH) / 2;

    /// <summary>
    /// Render a numbered body-part selection overlay, centered in the arena.
    /// Returns the list of body-part ids in display order (index = key 1-9).
    /// </summary>
    public static IReadOnlyList<string> RenderBodyPartMenu(TerminalHUD terminal, Fighter target)
    {
        var parts = target.Member.BodyParts
            .Select(bp => bp.Id)
            .Distinct()
            .Take(9)
            .ToList();

        // Black background fill + double border over the arena
        terminal.FillRect(BodyMenuX, BodyMenuY, BodyMenuW, BodyMenuH,
            ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(BodyMenuX, BodyMenuY, BodyMenuW, BodyMenuH,
            BoxStyle.Double, Config.Colors.Orange, Config.Colors.Black);

        int x = BodyMenuX + 1;
        int y = BodyMenuY + 1;
        terminal.Text(x, y++, $"AIM AT: {target.DisplayName}", Config.Colors.Orange, Config.Colors.Black);
        terminal.Text(x, y++, new string('─', BodyMenuW - 2), Config.Colors.DarkGray, Config.Colors.Black);

        for (int i = 0; i < parts.Count; i++)
        {
            terminal.Text(x, y++, $"{i + 1}: {parts[i]}", Config.Colors.White, Config.Colors.Black);
        }

        return parts;
    }

    /// <summary>Returns the arena-relative row offset for body-part menu items (for click dispatch).</summary>
    public static (int StartRow, int StartCol) BodyPartMenuItemOrigin()
        => (BodyMenuY + 3, BodyMenuX + 1); // 2 header rows

    // ── Fight-end overlay ─────────────────────────────────────────────

    public static void RenderFightEnd(TerminalHUD terminal, FightResult result)
    {
        string msg = result switch
        {
            FightResult.PartyWon   => "VICTORY!",
            FightResult.EnemyWon   => "DEFEAT...",
            FightResult.PartyFled  => "YOU FLED.",
            _                      => "FIGHT OVER"
        };
        Vector4 color = result == FightResult.PartyWon ? Config.Colors.LightGreen
                      : result == FightResult.EnemyWon ? Config.Colors.BrightPurple
                      : Config.Colors.Orange;

        int cx = 50 - msg.Length / 2;
        int cy = 50;
        terminal.FillRect(cx - 2, cy - 1, msg.Length + 4, 3, ' ', color, Config.Colors.Black);
        terminal.DrawBox(cx - 2, cy - 1, msg.Length + 4, 3, BoxStyle.Double, color, Config.Colors.Black);
        terminal.Text(cx, cy, msg, Config.Colors.Black, color);

        terminal.Text(cx - 4, cy + 3, "Press ENTER or ESC to exit", Config.Colors.DarkYellowGrey, Config.Colors.Black);
    }

    // ── Private helpers ───────────────────────────────────────────────

    private static void DrawDotBar(TerminalHUD terminal, int x, int y, int current, int max, Vector4 fullColor)
    {
        if (max <= 0) return;
        terminal.Text(x, y, "CP :", Config.Colors.DarkGray, Config.Colors.Black);
        int dotX = x + 5;
        for (int i = 0; i < max; i++)
        {
            Vector4 col = i < current ? fullColor : Config.Colors.DarkGray35;
            terminal.SetCell(dotX, y, Config.Symbols.NoeticPointMarker, col, Config.Colors.Black);
            dotX += 1;
        }
    }

    private static void DrawBar(TerminalHUD terminal, int x, int y, int maxWidth,
                                 int current, int max, string label,
                                 Vector4 fullColor, Vector4 lowColor)
    {
        if (max <= 0) return;
        int barW = maxWidth - label.Length - 2;
        if (barW < 1) return;

        int filled = (int)Math.Round((double)current / max * barW);
        filled = Math.Clamp(filled, 0, barW);

        Vector4 barColor = (double)current / max < 0.25 ? lowColor : fullColor;

        terminal.Text(x, y, $"{label}:", Config.Colors.DarkGray, Config.Colors.Black);
        for (int i = 0; i < barW; i++)
        {
            bool isFilled = i < filled;
            char ch       = isFilled ? '█' : '░';
            Vector4 col   = isFilled ? barColor : Config.Colors.DarkGray35;
            terminal.SetCell(x + label.Length + 1 + i, y, ch, col, Config.Colors.Black);
        }
    }
}
