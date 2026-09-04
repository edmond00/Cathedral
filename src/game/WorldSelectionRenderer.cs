// WorldSelectionRenderer.cs — the screen a new run opens on: the bare sky, and a moon to pick out
// of it. Draws the title, the hint and the confirmation box; the moons themselves are lit in the
// sky by the sky renderer, not here.
using System;
using OpenTK.Mathematics;
using Cathedral.Glyph;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;

namespace Cathedral.Game
{
    /// <summary>
    /// The terminal half of the world-selection screen.
    ///
    /// <para>The screen has two halves and they are deliberately separate: the sky is 3D and belongs
    /// to the sky renderer, which lights the hovered moon and blanks the chosen one; this draws the
    /// flat furniture over it — what the moon is called, what confirming will do, and the two
    /// buttons. Neither half knows about the other; the controller holds the selected ordinal and
    /// tells both.</para>
    /// </summary>
    public sealed class WorldSelectionRenderer
    {
        private readonly TerminalHUD _terminal;

        private int _hoverX = -1, _hoverY = -1;

        // Cached layout from the last Draw, so the hit tests answer for what is on screen.
        private int _boxX, _boxY, _boxW, _boxH;
        private int _confirmX, _confirmY, _confirmW;
        private int _cancelX,  _cancelY,  _cancelW;
        private bool _buttonsPainted;

        /// <summary>True when CONFIRM is drawn live rather than greyed — a moon has been chosen.</summary>
        public bool ConfirmEnabled { get; private set; }

        private const string ConfirmLabel = "[ CONFIRM ]";
        private const string CancelLabel  = "[ CANCEL ]";

