using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Narrative.Work;
using Cathedral.Game.Npc.Trade;
using Cathedral.Game.Creation;
using Cathedral.Game.Dialogue.Affinity;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Management;

/// <summary>
/// The protagonist's mental maps in the management menu (protagonist-only, like the journal): the
/// locations remembered, each one's map, and the routines learned there.
///
/// <para>Layout, one zone per question:</para>
/// <list type="bullet">
/// <item><b>Top</b>, from the left nav to the right rule — that location's map
///   (<see cref="DebugLocationMap"/>), as much of it as is remembered, with a yellow icon on its card
///   for every routine learned there. The pad pans it; clicking an icon shows that routine below. The
///   pointer over anything on it puts a label above the pointer (<see cref="DebugLocationMap.LabelAt"/>),
///   which is all the map's key there is.</item>
/// <item><b>Right column</b>, under the map (col 70+) — the locations remembered, newest first, so the
///   lowest one not locked is the next forgotten. Every slot the anamnesis could ever hold is drawn: a
///   map (its ☐/☑ is the lock, clickable), an empty slot this body can fill (○), and a slot beyond the
///   anamnesis as it stands (×). Clicking a name shows that map.</item>
/// <item><b>Middle</b> — a transparent porthole onto the world; the host aims the camera at the
///   selected location (<see cref="OnMapFocused"/>).</item>
/// <item><b>Bottom</b> — <c>&lt; n/N &gt;</c>, whose arrows step through the place's routines and
///   centre the map on each (the first is selected by default, and its icon blinks); then the
///   selected routine in full, with what its kind has to say: a merchant's catalogue, a job's pay,
///   a person's standing with you, a source's implement.</item>
/// </list>
/// </summary>
public class MentalMapsPanelRenderer
{
    private readonly TerminalHUD _terminal;
    private readonly PopupTerminalHUD? _popup;
    private readonly DebugLocationMap _map = new() { AlwaysPannable = true };

    // Top: the map, across the centre and the right column both.
    private const int MapLeft = 17;
    private const int MapRight = 98;    // to the frame's right rule
    private const int MapWidth = MapRight - MapLeft + 1;
    private const int MapTop = 1;       // the location's name above it

    // Right column geometry, under the map.
    private static int ListX => BodyArtViewer.PanelContentX;   // col 70
    private const int ListWidth = 28;   // to the frame's right rule
    private const int ListRowStep = 2;
    private const int CheckboxCols = 2;

    // Centre geometry, under the map (between the left nav separator at col 15 and the right column).
    private const int CenterLeft = 17;
    private const int CenterRight = 66;
    private const int CenterWidth = CenterRight - CenterLeft + 1;

    private const int SeparatorX = BodyArtViewer.PanelX - 1;   // col 67
    private const int PortholeHalf = 12;

    // The map last drawn, and the pointer's label as last put up: a move that changes only the
    // label (card to card, card to line) still repaints.
    private MentalMap? _shownMap;
    private string? _labelText;

    private static readonly Vector4 HoverBg    = new(0.18f, 0.15f, 0.02f, 1.0f);
    private static readonly Vector4 SelectedBg = new(0.07f, 0.06f, 0.01f, 1.0f);

    // ── Selection ─────────────────────────────────────────────────
    private int? _selectedLocation;
    private readonly Dictionary<int, string> _selectedRoutine = new();
    private int _lastFocusedLocationId = int.MinValue;

    // The selected routine's icon blinks, so it can be found on a large map.
    private readonly Stopwatch _blinkClock = Stopwatch.StartNew();
    private double _lastBlink;
    private bool _blinkOn = true;
    private const double BlinkInterval = 0.4;

    // The routine stepper's two arrows, as last drawn (x of each 3-cell button, and its row).
    private int _stepRow = -1, _prevX = -1, _nextX = -1;
    private const int StepButtonW = 3;

    // ── Hit areas, rebuilt every render ───────────────────────────
    private readonly List<(int Row, MentalMap Map)> _mapRows = new();

    private (int X, int Y) _hover = (-1, -1);

    /// <summary>
    /// Fired with the selected map's LocationId when the selection changes (and on activation). The
    /// host centres the world camera so the porthole shows that location.
    /// </summary>
    public Action<int>? OnMapFocused;

