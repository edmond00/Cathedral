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
        if (Blackout) return;
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
                          IReadOnlySet<string>? withheld = null)
    {
        ClearContent();
        int left = _layout.LEFT_PADDING + 1;
        int width = _layout.TERMINAL_WIDTH - _layout.RIGHT_PADDING - 1 - left;
        map.Render(_terminal, scene, here, period, left, _layout.CONTENT_START_Y, width, _layout.NARRATIVE_HEIGHT,
                   walker, follow: true, withheld, overlay: overlay);
    }

    // The map is drawn in grounds as much as in glyphs, so it is darkened in place — hue kept —
    // rather than flattened to grey on black. Once a frame: a second darkening would be a darker one.
    private const float MapShade = 0.38f;
    private bool _shaded;

    /// <summary>
    /// The whole terminal is black and nothing of the panel's own is drawn: no border, no header,
    /// no status row, and not the sky behind the padding either — every cell is opaque. The
    /// childhood is played so: a memory coming back in the dark, with the world, the sky and the
    /// ordinary screen first seen when it is time to get up. The boxes a text is read in are drawn
    /// as everywhere, and the line that says a mind is at work stays, alone on its row.
    /// </summary>
    public bool Blackout { get; set; }

    protected override bool Chromeless => Blackout;

    public override void Clear()
    {
        _shaded = false;
        if (!Blackout) { base.Clear(); return; }
        for (int y = 0; y < _layout.TERMINAL_HEIGHT; y++)
            for (int x = 0; x < _layout.TERMINAL_WIDTH; x++)
                _terminal.SetCell(x, y, ' ', Config.Colors.Black, Config.Colors.Black);
    }

    protected override void DimFrom(int startY)
    {
        if (_shaded) return;
        _shaded = true;
        _terminal.ShadeRect(
            _layout.LEFT_PADDING, startY,
            _layout.TERMINAL_WIDTH - _layout.LEFT_PADDING - _layout.RIGHT_PADDING,
            (_layout.TERMINAL_HEIGHT - _layout.BOTTOM_PADDING) - startY, MapShade);
    }

    /// <summary>
    /// The box a node's text is read in again, docked at the foot of the map: the mind that wrote it
    /// on the border, the text, and under it the chips of what it came to. An act is read with its
    /// <paramref name="odds"/> on a line of their own, in another ink than the text: they are a
    /// reckoning, not something a mind said.
    /// </summary>
    public void RenderReadingBox(string title, string text, IReadOnlyList<Outcome>? chips = null, string? odds = null)
    {
        int width = _layout.CONTENT_END_X - _layout.CONTENT_START_X;
        int inner = width - 4;
        var lines = WrapText(text, inner);
        int chipRows = chips?.Count ?? 0;
        int rows = Math.Min(lines.Count, Math.Max(1, _layout.NARRATIVE_HEIGHT / 3));
        int height = rows + (odds != null ? 1 : 0) + chipRows + (chipRows > 0 ? 1 : 0) + 2;
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
        if (odds != null)
            _terminal.Text(x + 2, y + 1 + rows, TrimTo(odds, inner), Config.Colors.DarkYellowGrey, Config.NarrativeUI.BackgroundColor);
        DrawChips(chips, x + 2, y + 1 + rows + (odds != null ? 1 : 0) + (chipRows > 0 ? 1 : 0), inner);
    }

    /// <summary>
    /// The box an act's result is read in, centred over the darkened map: its <paramref name="title"/>
    /// on its border (SUCCESS or FAILURE for an act), the act, then what it came to — one line each, marked and inked by whether it is a
    /// gain, a loss or neither — and a CONTINUE. Returns the button's region.
    /// </summary>
    public (int X, int Y, int Width) RenderResultBox(bool succeeded, string title, string act, IReadOnlyList<Outcome> chips, bool continueHovered)
    {
        DimContent();
        // As wide as its longest line asks, within the panel.
        int longest = Math.Max(act.Length, chips.Count == 0 ? 0 : chips.Max(c => c.Text.Length) + 2);
        int width = Math.Clamp(longest + 8, Math.Max(40, _layout.CONTENT_WIDTH / 2), _layout.CONTENT_WIDTH - 2);
        int inner = width - 8;
        var actLines = act.Length > 0 ? WrapText(act, inner) : new List<string>();
        // Border, blank, the act and a blank under it, the chips, blank, the button, blank, border.
        int height = (actLines.Count > 0 ? actLines.Count + 1 : 0) + chips.Count + 6;
        int x = (_layout.TERMINAL_WIDTH - width) / 2;
        int y = _layout.CONTENT_START_Y + (_layout.NARRATIVE_HEIGHT - height) / 2;
        var ground = Config.NarrativeUI.BackgroundColor;
        var ink = succeeded ? Config.Colors.GoldYellow : Config.Colors.LightPurple;

        _terminal.FillRect(x, y, width, height, ' ', Config.Colors.White, ground);
        _terminal.DrawBox(x, y, width, height, BoxStyle.Single, succeeded ? Config.Colors.DarkYellow : Config.Colors.DarkPurple, ground);
        _terminal.Text(x + width / 2, y, $" {title} ", ink, ground, TextAlignment.Center);

        int row = y + 2;
        foreach (var line in actLines)
            _terminal.Text(x + width / 2, row++, line, Config.Colors.LightGray85, ground, TextAlignment.Center);
        if (actLines.Count > 0) row++;
        DrawChips(chips, x + 4, row, inner);

        const string button = "[ CONTINUE ]";
        int buttonX = x + (width - button.Length) / 2, buttonY = y + height - 2;
        _terminal.Text(buttonX, buttonY, button,
            continueHovered ? Config.NarrativeUI.ContinueButtonHoverColor : Config.NarrativeUI.ContinueButtonColor,
            continueHovered ? Config.NarrativeUI.ContinueButtonHoverBackgroundColor : Config.NarrativeUI.ContinueButtonBackgroundColor);
        return (buttonX, buttonY, button.Length);
    }

    /// <summary>
    /// The box that stands over the map when the played member's last noetic point is spent: a
    /// title, why nothing else answers, and one button a row — the way out first, then each member
    /// who can still act. Purple, as a loss is. Returns each button's cells, in order.
    /// </summary>
    public List<(int X, int Y, int Width)> RenderSpentBox(string title, string message, IReadOnlyList<string> buttons, int hovered)
    {
        DimContent();
        // As wide as its line asks, so a short notice is one line and not a word left over on a
        // second; a long one wraps within the same bounds.
        int width = Math.Clamp(message.Length + 8, 40, Math.Min(72, _layout.CONTENT_WIDTH - 2));
        int inner = width - 8;
        var lines = WrapText(message, inner);
        // Border, blank, the message, blank, a button a row with a blank between, blank, border.
        int height = lines.Count + buttons.Count * 2 + 4;
        int x = (_layout.TERMINAL_WIDTH - width) / 2;
        int y = _layout.CONTENT_START_Y + (_layout.NARRATIVE_HEIGHT - height) / 2;
        var ground = Config.NarrativeUI.BackgroundColor;

        _terminal.FillRect(x, y, width, height, ' ', Config.Colors.White, ground);
        _terminal.DrawBox(x, y, width, height, BoxStyle.Single, Config.Colors.DarkPurple, ground);
        _terminal.Text(x + width / 2, y, title, Config.Colors.LightPurple, ground, TextAlignment.Center);

        int row = y + 2;
        foreach (var line in lines)
            _terminal.Text(x + 4, row++, line, Config.Colors.LightGray85, ground);
        row++;

        var regions = new List<(int X, int Y, int Width)>();
        for (int i = 0; i < buttons.Count; i++, row += 2)
        {
            string button = $"[ {buttons[i]} ]";
            int buttonX = x + (width - button.Length) / 2;
            bool over = i == hovered;
            _terminal.Text(buttonX, row, button,
                over ? Config.NarrativeUI.ContinueButtonHoverColor : Config.NarrativeUI.ContinueButtonColor,
                over ? Config.NarrativeUI.ContinueButtonHoverBackgroundColor : Config.NarrativeUI.ContinueButtonBackgroundColor);
            regions.Add((buttonX, row, button.Length));
        }
        return regions;
    }

    /// <summary>
    /// What an act came to, one line each: a mark and the text in the ink of its severity — gold
    /// for a gain, purple for a loss, grey for what is neither — on the box's own ground.
    /// </summary>
    private void DrawChips(IReadOnlyList<Outcome>? chips, int x, int y, int width)
    {
        if (chips == null) return;
        for (int r = 0; r < chips.Count; r++)
        {
            var (mark, ink) = chips[r].Severity switch
            {
                OutcomeSeverity.Negative => ('−', Config.Colors.LightPurple),
                OutcomeSeverity.Positive => ('+', Config.Colors.GoldYellow),
                _                        => ('·', Config.Colors.LightGray75),
            };
            _terminal.Text(x, y + r, $"{mark} {TrimTo(chips[r].Text, width - 2)}", ink, Config.NarrativeUI.BackgroundColor);
        }
    }

    private static string TrimTo(string s, int width) => s.Length <= width ? s : s[..Math.Max(0, width)];

    /// <summary>
    /// Render the footer status bar: where the member stands and the hour while nothing is happening,
    /// or what is.
    /// </summary>
    public void RenderStatusBar(string message = "")
    {
        // In the dark there is no status row: where the body is and what hour it is are not yet known.
        if (!Blackout) DrawStatusBar(message);
    }
}
