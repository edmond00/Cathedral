using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Terminal;

namespace Cathedral.Game.Dialogue;

/// <summary>
/// Renders the dialogue UI onto a TerminalHUD, reusing the same bordered-box layout
/// as <see cref="Cathedral.Game.NarrativeUI"/> via the shared <see cref="Cathedral.Game.TerminalPanelUI"/> base.
///
/// Visual structure (within the padding border):
///   Header row  : NPC name (left) | affinity bar (centre) | subject label (right)
///   Separator
///   Scrollable area : dialogue history (NPC + chosen player lines)
///   Pinned bottom   : selectable player-reply choices (while awaiting input)
///   Separator
///   Status bar
/// </summary>
public class DialogueUI : Cathedral.Game.TerminalPanelUI
{
    private readonly NpcInstance _npc;

    /// <summary>The scroll buffer owned by this UI instance (exposed for the controller).</summary>
    public DialogueScrollBuffer Buffer { get; }

    // Replica screen-row 竊・replica index  (rebuilt every Render call)
    private readonly Dictionary<int, int> _replicaLineToIndex = new();

    // Last scrollbar thumb position for drag support
    private (int StartY, int Height) _scrollbarThumb;

    public DialogueUI(TerminalHUD terminal, NpcInstance npc) : base(terminal)
    {
        _npc   = npc;
        Buffer = new DialogueScrollBuffer(_layout.CONTENT_WIDTH);
    }

    // 笏笏 Public API 笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏

    /// <summary>Returns the replica index for a screen cell, or -1.</summary>
    public int GetReplicaIndexAt(int mouseX, int mouseY)
        => _replicaLineToIndex.TryGetValue(mouseY, out int idx) ? idx : -1;

    /// <summary>True if the mouse is on the scrollbar thumb.</summary>
    public bool IsScrollbarThumbHit(int x, int y) => IsMouseOverScrollbarThumb(x, y, _scrollbarThumb);

    /// <summary>True if the mouse is on the scrollbar track (but not thumb).</summary>
    public bool IsScrollbarTrackHit(int x, int y) => IsMouseOverScrollbarTrack(x, y, _scrollbarThumb);

    /// <summary>Translate a mouse-Y inside the scrollbar track to a scroll offset.</summary>
    public int GetScrollOffsetFromMouse(int mouseY) => CalculateScrollOffsetFromMouseY(mouseY, Buffer.TotalLines);

    // 笏笏 Override Clear to also reset hit regions 笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏

    public override void Clear()
    {
        base.Clear();
        _replicaLineToIndex.Clear();
    }

    // 笏笏 Main render 笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏

    public void Render(DialogueState state)
    {
        Clear();
        RenderHeader();
        DrawHorizontalLine(_layout.TOP_PADDING + 1); // separator below header

        if (state.ErrorMessage != null)
        {
            ShowError(state.ErrorMessage);
            DrawStatusBar("Error — press ESC to exit");
            return;
        }

        if (state.IsDiceRollActive)
        {
            RenderDiceResult(state);
            DrawStatusBar(state.IsDiceRolling
                ? "Rolling..."
                : (state.DiceRollSucceeded ? "SUCCESS — click to continue." : "FAILURE — click to continue."));
            return;
        }

        if (state.IsLoadingGreeting)
        {
            ShowLoadingIndicator(_npc.DisplayName + " considers you…");
            DrawStatusBar("Waiting for NPC…");
            return;
        }

        RenderContent(state);
        _scrollbarThumb = RenderScrollbar(Buffer.TotalLines, Buffer.ScrollOffset, false);
        DrawStatusBar(BuildStatusText(state));
    }

    // 笏笏 Header 笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏

