using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Management;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Scene;

namespace Cathedral.Game;

/// <summary>
/// Modal world-view overlay over a travel destination: its mental map, as remembered, with an icon on
/// each card for every routine learned there. A routine that can be walked is a yellow icon, clickable
/// to set out and enter it on arrival; one that cannot (its kind's own check,
/// <see cref="Routine.Unavailability"/>) is grey, and hovering it says why. Whatever the pointer is over
/// on the map is named in a label above it (<see cref="HoverLabel"/>, put up by the host in the
/// mouse-following popup). The pad pans the map. A RETURN button dismisses the box back to the travel
/// plan.
/// </summary>
public class TravelRoutinesBox
{
    public enum ResultKind { None, Return, Selected }

    /// <summary>One routine on offer plus why it cannot be walked, if it cannot.</summary>
    public sealed class Entry
    {
        public Routine Routine { get; init; } = null!;
        public string? Reason { get; init; }
        public bool Available => Reason == null;
    }

    private readonly TerminalHUD _terminal;
    private readonly List<Entry> _entries;
    private readonly MentalMap _mentalMap;
    private readonly Scene.Scene? _scene;
    private readonly DebugLocationMap _map = new() { AlwaysPannable = true, Canvas = Config.Colors.Black };

    private (int X, int Y) _hover = (-1, -1);

    private int _boxX, _boxY, _boxW, _boxH;
    private int _returnY, _returnX, _returnW;

    private const int BoxWidth = 92, BoxHeight = 72;

    public TravelRoutinesBox(TerminalHUD terminal, List<Entry> entries, MentalMap mentalMap, Scene.Scene? scene)
    {
        _terminal  = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _entries   = entries ?? throw new ArgumentNullException(nameof(entries));
        _mentalMap = mentalMap ?? throw new ArgumentNullException(nameof(mentalMap));
        _scene     = scene;
    }

    /// <summary>
    /// The routines on offer, for <c>--cli</c>, kind by kind. A script picks one by index or by kind,
    /// because an icon's cell moves with the layout and the pan.
    /// </summary>
    public IReadOnlyList<(string Name, string Category, bool Available, string? Reason)> CliEntries
        => _entries.Select(e => (e.Routine.DisplayName, e.Routine.Category.CliId(), e.Available, e.Reason)).ToList();

    /// <summary>The routine behind entry <paramref name="index"/>, or null when out of range.</summary>
    public Routine? CliRoutineAt(int index)
        => index >= 0 && index < _entries.Count ? _entries[index].Routine : null;

    private static Vector4 BoxBg         => Config.Colors.Black;
    private static Vector4 HoverBg       => Config.Colors.DarkPurple;
    private static Vector4 TitleColor    => Config.Colors.BrightPurple;
    private static Vector4 BodyColor     => Config.Colors.LightPurpleGray;
    private static Vector4 SubtitleColor => Config.Colors.MediumGray60;
    private static Vector4 DisabledColor => Config.Colors.Purple;
    private static Vector4 KindColor     => Config.Colors.MediumYellow;

    private Entry? EntryOf(object? tag) => tag is string id ? _entries.FirstOrDefault(e => e.Routine.Id == id) : null;

    public void Render()
    {
        _boxW = Math.Min(BoxWidth, _terminal.Width);
        _boxH = Math.Min(BoxHeight, _terminal.Height);
        _boxX = Math.Max(0, (_terminal.Width  - _boxW) / 2);
        _boxY = Math.Max(0, (_terminal.Height - _boxH) / 2);
        _returnY = _boxY + _boxH - 2;

        _terminal.FillRect(_boxX, _boxY, _boxW, _boxH, ' ', BodyColor, BoxBg);
        DrawBorder();

        CenteredInBox(_boxY + 1, Truncate($"— ROUTINES · {_mentalMap.LocationName.ToUpperInvariant()} —", _boxW - 4), TitleColor, BoxBg);
        CenteredInBox(_boxY + 2, Truncate("Go straight to somewhere you know here: click a routine on the map.", _boxW - 4), SubtitleColor, BoxBg);

        int mapTop = _boxY + 3, mapH = _boxH - 8;
        var icons = _scene == null ? new List<DebugLocationMap.MapIcon>()
                  : MentalMapsPanelRenderer.Icons(_mentalMap, _scene, r => EntryOf(r.Id)?.Available ?? false)
                        .Where(i => EntryOf(i.Tag) != null).ToList();
        var hovered = EntryOf(_map.IconAt(_hover.X, _hover.Y));
        var focus = _scene == null ? null
                  : _entries.Where(e => e.Available).Select(e => e.Routine.FindArea(_scene)).FirstOrDefault(a => a != null)
                    ?? _scene.AllAreas.FirstOrDefault(a => _mentalMap.VisitedAreas.Contains(a.StableKey));
        if (_scene == null)
            CenteredInBox(mapTop + 4, "(the place cannot be drawn)", DisabledColor, BoxBg);
        else
            _map.Render(_terminal, _scene, here: null, period: hovered?.Routine.Time ?? TimePeriod.Noon,
                        _boxX + 2, mapTop, _boxW - 4, mapH,
                        icons: icons, hoveredIcon: hovered?.Available == true ? hovered.Routine.Id : null,
                        focus: focus, footRows: 0);

        DrawStatus(_boxY + _boxH - 4, hovered);
        DrawReturn();
    }

