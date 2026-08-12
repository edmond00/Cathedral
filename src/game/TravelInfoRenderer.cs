// TravelInfoRenderer.cs - Renders the compact travel info box, the TRAVEL action
// and CLEAR cancellation button shown above the bottom of the screen during
// WorldView when waypoints are set.
using System;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;

namespace Cathedral.Game
{
    /// <summary>
    /// Draws the centered travel-planning UI overlay and exposes button hit-tests.
    /// The renderer caches only the latest hover position; the controller passes a
    /// fresh <see cref="TravelEstimate"/> on each Draw() call.
    /// </summary>
    public sealed class TravelInfoRenderer
    {
        private readonly TerminalHUD _terminal;
        private int _hoverX = -1;
        private int _hoverY = -1;

        // Cached layout from the last Draw call (cell coords) so hit-testing matches what's on screen.
        private int _boxX, _boxY, _boxW, _boxH;
        private int _travelBtnX, _travelBtnY, _travelBtnW;
        private int _clearBtnX, _clearBtnY, _clearBtnW;
        private int _routinesBtnX, _routinesBtnY, _routinesBtnW;
        private bool _buttonsEnabled;
        // Whether the ROUTINES button is clickable (routines exist for this destination).
        private bool _routinesEnabled;
        // Set when someone in the party is carrying more than they can bear. The party cannot
        // set out at all until that is fixed, so both ROUTINES and TRAVEL go dead — CLEAR stays
        // live, since abandoning the plan is always allowed.
        private bool _travelBlocked;
        private string? _blockReason;

        // Track the area we actually painted so Erase() can wipe only those cells.
        private int _paintedX, _paintedY, _paintedW, _paintedH;
        private bool _painted;

        // A short-lived one-line notice shown where the box would be, for the times the world map has
        // something to say when there is no plan to draw — refusing to re-enter the location under
        // your feet, above all, which is otherwise a click that silently does nothing.
        private string?  _notice;
        private DateTime _noticeUntil = DateTime.MinValue;

        /// <summary>Shows <paramref name="text"/> centred above the bottom edge for a few seconds.</summary>
        public void ShowTransientMessage(string text)
        {
            _notice      = text;
            _noticeUntil = DateTime.UtcNow.AddSeconds(Config.TravelUI.NoticeSeconds);
        }

