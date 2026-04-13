using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Npc;
using Cathedral.Terminal;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Renders a dialogue tree session onto the terminal HUD.
///
/// Visual structure:
///   Header row  : NPC name (left) | affinity label (centre) | affinity pips (right)
///   Separator
///   Scrollable log  : NPC speech, player replicas, system messages
///   Options area    : selectable player replies (below log, always visible)
///   Separator
///   Status bar
///
/// Coordinate system: all x/y values are terminal cell coordinates.
/// </summary>
public class DialogueTreeUI : TerminalPanelUI
{
    private readonly NpcEntity    _npc;
    private readonly DialogueTree _tree;
    private readonly string       _partyMemberId;

    // Row → option index mapping, rebuilt every Render call
    private readonly Dictionary<int, int> _optionRowToIndex = new();

    public DialogueTreeUI(
        TerminalHUD   terminal,
        NpcEntity     npc,
        DialogueTree  tree,
        string        partyMemberId)
        : base(terminal)
    {
        _npc           = npc;
        _tree          = tree;
        _partyMemberId = partyMemberId;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>Returns the 0-based index of the option at the given cell row, or -1.</summary>
    public int GetOptionIndexAt(int mx, int my)
        => _optionRowToIndex.TryGetValue(my, out int idx) ? idx : -1;

    /// <summary>True when the mouse is over the dice-roll "[ Continue ]" button.</summary>
    public bool IsMouseOverContinue(int mx, int my)
        => IsMouseOverDiceRollButton(mx, my);

    // ── Main render entry ──────────────────────────────────────────────────────

    public void Render(DialogueSessionState state)
    {
        Clear();
        _optionRowToIndex.Clear();

        RenderHeader();
        DrawHorizontalLine(_layout.TOP_PADDING + 1);

        if (state.ErrorMessage != null)
        {
            ShowError(state.ErrorMessage);
            DrawStatusBar("Error — press ESC to exit.");
            return;
        }

        if (state.IsDiceRollActive)
        {
            ShowDiceRollIndicator(
                state.DiceCount,
                state.DiceDifficulty,
                state.IsDiceRolling,
                state.DiceFinalValues,
                state.IsContinueHovered);
            DrawStatusBar(state.IsDiceRolling
                ? "Rolling..."
                : (state.DiceSucceeded ? "SUCCESS — click to continue." : "FAILURE — click to continue."));
            return;
        }

        if (state.IsLoadingNpcReplica && state.Log.Count == 0)
        {
            ShowLoadingIndicator($"{_npc.DisplayName} considers…");
            DrawStatusBar("Waiting…");
            return;
        }

        RenderLogAndOptions(state);
        DrawStatusBar(BuildStatusText(state));
    }

    // ── Header ─────────────────────────────────────────────────────────────────

    private void RenderHeader()
    {
        int y = _layout.TOP_PADDING;

        // Left: NPC name
        _terminal.Text(_layout.CONTENT_START_X, y,
            $"• {_npc.DisplayName}",
            Config.NarrativeUI.HeaderColor, Config.NarrativeUI.BackgroundColor);

        // Centre: affinity label
        var    affinity = _npc.AffinityTable.GetLevel(_partyMemberId);
        string label    = affinity.ToShortLabel();
        int    labelX   = (_layout.TERMINAL_WIDTH - label.Length) / 2;
        _terminal.Text(labelX, y, label,
            Config.NarrativeUI.LoadingColor, Config.NarrativeUI.BackgroundColor);

        // Right: affinity pips  ● ● ● ○ ○  (0–5 filled)
        const int MaxPips = 5;
        int       lvl     = (int)affinity;
        var       pips    = new System.Text.StringBuilder();
        for (int i = 0; i < MaxPips; i++)
        {
            pips.Append(i < lvl ? '●' : '○');
            if (i < MaxPips - 1) pips.Append(' ');
        }
        string pipsStr = pips.ToString();
        int    pipsX   = _layout.TERMINAL_WIDTH - _layout.RIGHT_PADDING - pipsStr.Length - 1;
        if (pipsX > labelX + label.Length + 2)
            _terminal.Text(pipsX, y, pipsStr,
                Config.NarrativeUI.DiceGoldColor, Config.NarrativeUI.BackgroundColor);
    }

    // ── Log + options ──────────────────────────────────────────────────────────

    private void RenderLogAndOptions(DialogueSessionState state)
    {
        var logLines = BuildLogLines(state.Log);
        int optLineCount = ComputeOptionLineCount(state);

        int contentRows = _layout.NARRATIVE_HEIGHT;
        // Reserve option rows + 1 gap row at bottom; log gets the rest
        int logRows = optLineCount > 0
            ? Math.Max(1, contentRows - optLineCount - 1)
            : contentRows;

        // Auto-scroll: scrollOffset = lines scrolled back from the bottom of log
        // 0 → show latest entries; positive → show that many lines further back
        int scrollOffset = Math.Max(0, state.ScrollOffset);
        int autoStart    = Math.Max(0, logLines.Count - logRows);
        int lineStart    = Math.Max(0, autoStart - scrollOffset);
        int lineEnd      = Math.Min(logLines.Count, lineStart + logRows);

        // Draw log lines
        int screenY = _layout.CONTENT_START_Y;
        for (int i = lineStart; i < lineEnd; i++)
        {
            var (text, color) = logLines[i];
            if (!string.IsNullOrEmpty(text))
            {
                int    maxW = _scrollbarX - _layout.CONTENT_START_X;
                string t    = text.Length > maxW ? text[..maxW] : text;
                _terminal.Text(_layout.CONTENT_START_X, screenY, t,
                    color, Config.NarrativeUI.BackgroundColor);
            }
            screenY++;
        }

        // Scrollbar (reflects position in the full log history)
        RenderScrollbar(logLines.Count, lineStart, false);

        // Gap row + options
        if (optLineCount > 0)
        {
            screenY++; // blank gap row
            RenderOptions(state, screenY);
        }
    }

    // ── Log line builder ───────────────────────────────────────────────────────

    private List<(string Text, Vector4 Color)> BuildLogLines(List<DialogueLogEntry> entries)
    {
        var lines = new List<(string, Vector4)>();
        int maxW  = _scrollbarX - _layout.CONTENT_START_X;

        foreach (var entry in entries)
        {
            switch (entry.Type)
            {
                case DialogueLogEntryType.Separator:
                    lines.Add((new string('─', maxW), Config.NarrativeUI.StatusBarColor));
                    break;

                case DialogueLogEntryType.SystemMessage:
                    foreach (var l in WrapText(entry.Text, maxW))
                        lines.Add((l, Config.NarrativeUI.StatusBarColor));
                    break;

                case DialogueLogEntryType.NpcSpeaking:
                {
                    string prefix  = entry.Speaker != null ? $"{entry.Speaker}: " : "";
                    foreach (var l in WrapText(prefix + entry.Text, maxW))
                        lines.Add((l, Config.NarrativeUI.NarrativeColor));
                    break;
                }

                case DialogueLogEntryType.PlayerReplica:
                {
                    string prefix  = entry.Speaker != null ? $"{entry.Speaker}: " : "";
                    foreach (var l in WrapText(prefix + entry.Text, maxW))
                        lines.Add((l, Config.Colors.LightCyan));
                    break;
                }
            }
        }

        return lines;
    }

    // ── Option line count (for layout reservation) ─────────────────────────────

    private int ComputeOptionLineCount(DialogueSessionState state)
    {
        if (state.ConversationEnded) return 0;
        if (state.IsLoadingOptions)  return 2; // progress hint takes ~2 rows

        if (state.Options.Count == 0) return 0;

        int total = 0;
        int maxW  = _scrollbarX - _layout.CONTENT_START_X;
        foreach (var opt in state.Options)
        {
            string skill     = opt.Skill.DisplayName;
            string lvlDot    = new string(Config.Symbols.ModusMentisLevelIndicator, opt.Skill.Level);
            int    prefixLen = 2 + 1 + skill.Length + 1 + lvlDot.Length + 2; // "> [skill dots] "
            int    firstW    = Math.Max(4, maxW - prefixLen);
            var    wrapped   = WrapText($"\u201C{opt.ReplicaText}\u201D", firstW);
            total += Math.Max(1, wrapped.Count);
        }
        return total;
    }

    // ── Options renderer ────────────────────────────────────────────────────────

    private void RenderOptions(DialogueSessionState state, int startY)
    {
        if (state.ConversationEnded) return;

        int maxX = _scrollbarX - 1;

        if (state.IsLoadingOptions)
        {
            if (startY < _layout.CONTENT_END_Y)
            {
                string msg = state.OptionsTotal > 0
                    ? $"  Thinking of replies… ({state.OptionsLoaded}/{state.OptionsTotal})"
                    : "  Thinking of replies…";
                _terminal.Text(_layout.CONTENT_START_X, startY, msg,
                    Config.NarrativeUI.HistoryColor, Config.NarrativeUI.BackgroundColor);
            }
            return;
        }

        for (int r = 0; r < state.Options.Count; r++)
        {
            if (startY > _layout.CONTENT_END_Y) break;

            bool    hovered   = r == state.HoveredOptionIndex;
            Vector4 bg        = hovered ? Config.NarrativeUI.ActionHoverBackgroundColor : Config.NarrativeUI.BackgroundColor;
            Vector4 textFg    = hovered ? Config.NarrativeUI.ActionHoverColor           : Config.NarrativeUI.ActionNormalColor;
            Vector4 bracketFg = hovered ? Config.NarrativeUI.ActionHoverColor           : Config.Colors.DarkYellowGrey;

            var    opt       = state.Options[r];
            string skill     = opt.Skill.DisplayName;
            string lvlDot    = new string(Config.Symbols.ModusMentisLevelIndicator, opt.Skill.Level);
            int    prefixLen = 2 + 1 + skill.Length + 1 + lvlDot.Length + 2;
            int    firstW    = Math.Max(4, (maxX - _layout.CONTENT_START_X) - prefixLen);
            int    contX     = _layout.CONTENT_START_X + prefixLen;
            int    contW     = Math.Max(4, maxX - contX);

            var wrapped = WrapText($"\u201C{opt.ReplicaText}\u201D", contW);
            if (wrapped.Count > 0 && wrapped[0].Length > firstW)
            {
                string overflow = wrapped[0][firstW..].TrimStart();
                wrapped[0]      = wrapped[0][..firstW];
                if (!string.IsNullOrEmpty(overflow)) wrapped.Insert(1, overflow);
            }
            if (wrapped.Count == 0)
                wrapped.Add(opt.ReplicaText.Length > firstW ? opt.ReplicaText[..firstW] : opt.ReplicaText);

            // First line: prefix + first wrapped segment
            {
                int x = _layout.CONTENT_START_X;
                void PutSeg(string s, Vector4 fg, Vector4 segBg)
                {
                    if (x >= maxX || string.IsNullOrEmpty(s)) return;
                    int    avail = maxX - x;
                    string seg   = s.Length > avail ? s[..avail] : s;
                    _terminal.Text(x, startY, seg, fg, segBg);
                    x += seg.Length;
                }
                PutSeg("> ",    Config.NarrativeUI.NarrativeColor, Config.NarrativeUI.BackgroundColor);
                PutSeg("[",     bracketFg, bg);
                PutSeg(skill,   bracketFg, bg);
                PutSeg(" ",     bracketFg, bg);
                PutSeg(lvlDot,  Config.NarrativeUI.LoadingColor, bg);
                PutSeg("] ",    bracketFg, bg);
                if (wrapped.Count > 0) PutSeg(wrapped[0], textFg, bg);
            }
            _optionRowToIndex[startY] = r;
            startY++;

            // Continuation lines (word-wrap overflow)
            for (int ln = 1; ln < wrapped.Count && startY <= _layout.CONTENT_END_Y; ln++)
            {
                int    avail = maxX - contX;
                string seg   = wrapped[ln].Length > avail ? wrapped[ln][..avail] : wrapped[ln];
                _terminal.Text(contX, startY, seg, textFg, bg);
                _optionRowToIndex[startY] = r;
                startY++;
            }
        }
    }

    // ── Status bar text ────────────────────────────────────────────────────────

    private string BuildStatusText(DialogueSessionState state)
    {
        if (state.ConversationEnded)   return "Conversation ended — click to exit.";
        if (state.IsLoadingNpcReplica) return $"{_npc.DisplayName} is thinking…";
        if (state.IsLoadingOptions)    return $"Thinking of replies… ({state.OptionsLoaded}/{state.OptionsTotal})";
        if (state.IsLoadingReaction)   return "Waiting for response…";
        if (state.Options.Count > 0)   return "Click a reply. Scroll to read history.";
        return "";
    }
}
