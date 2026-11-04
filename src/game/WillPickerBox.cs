// WillPickerBox.cs - The world map's shortcut to a will's search: every will the party holds, and
// what each wants, in one box opened from the corner button beside the ☰.
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Game.Narrative;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;

namespace Cathedral.Game
{
    /// <summary>
    /// The box the world map's will button opens: the Memory tab's "Wants" rows without opening the
    /// protagonist screen. Two panes, because a party can hold dozens of wills and each wants up to
    /// <see cref="WillTargets.Max"/> things, so one list of every target would outrun the box:
    /// <list type="bullet">
    /// <item><b>left</b>, the wills — the protagonist's first, then each companion's, by name — one row
    /// each, scrolled by the wheel or the ▲ ▼ rows when there are more than fit;</item>
    /// <item><b>right</b>, the selected will's targets, each a button that starts the search, as a
    /// "Wants" row does on the Memory tab.</item>
    /// </list>
    /// The box only lists and answers clicks; the controller starts the search and closes it.
    /// </summary>
    public sealed class WillPickerBox
    {
        /// <summary>One will in the list: whose it is, its id and name, and its targets' purposes, read once.</summary>
        public sealed record Entry(PartyMember Member, string ModusMentisId, string Label, IReadOnlyList<string> Wants);

        /// <summary>What a click did.</summary>
        public enum ClickKind { None, Selected, Scrolled, Seek, Close }
        public readonly record struct Click(ClickKind Kind, Entry? Entry = null, int Want = -1);

        private const int BoxW = 76, BoxH = 26;
        private const int ListW = 28;               // the left pane's inner width
        private const string CloseLabel = "[ CLOSE ]";

        private readonly TerminalHUD _terminal;
        private readonly List<Entry> _entries;
        private int _selected;
        private int _scroll;
        private int _hoverX = -1, _hoverY = -1;

        // Layout of the last Draw, for hit-testing what is on screen.
        private int _boxX, _boxY;
        private readonly List<(int Index, int Y)> _listRows = new();
        private readonly List<(int Want, int X, int Y, int W)> _wantRows = new();
        private int _upY = -1, _downY = -1, _closeX, _closeY;
        private bool _painted;

        public WillPickerBox(TerminalHUD terminal, IEnumerable<Entry> entries)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _entries = entries.ToList();
        }

        public IReadOnlyList<Entry> Entries => _entries;
        public Entry? Selected => _entries.Count == 0 ? null : _entries[_selected];
        public int Scroll => _scroll;

        /// <summary>How many will rows the left pane shows at once.</summary>
        private int VisibleRows => BoxH - 8;   // title, gap, ▲, rows…, ▼, gap, CLOSE, border

        private int MaxScroll => Math.Max(0, _entries.Count - VisibleRows);

        public void SetHover(int x, int y) { _hoverX = x; _hoverY = y; }

        public bool IsOverBox(int x, int y)
            => _painted && x >= _boxX && x < _boxX + BoxW && y >= _boxY && y < _boxY + BoxH;

        /// <summary>The wheel: a notch scrolls the list one row (up reads back), whichever pane the pointer is over.</summary>
        public bool OnWheel(float delta)
        {
            int before = _scroll;
            _scroll = Math.Clamp(_scroll + (delta > 0 ? -1 : 1), 0, MaxScroll);
            return _scroll != before;
        }

        /// <summary>Selects a will by id (the first that matches), scrolling it into view; false when none is listed.</summary>
        public bool Select(string modusMentisId)
        {
            int i = _entries.FindIndex(e => e.ModusMentisId.Equals(modusMentisId, StringComparison.OrdinalIgnoreCase));
            if (i < 0) return false;
            _selected = i;
            if (_selected < _scroll) _scroll = _selected;
            if (_selected >= _scroll + VisibleRows) _scroll = _selected - VisibleRows + 1;
            return true;
        }

