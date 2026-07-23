using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Terminal;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Renders a dialogue tree session onto the terminal HUD.
///
/// Visual structure:
///   Header row  : NPC name (left) | affinity label (centre) | affinity pips (right)
///   Separator
///   Scrollable log  : NPC speech, selectable player replies, system messages — the replies are
///                     ordinary buffer lines (like narration action lines), so they scroll with
///                     the conversation; after a pick the chosen one stays highlighted and the
///                     rest grey out
///   Separator
///   Status bar
///
/// Coordinate system: all x/y values are terminal cell coordinates.
/// </summary>
public class DialogueTreeUI : TerminalPanelUI
{
    private readonly NpcEntity    _npc;
    private readonly string       _partyMemberId;

    /// <summary>
    /// The narration session's shared text history — the conversation's own lines plus everything
    /// that came before it in this location visit, greyed out.
    /// </summary>
    private readonly NarrationScrollBuffer _buffer;

    /// <summary>Rows reserved at the bottom of the content area for the footer exit button.</summary>
    private const int FooterRows = 2;

    // Row → option index mapping, rebuilt every Render call
    private readonly Dictionary<int, int> _optionRowToIndex = new();

    public DialogueTreeUI(
        TerminalHUD           terminal,
        NpcEntity             npc,
        string                partyMemberId,
        NarrationScrollBuffer scrollBuffer)
        : base(terminal)
    {
        _npc           = npc;
        _partyMemberId = partyMemberId;
        _buffer        = scrollBuffer;
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>Returns the 0-based index of the option at the given cell row, or -1.</summary>
    public int GetOptionIndexAt(int mx, int my)
        => _optionRowToIndex.TryGetValue(my, out int idx) ? idx : -1;

    /// <summary>
    /// Render the panel frame with a bottom status message, used during session setup
    /// (LLM slot acquisition) before any dialogue content exists. Keeping the same
    /// bordered frame as the narration panel avoids a black flash during the transition.
    /// </summary>
    public void RenderSetupFrame(string message, string? error = null)
    {
        Clear();
        RenderHeader();
        DrawHorizontalLine(_layout.TOP_PADDING + 1);

        if (error != null)
        {
            ShowError(error);
            DrawStatusBar("Error — press ESC to exit.");
        }
        else
        {
            // Keep the shared history visible (dimmed) under the progress bar — without it the log
            // area is empty for the whole slot-acquisition beat, which reads as a black flash
            // between the narration frame and the first dialogue frame.
            RenderLog(_layout.NARRATIVE_HEIGHT - FooterRows);
            DimContent();
            RenderCenterProgressBar();
            RenderWaitingStatus(message);
        }
    }

    // ── Main render entry ──────────────────────────────────────────────────────

    public void Render(DialogueSessionState state, DiceRollComponent dice)
    {
        Clear();
        _optionRowToIndex.Clear();

        // The footer button is only rendered in the interactive states below; clear its
        // click region each frame so stale zones don't linger during dice/loading states.
        state.ExitButtonRegion = default;

        // Header: NPC name/affinity. Always rendered — while the LLM is generating it's
        // greyed out along with the rest of the panel (see below) rather than replaced.
        string? generating = state.ErrorMessage == null ? BuildGeneratingText(state) : null;
        RenderHeader();
        DrawHorizontalLine(_layout.TOP_PADDING + 1);

        if (state.ErrorMessage != null)
        {
            ShowError(state.ErrorMessage);
            state.ExitButtonRegion = RenderExitButton("END", state.IsExitButtonHovered);
            DrawStatusBar("Error — end to exit.");
            return;
        }

        // Bottom rows are reserved for the footer exit button (END / INTERRUPT); the replies are
        // ordinary buffer lines inside the log window, so no extra area is reserved for them.
        RenderLog(_layout.NARRATIVE_HEIGHT - FooterRows, state);

        // Dice overlay: the conversation stays visible but greyed out underneath a
        // small dice box — same presentation as fight mode.
        if (state.IsDiceRollActive)
        {
            RenderDiceComponent(dice, state.IsContinueHovered);
            DrawStatusBar(state.IsDiceRolling
                ? "Rolling..."
                : (dice.IsCurrentlySuccess ? "SUCCESS — click to continue." : "FAILURE — click to continue."));
            return;
        }

        // LLM generating: grey out the whole panel (header included), with a centered
        // progress bar animation and the waiting message (animated ellipsis) on the footer.
        if (generating != null)
        {
            DimContent();
            RenderCenterProgressBar();
            RenderWaitingStatus(generating);
            return;
        }

        // Footer button — same placement as the narration panel: END once the
        // conversation has ended, INTERRUPT to walk away mid-conversation.
        state.ExitButtonRegion = RenderExitButton(
            state.ConversationEnded ? "END" : "INTERRUPT",
            state.IsExitButtonHovered);

        DrawStatusBar(BuildStatusText(state));
    }

    /// <summary>The generating-status message for the current loading state, or null when idle.</summary>
    private string? BuildGeneratingText(DialogueSessionState state)
    {
        if (state.IsLoadingReaction)   return $"{_npc.DisplayName} reacts…";
        if (state.IsLoadingNpcReplica) return $"{_npc.DisplayName} is thinking…";
        if (state.IsLoadingOptions)    return state.OptionsTotal > 0
            ? $"Thinking of replies… ({state.OptionsLoaded}/{state.OptionsTotal})"
            : "Thinking of replies…";
        return null;
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

        // Right: "Affinity ● ● ● ○ ○" — pips show the 0–5 affinity level
        const int MaxPips = 5;
        int       lvl     = (int)affinity;
        var       pips    = new System.Text.StringBuilder();
        for (int i = 0; i < MaxPips; i++)
        {
            pips.Append(i < lvl ? '●' : '○');
            if (i < MaxPips - 1) pips.Append(' ');
        }
        const string pipsLabel  = "Affinity ";
        string pipsStr    = pips.ToString();
        int    pipsX      = _layout.TERMINAL_WIDTH - _layout.RIGHT_PADDING - pipsStr.Length - 1;
        int    pipsLabelX = pipsX - pipsLabel.Length;
        if (pipsLabelX > labelX + label.Length + 2)
        {
            _terminal.Text(pipsLabelX, y, pipsLabel,
                Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
            _terminal.Text(pipsX, y, pipsStr,
                Config.NarrativeUI.DiceGoldColor, Config.NarrativeUI.BackgroundColor);
        }
    }

    // ── Log (conversation text + inline reply lines) ───────────────────────────

    /// <summary>
    /// Renders the shared-history log window (top <paramref name="logRows"/> rows of the content
    /// area) plus its scrollbar, and returns the row just below the window. Shared by the live
    /// render and <see cref="RenderSetupFrame"/>, so the log never disappears between frames.
    /// Live dialogue-option lines get their own styling and click rows; pass a null
    /// <paramref name="state"/> (setup frame) to render them without hover feedback.
    /// </summary>
    private int RenderLog(int logRows, DialogueSessionState? state = null)
    {
        var lines = _buffer.GetVisibleLines(logRows);

        int maxW    = _scrollbarX - _layout.CONTENT_START_X;
        int screenY = _layout.CONTENT_START_Y;
        foreach (var line in lines)
        {
            if (line.Type == LineType.DialogueOption && !line.IsHistory)
            {
                RenderOptionLine(line, screenY, maxW, state);
            }
            else if (!string.IsNullOrEmpty(line.Text))
            {
                string t = line.Text.Length > maxW ? line.Text[..maxW] : line.Text;
                _terminal.Text(_layout.CONTENT_START_X, screenY, t,
                    ColorFor(line), Config.NarrativeUI.BackgroundColor);
            }
            screenY++;
        }

        // Scrollbar (reflects position in the whole shared history)
        RenderScrollbar(_buffer.TotalLines, _buffer.ScrollOffset, false);
        return screenY;
    }

    /// <summary>
    /// One wrapped line of a selectable reply, straight from the scroll buffer. Three visual
    /// states, driven by the source block's <c>SelectedDialogueOptionIndex</c>: still choosing
    /// (clickable, hover-highlit, skill bracket coloured like narration action lines), the chosen
    /// reply after a pick (player colour), and its rejected siblings (greyed out).
    /// </summary>
    private void RenderOptionLine(RenderedLine line, int y, int maxW, DialogueSessionState? state)
    {
        int    idx      = line.DialogueOptionIndex;
        int    selected = line.SourceBlock?.SelectedDialogueOptionIndex ?? -1;
        string text     = line.Text.Length > maxW ? line.Text[..maxW] : line.Text;
        int    x        = _layout.CONTENT_START_X;

        if (selected >= 0)
        {
            // A resolved group: what the player said keeps the player's colour, the rest grey out.
            _terminal.Text(x, y, text,
                idx == selected ? Config.Colors.LightPurple : Config.NarrativeUI.DimmedContentColor,
                Config.NarrativeUI.BackgroundColor);
            return;
        }

        // Live group — register the click row and apply hover styling.
        _optionRowToIndex[y] = idx;
        bool    hovered   = state != null && idx == state.HoveredOptionIndex;
        Vector4 bg        = hovered ? Config.NarrativeUI.ActionHoverBackgroundColor : Config.NarrativeUI.BackgroundColor;
        Vector4 textFg    = hovered ? Config.NarrativeUI.ActionHoverColor           : Config.NarrativeUI.ActionNormalColor;
        Vector4 bracketFg = hovered ? Config.NarrativeUI.ActionHoverColor           : Config.Colors.DarkYellowGrey;

        // First lines read "> [Skill ▪▪] “…”"; continuation lines are plain indented text.
        int close = text.StartsWith("> [") ? text.IndexOf(']') : -1;
        if (close < 0)
        {
            _terminal.Text(x, y, text, textFg, bg);
            return;
        }

        // The run of level dots directly before the closing bracket gets the dice colour.
        int dotsStart = close;
        while (dotsStart > 3 && text[dotsStart - 1] == Config.Symbols.ModusMentisLevelIndicator)
            dotsStart--;

        _terminal.Text(x, y, "> ", Config.NarrativeUI.NarrativeColor, Config.NarrativeUI.BackgroundColor);
        _terminal.Text(x + 2,         y, text[2..dotsStart],     bracketFg, bg);
        _terminal.Text(x + dotsStart, y, text[dotsStart..close], Config.NarrativeUI.LoadingColor, bg);
        _terminal.Text(x + close,     y, "]",                    bracketFg, bg);
        if (close + 1 < text.Length)
            _terminal.Text(x + close + 1, y, text[(close + 1)..], textFg, bg);
    }

    /// <summary>
    /// Colour for one buffer line. Mirrors <c>NarrativeUI.RenderHistoryLine</c> so a line looks the
    /// same whichever panel is showing it.
    /// </summary>
    private static Vector4 ColorFor(RenderedLine line)
    {
        if (line.IsHistory)
            return line.Type == LineType.Separator
                ? Config.NarrativeUI.SeparatorColor
                : Config.NarrativeUI.HistoryColor;

        return line.Type switch
        {
            LineType.Separator => Config.NarrativeUI.SeparatorColor,
            LineType.Header    => Config.NarrativeUI.ModusMentisHeaderColor,
            _ => line.BlockType switch
            {
                NarrationBlockType.PlayerSpeaking => Config.Colors.LightPurple,
                NarrationBlockType.Outcome        => Config.NarrativeUI.StatusBarColor,
                _                                 => Config.NarrativeUI.NarrativeColor,
            },
        };
    }

    // ── Status bar text ────────────────────────────────────────────────────────

    private string BuildStatusText(DialogueSessionState state)
    {
        if (state.ConversationEnded)   return "Conversation ended — leave to exit.";
        if (state.Options.Count > 0)   return "Click a reply. Scroll to read history.";
        return "";
    }
}