        public WorldSelectionRenderer(TerminalHUD terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public void SetHover(int cellX, int cellY)
        {
            _hoverX = cellX;
            _hoverY = cellY;
        }

        public bool IsOverConfirmButton(int cellX, int cellY)
            => _buttonsPainted && ConfirmEnabled && cellY == _confirmY
            && cellX >= _confirmX && cellX < _confirmX + _confirmW;

        public bool IsOverCancelButton(int cellX, int cellY)
            => _buttonsPainted && cellY == _cancelY
            && cellX >= _cancelX && cellX < _cancelX + _cancelW;

        /// <summary>The cell a script should click to press CONFIRM, or (-1,-1) when it is not shown.</summary>
        public (int X, int Y) ConfirmCell() => _buttonsPainted ? (_confirmX + 1, _confirmY) : (-1, -1);

        /// <summary>The cell a script should click to press CANCEL.</summary>
        public (int X, int Y) CancelCell() => _buttonsPainted ? (_cancelX + 1, _cancelY) : (-1, -1);

        /// <summary>
        /// Paints the whole screen.
        ///
        /// <paramref name="selected"/> is the ordinal of the moon the player has clicked, or -1 while
        /// none is chosen; <paramref name="hoveredMoon"/> is the one under the cursor, which is shown
        /// in the box only when nothing has been chosen yet — a hover must not overwrite a decision
        /// that has already been made and is waiting on CONFIRM.
        /// </summary>
        public void Draw(int selected, int hoveredMoon, int moonCount)
        {
            const string ornament = "─ · ─ · ─ · ─ · ─ · ─ · ─ · ─";
            _terminal.CenteredText(Config.WorldSelectionUI.TitleRow - 2, ornament,
                Config.Colors.DarkGray35, Colors.Transparent);
            _terminal.CenteredText(Config.WorldSelectionUI.TitleRow, "CHOOSE  A  WORLD",
                Config.WorldSelectionUI.TitleColor, Colors.Transparent);
            _terminal.CenteredText(Config.WorldSelectionUI.HintRow,
                $"·  {moonCount} moons hang above you — click one, and you will wake beneath it  ·",
                Config.WorldSelectionUI.HintColor, Colors.Transparent);
            _terminal.CenteredText(Config.WorldSelectionUI.HintRow + 2,
                "the compass turns the sky  ·  click the empty dark to unchoose",
                Config.Colors.DarkGray40, Colors.Transparent);

            DrawBox(selected, hoveredMoon);
        }

        /// <summary>Wipes the whole screen back to transparent so the sky shows through again.</summary>
        public void Erase()
        {
            for (int y = 0; y < _terminal.Height; y++)
                for (int x = 0; x < _terminal.Width; x++)
                    _terminal.SetCell(x, y, ' ', Colors.Transparent, Colors.Transparent);
            _buttonsPainted = false;
        }

        private void DrawBox(int selected, int hoveredMoon)
        {
            _boxW = Config.WorldSelectionUI.BoxWidth;
            _boxH = Config.WorldSelectionUI.BoxHeight;
            _boxX = (_terminal.Width - _boxW) / 2;
            _boxY = _terminal.Height - _boxH - Config.WorldSelectionUI.BoxBottomMargin;
            _buttonsPainted = false;

            for (int dy = 0; dy < _boxH; dy++)
                for (int dx = 0; dx < _boxW; dx++)
                    _terminal.SetCell(_boxX + dx, _boxY + dy, ' ',
                        Config.WorldSelectionUI.BorderColor, Config.WorldSelectionUI.BackgroundColor);
            _terminal.DrawBox(_boxX, _boxY, _boxW, _boxH, BoxStyle.Single,
                Config.WorldSelectionUI.BorderColor, Config.WorldSelectionUI.BackgroundColor);

            int innerLeft = _boxX + 2;
            int contentY  = _boxY + 1;

            // The header is drawn in every state, including the empty one — a box that grows a title
            // only once something is under the cursor jumps as the cursor crosses the sky.
            const string title = "── THE MOONS ──";
            _terminal.Text(_boxX + (_boxW - title.Length) / 2, contentY, title,
                Config.WorldSelectionUI.TitleColor, Config.WorldSelectionUI.BackgroundColor);

            // Two separate readings, not one: what has been chosen — which is what CONFIRM will take
            // — and what the cursor happens to be over. They were one line while a hover replaced a
            // choice in the box, and the player then had no way to tell which of the two moons lit in
            // the sky the button was about to act on.
            DrawRow(innerLeft, contentY + 2, "Chosen",
                selected >= 0 ? SkyMoons.Name(selected) : "—",
                selected >= 0 ? Config.WorldSelectionUI.NameColor
                              : Config.WorldSelectionUI.LabelColor);

            // The seed is shown because it is the world's whole identity — two players who write it
            // down walk the same ground — and because it is the one thing about a world that can be
            // known before entering it. The ordinal behind it is not: that is how the moon is
            // addressed in code, and the name says the same thing better.
            DrawRow(innerLeft, contentY + 3, "Seed",
                selected >= 0 ? SkyMoons.WorldSeed(selected).ToString() : "—",
                Config.WorldSelectionUI.ValueColor);

            DrawRow(innerLeft, contentY + 4, "Pointed at",
                hoveredMoon >= 0 ? SkyMoons.Name(hoveredMoon) : "—",
                Config.WorldSelectionUI.HintColor);

            // Row contentY + 5 is left blank on purpose: it is the gap that keeps the last line of
            // text off the buttons.
            DrawButtons(confirmEnabled: selected >= 0);
        }

        private void DrawRow(int x, int y, string label, string value, Vector4 valueColor)
        {
            _terminal.Text(x, y, label,
                Config.WorldSelectionUI.LabelColor, Config.WorldSelectionUI.BackgroundColor);
            int vx = _boxX + _boxW - 2 - value.Length;
            if (vx < x + label.Length + 2) vx = x + label.Length + 2;
            _terminal.Text(vx, y, value, valueColor, Config.WorldSelectionUI.BackgroundColor);
        }

        private void DrawButtons(bool confirmEnabled)
        {
            ConfirmEnabled = confirmEnabled;
            _confirmW = ConfirmLabel.Length;
            _cancelW  = CancelLabel.Length;

            int row  = _boxY + _boxH - 2;
            int half = _boxW / 2;
            _cancelX  = _boxX + 1 + (half - _cancelW) / 2;
            _confirmX = _boxX + half + ((_boxW - half) - _confirmW) / 2;
            _cancelY  = row;
            _confirmY = row;
            _buttonsPainted = true;

            bool cancelHover = IsOverCancelButton(_hoverX, _hoverY);
            _terminal.Text(_cancelX, _cancelY, CancelLabel,
                cancelHover ? Config.WorldSelectionUI.CancelHoverTextColor       : Config.WorldSelectionUI.CancelTextColor,
                cancelHover ? Config.WorldSelectionUI.CancelHoverBackgroundColor : Config.WorldSelectionUI.CancelBackgroundColor);

            if (!confirmEnabled)
            {
                // Greyed rather than absent: the button is what the screen is for, and a screen whose
                // action only appears once you have done the right thing tells you nothing about what
                // the right thing was.
                _terminal.Text(_confirmX, _confirmY, ConfirmLabel,
                    Colors.DarkGray, Config.WorldSelectionUI.BackgroundColor);
                return;
            }

            bool confirmHover = IsOverConfirmButton(_hoverX, _hoverY);
            _terminal.Text(_confirmX, _confirmY, ConfirmLabel,
                confirmHover ? Config.WorldSelectionUI.ConfirmHoverTextColor       : Config.WorldSelectionUI.ConfirmTextColor,
                confirmHover ? Config.WorldSelectionUI.ConfirmHoverBackgroundColor : Config.WorldSelectionUI.ConfirmBackgroundColor);
        }

    }
}