        public Click OnClick(int x, int y)
        {
            if (!_painted) return default;
            if (y == _closeY && x >= _closeX && x < _closeX + CloseLabel.Length) return new(ClickKind.Close);
            if (y == _upY && x > _boxX && x <= _boxX + ListW + 1 && _scroll > 0)
            { _scroll--; return new(ClickKind.Scrolled); }
            if (y == _downY && x > _boxX && x <= _boxX + ListW + 1 && _scroll < MaxScroll)
            { _scroll++; return new(ClickKind.Scrolled); }
            foreach (var (index, ry) in _listRows)
                if (y == ry && x > _boxX && x <= _boxX + ListW + 1)
                {
                    _selected = index;
                    return new(ClickKind.Selected, _entries[index]);
                }
            foreach (var (want, wx, wy, ww) in _wantRows)
                if (y == wy && x >= wx && x < wx + ww)
                    return new(ClickKind.Seek, Selected, want);
            return default;
        }

        // ── --cli / --record handles ────────────────────────────────────────────────────────────

        /// <summary>The row of a will on screen; null when it is scrolled out of view or not listed.</summary>
        public (int X, int Y)? WillCell(string modusMentisId)
        {
            foreach (var (index, ry) in _listRows)
                if (_entries[index].ModusMentisId.Equals(modusMentisId, StringComparison.OrdinalIgnoreCase))
                    return (_boxX + 2, ry);
            return null;
        }

        /// <summary>The selected will's n-th target, as drawn.</summary>
        public (int X, int Y)? WantCell(int want)
        {
            foreach (var (w, wx, wy, _) in _wantRows)
                if (w == want) return (wx, wy);
            return null;
        }

        public (int X, int Y)? CloseCell => _painted ? (_closeX + 2, _closeY) : null;
        public (int X, int Y)? DownCell => _painted && _scroll < MaxScroll ? (_boxX + 2, _downY) : null;
        public (int X, int Y)? UpCell => _painted && _scroll > 0 ? (_boxX + 2, _upY) : null;

        /// <summary>The wills listed in the left pane right now, by id.</summary>
        public IEnumerable<string> VisibleWills => _listRows.Select(r => _entries[r.Index].ModusMentisId);

        // ── Drawing ─────────────────────────────────────────────────────────────────────────────

