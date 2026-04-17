using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;
using Cathedral.Game;
using Cathedral.Game.Narrative;

namespace Cathedral.Fight;

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

    // ── Left-panel button rows (public so FightModeWindow can dispatch clicks) ────
    public const int MoveButtonRow     = TopRows + 8;   // row 28
    public const int SkillButtonsStart = TopRows + 9;   // row 29, each skill +1 below
    public const int EndTurnButtonRow  = BotStart - 6;  // row 74
    public const int RunButtonRow      = BotStart - 5;  // row 75

    // ── Highlight \u2014 dim color for out-of-range tiles ─────────────────────
    private static readonly Vector4 ActiveFighterBg = new(0.35f, 0.22f, 0f, 1f); // dark amber

    // ── Top panel ─────────────────────────────────────────────────────

    /// <summary>Draw the initiative strip listing all fighters in initiative order.</summary>
    public static void RenderTopPanel(TerminalHUD terminal, FightState state)
    {
        terminal.FillRect(0, 0, 100, TopRows, ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(0, 0, 100, TopRows, BoxStyle.Single, Config.Colors.DarkGray, Config.Colors.Black);

        terminal.Text(2, 1, "INITIATIVE", Config.Colors.DarkYellowGrey, Config.Colors.Black);

        int x = 2;
        int y = 3;
        for (int i = 0; i < state.Fighters.Count; i++)
        {
            var f = state.Fighters[i];
            bool isActive = i == state.ActiveFighterIndex;
            bool isDead = !f.IsAlive;

            Vector4 fg = isDead       ? Config.Colors.DarkGray
                       : isActive     ? Config.Colors.Yellow
                       : f.Faction == FighterFaction.Party ? Config.Colors.White
                       : Config.Colors.Purple;
            Vector4 bg = isActive ? Config.Colors.DarkGray : Config.Colors.Black;

            string mark = isActive ? "▶ " : "  ";
            string label = $"{mark}{f.DisplayChar} {f.DisplayName} HP:{f.CurrentHp}/{f.MaxHp} CP:{f.CurrentCineticPoints}/{f.MaxCineticPoints}";
            if (isDead) label += " [DEAD]";

            // Wrap to next row if not enough space
            if (x + label.Length + 2 > RightStart)
            {
                x = 2;
                y += 2;
                if (y >= TopRows - 2) break;
            }

            terminal.Text(x, y, label, fg, bg);
            x += label.Length + 3;
        }
    }

    // ── Left panel ────────────────────────────────────────────────────

    /// <summary>
    /// Draw detail for <paramref name="fighter"/>: HP/CP bars plus clickable action buttons.
    /// <paramref name="isMoveMode"/> highlights the MOVE button; <paramref name="selectedSkillIndex"/> ≥ 0 highlights that skill.
    /// </summary>
    public static void RenderLeftPanel(TerminalHUD terminal, Fighter fighter,
                                        IReadOnlyList<FightingSkill> unlockedSkills,
                                        bool isMoveMode, int selectedSkillIndex,
                                        int hoveredButtonRow = -1)
    {
        terminal.FillRect(0, TopRows, LeftEnd, BotStart - TopRows, ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(0, TopRows, LeftEnd, BotStart - TopRows, BoxStyle.Single, Config.Colors.DarkGray, Config.Colors.Black);

        const int x = 1;
        int y = TopRows + 1;

        // Fighter name
        string name = fighter.DisplayName.Length > LeftEnd - 2
            ? fighter.DisplayName[..(LeftEnd - 2)]
            : fighter.DisplayName;
        terminal.Text(x, y++, name, Config.Colors.Yellow, Config.Colors.Black);
        y++; // blank

        // HP bar (row TopRows+3)
        DrawBar(terminal, x, y++, LeftEnd - 2, fighter.CurrentHp, fighter.MaxHp,
            "HP", Config.Colors.Yellow, Config.Colors.DarkPurple);

        // CP dot bar (row TopRows+4)
        DrawDotBar(terminal, x, y++, fighter.CurrentCineticPoints, fighter.MaxCineticPoints);

        y++; // blank

        // "ACTIONS:" label
        terminal.Text(x, y++, "ACTIONS:", Config.Colors.DarkYellowGrey, Config.Colors.Black);
        // divider
        terminal.Text(x, y++, new string('─', LeftEnd - 2), Config.Colors.DarkGray, Config.Colors.Black);

        // y is now at MoveButtonRow (28)
        // MOVE button
        {
            bool sel = isMoveMode;
            bool hov = !sel && hoveredButtonRow == MoveButtonRow;
            Vector4 fg = sel ? Config.Colors.Black
                       : hov ? Config.Colors.GoldYellow
                       : Config.Colors.White;
            Vector4 bg = sel ? Config.Colors.Yellow : Config.Colors.DarkGray;
            string label = (sel ? "* MOVE          " : "  MOVE          ")[..Math.Min(16, LeftEnd - 2)];
            terminal.Text(x, y++, label, fg, bg); // row MoveButtonRow
        }

        // Skill buttons (start at SkillButtonsStart = 29)
        for (int i = 0; i < unlockedSkills.Count && y < EndTurnButtonRow - 1; i++)
        {
            var skill = unlockedSkills[i];
            bool sel = !isMoveMode && i == selectedSkillIndex;
            bool hov = !sel && hoveredButtonRow == SkillButtonsStart + i;
            Vector4 fg = sel ? Config.Colors.Black
                       : hov ? Config.Colors.GoldYellow
                       : Config.Colors.White;
            Vector4 bg = sel ? Config.Colors.GoldYellow : Config.Colors.Black;
            string line = $"{i + 1} {skill.DisplayName} {skill.CineticPointsCost}CP";
            if (line.Length > LeftEnd - 2) line = line[..(LeftEnd - 2)];
            line = line.PadRight(LeftEnd - 2);
            terminal.Text(x, y++, line, fg, bg); // row SkillButtonsStart + i
        }

        // Divider before end/run
        int divY = EndTurnButtonRow - 1;
        terminal.Text(x, divY, new string('─', LeftEnd - 2), Config.Colors.DarkGray, Config.Colors.Black);

        // END TURN button (row 74)
        {
            bool hov = hoveredButtonRow == EndTurnButtonRow;
            terminal.Text(x, EndTurnButtonRow,
                "END TURN        "[..Math.Min(16, LeftEnd - 2)],
                hov ? Config.Colors.GoldYellow : Config.Colors.White,
                Config.Colors.DarkGray);
        }

        // RUN button (row 75) — greyed out unless on exit tile
        {
            bool onExit = fighter.X == FightArea.ExitCol && fighter.Y == FightArea.ExitRow;
            bool hov    = hoveredButtonRow == RunButtonRow;
            Vector4 runFg = !onExit    ? Config.Colors.DarkGray
                          : hov        ? Config.Colors.Yellow
                          : Config.Colors.Orange;
            terminal.Text(x, RunButtonRow,
                "RUN             "[..Math.Min(16, LeftEnd - 2)],
                runFg, Config.Colors.DarkGray);
        }
    }

    // ── Right panel — terrain legend ─────────────────────────────────

    /// <summary>Draw the terrain legend in the right panel (vertical layout, 20 cols wide).</summary>
    public static void RenderRightPanel(TerminalHUD terminal, FightArea area)
    {
        int panelW = 100 - RightStart;
        int panelH = BotStart - TopRows;
        terminal.FillRect(RightStart, TopRows, panelW, panelH, ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(RightStart, TopRows, panelW, panelH, BoxStyle.Single, Config.Colors.DarkGray, Config.Colors.Black);

        terminal.Text(RightStart + 1, TopRows + 1, "TERRAIN", Config.Colors.DarkYellowGrey, Config.Colors.Black);

        var entries = new (char Glyph, Vector4 Color, string Label)[]
        {
            ('⎆', Config.Colors.GoldYellow,      "Exit (⎆ blink)"),
            ('.', Config.Colors.DarkGray35,      "Free space"),
            ('·', Config.Colors.DarkYellowGrey,  "Soft (slow)"),
            ('~', Config.Colors.MediumYellow,    "Treacherous"),
            ('∴', Config.Colors.Purple,           "Dangerous"),
            ('#', Config.Colors.LightGray75,     "Hard obstacle"),
        };

        int maxLabel = panelW - 4; // glyph + space + label
        int y = TopRows + 3;
        foreach (var (glyph, color, label) in entries)
        {
            if (y >= BotStart - 1) break;
            terminal.SetCell(RightStart + 1, y, glyph, color, Config.Colors.Black);
            string lbl = label.Length > maxLabel ? label[..maxLabel] : label;
            terminal.Text(RightStart + 3, y, lbl, Config.Colors.LightGray, Config.Colors.Black);
            y += 2;
        }
    }

    // ── Bottom panel — action log ─────────────────────────────────────

    /// <summary>Draw the action log in the bottom panel (full width, newest entries at bottom).</summary>
    public static void RenderBottomPanel(TerminalHUD terminal, IReadOnlyList<string> actionLog, int scrollOffset)
    {
        int panelH = 100 - BotStart;
        terminal.FillRect(0, BotStart, 100, panelH, ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(0, BotStart, 100, panelH, BoxStyle.Single, Config.Colors.DarkGray, Config.Colors.Black);

        terminal.Text(1, BotStart + 1, "LOG", Config.Colors.DarkYellowGrey, Config.Colors.Black);

        int lineWidth = 98;
        int visibleLines = panelH - 3;

        var wrappedLines = new List<string>();
        foreach (var entry in actionLog)
        {
            if (entry.Length <= lineWidth)
            {
                wrappedLines.Add(entry);
            }
            else
            {
                for (int s = 0; s < entry.Length; s += lineWidth)
                    wrappedLines.Add(entry.Substring(s, Math.Min(lineWidth, entry.Length - s)));
            }
        }

        int total = wrappedLines.Count;
        int firstVisible = Math.Max(0, total - visibleLines - scrollOffset);
        for (int i = 0; i < visibleLines && firstVisible + i < total; i++)
            terminal.Text(1, BotStart + 3 + i, wrappedLines[firstVisible + i],
                Config.Colors.White, Config.Colors.Black);
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
                                          IReadOnlyList<(int X, int Y)>? previewPath = null)
    {
        FightAreaRenderer.UpdateBlink(terminal, blinkOn);

        // Dim out-of-range tiles; brighten in-range tiles so the clickable area is obvious.
        if (highlightCells != null)
        {
            for (int ay = 0; ay < FightArea.Height; ay++)
            for (int ax = 0; ax < FightArea.Width; ax++)
            {
                var cell  = area.GetCell(ax, ay);
                Vector4 fg;
                if (highlightCells.Contains((ax, ay)))
                {
                    // Boost toward white so in-range tiles are clearly readable
                    fg = new Vector4(
                        Math.Min(1f, cell.TextColor.X + 0.45f),
                        Math.Min(1f, cell.TextColor.Y + 0.45f),
                        Math.Min(1f, cell.TextColor.Z + 0.45f), 1f);
                }
                else
                {
                    fg = new Vector4(cell.TextColor.X * 0.25f, cell.TextColor.Y * 0.25f,
                                     cell.TextColor.Z * 0.25f, 1f);
                }
                terminal.SetCell(CenterX + ax, CenterY + ay, cell.Glyph, fg, Config.Colors.Black);
            }
        }

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
            Vector4 bg = isActive ? ActiveFighterBg : Config.Colors.Black;
            terminal.SetCell(tx, ty, f.DisplayChar, f.DisplayColor, bg);
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

    // ── Action button popup ───────────────────────────────────────────

    /// <summary>Fill a <see cref="PopupTerminalHUD"/> with info about the hovered left-panel button.</summary>
    public static void RenderActionPopup(PopupTerminalHUD popup, int buttonRow,
                                          IReadOnlyList<FightingSkill> skills, Fighter? fighter)
    {
        popup.Fill(0, 0, popup.Width, popup.Height, ' ', Config.Colors.White, Config.Colors.Black);
        popup.DrawBox(0, 0, popup.Width, popup.Height, Config.Colors.DarkGray, Config.Colors.Black);

        int y = 1;
        int w = popup.Width - 2;

        if (buttonRow == MoveButtonRow)
        {
            popup.DrawText(1, y++, "MOVE", Config.Colors.Yellow, Config.Colors.Black);
            y++;
            popup.DrawText(1, y++, "Move across the arena.", Config.Colors.White, Config.Colors.Black);
            if (fighter != null)
            {
                y++;
                int maxSteps = fighter.CurrentCineticPoints * Math.Max(1, fighter.MoveSpeed);
                popup.DrawText(1, y++, $"Range : {maxSteps} tiles", Config.Colors.DarkYellowGrey, Config.Colors.Black);
                popup.DrawText(1, y++, $"Speed : {Math.Max(1, fighter.MoveSpeed)} tile(s)/CP", Config.Colors.DarkYellowGrey, Config.Colors.Black);
            }
        }
        else if (buttonRow == EndTurnButtonRow)
        {
            popup.DrawText(1, y++, "END TURN", Config.Colors.Yellow, Config.Colors.Black);
            y++;
            popup.DrawText(1, y++, "Pass to the next fighter.", Config.Colors.White, Config.Colors.Black);
        }
        else if (buttonRow == RunButtonRow)
        {
            popup.DrawText(1, y++, "RUN", Config.Colors.Orange, Config.Colors.Black);
            y++;
            popup.DrawText(1, y++, "Flee from combat.", Config.Colors.White, Config.Colors.Black);
            y++;
            popup.DrawText(1, y++, "Requires standing on", Config.Colors.DarkYellowGrey, Config.Colors.Black);
            popup.DrawText(1, y++, "the exit tile (\u2386).", Config.Colors.DarkYellowGrey, Config.Colors.Black);
        }
        else
        {
            int skillIdx = buttonRow - SkillButtonsStart;
            if (skillIdx >= 0 && skillIdx < skills.Count && fighter != null)
            {
                var skill = skills[skillIdx];
                popup.DrawText(1, y++, skill.DisplayName, Config.Colors.Yellow, Config.Colors.Black);
                y++;
                // Wrap description to popup width
                string desc = skill.Description;
                for (int s = 0; s < desc.Length && y < popup.Height - 4; s += w)
                {
                    popup.DrawText(1, y++, desc.Substring(s, Math.Min(w, desc.Length - s)),
                        Config.Colors.White, Config.Colors.Black);
                }
                y++;
                popup.DrawText(1, y++, $"Dice  : {skill.TotalDice(fighter)}  Range: {skill.Range}",
                    Config.Colors.DarkYellowGrey, Config.Colors.Black);
                popup.DrawText(1, y++, $"Cost  : {skill.CineticPointsCost}CP",
                    Config.Colors.DarkYellowGrey, Config.Colors.Black);
                popup.DrawText(1, y++, $"Effect: {skill.EffectType}",
                    Config.Colors.DarkYellowGrey, Config.Colors.Black);
            }
        }
    }

    // ── Fighter hover popup ───────────────────────────────────────────

    /// <summary>Fill a <see cref="PopupTerminalHUD"/> with fighter stats for the hover tooltip.</summary>
    public static void RenderFighterPopup(PopupTerminalHUD popup, Fighter fighter)
    {
        // Fill entire popup with black before drawing anything (transparent cells show through)
        popup.Fill(0, 0, popup.Width, popup.Height, ' ', Config.Colors.White, Config.Colors.Black);
        popup.DrawBox(0, 0, popup.Width, popup.Height, Config.Colors.DarkGray, Config.Colors.Black);

        int y = 1;
        popup.DrawText(1, y++, fighter.DisplayName, Config.Colors.Yellow, Config.Colors.Black);
        popup.DrawText(1, y++, $"HP: {fighter.CurrentHp}/{fighter.MaxHp}", Config.Colors.Yellow, Config.Colors.Black);
        popup.DrawText(1, y++, $"CP: {fighter.CurrentCineticPoints}/{fighter.MaxCineticPoints}", Config.Colors.Yellow, Config.Colors.Black);
        popup.DrawText(1, y++, $"DEF: {fighter.NaturalDefense}", Config.Colors.White, Config.Colors.Black);

        if (fighter.Member.Wounds.Count > 0)
        {
            y++;
            popup.DrawText(1, y++, "Wounds:", Config.Colors.BrightPurple, Config.Colors.Black);
            foreach (var w in fighter.Member.Wounds.Take(5))
                popup.DrawText(2, y++, w.WoundName, Config.Colors.Purple, Config.Colors.Black);
        }
    }

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

    private static void DrawDotBar(TerminalHUD terminal, int x, int y, int current, int max)
    {
        if (max <= 0) return;
        terminal.Text(x, y, "CP :", Config.Colors.DarkGray, Config.Colors.Black);
        int dotX = x + 5;
        for (int i = 0; i < max; i++)
        {
            Vector4 col = i < current ? Config.Colors.Yellow : Config.Colors.DarkGray35;
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