        public TravelInfoRenderer(TerminalHUD terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public void SetHover(int cellX, int cellY)
        {
            _hoverX = cellX;
            _hoverY = cellY;
        }

        public bool IsOverBox(int cellX, int cellY)
            => cellX >= _boxX && cellX < _boxX + _boxW
            && cellY >= _boxY && cellY < _boxY + _boxH;

        public bool IsOverTravelButton(int cellX, int cellY)
            => _buttonsEnabled
               && !_travelBlocked
               && cellY == _travelBtnY
               && cellX >= _travelBtnX
               && cellX < _travelBtnX + _travelBtnW;

        public bool IsOverClearButton(int cellX, int cellY)
            => _buttonsEnabled
               && cellY == _clearBtnY
               && cellX >= _clearBtnX
               && cellX < _clearBtnX + _clearBtnW;

        /// <summary>True when the (enabled) ROUTINES button is under the given cell.</summary>
        public bool IsOverRoutinesButton(int cellX, int cellY)
            => _buttonsEnabled
               && _routinesEnabled
               && !_travelBlocked
               && cellY == _routinesBtnY
               && cellX >= _routinesBtnX
               && cellX < _routinesBtnX + _routinesBtnW;

        /// <summary>
        /// Renders the box if there is a viable plan, otherwise erases anything left
        /// from a previous frame.
        ///
        /// <paramref name="overloadWarning"/>, when non-null, means someone in the party is over
        /// their carrying limit: the message is shown in the box and both ROUTINES and TRAVEL are
        /// disabled until the player lightens the load.
        /// </summary>
        public void Draw(int waypointCount, int maxWaypoints, TravelEstimate? estimate,
            string? destinationName, bool routinesAvailable = false, string? overloadWarning = null)
        {
            _routinesEnabled = routinesAvailable;
            _blockReason     = overloadWarning;
            _travelBlocked   = overloadWarning != null;

            // Nothing to show when no waypoints are set — keep the world view clean, except for a
            // notice still inside its window. Drawn AFTER Erase, which wipes the region it lives in.
            if (waypointCount == 0)
            {
                Erase();
                _buttonsEnabled = false;
                DrawNotice();
                return;
            }

            _boxW = Config.TravelUI.BoxWidth;
            _boxH = Config.TravelUI.BoxHeight;
            _boxX = (_terminal.Width - _boxW) / 2;
            _boxY = _terminal.Height - _boxH - Config.TravelUI.BoxBottomMargin;
            _buttonsEnabled = false;

            // Box background + border
            DrawFilledBox(_boxX, _boxY, _boxW, _boxH,
                Config.TravelUI.BorderColor, Config.TravelUI.BackgroundColor);

            int innerLeft  = _boxX + 2;
            int valueCol   = _boxX + _boxW - 2;
            int contentY   = _boxY + 1;

            // Title
            string title = "── TRAVEL PLAN ──";
            int titleX = _boxX + (_boxW - title.Length) / 2;
            _terminal.Text(titleX, contentY, title,
                Config.TravelUI.TitleColor, Config.TravelUI.BackgroundColor);

            if (estimate == null || !estimate.HasPath)
            {
                _terminal.Text(innerLeft, contentY + 2,
                    "No route — unreachable on foot.",
                    Config.TravelUI.DangerColor, Config.TravelUI.BackgroundColor);
                DrawButtons(estimate: null);
                MarkPainted();
                return;
            }

            // Info rows (starting two rows below the title for a small breathing gap).
            DrawRow(innerLeft, valueCol, contentY + 2, "Destination",
                Truncate(destinationName ?? "—", _boxW - 18),
                Config.TravelUI.ValueAccentColor);
            DrawRow(innerLeft, valueCol, contentY + 3, "Travel time",
                FormatDuration(estimate.TotalDurationDays),
                Config.TravelUI.ValueColor);
            DrawRow(innerLeft, valueCol, contentY + 4, "Vital heat",
                estimate.TotalVitalHeat.ToString("F1"),
                Config.TravelUI.ValueColor);
            DrawRow(innerLeft, valueCol, contentY + 5, "Encounter risk",
                Pct(estimate.TotalEncounterChance),
                ColorForRisk(estimate.TotalEncounterChance));
            DrawRow(innerLeft, valueCol, contentY + 6, "Starvation risk",
                estimate.StarvationRisk ? "yes" : "no",
                estimate.StarvationRisk ? Config.TravelUI.DangerColor : Config.TravelUI.ValueColor);

            // Row contentY + 7 is normally padding above the buttons; an overload warning takes it,
            // directly above the two buttons it has just disabled.
            DrawOverloadWarning(innerLeft, contentY + 7);

            // Empty row (contentY + 9) — visual padding below the buttons.
            DrawButtons(estimate);
            MarkPainted();
        }

        /// <summary>
        /// Draws the "someone is overloaded" line in purple — the colour this UI already uses for
        /// wounds and other things standing between the player and what they wanted to do.
        /// </summary>
        private void DrawOverloadWarning(int x, int y)
        {
            if (_blockReason == null) return;

            _terminal.Text(x, y, Truncate(_blockReason, _boxW - 4),
                Config.Colors.BrightPurple, Config.TravelUI.BackgroundColor);
        }

        private void DrawButtons(TravelEstimate? estimate)
        {
            // Three buttons on one row: ROUTINES | CLEAR | TRAVEL, each centered within a third of the box.
            const string routinesLabel = "[ROUTINES]";
            const string clearLabel    = "[ CLEAR ]";
            const string travelLabel   = "[ TRAVEL ]";

            _routinesBtnW = routinesLabel.Length;
            _clearBtnW    = clearLabel.Length;
            _travelBtnW   = travelLabel.Length;
            int row = _boxY + _boxH - 3; // one empty row above the bottom border

            int third = _boxW / 3;
            _routinesBtnX = _boxX + 1 + (third - _routinesBtnW) / 2;
            _clearBtnX    = _boxX + third + (third - _clearBtnW) / 2;
            _travelBtnX   = _boxX + 2 * third + ((_boxW - 2 * third) - _travelBtnW) / 2;

            _routinesBtnY = row;
            _clearBtnY    = row;
            _travelBtnY   = row;
            _buttonsEnabled = true;

            // ROUTINES button — greyed out when no routine exists for this destination, or when
            // the party is too loaded to go anywhere (a routine is a journey like any other).
            if (_routinesEnabled && !_travelBlocked)
            {
                bool routinesHover = IsOverRoutinesButton(_hoverX, _hoverY);
                _terminal.Text(_routinesBtnX, _routinesBtnY, routinesLabel,
                    routinesHover ? Config.TravelUI.TravelButtonHoverTextColor       : Config.TravelUI.TravelButtonTextColor,
                    routinesHover ? Config.TravelUI.TravelButtonHoverBackgroundColor : Config.TravelUI.TravelButtonBackgroundColor);
            }
            else
            {
                _terminal.Text(_routinesBtnX, _routinesBtnY, routinesLabel,
                    Colors.DarkGray, Config.TravelUI.BackgroundColor);
            }

            // CLEAR button (always enabled when there are waypoints).
            bool clearHover = IsOverClearButton(_hoverX, _hoverY);
            _terminal.Text(_clearBtnX, _clearBtnY, clearLabel,
                clearHover ? Config.TravelUI.ClearButtonHoverTextColor       : Config.TravelUI.ClearButtonTextColor,
                clearHover ? Config.TravelUI.ClearButtonHoverBackgroundColor : Config.TravelUI.ClearButtonBackgroundColor);

            // TRAVEL button — needs a viable plan and a party light enough to walk it.
            bool travelEnabled = estimate != null && estimate.HasPath && !_travelBlocked;
            if (!travelEnabled)
            {
                _terminal.Text(_travelBtnX, _travelBtnY, travelLabel,
                    Colors.DarkGray, Config.TravelUI.BackgroundColor);
                return;
            }

            bool travelHover = IsOverTravelButton(_hoverX, _hoverY);
            _terminal.Text(_travelBtnX, _travelBtnY, travelLabel,
                travelHover ? Config.TravelUI.TravelButtonHoverTextColor       : Config.TravelUI.TravelButtonTextColor,
                travelHover ? Config.TravelUI.TravelButtonHoverBackgroundColor : Config.TravelUI.TravelButtonBackgroundColor);
        }

        /// <summary>Erases whatever area was last painted so transparent passthrough is restored.</summary>
        public void Erase()
        {
            if (!_painted) return;
            for (int dy = 0; dy < _paintedH; dy++)
                for (int dx = 0; dx < _paintedW; dx++)
                    _terminal.SetCell(_paintedX + dx, _paintedY + dy, ' ',
                        Colors.Transparent, Colors.Transparent);
            _painted = false;
            _buttonsEnabled = false;
        }

        /// <summary>
        /// Paints the transient notice on one centred line where the travel box would sit, and marks
        /// that line as the painted region so the next Erase clears it. Expired or empty: nothing.
        /// </summary>
        private void DrawNotice()
        {
            if (string.IsNullOrEmpty(_notice) || DateTime.UtcNow >= _noticeUntil)
            {
                _notice = null;
                return;
            }

            string text = _notice;
            _boxW = Math.Min(text.Length + 4, Math.Max(4, _terminal.Width));
            _boxH = 1;
            _boxX = (_terminal.Width - _boxW) / 2;
            _boxY = _terminal.Height - Config.TravelUI.BoxHeight - Config.TravelUI.BoxBottomMargin;

            _terminal.FillRect(_boxX, _boxY, _boxW, _boxH, ' ',
                Config.TravelUI.BorderColor, Config.TravelUI.BackgroundColor);
            _terminal.Text(_boxX + 2, _boxY, text,
                Config.TravelUI.DangerColor, Config.TravelUI.BackgroundColor);
            MarkPainted();
        }

        private void MarkPainted()
        {
            _paintedX = _boxX;
            _paintedY = _boxY;
            _paintedW = _boxW;
            _paintedH = _boxH;
            _painted = true;
        }

        private void DrawRow(int leftX, int valueRightX, int y, string label, string value, Vector4 valueColor)
        {
            _terminal.Text(leftX, y, label, Config.TravelUI.LabelColor, Config.TravelUI.BackgroundColor);
            int vx = valueRightX - value.Length + 1;
            if (vx < leftX + label.Length + 2) vx = leftX + label.Length + 2;
            _terminal.Text(vx, y, value, valueColor, Config.TravelUI.BackgroundColor);
        }

        private static Vector4 ColorForRisk(float p)
        {
            if (p >= 0.5f) return Config.TravelUI.DangerColor;
            if (p >= 0.2f) return Config.TravelUI.WarningColor;
            return Config.TravelUI.ValueColor;
        }

        private void DrawFilledBox(int x, int y, int w, int h, Vector4 border, Vector4 background)
        {
            for (int dy = 0; dy < h; dy++)
                for (int dx = 0; dx < w; dx++)
                    _terminal.SetCell(x + dx, y + dy, ' ', border, background);
            _terminal.DrawBox(x, y, w, h, BoxStyle.Single, border, background);
        }

        /// <summary>
        /// Formats a travel duration in days — the only time unit the world keeps.
        /// A trip always costs at least one day.
        /// </summary>
        private static string FormatDuration(float days)
        {
            int whole = Math.Max(1, (int)Math.Round(days));
            return whole == 1 ? "1 day" : $"{whole} days";
        }

        private static string Pct(float p) => $"{Math.Round(p * 100f)}%";

        private static string Truncate(string s, int maxLen)
        {
            if (maxLen <= 0) return string.Empty;
            return s.Length <= maxLen ? s : s.Substring(0, Math.Max(0, maxLen - 1)) + "…";
        }
    }
}