    /// <summary>
    /// A scene of the location, handed its mental map, to draw — built by the host, which owns the
    /// scene factories, and cached there. Null when it cannot be built.
    /// </summary>
    public Func<int, Scene.Scene?>? SceneFor;

    /// <summary>
    /// How the person a routine is about stands with the protagonist today — the level and whether
    /// they count you an enemy — read from the location's stored memory, or null when that location
    /// was never visited. Set by the host, which owns the location states.
    /// </summary>
    public Func<int, string, (AffinityLevel Level, bool Enemy)?>? RelationLookup;

    public MentalMapsPanelRenderer(TerminalHUD terminal, PopupTerminalHUD? popup = null)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _popup = popup;
    }

    /// <summary>True while something clickable is hovered (for SFX feedback).</summary>
    public bool IsHovering => HoverKey() != null;

    public void ClearHover()
    {
        _hover = (-1, -1);
        HideLabel();
    }

    /// <summary>Takes the pointer's label down, when this panel put one up (the popup is shared).</summary>
    public void HideLabel()
    {
        if (_labelText == null) return;
        _labelText = null;
        _popup?.Clear();
    }

    /// <summary>Called when the tab becomes active: refocuses the porthole on the selection.</summary>
    public void OnActivated(Protagonist protagonist) => FireFocus(protagonist, force: true);

    private MentalMap? SelectedMap(Protagonist p)
    {
        if (p.MentalMaps.Count == 0) return null;
        return p.MentalMaps.FirstOrDefault(m => m.LocationId == _selectedLocation) ?? p.MentalMaps[^1];   // newest
    }

    /// <summary>The selected routine: the one last picked on this map, or its first (<see cref="Ordered"/>).</summary>
    private Routine? SelectedRoutine(MentalMap? map)
    {
        if (map == null) return null;
        var ordered = Ordered(map);
        return _selectedRoutine.TryGetValue(map.LocationId, out var id) && ordered.FirstOrDefault(r => r.Id == id) is { } picked
            ? picked : ordered.FirstOrDefault();
    }

    /// <summary>A map's routines kind by kind, then by hour: the order of the icons and of the stepper.</summary>
    private static List<Routine> Ordered(MentalMap map)
        => map.Routines.OrderBy(r => Array.IndexOf(RoutineCategories.All, r.Category)).ThenBy(r => r.Time).ToList();

    /// <summary>
    /// Selects the next (<paramref name="step"/> 1) or previous (-1) routine of the map shown, round
    /// the end, and centres the map on it. False when the map has fewer than two routines.
    /// </summary>
    public bool StepRoutine(int step, Protagonist protagonist)
    {
        if (SelectedMap(protagonist) is not { } map) return false;
        var ordered = Ordered(map);
        if (ordered.Count < 2) return false;
        int at = ordered.IndexOf(SelectedRoutine(map)!);
        _selectedRoutine[map.LocationId] = ordered[((at + step) % ordered.Count + ordered.Count) % ordered.Count].Id;
        _map.Recentre();
        _blinkOn = true;
        _lastBlink = _blinkClock.Elapsed.TotalSeconds;
        return true;
    }

    /// <summary>An arrow key pans the map, as the pad does; false for any other key.</summary>
    public bool PanMap(OpenTK.Windowing.GraphicsLibraryFramework.Keys key) => _shownMap != null && _map.HandleKey(key);

    /// <summary>Flips the selected icon's blink when it is due; true when the panel needs redrawing.</summary>
    public bool UpdateBlink()
    {
        if (_shownMap is not { Routines.Count: > 0 }) return false;
        double now = _blinkClock.Elapsed.TotalSeconds;
        if (now - _lastBlink < BlinkInterval) return false;
        _lastBlink = now;
        _blinkOn = !_blinkOn;
        return true;
    }

    /// <summary>
    /// Shows the first routine of a kind, on the newest map that holds one — <c>manage Chorography buy</c>
    /// for <c>--cli</c>, which cannot aim at an icon whose cell moves with the layout.
    /// </summary>
    public bool SelectRoutineOfKind(RoutineCategory category, Protagonist protagonist)
    {
        for (int i = protagonist.MentalMaps.Count - 1; i >= 0; i--)
        {
            var map = protagonist.MentalMaps[i];
            if (map.Routines.FirstOrDefault(r => r.Category == category) is not { } routine) continue;
            Select(map, protagonist);
            _selectedRoutine[map.LocationId] = routine.Id;
            _map.Recentre();
            return true;
        }
        return false;
    }

    /// <summary>
    /// The screen cell of the middle of the card named <paramref name="areaName"/> on the map shown,
    /// panned into view first — <c>point card</c> for <c>--cli</c>. Null when no card has that name.
    /// </summary>
    public (int X, int Y)? CliCardCell(string areaName, Protagonist protagonist)
    {
        var map = SelectedMap(protagonist);
        var area = map == null ? null : SceneFor?.Invoke(map.LocationId)?.AllAreas
            .FirstOrDefault(a => a.DisplayName.Equals(areaName, StringComparison.OrdinalIgnoreCase));
        if (area == null) return null;
        _map.Reveal(area);
        Render(protagonist);
        return _map.ScreenCellOf(area);
    }

    private void Select(MentalMap map, Protagonist protagonist)
    {
        _selectedLocation = map.LocationId;
        FireFocus(protagonist, force: false);
    }

    // ═══════════════════════════════════════════════════════════════
    // Render
    // ═══════════════════════════════════════════════════════════════

    public void Render(Protagonist protagonist)
    {
        _mapRows.Clear();

        int cy = _terminal.Height / 2;
        int bandTop = cy - PortholeHalf, bandBottom = cy + PortholeHalf;

        for (int y = bandTop; y < _terminal.Height; y++)
            _terminal.SetCell(SeparatorX, y, '│', Config.Colors.DarkGray35, Config.Colors.Black);

        RenderList(protagonist, bandTop);

        var map = SelectedMap(protagonist);
        _shownMap = map;
        RenderMap(map, bandTop - 2);
        DottedHLine(bandTop - 1, MapLeft, MapRight);
        RenderPorthole(bandTop, bandBottom);
        DottedHLine(bandBottom + 1, CenterLeft, CenterRight);
        RenderStepper(map, bandBottom + 2);
        RenderDetail(protagonist, map, bandBottom + 4);
        ShowLabel();
    }

    /// <summary>What the pointer is over on the map, as its label says it; null over nothing.</summary>
    private DebugLocationMap.HoverLabel? Label()
        => _shownMap is { } map ? _map.LabelAt(_hover.X, _hover.Y, tag => IconLabel(map, tag)) : null;

    private static DebugLocationMap.HoverLabel? IconLabel(MentalMap map, object tag)
        => map.Routines.FirstOrDefault(r => Equals(r.Id, tag)) is { } r
            ? new($"{r.Category.Label()}: {r.DisplayName} · {r.Time.Label().ToLowerInvariant()}", true)
            : null;

    /// <summary>Puts the label up above the pointer, or takes it down; only when it changed.</summary>
    private void ShowLabel()
    {
        if (_popup == null) return;
        var label = Label();
        if (label?.Text == _labelText) return;
        _labelText = label?.Text;
        DebugLocationMap.ShowLabel(_popup, label);
    }

    /// <summary>The locations remembered, newest first, then the slots still free and those beyond reach.</summary>
    private void RenderList(Protagonist p, int top)
    {
        int x = ListX;
        int usable = p.GetMentalMapSlots();
        _terminal.Text(x, top + 1, "M E N T A L   M A P S", Config.Colors.BrightYellow, Config.Colors.Black);
        _terminal.Text(x, top + 2, new string('─', ListWidth), Config.Colors.DarkGray35, Config.Colors.Black);
        _terminal.Text(x, top + 3, $"Remembered: {p.MentalMaps.Count} / {usable}", Config.Colors.LightGray75, Config.Colors.Black);
        _terminal.Text(x, top + 4, "☐ held  ☑ locked", Config.Colors.DarkGray35, Config.Colors.Black);

        var selected = SelectedMap(p);
        int grid = Math.Max(p.GetMentalMapSlotsAtBest(), Math.Max(usable, p.MentalMaps.Count));
        int last = _terminal.Height - 5;
        int row = top + 6;
        for (int i = 0; i < grid && row <= last; i++, row += ListRowStep)
        {
            if (i < p.MentalMaps.Count)
            {
                var m = p.MentalMaps[p.MentalMaps.Count - 1 - i];
                bool isSel  = selected != null && m.LocationId == selected.LocationId;
                bool hovRow = _hover.Y == row && _hover.X >= x && _hover.X < x + ListWidth;
                bool hovBox = hovRow && _hover.X < x + CheckboxCols;

                Vector4 fg = isSel ? Config.Colors.BrightYellow
                           : hovRow || m.Locked ? Config.Colors.MediumYellow : Config.Colors.LightGray75;
                Vector4 bg = isSel || hovRow ? SelectedBg : Config.Colors.Black;
                string count = m.Routines.Count > 0 ? $" {m.Routines.Count}" : "";
                string name  = $"{(m.Locked ? "☑" : "☐")} {m.LocationName}";
                if (name.Length > ListWidth - count.Length) name = name[..(ListWidth - count.Length - 1)] + "…";
                _terminal.FillRect(x, row, ListWidth, 1, ' ', fg, bg);
                _terminal.Text(x, row, name, fg, bg);
                if (count.Length > 0)
                    _terminal.Text(x + ListWidth - count.Length, row, count, Config.Colors.DarkYellowGrey, bg);
                if (hovBox)
                    _terminal.Text(x, row, m.Locked ? "☑" : "☐", Config.Colors.BrightYellow, HoverBg);
                _mapRows.Add((row, m));
            }
            else
                _terminal.Text(x, row, i < usable ? "○ empty" : "× ·····",
                               i < usable ? Config.Colors.DarkGray35 : Config.Colors.DarkGray20, Config.Colors.Black);
        }

        _terminal.Text(x, _terminal.Height - 3, "The lowest one not locked is", Config.Colors.DarkGray35, Config.Colors.Black);
        _terminal.Text(x, _terminal.Height - 2, "forgotten first.", Config.Colors.DarkGray35, Config.Colors.Black);
    }

    /// <summary>The selected location's map, across the top down to <paramref name="lastRow"/>.</summary>
    private void RenderMap(MentalMap? map, int lastRow)
    {
        var bg = Config.NarrativeUI.BackgroundColor;
        _terminal.FillRect(MapLeft, 0, MapWidth, lastRow + 1, ' ', Config.Colors.Black, bg);
        if (map == null)
        {
            CenteredText(MapTop + 4, "No place remembered yet.", Config.Colors.MediumGray60, bg, MapLeft, MapWidth);
            return;
        }
        CenteredText(0, map.LocationName.ToUpperInvariant(), Config.Colors.BrightYellow, bg, MapLeft, MapWidth);

        var scene = SceneFor?.Invoke(map.LocationId);
        var icons = scene == null ? new List<DebugLocationMap.MapIcon>() : Icons(map, scene);
        var selected = SelectedRoutine(map);
        var focus = (scene == null ? null : selected?.FindArea(scene))
                  ?? scene?.AllAreas.FirstOrDefault(a => map.VisitedAreas.Contains(a.StableKey));
        _map.Render(_terminal, scene, here: null, period: selected?.Time ?? TimePeriod.Noon,
                    MapLeft, MapTop, MapWidth, lastRow - MapTop + 1,
                    icons: icons, hoveredIcon: _map.IconAt(_hover.X, _hover.Y), selectedIcon: selected?.Id,
                    focus: focus, footRows: 0, selectedLit: _blinkOn);
    }

    /// <summary>
    /// <c>&lt; n/N &gt;</c> centred under the porthole: which of the place's routines is shown, and
    /// the arrows to step through them. Nothing when the place has no routine; the arrows only when
    /// it has more than one.
    /// </summary>
    private void RenderStepper(MentalMap? map, int row)
    {
        _stepRow = _prevX = _nextX = -1;
        if (map == null || map.Routines.Count == 0) return;
        var ordered = Ordered(map);
        string count = $" {ordered.IndexOf(SelectedRoutine(map)!) + 1}/{ordered.Count} ";
        int width = StepButtonW * 2 + count.Length;
        int x = CenterLeft + (CenterWidth - width) / 2;
        _terminal.Text(x + StepButtonW, row, count, Config.Colors.LightGray75, Config.Colors.Black);
        if (ordered.Count < 2) return;

        _stepRow = row;
        _prevX = x;
        _nextX = x + StepButtonW + count.Length;
        StepButton(_prevX, " < ");
        StepButton(_nextX, " > ");

        void StepButton(int bx, string label)
        {
            bool hot = _hover.Y == row && _hover.X >= bx && _hover.X < bx + StepButtonW;
            _terminal.Text(bx, row, label, hot ? Config.Colors.Black : Config.Colors.BrightYellow,
                           hot ? Config.Colors.BrightYellow : SelectedBg);
        }
    }

    /// <summary>-1 or 1 when (x, y) is on the stepper's previous or next arrow, else 0.</summary>
    private int StepAt(int x, int y)
        => _stepRow < 0 || y != _stepRow ? 0
         : x >= _prevX && x < _prevX + StepButtonW ? -1
         : x >= _nextX && x < _nextX + StepButtonW ? 1
         : 0;

    /// <summary>An icon per routine on its area's card, kind by kind; a routine whose area is gone has none.</summary>
    public static List<DebugLocationMap.MapIcon> Icons(MentalMap map, Scene.Scene scene,
                                                       Func<Routine, bool>? enabled = null)
        => Ordered(map)
              .Select(r => (Routine: r, Area: r.FindArea(scene)))
              .Where(x => x.Area != null)
              .Select(x => new DebugLocationMap.MapIcon(x.Area!, x.Routine.Category.Icon(),
                                                        enabled?.Invoke(x.Routine) ?? true, x.Routine.Id))
              .ToList();

    /// <summary>A transparent window onto the always-rendered world, with a small mark at its centre.</summary>
    private void RenderPorthole(int bandTop, int bandBottom)
    {
        int cx = _terminal.Width / 2, cy = _terminal.Height / 2;
        for (int y = bandTop; y <= bandBottom; y++)
            for (int sx = CenterLeft; sx <= CenterRight; sx++)
                _terminal.SetCell(sx, y, ' ', Config.Colors.Transparent, Config.Colors.Transparent);

        // The camera puts the location at the screen's exact centre, which on an even grid is the
        // corner cells cx-1 and cx meet at, not a cell's middle: the mark brackets that corner,
        // two cells either side of it.
        _terminal.SetCell(cx - 2, cy - 2, '┌', Config.Colors.BrightYellow, Config.Colors.Transparent);
        _terminal.SetCell(cx + 1, cy - 2, '┐', Config.Colors.BrightYellow, Config.Colors.Transparent);
        _terminal.SetCell(cx - 2, cy + 1, '└', Config.Colors.BrightYellow, Config.Colors.Transparent);
        _terminal.SetCell(cx + 1, cy + 1, '┘', Config.Colors.BrightYellow, Config.Colors.Transparent);
    }

    // ═══════════════════════════════════════════════════════════════
    // Detail (bottom zone)
    // ═══════════════════════════════════════════════════════════════

    private void RenderDetail(Protagonist p, MentalMap? map, int row)
    {
        int x = CenterLeft;
        if (map == null)
        {
            CenterText(x, row, "Every place visited is remembered — its map, and the", Config.Colors.MediumGray60);
            CenterText(x, row + 1, "routines learned there — as many as the anamnesis holds.", Config.Colors.MediumGray60);
            return;
        }

        var r = SelectedRoutine(map);
        if (r == null)
        {
            CenterText(x, row++, map.LocationName, Config.Colors.BrightYellow);
            CenterText(x, row++, $"{map.VisitedAreas.Count} areas visited · {map.SeenAreas.Count} seen · "
                               + $"{map.Routines.Count} routine{(map.Routines.Count == 1 ? "" : "s")}", Config.Colors.LightGray75);
            row++;
            CenterText(x, row, "No routine learned here.", Config.Colors.DarkGray35);
            return;
        }

        CenterText(x, row++, r.DisplayName, Config.Colors.BrightYellow);
        CenterText(x, row++, $"{r.Category.Label()} · {r.AreaName} · {r.Time.Label().ToLowerInvariant()}", Config.Colors.LightGray75);
        row++;

        int last = _terminal.Height - 2;
        foreach (var (text, fg, coin) in DetailLines(r, p))
        {
            if (row > last) break;
            CenterText(x, row, text, fg);
            // A price's coin keeps its own metal colour, as in the trade menu: the glyph is the line's
            // last character, so it is repainted over the plain text.
            if (coin is { } c && text.Length > 0 && text.Length <= CenterWidth)
                _terminal.SetCell(x + text.Length - 1, row, CoinGlyph(c), CoinColor(c), Config.Colors.Black);
            row++;
        }
    }

    private IEnumerable<(string Text, Vector4 Fg, CoinType? Coin)> DetailLines(Routine r, Protagonist p)
    {
        foreach (var (text, fg) in PlainDetailLines(r, p)) yield return (text, fg, null);
        if (r is TradeRoutine trade)
            foreach (var line in trade.Catalogue)
                yield return (CatalogueLine(line), Config.Colors.MediumGray60, line.Coin);
    }

    private IEnumerable<(string, Vector4)> PlainDetailLines(Routine r, Protagonist p)
    {
        var plain = Config.Colors.MediumGray60;
        var head  = Config.Colors.White;
        switch (r)
        {
            case GoToRoutine:
                yield return ("Opens your narration there, at that hour.", plain);
                break;

            case MeetRoutine meet:
                yield return ($"{meet.NpcName}, {meet.NpcDescription}", head);
                yield return (RelationText(meet), plain);
                yield return ("Opens your narration with your eyes on them.", plain);
                break;

            case TradeRoutine trade:
                yield return ($"{trade.NpcName}, {trade.NpcDescription}", head);
                yield return (RelationText(trade), plain);
                yield return ("", plain);
                yield return (trade.Mode == TradeMode.Sell ? "WHAT THEY BUY" : "WHAT THEY SELL", head);
                if (trade.Catalogue.Count == 0)
                    yield return ("  · nothing", Config.Colors.DarkGray35);
                break;   // the catalogue lines follow, coloured — see DetailLines

            case WorkRoutine work:
                yield return ($"{work.NpcName}, {work.NpcDescription}", head);
                yield return (RelationText(work), plain);
                yield return ("", plain);
                if (JobRegistry.Instance.GetById(work.JobId) is { } job)
                {
                    yield return ($"As {job.WithArticle()}", head);
                    yield return ($"  · pay: one {CoinName(job.PayCoin)} every {job.DaysPerCoin.ToString("0.#", CultureInfo.InvariantCulture)} days", plain);
                    var trains = job.ModusMentisIds
                        .Select(id => ModusMentisRegistry.Instance.GetModusMentis(id)?.DisplayName ?? id);
                    yield return ($"  · trains: {string.Join(", ", trains)}", plain);
                }
                else yield return ($"As {work.JobTitle} — work no longer offered", Config.Colors.DarkGray35);
                break;

            case GatherRoutine gather:
                yield return (RoutineRecorder.SameThing(gather.ItemName, gather.SourceName)
                    ? gather.SourceName : $"{gather.ItemName} from {gather.SourceName}", head);
                yield return (gather.NeedsTool ? $"  · needs: {gather.ToolItemName}" : "  · needs no implement", plain);
                yield return ("", plain);
                yield return ("Stay some days and take what grows back:", Config.Colors.DarkGray35);
                yield return ("the longer the stay, the more it yields.", Config.Colors.DarkGray35);
                break;
        }
    }

    private string RelationText(NpcRoutine r)
    {
        var rel = RelationLookup?.Invoke(r.LocationId, r.NpcId);
        if (rel is not { } known) return "  · relation: unknown";
        if (known.Enemy) return "  · relation: counts you an enemy";
        return $"  · relation: {known.Level.ToShortLabel()}";
    }

    private static string CatalogueLine(TradeLine line)
    {
        string price = $"{line.Price}{CoinGlyph(line.Coin)}";
        const int width = 40;
        string name = line.Name.Length > width - price.Length - 5 ? line.Name[..(width - price.Length - 6)] + "…" : line.Name;
        return $"  · {name} {new string('.', Math.Max(1, width - 5 - name.Length - price.Length))} {price}";
    }

    private static char CoinGlyph(CoinType c) => c switch
    {
        CoinType.Gold   => Config.Symbols.GoldCoinSymbol,
        CoinType.Silver => Config.Symbols.SilverCoinSymbol,
        _               => Config.Symbols.CopperCoinSymbol,
    };

    private static Vector4 CoinColor(CoinType c) => c switch
    {
        CoinType.Gold   => Config.Colors.CoinGold,
        CoinType.Silver => Config.Colors.CoinSilver,
        _               => Config.Colors.CoinCopper,
    };

    private static string CoinName(CoinType c) => c switch
    {
        CoinType.Gold   => "gold",
        CoinType.Silver => "silver",
        _               => "copper",
    };

    // ═══════════════════════════════════════════════════════════════
    // Input
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Tracks the cursor; true when it moved onto a different clickable (or off one), or onto
    /// something the label names differently. Only the first is the host's cue to tick
    /// (<see cref="IsHovering"/>).
    /// </summary>
    public bool ProcessHover(int x, int y)
    {
        string? before = HoverKey();
        _hover = (x, y);
        _map.SetHover(x, y);
        return HoverKey() != before || Label()?.Text != _labelText;
    }

    private string? HoverKey()
        => MapRowAt(_hover.X, _hover.Y) is { } m ? $"map:{m.Map.LocationId}:{_hover.X < ListX + CheckboxCols}"
         : _map.IconAt(_hover.X, _hover.Y) is { } tag ? $"icon:{tag}"
         : StepAt(_hover.X, _hover.Y) is var step and not 0 ? $"step:{step}"
         : _map.PadArrowHovered ? "pad"
         : null;

    /// <summary>
    /// A click on a location's checkbox toggles its lock; on its name shows its map (and refocuses
    /// the porthole); on a routine icon shows that routine below; on a stepper arrow shows the next or
    /// previous routine and centres the map on it; on the map's pad pans it.
    /// Returns true when something changed.
    /// </summary>
    public bool ProcessClick(int x, int y, Protagonist protagonist)
    {
        if (MapRowAt(x, y) is { } row)
        {
            if (x < ListX + CheckboxCols)
            {
                row.Map.Locked = !row.Map.Locked;
                return true;
            }
            if (row.Map.LocationId == SelectedMap(protagonist)?.LocationId) return false;
            Select(row.Map, protagonist);
            return true;
        }

        if (_map.IconAt(x, y) is string id && SelectedMap(protagonist) is { } map)
        {
            _selectedRoutine[map.LocationId] = id;
            return true;
        }

        if (StepAt(x, y) is var step and not 0)
            return StepRoutine(step, protagonist);

        return _map.HandleClick(x, y);
    }

    /// <summary>
    /// What a press at (x, y) would act on, named for <c>click element</c>: <c>place:&lt;name&gt;</c>
    /// (a remembered place in the list), <c>lock:&lt;name&gt;</c> (its checkbox), <c>routine-prev</c> /
    /// <c>routine-next</c> (the stepper). Names are lower case with underscores for spaces.
    /// </summary>
    public string? CliControlIdAt(int x, int y)
    {
        if (MapRowAt(x, y) is { } row)
            return (x < ListX + CheckboxCols ? "lock:" : "place:") + row.Map.LocationName.ToLowerInvariant().Replace(' ', '_');
        return StepAt(x, y) switch { -1 => "routine-prev", 1 => "routine-next", _ => null };
    }

    private (int Row, MentalMap Map)? MapRowAt(int x, int y)
    {
        if (x < ListX || x >= ListX + ListWidth) return null;
        foreach (var r in _mapRows)
            if (r.Row == y) return r;
        return null;
    }

    private void FireFocus(Protagonist protagonist, bool force)
    {
        var m = SelectedMap(protagonist);
        if (m == null) return;
        if (!force && m.LocationId == _lastFocusedLocationId) return;
        _lastFocusedLocationId = m.LocationId;
        OnMapFocused?.Invoke(m.LocationId);
    }

    // ═══════════════════════════════════════════════════════════════
    // Drawing helpers
    // ═══════════════════════════════════════════════════════════════

    private void DottedHLine(int y, int from, int to)
    {
        for (int gx = from; gx <= to; gx += 2)
            _terminal.SetCell(gx, y, '─', Config.Colors.DarkGray35, Config.Colors.Black);
    }

    /// <summary>Draws text centred across [<paramref name="left"/>, <paramref name="left"/> + <paramref name="width"/>).</summary>
    private void CenteredText(int y, string text, Vector4 fg, Vector4 bg, int left, int width)
    {
        if (text.Length > width) text = text.Substring(0, width - 1) + "…";
        _terminal.Text(left + (width - text.Length) / 2, y, text, fg, bg);
    }

    /// <summary>Draws text in the centre, truncated to its width.</summary>
    private void CenterText(int x, int y, string text, Vector4 fg, Vector4? bg = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (text.Length > CenterWidth) text = text.Substring(0, CenterWidth - 1) + "…";
        _terminal.Text(x, y, text, fg, bg ?? Config.Colors.Black);
    }
}
