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
        private bool _buttonsEnabled;

        // Track the area we actually painted so Erase() can wipe only those cells.
        private int _paintedX, _paintedY, _paintedW, _paintedH;
        private bool _painted;

        public TravelInfoRenderer(TerminalHUD terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public void SetHover(int cellX, int cellY)
        {
            _hoverX = cellX;
            _hoverY = cellY;
        }

        public bool IsOverTravelButton(int cellX, int cellY)
            => _buttonsEnabled
               && cellY == _travelBtnY
               && cellX >= _travelBtnX
               && cellX < _travelBtnX + _travelBtnW;

        public bool IsOverClearButton(int cellX, int cellY)
            => _buttonsEnabled
               && cellY == _clearBtnY
               && cellX >= _clearBtnX
               && cellX < _clearBtnX + _clearBtnW;

        /// <summary>
        /// Renders the box if there is a viable plan, otherwise erases anything left
        /// from a previous frame.
        /// </summary>
        public void Draw(int waypointCount, int maxWaypoints, TravelEstimate? estimate,
            string? destinationName)
        {
            // Nothing to show when no waypoints are set — keep the world view clean.
            if (waypointCount == 0)
            {
                Erase();
                _buttonsEnabled = false;
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
                FormatDuration(estimate.TotalDurationHours),
                Config.TravelUI.ValueColor);
            DrawRow(innerLeft, valueCol, contentY + 4, "Vital heat",
                estimate.TotalVitalHeat.ToString("F1"),
                Config.TravelUI.ValueColor);
            DrawRow(innerLeft, valueCol, contentY + 5, "Encounter risk",
                Pct(estimate.TotalEncounterChance),
                ColorForRisk(estimate.TotalEncounterChance));
            DrawRow(innerLeft, valueCol, contentY + 6, "Starvation risk",
                Pct(estimate.StarvationRisk),
                ColorForRisk(estimate.StarvationRisk));

            // Empty row (contentY + 7) — visual padding above the buttons.
            // Empty row (contentY + 9) — visual padding below the buttons.
            DrawButtons(estimate);
            MarkPainted();
        }

        private void DrawButtons(TravelEstimate? estimate)
        {
            // Place buttons on the same row, CLEAR on the left, TRAVEL on the right,
            // with an empty padding row both above and below for readability.
            const string clearLabel  = "[ CLEAR ]";
            const string travelLabel = "[ TRAVEL ]";

            _clearBtnW  = clearLabel.Length;
            _travelBtnW = travelLabel.Length;
            int row = _boxY + _boxH - 3; // one empty row above the bottom border

            // Split the box width into two halves and center one button in each half.
            int halfWidth = _boxW / 2;
            _clearBtnX  = _boxX + (halfWidth - _clearBtnW) / 2;
            _travelBtnX = _boxX + halfWidth + ((_boxW - halfWidth) - _travelBtnW) / 2;

            _clearBtnY  = row;
            _travelBtnY = row;
            _buttonsEnabled = true;

            // CLEAR button (always enabled when there are waypoints).
            bool clearHover = IsOverClearButton(_hoverX, _hoverY);
            _terminal.Text(_clearBtnX, _clearBtnY, clearLabel,
                clearHover ? Config.TravelUI.ClearButtonHoverTextColor       : Config.TravelUI.ClearButtonTextColor,
                clearHover ? Config.TravelUI.ClearButtonHoverBackgroundColor : Config.TravelUI.ClearButtonBackgroundColor);

            // TRAVEL button — only meaningful when there's a viable plan.
            bool travelEnabled = estimate != null && estimate.HasPath;
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
        /// Formats a duration as "X mo Y d", "X d Y h", or "X h" — auto-promotes to the
        /// largest meaningful unit. One month is treated as 30 in-game days.
        /// </summary>
        private static string FormatDuration(float hours)
        {
            const float hoursPerDay = 24f;
            const float daysPerMonth = 30f;
            const float hoursPerMonth = hoursPerDay * daysPerMonth;

            if (hours < hoursPerDay)
                return $"{hours:F0} h";

            if (hours < hoursPerMonth)
            {
                int days = (int)(hours / hoursPerDay);
                int remHours = (int)Math.Round(hours - days * hoursPerDay);
                return remHours == 0 ? $"{days} d" : $"{days} d {remHours} h";
            }

            int months = (int)(hours / hoursPerMonth);
            int remDays = (int)Math.Round((hours - months * hoursPerMonth) / hoursPerDay);
            if (remDays >= (int)daysPerMonth) { months++; remDays = 0; }
            return remDays == 0 ? $"{months} mo" : $"{months} mo {remDays} d";
        }

        private static string Pct(float p) => $"{Math.Round(p * 100f)}%";

        private static string Truncate(string s, int maxLen)
        {
            if (maxLen <= 0) return string.Empty;
            return s.Length <= maxLen ? s : s.Substring(0, Math.Max(0, maxLen - 1)) + "…";
        }
    }
}