    private void RenderHeader()
    {
        int y = _layout.TOP_PADDING;

        // Left: NPC name
        _terminal.Text(_layout.CONTENT_START_X, y,
            $"• {_npc.DisplayName}",
            Config.NarrativeUI.HeaderColor, Config.NarrativeUI.BackgroundColor);

        // Centre: affinity bar
        float pct    = Math.Clamp(_npc.Affinity / 100f, 0f, 1f);
        int   barW   = 16;
        int   filled = (int)(pct * barW);
        string bar   = $"{_npc.Affinity:F0} [{new string('\u2588', filled)}{new string('\u2591', barW - filled)}]";
        int barX     = (_layout.TERMINAL_WIDTH - bar.Length) / 2;
        _terminal.Text(barX, y, bar, Config.NarrativeUI.LoadingColor, Config.NarrativeUI.BackgroundColor);

        // Right: subject label
        string subject  = _npc.CurrentSubjectNode.ContextDescription;
        int    subjectX = _layout.TERMINAL_WIDTH - _layout.RIGHT_PADDING - subject.Length - 1;
        if (subjectX > barX + bar.Length + 2)
            _terminal.Text(subjectX, y, subject,
                Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
    }

    // 笏笏 Scrollable content area 笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏笏

    private void RenderContent(DialogueState state)
    {
        _replicaLineToIndex.Clear();

        bool hasReplicas = !state.IsLoadingReplicas
            && state.Replicas.Count > 0
            && !state.ConversationEnded;

        int bufferRows = _layout.NARRATIVE_HEIGHT - _layout.SEPARATOR_HEIGHT;

        // Dialogue history (scroll buffer)
        var visibleLines = Buffer.GetVisibleLines(bufferRows);
        for (int i = 0; i < visibleLines.Count; i++)
        {
            int     screenY   = _layout.CONTENT_START_Y + i;
            var     line      = visibleLines[i];
            Vector4 textColor = line.BlockType switch
            {
                DialogueBlockType.NpcSpeaking   => Config.NarrativeUI.NarrativeColor,
                DialogueBlockType.PlayerReplica => Config.Colors.LightCyan,
                DialogueBlockType.SystemMessage => Config.NarrativeUI.HistoryColor,
                DialogueBlockType.DiceRoll      => Config.Colors.OrangeYellow,
                _                               => Config.NarrativeUI.NarrativeColor
            };
            string histText = line.Text;
            int    maxW = _scrollbarX - _layout.CONTENT_START_X;
            if (histText.Length > maxW) histText = histText[..maxW];
            _terminal.Text(_layout.CONTENT_START_X, screenY, histText, textColor, Config.NarrativeUI.BackgroundColor);
        }

        // Loading-replicas progress hint
        if (state.IsLoadingReplicas)
        {
            int loadY = _layout.CONTENT_START_Y + visibleLines.Count + 1;
            if (loadY < _layout.CONTENT_END_Y)
            {
                string msg = state.ReplicasTotal > 0
                    ? $"  Generating reply {state.ReplicasLoaded + 1}/{state.ReplicasTotal}..."
                    : "  Generating replies...";
                _terminal.Text(_layout.CONTENT_START_X, loadY, msg,
                    Config.NarrativeUI.HistoryColor, Config.NarrativeUI.BackgroundColor);
            }
        }

        // Selectable reply choices inline after history, narration-action style
        if (hasReplicas)
        {
            int y    = _layout.CONTENT_START_Y + visibleLines.Count + 1;
            int maxX = _scrollbarX - 1;

            for (int r = 0; r < state.Replicas.Count; r++)
            {
                if (y > _layout.CONTENT_END_Y) break;

                bool    hovered   = r == state.HoveredReplicaIndex;
                Vector4 bg        = hovered ? Config.NarrativeUI.ActionHoverBackgroundColor : Config.NarrativeUI.BackgroundColor;
                Vector4 textFg    = hovered ? Config.NarrativeUI.ActionHoverColor           : Config.NarrativeUI.ActionNormalColor;
                Vector4 bracketFg = hovered ? Config.NarrativeUI.ActionHoverColor           : Config.Colors.DarkYellowGrey;

                var    opt        = state.Replicas[r];
                string skillName  = opt.Skill.DisplayName;
                string levelDots  = new string(Config.Symbols.ModusMentisLevelIndicator, opt.Skill.Level);

                // Prefix segments: "> [SkillName levelDots] "
                int prefixLen   = 2 + 1 + skillName.Length + 1 + levelDots.Length + 2;
                int firstLineW  = Math.Max(4, (maxX - _layout.CONTENT_START_X) - prefixLen);
                int contIndent  = _layout.CONTENT_START_X + prefixLen;
                int contW       = Math.Max(4, maxX - contIndent);

                // Word-wrap the replica text
                string rawText   = $"\u0022{opt.ReplicaText}\u0022";
                var    wrapped   = WrapText(rawText, contW);
                // Ensure first segment fits firstLineW
                if (wrapped.Count > 0 && wrapped[0].Length > firstLineW)
                {
                    string overflow = wrapped[0][firstLineW..].TrimStart();
                    wrapped[0]      = wrapped[0][..firstLineW];
                    if (!string.IsNullOrEmpty(overflow)) wrapped.Insert(1, overflow);
                }
                if (wrapped.Count == 0) wrapped.Add(rawText.Length > firstLineW ? rawText[..firstLineW] : rawText);

                // Render first line: prefix + first text segment
                {
                    int x = _layout.CONTENT_START_X;
                    void PutSeg(string s, Vector4 fg, Vector4 segBg)
                    {
                        if (x >= maxX || string.IsNullOrEmpty(s)) return;
                        int avail = maxX - x;
                        string seg = s.Length > avail ? s[..avail] : s;
                        _terminal.Text(x, y, seg, fg, segBg);
                        x += seg.Length;
                    }
                    PutSeg("> ",      Config.NarrativeUI.NarrativeColor, Config.NarrativeUI.BackgroundColor);
                    PutSeg("[",       bracketFg, bg);
                    PutSeg(skillName, bracketFg, bg);
                    PutSeg(" ",       bracketFg, bg);
                    PutSeg(levelDots, Config.NarrativeUI.LoadingColor, bg);
                    PutSeg("] ",      bracketFg, bg);
                    if (wrapped.Count > 0) PutSeg(wrapped[0], textFg, bg);
                }
                _replicaLineToIndex[y] = r;
                y++;

                // Continuation lines (word-wrapped remainder)
                for (int ln = 1; ln < wrapped.Count && y <= _layout.CONTENT_END_Y; ln++)
                {
                    int avail = maxX - contIndent;
                    string seg = wrapped[ln].Length > avail ? wrapped[ln][..avail] : wrapped[ln];
                    _terminal.Text(contIndent, y, seg, textFg, bg);
                    _replicaLineToIndex[y] = r;
                    y++;
                }
            }
        }
    }

    private void RenderDiceResult(DialogueState state)
    {
        ShowDiceRollIndicator(
            state.DiceRollNumberOfDice,
            state.DiceRollDifficulty,
            state.IsDiceRolling,
            state.DiceRollFinalValues,
            state.IsDiceRollButtonHovered);
    }

    private string BuildStatusText(DialogueState state)
    {
        if (state.ConversationEnded)  return "Conversation ended — click to exit.";
        if (state.IsLoadingReplicas)  return $"Thinking of replies… ({state.ReplicasLoaded}/{state.ReplicasTotal})";
        if (state.IsLoadingResponse) return "Waiting for response…";
        if (state.Replicas.Count > 0) return "Click a reply to speak. Scroll with mouse wheel.";
        return "";
    }
}