        public void Draw()
        {
            _boxX = (_terminal.Width - BoxW) / 2;
            _boxY = (_terminal.Height - BoxH) / 2 - 6;
            var bg = Config.TravelUI.BackgroundColor;
            var border = Config.TravelUI.BorderColor;
            _listRows.Clear();
            _wantRows.Clear();

            // Frame, with a divider between the panes.
            int divX = _boxX + ListW + 2;
            for (int y = 0; y < BoxH; y++)
                for (int x = 0; x < BoxW; x++)
                {
                    bool top = y == 0, bottom = y == BoxH - 1, left = x == 0, right = x == BoxW - 1;
                    bool div = _boxX + x == divX && y >= 2 && y <= BoxH - 4;
                    char c = top && left ? '┌' : top && right ? '┐' : bottom && left ? '└' : bottom && right ? '┘'
                           : top || bottom ? '─' : left || right ? '│' : div ? '│' : ' ';
                    _terminal.SetCell(_boxX + x, _boxY + y, c, border, bg);
                }

            const string title = "── WHAT DO YOU SEEK? ──";
            _terminal.Text(_boxX + (BoxW - title.Length) / 2, _boxY + 1, title, Config.TravelUI.TitleColor, bg);

            int listX = _boxX + 2;
            int firstRow = _boxY + 4;
            _upY = _boxY + 3;
            _downY = firstRow + VisibleRows;

            if (_entries.Count == 0)
            {
                _terminal.Text(listX, firstRow, "No will is held.", Config.TravelUI.LabelColor, bg);
            }
            else
            {
                _scroll = Math.Clamp(_scroll, 0, MaxScroll);
                DrawArrow(_upY, "▲", _scroll > 0, $"{_scroll} more");
                for (int r = 0; r < VisibleRows && _scroll + r < _entries.Count; r++)
                {
                    int i = _scroll + r, y = firstRow + r;
                    bool sel = i == _selected;
                    bool hov = _hoverY == y && _hoverX > _boxX && _hoverX < divX;
                    Vector4 fg = sel ? Config.Colors.Black : hov ? Config.Colors.White : Config.TravelUI.ValueColor;
                    Vector4 rowBg = sel ? Config.Colors.BrightYellow : hov ? Config.TravelUI.ClearButtonBackgroundColor : bg;
                    string text = Fit(_entries[i].Label, ListW - 1);
                    _terminal.Text(listX - 1, y, " " + text.PadRight(ListW - 1), fg, rowBg);
                    _listRows.Add((i, y));
                }
                int below = _entries.Count - _scroll - VisibleRows;
                DrawArrow(_downY, "▼", below > 0, $"{below} more");

                // Right pane: the selected will and its targets.
                var sel2 = Selected!;
                int rx = divX + 2, rw = _boxX + BoxW - 2 - rx;
                _terminal.Text(rx, _boxY + 3, Fit(sel2.Label, rw), Config.TravelUI.TitleColor, bg);
                _terminal.Text(rx, _boxY + 4, "wants", Config.TravelUI.LabelColor, bg);
                for (int t = 0; t < sel2.Wants.Count; t++)
                {
                    int y = _boxY + 6 + t * 2;
                    string text = Fit($"▸ {sel2.Wants[t]}", rw);
                    bool hov = _hoverY == y && _hoverX >= rx && _hoverX < rx + text.Length;
                    _terminal.Text(rx, y, text,
                        hov ? Config.Colors.Black : Config.TravelUI.ValueAccentColor,
                        hov ? Config.Colors.BrightYellow : bg);
                    _wantRows.Add((t, rx, y, text.Length));
                }
                _terminal.Text(rx, _boxY + BoxH - 5, Fit("Press a want to seek it in every place", rw), Config.TravelUI.HintColor, bg);
                _terminal.Text(rx, _boxY + BoxH - 4, Fit("within reach, nearest first.", rw), Config.TravelUI.HintColor, bg);
            }

            _closeY = _boxY + BoxH - 2;
            _closeX = _boxX + (BoxW - CloseLabel.Length) / 2;
            bool closeHov = _hoverY == _closeY && _hoverX >= _closeX && _hoverX < _closeX + CloseLabel.Length;
            _terminal.Text(_closeX, _closeY, CloseLabel,
                closeHov ? Config.TravelUI.ClearButtonHoverTextColor       : Config.TravelUI.ClearButtonTextColor,
                closeHov ? Config.TravelUI.ClearButtonHoverBackgroundColor : Config.TravelUI.ClearButtonBackgroundColor);
            _painted = true;
        }

        private void DrawArrow(int y, string arrow, bool live, string more)
        {
            var bg = Config.TravelUI.BackgroundColor;
            string text = live ? $"{arrow} {more}" : "";
            bool hov = live && _hoverY == y && _hoverX > _boxX && _hoverX <= _boxX + ListW + 1;
            _terminal.Text(_boxX + 1, y, (" " + text).PadRight(ListW + 1),
                hov ? Config.Colors.White : Config.TravelUI.LabelColor, hov ? Config.TravelUI.ClearButtonBackgroundColor : bg);
        }

        /// <summary>Wipes the box back to transparent, so the world shows and takes clicks again.</summary>
        public void Erase()
        {
            if (!_painted) return;
            for (int y = 0; y < BoxH; y++)
                for (int x = 0; x < BoxW; x++)
                    _terminal.SetCell(_boxX + x, _boxY + y, ' ', Colors.Transparent, Colors.Transparent);
            _painted = false;
        }

        private static string Fit(string s, int max) => s.Length <= max ? s : s[..Math.Max(0, max - 1)] + "…";
    }
}
