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
    UnaffordableSkill,
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
    public  const int CenterX    = 20;  // Matches FightAreaRenderer.OffsetX
    public  const int CenterY    = 20;  // Matches FightAreaRenderer.OffsetY

    // ── Top panel split: action menu on the left half, action info on the right half ──
    /// <summary>X boundary inside the top panel: cols [0..ActionMenuRight) = action menu, [ActionMenuRight..100) = info.</summary>
    public const int ActionMenuRight   = 50;
    public const int MoveButtonRow     = 3;             // top of action menu (just below header + divider)
    public const int SkillButtonsStart = 4;             // each skill +1 below
    public const int EndTurnButtonRow  = TopRows - 3;   // row 17
    public const int RunButtonRow      = TopRows - 2;   // row 18

    // ── Highlight \u2014 dim color for out-of-range tiles ─────────────────────
    private static readonly Vector4 ActiveFighterBg = new(0.35f, 0.22f, 0f, 1f); // dark amber

    // ── Left pan — vertical fighter detail (cols 0..LeftEnd, rows TopRows..BotStart) ──

    /// <summary>
    /// Draw the active or hovered fighter's detail card stacked vertically in the left pan.
    /// </summary>
    public static IReadOnlyList<(int Y, FightStatusEffect Effect)> RenderDetailPanel(
        TerminalHUD terminal, Fighter? fighter, bool isHoverOverride,
        int hoveredStateY = -1)
    {
        int boxH = BotStart - TopRows;
        terminal.FillRect(0, TopRows, LeftEnd, boxH, ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(0, TopRows, LeftEnd, boxH, BoxStyle.Single, Config.Colors.DarkGray, Config.Colors.Black);

        const int x = 1;
        int innerW = LeftEnd - 2;
        int y = TopRows + 1;

        if (fighter == null)
        {
            terminal.Text(x, y, "DETAIL", Config.Colors.DarkYellowGrey, Config.Colors.Black);
            return Array.Empty<(int Y, FightStatusEffect Effect)>();
        }

        Vector4 nameColor = fighter.Faction == FighterFaction.Party
            ? Config.Colors.Yellow : Config.Colors.BrightPurple;
        string factionTag = fighter.Faction == FighterFaction.Party ? "PARTY" : "ENEMY";
        bool isEnemy = fighter.Faction == FighterFaction.Enemy;
        Vector4 barFull = isEnemy ? Config.Colors.BrightPurple : Config.Colors.Yellow;
        Vector4 dotFull = isEnemy ? Config.Colors.BrightPurple : Config.Colors.Yellow;

        // Faction tag + name (truncated)
        terminal.Text(x, y++, factionTag, nameColor, Config.Colors.Black);
        string name = fighter.DisplayName.Length > innerW
            ? fighter.DisplayName[..innerW] : fighter.DisplayName;
        terminal.Text(x, y++, name, nameColor, Config.Colors.Black);

        if (isHoverOverride)
            terminal.Text(x, y++, "(hovered)", Config.Colors.DarkYellowGrey, Config.Colors.Black);
        else
            y++; // keep spacing stable

        if (!fighter.IsAlive)
            terminal.Text(x, y++, "[DEAD]", Config.Colors.DarkGray, Config.Colors.Black);
        else
            y++;

        // HP bar
        DrawBar(terminal, x, y++, innerW, fighter.CurrentHp, fighter.MaxHp,
            "HP", barFull, Config.Colors.DarkPurple);
        terminal.Text(x, y++, $"  {fighter.CurrentHp}/{fighter.MaxHp}",
            Config.Colors.White, Config.Colors.Black);

        // CP dots
        terminal.Text(x, y, "CP:", Config.Colors.DarkGray, Config.Colors.Black);
        int dotX = x + 4;
        int cpMax = Math.Max(1, fighter.MaxCineticPoints);
        for (int i = 0; i < cpMax && dotX < x + innerW; i++)
        {
            Vector4 col = i < fighter.CurrentCineticPoints ? dotFull : Config.Colors.DarkGray35;
            terminal.SetCell(dotX, y, Config.Symbols.NoeticPointMarker, col, Config.Colors.Black);
            dotX++;
        }
        y++;
        terminal.Text(x, y++, $"  {fighter.CurrentCineticPoints}/{fighter.MaxCineticPoints}",
            Config.Colors.White, Config.Colors.Black);

        // FX (status effects)
        if (fighter.ActiveEffects.Count > 0)
        {
            var sb = new System.Text.StringBuilder("FX:");
            foreach (var fx in fighter.ActiveEffects) sb.Append(' ').Append(fx.DisplayLabel);
            string fxLine = sb.ToString();
            if (fxLine.Length > innerW) fxLine = fxLine[..innerW];
            terminal.Text(x, y++, fxLine, Config.Colors.BrightPurple, Config.Colors.Black);
        }

        y++; // spacer

        // Combat stats — one per line. DEF and MOV show the EFFECTIVE figures, so a stance or a
        // sprint is visible in the numbers the player is actually going to be measured by.
        int def = fighter.NaturalDefense + fighter.BonusDefenseDice;
        string defSuffix = fighter.BonusDefenseDice > 0 ? $" (+{fighter.BonusDefenseDice})" : "";
        string movSuffix = fighter.EffectiveMoveSpeed != Math.Max(1, fighter.MoveSpeed) ? " ×" : "";
        terminal.Text(x, y++, $"INIT: {fighter.InitiativeValue}", Config.Colors.LightGray, Config.Colors.Black);
        terminal.Text(x, y++, $"ATK : {fighter.NaturalAttack}",   Config.Colors.LightGray, Config.Colors.Black);
        terminal.Text(x, y++, $"DEF : {def}{defSuffix}",          Config.Colors.LightGray, Config.Colors.Black);
        terminal.Text(x, y++, $"MOV : {fighter.EffectiveMoveSpeed}{movSuffix}", Config.Colors.LightGray, Config.Colors.Black);
        terminal.Text(x, y++, $"RUN : {fighter.RunawayDiceCount}d",Config.Colors.LightGray, Config.Colors.Black);
        terminal.Text(x, y++, $"MCP : {fighter.MaxCineticPoints}", Config.Colors.LightGray, Config.Colors.Black);

        y++; // spacer

        // STATE — active status effects with hover-to-describe
        terminal.Text(x, y++, "STATE", Config.Colors.DarkYellowGrey, Config.Colors.Black);

        var stateLayout = new List<(int Y, FightStatusEffect Effect)>();
        var effects = fighter.ActiveEffects;
        if (effects.Count == 0)
        {
            terminal.Text(x, y++, "(none)", Config.Colors.DarkGray, Config.Colors.Black);
        }
        else
        {
            // Reserve at least 5 lines at the bottom for the hovered effect's description.
            int rowsForList = Math.Max(1, BotStart - y - 6);
            int shown = Math.Min(effects.Count, rowsForList);
            for (int i = 0; i < shown; i++)
            {
                var fx = effects[i];
                bool isHov = hoveredStateY == y;
                terminal.Text(x, y, fx.DisplayLabel, fx.DisplayColor, Config.Colors.Black);
                string nm = fx.DisplayName;
                int nmMax = innerW - 4;
                if (nm.Length > nmMax) nm = nm[..nmMax];
                terminal.Text(x + 4, y, nm,
                    isHov ? Config.Colors.GoldYellow : Config.Colors.White,
                    Config.Colors.Black);
                stateLayout.Add((y, fx));
                y++;
            }
            if (effects.Count > shown)
                terminal.Text(x, y++, $"+{effects.Count - shown} more",
                    Config.Colors.DarkGray, Config.Colors.Black);
        }

        // Hovered effect's description in the remaining vertical space.
        var hovered = stateLayout.FirstOrDefault(r => r.Y == hoveredStateY);
        if (hovered.Effect != null)
        {
            y++; // blank
            terminal.Text(x, y++, "── INFO ──", Config.Colors.DarkYellowGrey, Config.Colors.Black);
            string desc = hovered.Effect.Description ?? "";
            int linesAvail = BotStart - y - 1;
            for (int s = 0, line = 0; s < desc.Length && line < linesAvail; s += innerW, line++)
            {
                terminal.Text(x, y++,
                    desc.Substring(s, Math.Min(innerW, desc.Length - s)),
                    Config.Colors.LightGray, Config.Colors.Black);
            }
        }

        return stateLayout;
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
        IReadOnlyList<FightingSkill> unaffordableSkills,
        bool isMoveMode, int selectedSkillIndex, int selectedLearnableSkillIndex,
        string? expandedMediumKey,
        int hoveredButtonRow,
        // The fight state rather than the raw used-actions set: whether a row counts as spent is
        // FightState.IsActionUsed's call, and it can be lifted by an effect (Iron Nerves). Reading
        // the set directly would keep greying out rows the fighter can in fact use again.
        FightState? state,
        bool runUsed,
        int scrollOffset,
        bool scrollbarHot,
        out int maxScrollOffset)
    {
        var layout = new List<LeftPanelRow>();
        maxScrollOffset = 0;

        // Top-left = action menu (cols 0..ActionMenuRight, rows 0..TopRows-1)
        int boxW = ActionMenuRight;
        int innerW = boxW - 2;
        terminal.FillRect(0, 0, boxW, TopRows, ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(0, 0, boxW, TopRows, BoxStyle.Single, Config.Colors.DarkGray, Config.Colors.Black);

        const int x = 1;
        int y = 1;

        // "ACTIONS:" header + divider
        terminal.Text(x, y++, "ACTIONS", Config.Colors.DarkYellowGrey, Config.Colors.Black);
        terminal.Text(x, y++, new string('─', innerW), Config.Colors.DarkGray, Config.Colors.Black);

        // ── Enemy turn: skip action buttons, show a status message instead ──
        if (!fighter.IsPlayerControlled)
        {
            int midRow = (y + (EndTurnButtonRow - 1)) / 2;
            string msg = "── ENEMY TURN ──";
            int mx = 1 + Math.Max(0, (innerW - msg.Length) / 2);
            terminal.Text(mx, midRow, msg, Config.Colors.Purple, Config.Colors.Black);
            return layout;
        }

        // y is now at MoveButtonRow (3) — MOVE button (yellow when selected)
        {
            bool sel = isMoveMode;
            bool hov = !sel && hoveredButtonRow == MoveButtonRow;
            Vector4 fg = sel ? Config.Colors.Black
                       : hov ? Config.Colors.GoldYellow
                       : Config.Colors.White;
            Vector4 bg = sel ? Config.Colors.Yellow : Config.Colors.Black;
            string label = (sel ? "* MOVE" : "  MOVE").PadRight(innerW);
            terminal.Text(x, y++, label, fg, bg);
        }

        // ── Grouped skill list ──
        // Build groups: key → (label, ordered list of (index, skill, learnable))
        var groups = new List<(string Key, string Label, List<(int Idx, FightingSkill Skill, bool Learnable, bool Unaffordable)> Skills)>();

        void AddToGroup(string key, string label, int idx, FightingSkill skill, bool learnable, bool unaffordable = false)
        {
            var entry = groups.FirstOrDefault(g => g.Key == key);
            if (entry.Key == null)
            {
                entry = (key, label, new List<(int, FightingSkill, bool, bool)>());
                groups.Add(entry);
            }
            entry.Skills.Add((idx, skill, learnable, unaffordable));
        }

        // ── Organ-medium groups ──
        // Organs whose parts strike independently (Organ.PartsAreIndependentMediums, e.g. hands)
        // yield one tab per part, each with the full skill list; because the once-per-turn lock
        // is keyed on (tab, skill), the same skill (e.g. Punch) is usable once per part. All other
        // organs (legs, single-part organs) yield a single whole-organ tab.
        void AddOrganSkillToGroups(int idx, FightingSkill s, bool learnable, bool unaffordable)
        {
            // Add this skill under every organ-type medium it belongs to that the fighter has.
            foreach (var medium in s.Mediums.Where(m => m.Type == MediumType.OrganMedium))
            {
                string organId = medium.OrganId ?? s.SkillId;
                var organ = fighter.Member.GetOrganById(organId);
                if (organ == null) continue; // fighter lacks this organ
                if (organ.PartsAreIndependentMediums && organ.Parts.Count > 1)
                {
                    foreach (var part in organ.Parts)
                        AddToGroup(OrganPartKey(part.Id), part.DisplayName, idx, s, learnable, unaffordable);
                }
                else
                {
                    AddToGroup(OrganKey(organId), organ.DisplayName,
                        idx, s, learnable, unaffordable);
                }
            }
        }

        for (int i = 0; i < unlockedSkills.Count; i++)
            if (unlockedSkills[i].Mediums.Any(m => m.Type == MediumType.OrganMedium))
                AddOrganSkillToGroups(i, unlockedSkills[i], learnable: false, unaffordable: false);
        for (int i = 0; i < learnableSkills.Count; i++)
            if (learnableSkills[i].Mediums.Any(m => m.Type == MediumType.OrganMedium))
                AddOrganSkillToGroups(i, learnableSkills[i], learnable: true, unaffordable: false);
        for (int i = 0; i < unaffordableSkills.Count; i++)
            if (unaffordableSkills[i].Mediums.Any(m => m.Type == MediumType.OrganMedium))
                AddOrganSkillToGroups(i, unaffordableSkills[i], learnable: false, unaffordable: true);

        // ── Body-part-medium groups ──
        // A body-part region (e.g. upper_limbs) is always a single whole-region tab; its
        // dice level is the region's total score (sum of its organs).
        void AddBodyPartSkillToGroups(int idx, FightingSkill s, bool learnable, bool unaffordable)
        {
            foreach (var medium in s.Mediums.Where(m => m.Type == MediumType.BodyPartMedium))
            {
                string bodyPartId = medium.BodyPartId ?? s.SkillId;
                var bodyPart = fighter.Member.GetBodyPartById(bodyPartId);
                string label = bodyPart?.DisplayName
                    ?? BodyPartMediumRegistry.GetById(bodyPartId)?.DisplayName
                    ?? OrganLabel(bodyPartId);
                AddToGroup(BodyPartKey(bodyPartId), label, idx, s, learnable, unaffordable);
            }
        }

        for (int i = 0; i < unlockedSkills.Count; i++)
            if (unlockedSkills[i].Mediums.Any(m => m.Type == MediumType.BodyPartMedium))
                AddBodyPartSkillToGroups(i, unlockedSkills[i], learnable: false, unaffordable: false);
        for (int i = 0; i < learnableSkills.Count; i++)
            if (learnableSkills[i].Mediums.Any(m => m.Type == MediumType.BodyPartMedium))
                AddBodyPartSkillToGroups(i, learnableSkills[i], learnable: true, unaffordable: false);
        for (int i = 0; i < unaffordableSkills.Count; i++)
            if (unaffordableSkills[i].Mediums.Any(m => m.Type == MediumType.BodyPartMedium))
                AddBodyPartSkillToGroups(i, unaffordableSkills[i], learnable: false, unaffordable: true);

        // ── Weapon-medium groups: one tab per equipped weapon, skills in category order ──
        // Pre-index unlocked/learnable weapon skills for O(1) lookup.
        var unlockedWeaponIdx    = new Dictionary<string, int>();
        var learnableWeaponIdx   = new Dictionary<string, int>();
        var unaffordableWeaponIdx = new Dictionary<string, int>();
        for (int i = 0; i < unlockedSkills.Count; i++)
            if (unlockedSkills[i].Medium.Type == MediumType.WeaponMedium)
                unlockedWeaponIdx[unlockedSkills[i].SkillId] = i;
        for (int i = 0; i < learnableSkills.Count; i++)
            if (learnableSkills[i].Medium.Type == MediumType.WeaponMedium)
                learnableWeaponIdx[learnableSkills[i].SkillId] = i;
        for (int i = 0; i < unaffordableSkills.Count; i++)
            if (unaffordableSkills[i].Medium.Type == MediumType.WeaponMedium)
                unaffordableWeaponIdx[unaffordableSkills[i].SkillId] = i;

        // One tab per equipped weapon (per anchor). With a weapon in each hand, even when
        // it's the same category, you get TWO independent tabs so the same skill can be
        // used once per tab per turn.
        bool hasRight = fighter.Member.EquippedItems[EquipmentAnchor.RightHold].OfType<IWeaponItem>().Any();
        bool hasLeft  = fighter.Member.EquippedItems[EquipmentAnchor.LeftHold].OfType<IWeaponItem>().Any();
        bool needHandSuffix = hasRight && hasLeft;

        foreach (var anchor in new[] { EquipmentAnchor.RightHold, EquipmentAnchor.LeftHold })
        {
            foreach (var item in fighter.Member.EquippedItems[anchor].OfType<IWeaponItem>())
            {
                var category = WeaponMediumRegistry.GetById(item.WeaponCategory);
                if (category == null) continue;

                string groupKey   = $"weapon:{anchor}:{category.CategoryId}";
                string groupLabel = (item as Item)?.DisplayName ?? category.DisplayName;
                if (needHandSuffix)
                    groupLabel += anchor == EquipmentAnchor.RightHold ? " (R)" : " (L)";

                foreach (var skillId in category.SkillIds)
                {
                    if (unlockedWeaponIdx.TryGetValue(skillId, out int uIdx))
                    {
                        AddToGroup(groupKey, groupLabel, uIdx, unlockedSkills[uIdx], false);
                    }
                    else if (unaffordableWeaponIdx.TryGetValue(skillId, out int aIdx))
                    {
                        AddToGroup(groupKey, groupLabel, aIdx, unaffordableSkills[aIdx], false, unaffordable: true);
                    }
                    else if (learnableWeaponIdx.TryGetValue(skillId, out int lIdx))
                    {
                        AddToGroup(groupKey, groupLabel, lIdx, learnableSkills[lIdx], true);
                        break; // only the first unknown per weapon tab is shown as learnable
                    }
                }
            }
        }

        // Stable ordering: body mediums (organ + body-part) first, then weapon mediums. Organ
        // parts of the same organ stay adjacent (grouped by organ id) and sort Left-before-Right
        // by label.
        static bool IsBodyGroup((string Key, string Label, List<(int, FightingSkill, bool, bool)> Skills) g) =>
            g.Skills.Count > 0 && g.Skills[0].Item2.Mediums.All(m => m.Type != MediumType.WeaponMedium);
        groups = groups
            .OrderBy(g => IsBodyGroup(g) ? 0 : 1)
            .ThenBy(g => IsBodyGroup(g) ? g.Key + "/" + g.Label : g.Label)
            .ToList();

        // Build the full ordered list of row renderers (medium headers + expanded skills) so the
        // panel can render only the slice that fits and show a scrollbar when content overflows.
        // `textW` is captured by the closures and reduced by 1 once we know a scrollbar is needed.
        int textW = innerW;
        var renderers = new List<Action<int>>();

        foreach (var grp in groups)
        {
            string headerKey   = grp.Key;
            string headerLabel = grp.Label;
            bool   isExpanded  = grp.Key == expandedMediumKey;

            renderers.Add(rowY =>
            {
                bool hovHeader  = hoveredButtonRow == rowY;
                Vector4 headFg  = hovHeader ? Config.Colors.GoldYellow : Config.Colors.DarkYellowGrey;
                string marker   = isExpanded ? "▼" : "▶";
                string headLine = $"{marker} {headerLabel}";
                if (headLine.Length > textW) headLine = headLine[..textW];
                terminal.Text(x, rowY, headLine.PadRight(textW), headFg, Config.Colors.Black);
                layout.Add(new LeftPanelRow(rowY, LeftPanelRowKind.Medium, headerKey, -1));
            });

            if (!isExpanded) continue;

            foreach (var (idx, skill, learnable, unaffordable) in grp.Skills)
            {
                int    sIdx   = idx;
                var    sSkill = skill;
                bool   sLearn = learnable;
                bool   sUnaff = unaffordable;
                string sKey   = grp.Key;

                renderers.Add(rowY =>
                {
                    bool used = state?.IsActionUsed(fighter, sKey, sSkill.SkillId) == true;
                    bool sel = !isMoveMode && !used && !sUnaff
                        && (sLearn ? sIdx == selectedLearnableSkillIndex : sIdx == selectedSkillIndex);
                    bool hov = !sel && !used && hoveredButtonRow == rowY;

                    Vector4 fg, bg;
                    if (used)
                    {
                        fg = Config.Colors.DarkGray35;
                        bg = Config.Colors.Black;
                    }
                    else if (sUnaff)
                    {
                        fg = Config.Colors.DarkGray40;
                        bg = Config.Colors.Black;
                    }
                    else if (sLearn)
                    {
                        fg = sel ? Config.Colors.Black
                           : hov ? Config.Colors.GoldYellow
                           : Config.Colors.LightPurple;
                        bg = sel ? Config.Colors.Purple : Config.Colors.Black;
                    }
                    else
                    {
                        fg = sel ? Config.Colors.Black
                           : hov ? Config.Colors.GoldYellow
                           : Config.Colors.White;
                        bg = sel ? Config.Colors.GoldYellow : Config.Colors.Black;
                    }

                    string prefix = sLearn ? "  ? " : "    ";
                    string line = $"{prefix}{sSkill.DisplayName} {sSkill.CineticPointsCost}CP";
                    if (line.Length > textW) line = line[..textW];
                    terminal.Text(x, rowY, line.PadRight(textW), fg, bg);
                    layout.Add(new LeftPanelRow(rowY,
                        sUnaff ? LeftPanelRowKind.UnaffordableSkill :
                        sLearn ? LeftPanelRowKind.LearnableSkill :
                                 LeftPanelRowKind.UnlockedSkill,
                        sKey, sIdx));
                });
            }
        }

        // Visible window: rows [SkillButtonsStart .. EndTurnButtonRow-1) — the divider sits below.
        int listTop     = SkillButtonsStart;
        int visibleRows = (EndTurnButtonRow - 1) - listTop;
        int total       = renderers.Count;
        maxScrollOffset = Math.Max(0, total - visibleRows);
        int scroll      = Math.Clamp(scrollOffset, 0, maxScrollOffset);
        bool hasScroll  = total > visibleRows;
        if (hasScroll) textW = innerW - 1; // reserve the rightmost inner column for the scrollbar

        for (int i = 0; i < visibleRows && scroll + i < total; i++)
            renderers[scroll + i](listTop + i);

        if (hasScroll)
            DrawActionScrollbar(terminal, x + innerW - 1, listTop, visibleRows, total, scroll, maxScrollOffset, scrollbarHot);

        // Divider before end/run
        int divY = EndTurnButtonRow - 1;
        terminal.Text(x, divY, new string('─', innerW), Config.Colors.DarkGray, Config.Colors.Black);

        // END TURN button
        {
            bool hov = hoveredButtonRow == EndTurnButtonRow;
            terminal.Text(x, EndTurnButtonRow,
                "  END TURN".PadRight(innerW),
                hov ? Config.Colors.GoldYellow : Config.Colors.White,
                Config.Colors.Black);
        }

        // RUN button — dim (not invisible) when not on exit tile, or after one attempt
        {
            bool onExit = fighter.X == FightArea.ExitCol && fighter.Y == FightArea.ExitRow;
            bool hov    = !runUsed && hoveredButtonRow == RunButtonRow;
            Vector4 runFg = runUsed || !onExit ? Config.Colors.DarkGray35
                          : hov                ? Config.Colors.Yellow
                          : Config.Colors.Orange;
            terminal.Text(x, RunButtonRow,
                "  RUN AWAY".PadRight(innerW),
                runFg, Config.Colors.Black);
        }

        return layout;
    }

    /// <summary>
    /// Draws a vertical scrollbar (track + proportional thumb) for the action menu at column
    /// <paramref name="barX"/>, spanning <paramref name="visibleRows"/> rows from <paramref name="top"/>.
    /// </summary>
    private static void DrawActionScrollbar(TerminalHUD terminal, int barX, int top,
        int visibleRows, int total, int scroll, int maxScroll, bool hot)
    {
        // Gray scrollbar; the thumb brightens a little when hovered or being dragged.
        Vector4 trackColor = Config.Colors.DarkGray;
        Vector4 thumbColor = hot ? Config.Colors.LightGray75 : Config.Colors.MediumGray60;

        for (int r = 0; r < visibleRows; r++)
            terminal.SetCell(barX, top + r, '│', trackColor, Config.Colors.Black);

        int thumbH = Math.Max(1, (int)Math.Round((double)visibleRows * visibleRows / total));
        thumbH = Math.Min(thumbH, visibleRows);
        int travel = visibleRows - thumbH;
        int thumbPos = maxScroll <= 0 ? 0 : (int)Math.Round((double)scroll / maxScroll * travel);
        thumbPos = Math.Clamp(thumbPos, 0, travel);

        for (int r = 0; r < thumbH; r++)
            terminal.SetCell(barX, top + thumbPos + r, '█', thumbColor, Config.Colors.Black);
    }

    // ── Left-panel info box (bottom half) ─────────────────────────────

    /// <summary>What's being described in the left-panel info box.</summary>
    public enum LeftInfoKind { None, Move, EndTurn, Run, Skill, LearnableSkill }

    /// <summary>
    /// Draw the bottom-half action-info box (rows <see cref="LeftSplitRow"/>..<see cref="BotStart"/>).
    /// Shows details for the hovered/selected action.
    /// </summary>
    public static void RenderLeftInfoPanel(TerminalHUD terminal,
        LeftInfoKind kind, FightingSkill? skill, Fighter? fighter,
        string? organPartId = null, string? activeOrganId = null, string? mediumKey = null)
    {
        // Top-right box (cols ActionMenuRight..100, rows 0..TopRows-1)
        int boxX = ActionMenuRight;
        int boxW = 100 - ActionMenuRight;
        int boxH = TopRows;
        terminal.FillRect(boxX, 0, boxW, boxH, ' ', Config.Colors.White, Config.Colors.Black);

        bool isLearn = kind == LeftInfoKind.LearnableSkill;
        Vector4 border = isLearn ? Config.Colors.Purple : Config.Colors.DarkGray;
        terminal.DrawBox(boxX, 0, boxW, boxH, BoxStyle.Single, border, Config.Colors.Black);

        int x = boxX + 1;
        int y = 1;
        int innerW = boxW - 2;

        terminal.Text(x, y++, "INFO", Config.Colors.DarkYellowGrey, Config.Colors.Black);
        terminal.Text(x, y++, new string('─', innerW), Config.Colors.DarkGray, Config.Colors.Black);

        void TextLine(string s, Vector4 col)
        {
            if (s.Length > innerW) s = s[..innerW];
            terminal.Text(x, y++, s, col, Config.Colors.Black);
        }
        void WrapText(string s, Vector4 col)
        {
            for (int i = 0; i < s.Length && y < boxH - 1; i += innerW)
                TextLine(s.Substring(i, Math.Min(innerW, s.Length - i)), col);
        }
        // Pad-or-clip to a fixed width so the level-breakdown rows column up.
        static string Trim(string s, int w) => s.Length > w ? s[..w] : s.PadRight(w);

        switch (kind)
        {
            case LeftInfoKind.Move:
                TextLine("MOVE", Config.Colors.Yellow);
                y++;
                WrapText("Walk across the arena tile by tile.", Config.Colors.White);
                if (fighter != null)
                {
                    y++;
                    int maxSteps = fighter.CurrentCineticPoints * fighter.EffectiveMoveSpeed;
                    TextLine($"Range : {maxSteps} tiles", Config.Colors.LightGray);
                    TextLine($"Speed : {fighter.EffectiveMoveSpeed}/CP", Config.Colors.LightGray);
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
                WrapText("Flee combat. Requires standing on the exit tile. Need at least one six.", Config.Colors.White);
                if (fighter != null)
                {
                    y++;
                    TextLine($"Cost: 1 CP", Config.Colors.LightGray);
                    TextLine($"Dice: {fighter.RunawayDiceCount}", Config.Colors.LightGray);
                }
                break;

            case LeftInfoKind.Skill:
            case LeftInfoKind.LearnableSkill:
                if (skill == null || fighter == null) break;
                Vector4 titleC = isLearn ? Config.Colors.LightPurple : Config.Colors.Yellow;
                TextLine(skill.DisplayName, titleC);
                TextLine($"Medium: {MediumLabelFor(skill, fighter, organPartId, activeOrganId)}", Config.Colors.DarkYellowGrey);
                if (isLearn) TextLine("(unknown — learn first)", titleC);
                y++;
                WrapText(skill.Description ?? "", Config.Colors.White);
                y++;
                // Resolve the medium the SAME way the roll does, or a multi-medium skill quotes
                // its primary medium's numbers while the roll uses the tab's.
                var activeMedium = ActiveMediumFromKey(skill, mediumKey);
                var breakdown    = skill.GetLevelBreakdown(fighter, organPartId, activeMedium);
                bool isBuff      = skill.EffectType == FightingSkillEffect.Buff;

                if (isBuff)
                {
                    // A buff has no target and rolls nothing — vital heat IS its cost model, and
                    // level makes it cheaper rather than stronger. A dice line here would be a lie.
                    TextLine($"VH    : {skill.VitalHeatCostFor(fighter, organPartId, activeMedium)}",
                             Config.Colors.Orange);
                }
                else
                {
                    TextLine($"Dice  : {skill.TotalDice(fighter, organPartId, activeMedium) + (skill.IsSelfTargeting ? 0 : fighter.NaturalAttack)}",
                             Config.Colors.LightGray);
                }
                TextLine($"Cost  : {skill.CineticPointsCost} CP", Config.Colors.LightGray);
                if (!isBuff)
                    TextLine(skill.MinRange > 1
                        ? $"Range : {skill.MinRange}-{skill.Range}"
                        : $"Range : {skill.Range}", Config.Colors.LightGray);
                TextLine($"Effect: {skill.EffectType}", Config.Colors.LightGray);
                if (skill.EffectType == FightingSkillEffect.Attack)
                    TextLine($"Wound : {skill.WoundTargetMode}", Config.Colors.LightGray);

                // ── Where the level comes from ────────────────────────────────
                // The single figure above is the sum of terms the player can actually raise, so
                // spell them out: which medium, which modi mentis, and what each contributes.
                y++;
                TextLine($"LEVEL {breakdown.Total}", Config.Colors.DarkYellowGrey);
                TextLine($"  {Trim(breakdown.MediumLabel, 12)} {breakdown.MediumLevel}"
                       + (breakdown.MediumMultiplicator != 1 ? $"x{breakdown.MediumMultiplicator}" : ""),
                         Config.Colors.MediumGray60);
                if (breakdown.ModiMentis.Count == 0)
                    TextLine("  (no modus mentis)", Config.Colors.DarkGray);
                else
                    foreach (var (id, lvl) in breakdown.ModiMentis)
                        TextLine($"  {Trim(id.Replace('_', ' '), 12)} {lvl}"
                               + (breakdown.SkillMultiplicator != 1 ? $"x{breakdown.SkillMultiplicator}" : ""),
                                 Config.Colors.MediumGray60);

                if (isLearn)
                {
                    string? learnOrganId    = activeOrganId;
                    string? learnBodyPartId = BodyPartIdFromKey(mediumKey);
                    int diff = Math.Max(0, (learnOrganId    != null ? skill.GetMediumPositionForOrganId(learnOrganId)
                                         : learnBodyPartId != null ? skill.GetMediumPositionForBodyPartId(learnBodyPartId)
                                         : skill.MediumPosition) - 1);
                    int dice = Math.Max(1, fighter.FightLearningStat);
                    y++;
                    TextLine($"LEARN: {dice}d", titleC);
                    TextLine($"need {diff} sixes", titleC);
                }
                else if (!isBuff && skill.VitalHeatCost > 0)
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

    // ── Organ medium keys ─────────────────────────────────────────────
    // Two flavours of organ fighting-medium tab, distinguished by prefix:
    //   "organ:<organId>"      → the whole organ is one medium (level = organ total score)
    //   "organpart:<partId>"   → one part is its own medium (level = that part's score)
    // Which one an organ uses is decided by Organ.PartsAreIndependentMediums.

    /// <summary>Prefix marking a left-panel group key as a whole-organ medium.</summary>
    public const string OrganKeyPrefix = "organ:";
    /// <summary>Prefix marking a left-panel group key as a single-organ-part medium.</summary>
    public const string OrganPartKeyPrefix = "organpart:";
    /// <summary>Prefix marking a left-panel group key as a whole body-part-region medium.</summary>
    public const string BodyPartKeyPrefix = "bodypart:";

    /// <summary>Build the left-panel group key for a body-part region (e.g. "bodypart:upper_limbs").</summary>
    public static string BodyPartKey(string bodyPartId) => BodyPartKeyPrefix + bodyPartId;

    /// <summary>Build the left-panel group key for a whole organ (e.g. "organ:legs").</summary>
    public static string OrganKey(string organId) => OrganKeyPrefix + organId;

    /// <summary>Build the left-panel group key for an organ part (e.g. "organpart:left_hand").</summary>
    public static string OrganPartKey(string partId) => OrganPartKeyPrefix + partId;

    /// <summary>
    /// Extract the organ id from a whole-organ group key ("organ:xxx"), or null for other key types.
    /// </summary>
    public static string? OrganIdFromKey(string? key) =>
        key != null && key.StartsWith(OrganKeyPrefix, StringComparison.Ordinal)
            ? key[OrganKeyPrefix.Length..]
            : null;

    /// <summary>
    /// Extract the body-part id from a body-part group key ("bodypart:xxx"), or null for other key types.
    /// </summary>
    public static string? BodyPartIdFromKey(string? key) =>
        key != null && key.StartsWith(BodyPartKeyPrefix, StringComparison.Ordinal)
            ? key[BodyPartKeyPrefix.Length..]
            : null;

    /// <summary>
    /// Extract the organ-part id from a group key, or null when the key is not a part key
    /// (i.e. a whole-organ medium, whose level is computed from the organ's total score).
    /// </summary>
    public static string? OrganPartIdFromKey(string? key) =>
        key != null && key.StartsWith(OrganPartKeyPrefix, StringComparison.Ordinal)
            ? key[OrganPartKeyPrefix.Length..]
            : null;

    /// <summary>
    /// Resolves the active <see cref="FightingMedium"/> for a skill from a left-panel tab key.
    /// For organ tabs ("organ:xxx" / "organpart:xxx") returns the matching organ medium in the
    /// skill; for body-part tabs ("bodypart:xxx") the matching body-part medium; otherwise null.
    ///
    /// <para>
    /// It lives here rather than on the adapter because both the roll and the INFO panel must agree
    /// on it. A multi-medium skill (flesh_tear is in both the fangs and teeth lists) has a different
    /// level under each tab, so a panel that skipped this would quote the primary medium's dice
    /// while the roll used the tab's.
    /// </para>
    /// </summary>
    public static FightingMedium? ActiveMediumFromKey(FightingSkill skill, string? mediumKey)
    {
        if (mediumKey == null) return null;
        if (mediumKey.StartsWith(OrganKeyPrefix, StringComparison.Ordinal))
        {
            string organId = mediumKey[OrganKeyPrefix.Length..];
            return skill.GetMediumForOrganId(organId);
        }
        if (mediumKey.StartsWith(OrganPartKeyPrefix, StringComparison.Ordinal))
        {
            // part key — the organ id is inferred from Mediums; return the first organ-type medium
            return skill.Mediums.FirstOrDefault(m => m.Type == MediumType.OrganMedium);
        }
        if (mediumKey.StartsWith(BodyPartKeyPrefix, StringComparison.Ordinal))
        {
            string bodyPartId = mediumKey[BodyPartKeyPrefix.Length..];
            return skill.GetMediumForBodyPartId(bodyPartId);
        }
        return null;
    }

    /// <summary>Human-readable label for an organ id (e.g. "hands" → "Hands").</summary>
    private static string OrganLabel(string? organId)
    {
        string raw = organId ?? "?";
        raw = raw.Replace('_', ' ');
        return raw.Length == 0 ? raw : char.ToUpperInvariant(raw[0]) + raw[1..];
    }

    /// <summary>Display label for the medium of a skill shown in the info panel.</summary>
    private static string MediumLabelFor(FightingSkill s, Fighter? fighter = null,
        string? organPartId = null, string? activeOrganId = null)
    {
        if (s.Mediums.Any(m => m.Type == MediumType.OrganMedium))
        {
            string targetOrganId = activeOrganId ?? s.Medium.OrganId ?? "";
            if (organPartId != null && fighter != null)
            {
                var organ = fighter.Member.GetOrganById(targetOrganId);
                if (organ != null && organ.Parts.Count > 1)
                {
                    var part = organ.Parts.FirstOrDefault(p => p.Id == organPartId);
                    if (part != null) return part.DisplayName;
                }
            }
            return OrganLabel(targetOrganId);
        }

        if (s.Mediums.Any(m => m.Type == MediumType.BodyPartMedium))
        {
            string bodyPartId = activeOrganId ?? s.Medium.BodyPartId ?? "";
            return fighter?.Member.GetBodyPartById(bodyPartId)?.DisplayName
                ?? BodyPartMediumRegistry.GetById(bodyPartId)?.DisplayName
                ?? OrganLabel(s.Medium.BodyPartId);
        }

        // Weapon: list all categories that contain this skill
        var cats = WeaponMediumRegistry.GetCategoriesContaining(s.SkillId).ToList();
        return cats.Count == 0
            ? "Weapon"
            : string.Join(", ", cats.Select(c => c.DisplayName));
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

        // Build the legend directly from CharMapping.Default so the swatch always
        // matches what the arena renderer actually paints on each tile.
        var mapping = CharMapping.Default;
        var entries = new (TerrainType Type, string Label)[]
        {
            (TerrainType.Exit,                "Exit"),
            (TerrainType.FreeSpace,           "Free space"),
            (TerrainType.SoftObstacle,        "Soft (slow)"),
            (TerrainType.TreacherousTerrain,  "Treacherous"),
            (TerrainType.DangerousTerrain,    "Dangerous"),
            (TerrainType.HardObstacle,        "Hard obstacle"),
        };

        int maxLabel = panelW - 4;
        int y = TopRows + 3;
        foreach (var (type, label) in entries)
        {
            if (y >= RightSplitRow - 1) break;
            var appearance = mapping[type];
            char glyph = appearance.Chars.Length > 0 ? appearance.Chars[0] : '?';
            terminal.SetCell(RightStart + 1, y, glyph, appearance.TextColor, appearance.BgColor);
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
        LogEntryType.Wound         => Config.Colors.BrightPurple,
        LogEntryType.SpecialEffect => new Vector4(1.0f, 0.5f, 0.0f, 1.0f),  // Orange
        LogEntryType.Learning      => Config.Colors.LightPurple,
        LogEntryType.Defense       => Config.Colors.LightPurpleGray,
        _                          => Config.Colors.White,
    };

    /// <summary>
    /// Draw the action log in the bottom panel (full width, newest entries at bottom).
    /// Always shows the tail — there is no scroll offset; the fight's history lives in the moment.
    /// </summary>
    public static void RenderBottomPanel(TerminalHUD terminal,
        IReadOnlyList<(string Text, LogEntryType Type)> actionLog)
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
        int firstVisible = Math.Max(0, total - visibleLines);
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
                                          HashSet<(int X, int Y)>? hoverCells = null,
                                          (int X, int Y)? attackPreviewCell = null,
                                          Fighter? hoveredFighter = null)
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

            if (highlightCells != null)
            {
                if (highlightCells.Contains((ax, ay)))
                {
                    // In-range: keep natural colors with a very subtle luminosity pulse
                    // tied to the global blink so the playable zone breathes faintly.
                    float pulse = blinkOn ? 0.02f : -0.02f;
                    var fg = new Vector4(
                        Math.Clamp(cell.TextColor.X + pulse, 0f, 1f),
                        Math.Clamp(cell.TextColor.Y + pulse, 0f, 1f),
                        Math.Clamp(cell.TextColor.Z + pulse, 0f, 1f), 1f);
                    var bg = new Vector4(
                        Math.Clamp(cell.BgColor.X + pulse, 0f, 1f),
                        Math.Clamp(cell.BgColor.Y + pulse, 0f, 1f),
                        Math.Clamp(cell.BgColor.Z + pulse, 0f, 1f), 1f);
                    terminal.SetCell(CenterX + ax, CenterY + ay, cell.Glyph, fg, bg);
                }
                else
                {
                    // Out-of-range: darken both foreground and background.
                    var fg = new Vector4(cell.TextColor.X * 0.25f, cell.TextColor.Y * 0.25f,
                                         cell.TextColor.Z * 0.25f, 1f);
                    var bg = new Vector4(cell.BgColor.X   * 0.25f, cell.BgColor.Y   * 0.25f,
                                         cell.BgColor.Z   * 0.25f, 1f);
                    terminal.SetCell(CenterX + ax, CenterY + ay, cell.Glyph, fg, bg);
                }
            }
            else if (hoverCells != null)
            {
                // Hover active but this cell is not a target — darken both fg and bg
                var fg = new Vector4(cell.TextColor.X * 0.25f, cell.TextColor.Y * 0.25f,
                                     cell.TextColor.Z * 0.25f, 1f);
                var bg = new Vector4(cell.BgColor.X   * 0.25f, cell.BgColor.Y   * 0.25f,
                                     cell.BgColor.Z   * 0.25f, 1f);
                terminal.SetCell(CenterX + ax, CenterY + ay, cell.Glyph, fg, bg);
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
            bool isAttackPreview = attackPreviewCell.HasValue
                && attackPreviewCell.Value.X == f.X && attackPreviewCell.Value.Y == f.Y;

            if (isAttackPreview)
            {
                // Inverted colors: swap fg and bg of the fighter glyph
                terminal.SetCell(tx, ty, f.DisplayChar, Config.Colors.Black, f.DisplayColor);
            }
            else if (f == hoveredFighter)
            {
                // Hovering this fighter's INITIATIVE row: mark them on the map. The pairing already
                // worked the other way round (hovering the map highlighted the row), which made the
                // list readable from the arena but not the arena readable from the list.
                terminal.SetCell(tx, ty, f.DisplayChar, Config.Colors.Black, Config.Colors.Yellow);
            }
            else if (hoverCells != null && hoverCells.Contains((f.X, f.Y)))
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

        // Attack preview on an empty cell (rare — most skill targets are fighters)
        if (attackPreviewCell.HasValue)
        {
            var (px, py) = attackPreviewCell.Value;
            bool occupied = fighters.Any(ff => ff.IsAlive && ff.X == px && ff.Y == py);
            if (!occupied)
            {
                var cell = area.GetCell(px, py);
                terminal.SetCell(CenterX + px, CenterY + py, cell.Glyph,
                    Config.Colors.Black, cell.TextColor);
            }
        }
    }

    // ── Dice overlay ─────────────────────────────────────────────────

    /// <summary>
    /// Draw the dice roll animation overlay, centered in the arena.
    /// The fight UI underneath is greyed out first so the dice box stands out —
    /// same presentation as the narration and dialogue panels.
    /// </summary>
    public static bool RenderDiceOverlay(TerminalHUD terminal, DiceRollComponent dice, bool continueHovered)
    {
        terminal.DimRect(0, 0, terminal.Width, terminal.Height,
            Config.Colors.DarkGray35, Config.Colors.Black);
        int cx = CenterX + FightArea.Width / 2;
        int cy = CenterY + FightArea.Height / 2;
        return dice.Render(terminal, cx, cy, continueHovered);
    }

    // ── Terrain interrupt popup overlay (purplish) ───────────────────
    private const int InterruptW = 50;
    private const int InterruptH = 8;
    private const int InterruptX = CenterX + (FightArea.Width  - InterruptW) / 2;
    private const int InterruptY = CenterY + (FightArea.Height - InterruptH) / 2;

    /// <summary>
    /// Purplish overlay summarising a bleeding-drain event at the party fighter's turn start.
    /// Click anywhere → dismiss → the turn proceeds. Same geometry as the interrupt popup.
    /// </summary>
    public static void RenderBleedingDrainPopup(TerminalHUD terminal, string fighterName,
        int bleedingLevel, int humorsDrained, int vitalHeatDrained, bool collapsed)
    {
        terminal.FillRect(InterruptX, InterruptY, InterruptW, InterruptH + 2,
            ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(InterruptX, InterruptY, InterruptW, InterruptH + 2,
            BoxStyle.Double, Config.Colors.BrightPurple, Config.Colors.Black);

        terminal.Text(InterruptX + 2, InterruptY + 1,
            $"── BLEEDING (level {bleedingLevel}) ──",
            Config.Colors.BrightPurple, Config.Colors.Black);

        terminal.Text(InterruptX + 2, InterruptY + 3,
            $"{fighterName} bleeds at the start of the turn.",
            Config.Colors.LightPurple, Config.Colors.Black);
        terminal.Text(InterruptX + 2, InterruptY + 4,
            $"  Humors drained : {humorsDrained}",
            Config.Colors.LightPurple, Config.Colors.Black);
        terminal.Text(InterruptX + 2, InterruptY + 5,
            $"  Vital heat lost: {vitalHeatDrained}",
            Config.Colors.LightPurple, Config.Colors.Black);

        if (collapsed)
            terminal.Text(InterruptX + 2, InterruptY + 7,
                "Body humors fully depleted — collapse!",
                Config.Colors.BrightPurple, Config.Colors.Black);

        terminal.Text(InterruptX + 2, InterruptY + InterruptH,
            "(click anywhere to continue)",
            Config.Colors.DarkYellowGrey, Config.Colors.Black);
    }

    // ── Vital-heat consumption box (buff skills) ─────────────────────

    /// <summary>
    /// Shows what a buff cost: the skill, the heat asked for, and every humor the rotation actually
    /// drew, in order and in its own colour.
    ///
    /// <para>
    /// A sibling of the travel box rather than a reuse of it — <c>TravelProgressRenderer</c> is
    /// pinned bottom-centre, where the fight's full-width action log lives, and its content is
    /// trip-shaped (cells travelled, biome). This one sits over the arena like every other fight
    /// overlay and names the skill instead. There is no Continue button: the box reports a cost
    /// already paid, so there is nothing to accept.
    /// </para>
    /// </summary>
    public static void RenderVitalHeatBox(TerminalHUD terminal, string fighterName, string skillName,
        int required, IReadOnlyList<BodyHumor> drawn)
    {
        // Tall enough for the header, the two summary rows and one row per humor drawn.
        int boxH = 7 + Math.Max(1, drawn.Count);
        int boxW = InterruptW;
        int boxX = CenterX + (FightArea.Width  - boxW) / 2;
        int boxY = CenterY + (FightArea.Height - boxH) / 2;

        terminal.DimRect(0, 0, terminal.Width, terminal.Height,
            Config.Colors.DarkGray35, Config.Colors.Black);
        terminal.FillRect(boxX, boxY, boxW, boxH, ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(boxX, boxY, boxW, boxH, BoxStyle.Double, Config.Colors.Orange, Config.Colors.Black);

        int tx = boxX + 2;
        terminal.Text(tx, boxY + 1, "── VITAL HEAT ──", Config.Colors.Orange, Config.Colors.Black);
        terminal.Text(tx, boxY + 3, Truncate($"{fighterName} — {skillName}", boxW - 4),
            Config.Colors.Yellow, Config.Colors.Black);

        int totalHeat = drawn.Sum(h => h.VitalHeat);
        terminal.Text(tx, boxY + 4, $"  Heat required : {required}",
            Config.Colors.LightGray, Config.Colors.Black);

        int row = boxY + 6;
        if (drawn.Count == 0)
        {
            // Every queue was already critical — the buff still applied, but nothing was left to burn.
            terminal.Text(tx, row, "  nothing left to burn",
                Config.Colors.DarkGray, Config.Colors.Black);
        }
        else
        {
            foreach (var h in drawn)
            {
                string sign = h.VitalHeat >= 0 ? "+" : "";
                terminal.Text(tx, row++, Truncate($"  {h.Symbol} {h.Name} {sign}{h.VitalHeat}", boxW - 4),
                    h.Color, Config.Colors.Black);
            }
            terminal.Text(tx, row, $"  drawn: {drawn.Count} humor(s), {totalHeat} heat",
                Config.Colors.DarkYellowGrey, Config.Colors.Black);
        }
    }

    /// <summary>Clip <paramref name="s"/> to <paramref name="w"/> cells.</summary>
    private static string Truncate(string s, int w) => s.Length > w ? s[..w] : s;

    /// <summary>
    /// Draws a purplish overlay box describing a mid-move terrain interrupt.
    /// The caller is responsible for dismissing it (click anywhere → end turn).
    /// </summary>
    public static void RenderTerrainInterruptPopup(TerminalHUD terminal, string message)
    {
        terminal.FillRect(InterruptX, InterruptY, InterruptW, InterruptH,
            ' ', Config.Colors.White, Config.Colors.Black);
        terminal.DrawBox(InterruptX, InterruptY, InterruptW, InterruptH,
            BoxStyle.Double, Config.Colors.BrightPurple, Config.Colors.Black);

        terminal.Text(InterruptX + 2, InterruptY + 1,
            "── INTERRUPT ──", Config.Colors.BrightPurple, Config.Colors.Black);

        int innerW = InterruptW - 4;
        int row = InterruptY + 3;
        string msg = message ?? "";
        for (int s = 0; s < msg.Length && row < InterruptY + InterruptH - 2; s += innerW)
            terminal.Text(InterruptX + 2, row++,
                msg.Substring(s, Math.Min(innerW, msg.Length - s)),
                Config.Colors.LightPurple, Config.Colors.Black);

        terminal.Text(InterruptX + 2, InterruptY + InterruptH - 2,
            "(click anywhere to continue)", Config.Colors.DarkYellowGrey, Config.Colors.Black);
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
        Vector4 color = result == FightResult.PartyWon ? Config.Colors.GoldYellow
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
