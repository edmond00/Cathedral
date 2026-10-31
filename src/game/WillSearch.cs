// WillSearch.cs - Seeking one of a will's targets across every place within travel range, from the
// Memory tab's "Wants" rows, and the box that shows the search going.
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
    /// One search, place by place: every vertex the avatar could set out for is checked in turn —
    /// is there a road to it, and would the will's target be met there on arrival
    /// (<see cref="Cathedral.Game.Scene.ActionPathPlanner.Plan"/> from the entry area at the place's
    /// own arrival hour)? The controller does the checking (it alone can build a place's scene); this
    /// holds the queue, the progress and what was found.
    /// </summary>
    public sealed class WillSearch
    {
        public PartyMember Member { get; }
        public string ModusMentisName { get; }
        public WillTarget Target { get; }

        /// <summary>Every vertex to check, nearest by straight line first so the box reads outward.</summary>
        public IReadOnlyList<int> Candidates { get; }

        /// <summary>How many candidates have been checked.</summary>
        public int Checked { get; private set; }

        /// <summary>The vertex last checked (the box's "checking" line); -1 before the first.</summary>
        public int Current { get; private set; } = -1;

        /// <summary>Every place where the target would be met: its vertex and the length of the road to it in cells.</summary>
        public List<(int Vertex, int Cells)> Found { get; } = new();

        public DateTime StartedAt { get; } = DateTime.UtcNow;

        public bool IsDone => Checked >= Candidates.Count;

        public WillSearch(PartyMember member, string modusMentisName, WillTarget target, IReadOnlyList<int> candidates)
        {
            Member          = member;
            ModusMentisName = modusMentisName;
            Target          = target;
            Candidates      = candidates;
        }

        /// <summary>The next vertex to check, or -1 when every one has been.</summary>
        public int Next() => IsDone ? -1 : Candidates[Checked];

        /// <summary>Records the outcome of checking <see cref="Next"/>: the road's length when the target is met there, else null.</summary>
        public void Record(int vertex, int? cells)
        {
            Current = vertex;
            Checked++;
            if (cells is int c) Found.Add((vertex, c));
        }

        /// <summary>The nearest place found (fewest cells of road, then lowest vertex), or null.</summary>
        public (int Vertex, int Cells)? Nearest
            => Found.Count == 0 ? null : Found.OrderBy(f => f.Cells).ThenBy(f => f.Vertex).First();
    }

    /// <summary>
    /// The box drawn over the world map while a <see cref="WillSearch"/> runs: whose will, what it
    /// seeks, a progress bar over the places within reach, the place being checked and a CANCEL.
    /// Same frame, colours and position as <see cref="TravelInfoRenderer"/>'s box, which takes its
    /// place once the search has proposed a journey.
    /// </summary>
    public sealed class WillSearchRenderer
    {
        private const int BoxW = 40;
        private const int BoxH = 11;
        private const string CancelLabel = "[ CANCEL ]";

        private readonly TerminalHUD _terminal;
        private int _boxX, _boxY;
        private int _cancelX, _cancelY;
        private bool _painted;
        private int _hoverX = -1, _hoverY = -1;

        public WillSearchRenderer(TerminalHUD terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public void SetHover(int x, int y) { _hoverX = x; _hoverY = y; }

        public bool IsOverBox(int x, int y)
            => _painted && x >= _boxX && x < _boxX + BoxW && y >= _boxY && y < _boxY + BoxH;

        public bool IsOverCancelButton(int x, int y)
            => _painted && y == _cancelY && x >= _cancelX && x < _cancelX + CancelLabel.Length;

        /// <summary>The CANCEL button's cell, for <c>--cli</c>.</summary>
        public (int X, int Y)? CancelCell => _painted ? (_cancelX, _cancelY) : null;

        public void Draw(WillSearch search, string? currentName)
        {
            _boxX = (_terminal.Width - BoxW) / 2;
            _boxY = _terminal.Height - Config.TravelUI.BoxHeight - Config.TravelUI.BoxBottomMargin;
            var bg = Config.TravelUI.BackgroundColor;

            // Frame
            for (int y = 0; y < BoxH; y++)
                for (int x = 0; x < BoxW; x++)
                {
                    bool top = y == 0, bottom = y == BoxH - 1, left = x == 0, right = x == BoxW - 1;
                    char c = top && left ? '┌' : top && right ? '┐' : bottom && left ? '└' : bottom && right ? '┘'
                           : top || bottom ? '─' : left || right ? '│' : ' ';
                    _terminal.SetCell(_boxX + x, _boxY + y, c, Config.TravelUI.BorderColor, bg);
                }

            int inner = _boxX + 2, innerW = BoxW - 4;
            string title = $"── {search.ModusMentisName.ToUpperInvariant()} SEEKS ──";
            if (title.Length > BoxW - 2) title = title[..(BoxW - 2)];
            _terminal.Text(_boxX + (BoxW - title.Length) / 2, _boxY + 1, title, Config.TravelUI.TitleColor, bg);
            _terminal.Text(inner, _boxY + 3, Truncate(search.Target.Purpose, innerW), Config.TravelUI.ValueAccentColor, bg);

            // Progress bar, with a bright head that sweeps while the search runs, and the count.
            int total = Math.Max(1, search.Candidates.Count);
            string count = $" {search.Checked}/{search.Candidates.Count}";
            int barW = innerW - count.Length;
            int filled = (int)Math.Round(barW * (double)search.Checked / total);
            int head = (int)((DateTime.UtcNow - search.StartedAt).TotalSeconds * 12) % Math.Max(1, barW);
            for (int i = 0; i < barW; i++)
            {
                bool full = i < filled;
                Vector4 fg = full ? Config.Colors.BrightYellow : i == head ? Config.Colors.LightGray75 : Config.Colors.DarkGray35;
                _terminal.SetCell(inner + i, _boxY + 5, full ? '█' : i == head ? '▒' : '░', fg, bg);
            }
            _terminal.Text(inner + barW, _boxY + 5, count, Config.TravelUI.ValueColor, bg);

            string checking = currentName == null ? "setting out in thought…" : $"checking {currentName}";
            _terminal.Text(inner, _boxY + 6, Truncate(checking, innerW), Config.TravelUI.LabelColor, bg);
            string found = search.Found.Count switch
            {
                0 => "nothing found yet",
                1 => "1 place found",
                int n => $"{n} places found",
            };
            _terminal.Text(inner, _boxY + 7, found, search.Found.Count > 0 ? Config.TravelUI.ValueColor : Config.TravelUI.LabelColor, bg);

            _cancelY = _boxY + BoxH - 2;
            _cancelX = _boxX + (BoxW - CancelLabel.Length) / 2;
            bool hover = _hoverY == _cancelY && _hoverX >= _cancelX && _hoverX < _cancelX + CancelLabel.Length;
            _terminal.Text(_cancelX, _cancelY, CancelLabel,
                hover ? Config.TravelUI.ClearButtonHoverTextColor       : Config.TravelUI.ClearButtonTextColor,
                hover ? Config.TravelUI.ClearButtonHoverBackgroundColor : Config.TravelUI.ClearButtonBackgroundColor);
            _painted = true;
        }

        /// <summary>Wipes the box back to transparent, so clicks fall through to the world again.</summary>
        public void Erase()
        {
            if (!_painted) return;
            for (int y = 0; y < BoxH; y++)
                for (int x = 0; x < BoxW; x++)
                    _terminal.SetCell(_boxX + x, _boxY + y, ' ', Colors.Transparent, Colors.Transparent);
            _painted = false;
        }

        private static string Truncate(string s, int max) => s.Length <= max ? s : s[..Math.Max(0, max - 1)] + "…";
    }
}
