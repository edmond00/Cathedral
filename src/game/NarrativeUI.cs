using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene;

namespace Cathedral.Game;

/// <summary>
/// What the narration draws on the terminal panel round and over its map: the header (who is played,
/// whose ground they stand on, their noetic points), the map itself, the box a node's text is read in
/// again, and the box an act's result is read in. The boxes shared with the conversation panel — the
/// dice, the hours passing, the text being written — are <see cref="TerminalPanelUI"/>'s.
/// </summary>
public class NarrativeUI : TerminalPanelUI
{
    public NarrativeUI(TerminalHUD terminal) : base(terminal) { }

    /// <summary>
    /// Render the header: the member played (left), whose ground they stand on (centre) and their
    /// noetic points (right). Pass <paramref name="showNoeticPoints"/> as false for the phases that
    /// keep no budget (childhood reminescence, get-up).
    /// </summary>
    /// <param name="trespassing">True in a private area, where everything done is a crime and no
    /// routine is learned; false on open ground; null where the question does not arise (no real
    /// place), which draws nothing.</param>
    public void RenderHeader(string memberName, int noeticPoints, int maxNoeticPoints,
        bool showNoeticPoints = true, bool? trespassing = null)
    {
        int headerY = _layout.TOP_PADDING;
        _terminal.Text(_layout.CONTENT_START_X, headerY, $"[{memberName.ToUpper()}]",
                       Config.NarrativeUI.HeaderColor, Config.NarrativeUI.BackgroundColor);

        // Centre: the ground. Shown in both states rather than only as a warning, so its absence is
        // never mistaken for safety — the one fact that turns every act here into a crime.
        if (trespassing is { } trespass)
        {
            string ground = trespass ? "◆ TRESPASSING ◆" : "open ground";
            var fg = trespass ? Config.Colors.BrightPurple : Config.NarrativeUI.HistoryColor;
            int groundX = _layout.CONTENT_START_X
                        + (_layout.CONTENT_END_X - _layout.CONTENT_START_X - ground.Length) / 2;
            _terminal.Text(groundX, headerY, ground, fg, Config.NarrativeUI.BackgroundColor);
        }

        // Right: the noetic points, named. The markers alone were a row of unexplained circles — the
        // one number the whole narration is spent against, with nothing saying what it counts.
        if (showNoeticPoints)
        {
            const string label = "NOETIC ";
            int labelX  = _layout.CONTENT_END_X - (label.Length + 1 + maxNoeticPoints + 1);
            int markerX = labelX + label.Length + 1;
            _terminal.Text(labelX, headerY, label, Config.NarrativeUI.HeaderColor, Config.NarrativeUI.BackgroundColor);
            _terminal.Text(markerX - 1, headerY, "[", Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
            for (int i = 0; i < maxNoeticPoints; i++)
                _terminal.Text(markerX + i, headerY, Config.Symbols.NoeticPointMarker.ToString(),
                    i < noeticPoints ? Config.NarrativeUI.LoadingColor : Config.NarrativeUI.HistoryColor,
                    Config.NarrativeUI.BackgroundColor);
            _terminal.Text(markerX + maxNoeticPoints, headerY, "]", Config.NarrativeUI.StatusBarColor, Config.NarrativeUI.BackgroundColor);
        }

        DrawHorizontalLine(_layout.TOP_PADDING + 1);
    }

    /// <summary>
    /// Draws the location map over the whole content area, with what the narration draws over it.
    /// <paramref name="walker"/> is the canvas cell of somebody shown walking, which the view follows.
    /// </summary>
    public void RenderMap(LocationMap map, Scene.Scene scene, Area here, TimePeriod period,
                          LocationMap.IMapOverlay overlay, (int X, int Y)? walker = null,
                          char walkerGlyph = Config.GlyphSphere.ProtagonistChar,
                          IReadOnlySet<string>? withheld = null)
    {
        ClearContent();
        int left = _layout.LEFT_PADDING + 1;
        int width = _layout.TERMINAL_WIDTH - _layout.RIGHT_PADDING - 1 - left;
        map.Render(_terminal, scene, here, period, left, _layout.CONTENT_START_Y, width, _layout.NARRATIVE_HEIGHT,
                   walker, follow: true, withheld, overlay: overlay, partyGlyph: walkerGlyph);
    }

    /// <summary>
    /// The box a node's text is read in again, docked at the foot of the map: the mind that wrote it
    /// on the border, the text, and under it the chips of what it came to.
    /// </summary>
    public void RenderReadingBox(string title, string text, IReadOnlyList<Outcome>? chips = null)
    {
        int width = _layout.CONTENT_END_X - _layout.CONTENT_START_X;
        int inner = width - 4;
        var lines = WrapText(text, inner);
        var chipRows = ChipRows(chips, inner);
        int rows = Math.Min(lines.Count, Math.Max(1, _layout.NARRATIVE_HEIGHT / 3));
        int height = rows + chipRows.Count + (chipRows.Count > 0 ? 1 : 0) + 2;
        int x = _layout.CONTENT_START_X;
        int y = _layout.SEPARATOR_Y - height;

        _terminal.FillRect(x, y, width, height, ' ', Config.Colors.White, Config.NarrativeUI.BackgroundColor);
        _terminal.DrawBox(x, y, width, height, BoxStyle.Single, Config.NarrativeUI.SeparatorColor, Config.NarrativeUI.BackgroundColor);
        if (title.Length > 0)
            _terminal.Text(x + 2, y, $" {title} ", Config.NarrativeUI.ModusMentisHeaderColor, Config.NarrativeUI.BackgroundColor);

        for (int i = 0; i < rows; i++)
        {
            // A text too long for the box ends on an ellipsis rather than mid-sentence without one.
            string line = i == rows - 1 && lines.Count > rows ? TrimTo(lines[i], inner - 2) + " …" : lines[i];
            _terminal.Text(x + 2, y + 1 + i, line, Config.NarrativeUI.NarrativeColor, Config.NarrativeUI.BackgroundColor);
        }
        DrawChips(chipRows, x + 2, y + 1 + rows + (chipRows.Count > 0 ? 1 : 0), inner);
    }

    /// <summary>
    /// The box an act's result is read in, centred over the greyed map: how it went, the chips of
    /// what it came to, and a CONTINUE. Returns the button's region.
    /// </summary>
    public (int X, int Y, int Width) RenderResultBox(string title, bool succeeded, IReadOnlyList<Outcome> chips, bool continueHovered)
    {
        DimContent();
        // As wide as its longest chip asks, within the panel.
        int longest = chips.Count == 0 ? 0 : chips.Max(c => c.Text.Length) + 2;
        int width = Math.Clamp(longest + 4, Math.Max(40, _layout.CONTENT_WIDTH / 2), _layout.CONTENT_WIDTH - 2);
        int inner = width - 4;
        var titleLines = WrapText(title, inner);
        var chipRows = ChipRows(chips, inner);
        int height = titleLines.Count + chipRows.Count + 6;
        int x = (_layout.TERMINAL_WIDTH - width) / 2;
        int y = _layout.CONTENT_START_Y + (_layout.NARRATIVE_HEIGHT - height) / 2;

        _terminal.FillRect(x, y, width, height, ' ', Config.Colors.White, Config.NarrativeUI.BackgroundColor);
        _terminal.DrawBox(x, y, width, height, BoxStyle.Single, Config.NarrativeUI.SeparatorColor, Config.NarrativeUI.BackgroundColor);
        var ink = succeeded ? Config.Colors.GoldYellow : Config.Colors.LightPurple;
        for (int i = 0; i < titleLines.Count; i++)
            _terminal.Text(x + width / 2, y + 1 + i, titleLines[i], ink, Config.NarrativeUI.BackgroundColor, TextAlignment.Center);
        DrawChips(chipRows, x + 2, y + 2 + titleLines.Count, inner);

        const string button = "[ CONTINUE ]";
        int buttonX = x + (width - button.Length) / 2, buttonY = y + height - 2;
        _terminal.Text(buttonX, buttonY, button,
            continueHovered ? Config.NarrativeUI.ContinueButtonHoverColor : Config.NarrativeUI.ContinueButtonColor,
            continueHovered ? Config.NarrativeUI.ContinueButtonHoverBackgroundColor : Config.NarrativeUI.ContinueButtonBackgroundColor);
        return (buttonX, buttonY, button.Length);
    }

    /// <summary>Chips laid out in rows no wider than <paramref name="width"/>, each a band of its severity's colour.</summary>
    private static List<List<Outcome>> ChipRows(IReadOnlyList<Outcome>? chips, int width)
    {
        var rows = new List<List<Outcome>>();
        if (chips == null) return rows;
        int used = 0;
        foreach (var chip in chips)
        {
            int w = Math.Min(chip.Text.Length, width - 2) + 2;
            if (rows.Count == 0 || used + w + 1 > width) { rows.Add(new()); used = 0; }
            rows[^1].Add(chip);
            used += w + 1;
        }
        return rows;
    }

    private void DrawChips(List<List<Outcome>> rows, int x, int y, int width)
    {
        for (int r = 0; r < rows.Count; r++)
        {
            int cx = x;
            foreach (var chip in rows[r])
            {
                var band = chip.Severity switch
                {
                    OutcomeSeverity.Negative => Config.NarrativeUI.OutcomeReportNegativeBackground,
                    OutcomeSeverity.Positive => Config.NarrativeUI.OutcomeReportPositiveBackground,
                    _                        => Config.NarrativeUI.OutcomeReportNeutralBackground,
                };
                string text = $" {TrimTo(chip.Text, width - 2)} ";
                _terminal.Text(cx, y + r, text, Config.NarrativeUI.OutcomeReportTextColor, band);
                cx += text.Length + 1;
            }
        }
    }

    private static string TrimTo(string s, int width) => s.Length <= width ? s : s[..Math.Max(0, width)];

    /// <summary>
    /// Render the footer status bar: where the member stands and the hour while nothing is happening,
    /// or what is.
    /// </summary>
    public void RenderStatusBar(string message = "") => DrawStatusBar(message);
}