    /// <summary>
    /// What the pointer is over on the map, for the label above it: a routine that can be walked is
    /// clickable, one that cannot says why; a card, a line or the pad as the map names them.
    /// </summary>
    public DebugLocationMap.HoverLabel? HoverLabel
        => _map.LabelAt(_hover.X, _hover.Y, tag => EntryOf(tag) is not { } e ? null
              : e.Available ? new($"{e.Routine.Category.Label()}: {e.Routine.DisplayName} · {e.Routine.Time.Label().ToLowerInvariant()}", true)
              : new($"{e.Routine.DisplayName}: {e.Reason}", false));

    /// <summary>What the cursor is on: the routine, its place and hour, and why it cannot be walked.</summary>
    private void DrawStatus(int y, Entry? e)
    {
        int x = _boxX + 2, w = _boxW - 4;
        _terminal.FillRect(x, y, w, 1, ' ', BodyColor, BoxBg);
        if (e == null)
        {
            string count = _entries.Count == 0 ? "No routine learned here."
                         : $"{_entries.Count(en => en.Available)} of {_entries.Count} routines can be walked.";
            CenteredInBox(y, count, SubtitleColor, BoxBg);
            return;
        }
        string tag = $"{e.Routine.Category.Label().ToUpperInvariant()} ";
        string label = $"{e.Routine.DisplayName} · {e.Routine.AreaName} · {e.Routine.Time.Label().ToLowerInvariant()}"
                     + (e.Available ? "" : $"  ({e.Reason})");
        _terminal.Text(x, y, tag, e.Available ? KindColor : DisabledColor, BoxBg);
        _terminal.Text(x + tag.Length, y, Truncate(label, w - tag.Length), e.Available ? TitleColor : DisabledColor, BoxBg);
    }

    private void DrawBorder()
    {
        int x0 = _boxX, y0 = _boxY, x1 = _boxX + _boxW - 1, y1 = _boxY + _boxH - 1;
        for (int x = x0; x <= x1; x++)
        {
            _terminal.SetCell(x, y0, '─', TitleColor, BoxBg);
            _terminal.SetCell(x, y1, '─', TitleColor, BoxBg);
        }
        for (int y = y0; y <= y1; y++)
        {
            _terminal.SetCell(x0, y, '│', TitleColor, BoxBg);
            _terminal.SetCell(x1, y, '│', TitleColor, BoxBg);
        }
        _terminal.SetCell(x0, y0, '┌', TitleColor, BoxBg);
        _terminal.SetCell(x1, y0, '┐', TitleColor, BoxBg);
        _terminal.SetCell(x0, y1, '└', TitleColor, BoxBg);
        _terminal.SetCell(x1, y1, '┘', TitleColor, BoxBg);
    }

    private void DrawReturn()
    {
        const string label = "[ RETURN ]";
        _returnW = label.Length;
        _returnX = _boxX + (_boxW - _returnW) / 2;
        bool hovered = OverReturn(_hover.X, _hover.Y);
        _terminal.FillRect(_boxX + 1, _returnY, _boxW - 2, 1, ' ', BodyColor, BoxBg);
        _terminal.Text(_returnX, _returnY, label, TitleColor, hovered ? HoverBg : BoxBg);
    }

    /// <summary>
    /// The screen cell of the middle of the card named <paramref name="areaName"/>, panned into view
    /// first — <c>point card</c> for <c>--cli</c>. Null when no card has that name.
    /// </summary>
    public (int X, int Y)? CliCardCell(string areaName)
    {
        var area = _scene?.AllAreas.FirstOrDefault(a => a.DisplayName.Equals(areaName, StringComparison.OrdinalIgnoreCase));
        if (area == null) return null;
        _map.Reveal(area);
        Render();
        return _map.ScreenCellOf(area);
    }

    /// <summary>Tracks the cursor. True when it came onto something clickable — the caller's cue to tick.</summary>
    public bool OnMouseMove(int x, int y)
    {
        string? before = HoverKey();
        _hover = (x, y);
        _map.SetHover(x, y);
        string? after = HoverKey();
        return after != null && after != before;
    }

    private string? HoverKey()
        => OverReturn(_hover.X, _hover.Y) ? "return"
         : EntryOf(_map.IconAt(_hover.X, _hover.Y)) is { Available: true } e ? $"icon:{e.Routine.Id}"
         : _map.PadArrowHovered ? "pad"
         : null;

    /// <summary>Returns the click outcome; for <see cref="ResultKind.Selected"/> the routine is set.</summary>
    public (ResultKind kind, Routine? routine) OnMouseClick(int x, int y)
    {
        if (OverReturn(x, y)) return (ResultKind.Return, null);
        if (EntryOf(_map.IconAt(x, y)) is { } e)
            return e.Available ? (ResultKind.Selected, e.Routine) : (ResultKind.None, null);
        _map.HandleClick(x, y);
        return (ResultKind.None, null);
    }

    private bool OverReturn(int x, int y) => y == _returnY && x >= _returnX && x < _returnX + _returnW;

    private void CenteredInBox(int y, string text, Vector4 fg, Vector4 bg)
    {
        int x = _boxX + (_boxW - text.Length) / 2;
        _terminal.Text(x, y, text, fg, bg);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : (max <= 1 ? s.Substring(0, Math.Max(0, max)) : s.Substring(0, max - 1) + "…");
}
