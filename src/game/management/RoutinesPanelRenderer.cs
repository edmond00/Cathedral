using System;
using System.Collections.Generic;
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

namespace Cathedral.Game.Management;

/// <summary>
/// The protagonist's learned routines in the management menu (protagonist-only, like the journal).
///
/// <para>Layout, one zone per question:</para>
/// <list type="bullet">
/// <item><b>Right column</b> (col 70+) — the kinds of routine (<see cref="RoutineCategories.All"/>),
///   each with how many of its slots are filled. Clicking one shows its slots.</item>
/// <item><b>Top of the centre</b> — that kind's slots, in two columns. Every slot the anamnesis could
///   ever hold is drawn, so the grid says three things at once: a slot holding a routine (its
///   ☐/☑ is the eviction lock, clickable), an empty slot this body can fill (○), and a slot beyond
///   the anamnesis as it stands (×). Names are cut to the column; the full name is below.</item>
/// <item><b>Middle</b> — a transparent porthole onto the world; the host aims the camera at the
///   selected routine's location (<see cref="OnRoutineFocused"/>).</item>
/// <item><b>Bottom</b> — the selected routine in full, with what its kind has to say: a merchant's
///   catalogue, a job's pay, a person's standing with you, a source's dice.</item>
/// </list>
/// </summary>
public class RoutinesPanelRenderer
{
    private readonly TerminalHUD _terminal;

    // Right column geometry.
    private static int ListX => BodyArtViewer.PanelContentX;   // col 70
    private const int ListWidth = 28;   // to the frame's right rule
    private const int CategoryStartRow = 7;
    private const int CategoryRowStep = 2;

    // Centre geometry (between the left nav separator at col 15 and the right column at col 70).
    private const int CenterLeft = 17;
    private const int CenterRight = 66;
    private const int CenterWidth = CenterRight - CenterLeft + 1;
    private const int ColumnGap = 2;
    private const int ColumnWidth = (CenterWidth - ColumnGap) / 2;   // 24
    private const int CheckboxCols = 2;
    private const int SlotStartRow = 5;

    private const int SeparatorX = BodyArtViewer.PanelX - 1;   // col 67
    private const int PortholeHalf = 16;

    private static readonly Vector4 HoverBg    = new(0.18f, 0.15f, 0.02f, 1.0f);
    private static readonly Vector4 SelectedBg = new(0.07f, 0.06f, 0.01f, 1.0f);

    // ── Selection ─────────────────────────────────────────────────
    private RoutineCategory _category = RoutineCategory.GoTo;
    private readonly Dictionary<RoutineCategory, string> _selectedId = new();
    private int _lastFocusedLocationId = int.MinValue;

    // ── Hit areas, rebuilt every render ───────────────────────────
    private readonly List<(int Row, RoutineCategory Category)> _categoryRows = new();
    private readonly List<(int Row, int X, Routine Routine)> _slotRows = new();
    private int _lockRow = -1, _lockWidth;

    private (int X, int Y) _hover = (-1, -1);

    /// <summary>
    /// Fired with the selected routine's LocationId when the selection changes (and on activation).
    /// The host centres the world camera so the porthole shows that location.
    /// </summary>
    public Action<int>? OnRoutineFocused;

    /// <summary>
    /// How the person a routine is about stands with the protagonist today — the level and whether
    /// they count you an enemy — read from the location's stored memory, or null when that location
    /// was never visited. Set by the host, which owns the location states.
    /// </summary>
    public Func<int, string, (AffinityLevel Level, bool Enemy)?>? RelationLookup;

    public RoutinesPanelRenderer(TerminalHUD terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    /// <summary>True while something clickable is hovered (for SFX feedback).</summary>
    public bool IsHovering => HoveredCategory() != null || HoveredSlot() != null || OverLock();

    public void ClearHover() => _hover = (-1, -1);

    /// <summary>The kind shown, for <c>--cli</c>.</summary>
    public RoutineCategory Category => _category;

    /// <summary>Called when the Routines tab becomes active: refocuses the porthole on the selection.</summary>
    public void OnActivated(Protagonist protagonist) => FireFocus(protagonist, force: true);

    /// <summary>Shows one kind's slots — the right column's click, for <c>--cli</c> too.</summary>
    public void SelectCategory(RoutineCategory category, Protagonist protagonist)
    {
        _category = category;
        FireFocus(protagonist, force: false);
    }

    private Routine? Selected(Protagonist p)
    {
        var mine = p.RoutinesOf(_category);
        if (mine.Count == 0) return null;
        if (_selectedId.TryGetValue(_category, out var id) && mine.FirstOrDefault(r => r.Id == id) is { } r)
            return r;
        return mine[^1];   // newest
    }

    // ═══════════════════════════════════════════════════════════════
    // Render
    // ═══════════════════════════════════════════════════════════════

    public void Render(Protagonist protagonist)
    {
        _categoryRows.Clear();
        _slotRows.Clear();
        _lockRow = -1;

        for (int y = 0; y < _terminal.Height; y++)
            _terminal.SetCell(SeparatorX, y, '│', Config.Colors.DarkGray35, Config.Colors.Black);

        RenderCategories(protagonist);

        int cy = _terminal.Height / 2;
        int bandTop = cy - PortholeHalf, bandBottom = cy + PortholeHalf;

        RenderSlots(protagonist, bandTop - 2);
        DottedHLine(bandTop - 1);
        RenderPorthole(bandTop, bandBottom);
        DottedHLine(bandBottom + 1);
        RenderDetail(protagonist, bandBottom + 3);
    }

    private void RenderCategories(Protagonist p)
    {
        int x = ListX;
        _terminal.Text(x, 1, "R O U T I N E S", Config.Colors.BrightYellow, Config.Colors.Black);
        _terminal.Text(x, 3, new string('─', ListWidth), Config.Colors.DarkGray35, Config.Colors.Black);
        _terminal.Text(x, 4, $"Slots per kind: {p.GetRoutineSlots()}", Config.Colors.LightGray75, Config.Colors.Black);

        int row = CategoryStartRow;
        foreach (var c in RoutineCategories.All)
        {
            bool selected = c == _category;
            bool hovered  = _hover.Y == row && _hover.X >= x && _hover.X < x + ListWidth;
            int used = p.RoutinesOf(c).Count;

            Vector4 fg = selected ? Config.Colors.BrightYellow
                       : hovered  ? Config.Colors.MediumYellow
                       : used > 0 ? Config.Colors.LightGray75 : Config.Colors.MediumGray60;
            Vector4 bg = selected || hovered ? SelectedBg : Config.Colors.Black;

            string count = $"{used} / {p.GetRoutineSlots()}";
            string label = (selected ? "▸ " : "  ") + c.Label();
            _terminal.FillRect(x, row, ListWidth, 1, ' ', fg, bg);
            _terminal.Text(x, row, label, fg, bg);
            _terminal.Text(x + ListWidth - count.Length, row, count, fg, bg);

            _categoryRows.Add((row, c));
            row += CategoryRowStep;
        }
    }

    /// <summary>The selected kind's slot grid, from the top of the centre down to <paramref name="lastRow"/>.</summary>
    private void RenderSlots(Protagonist p, int lastRow)
    {
        var routines = p.RoutinesOf(_category);
        int usable   = p.GetRoutineSlots();
        int rowsFree = lastRow - SlotStartRow + 1;
        // Every slot the body could ever have, so what an anamnesis point would buy is visible — but
        // never more than the space holds, nor fewer than what is usable or filled.
        int grid     = Math.Min(2 * rowsFree, Math.Max(p.GetRoutineSlotsAtBest(), Math.Max(usable, routines.Count)));
        int rows     = (grid + 1) / 2;
        int spacing  = rows * 2 <= rowsFree ? 2 : 1;

        string title = string.Join(" ", _category.Label().ToUpperInvariant().ToCharArray());
        CenterText(CenterLeft, 1, title, Config.Colors.BrightYellow);
        string count = $"{routines.Count} / {usable}";
        _terminal.Text(CenterRight + 1 - count.Length, 1, count, Config.Colors.LightGray75, Config.Colors.Black);
        CenterText(CenterLeft, 2, "☐ learned  ☑ kept  ○ empty  × beyond anamnesis", Config.Colors.DarkGray35);

        var selected = Selected(p);
        var duplicates = routines.GroupBy(r => r.Name).Where(g => g.Count() > 1).Select(g => g.Key).ToHashSet();

        for (int i = 0; i < grid; i++)
        {
            // Column-major: the left column fills top to bottom first, as a list is read.
            int col = i / rows, line = i % rows;
            int x = CenterLeft + col * (ColumnWidth + ColumnGap);
            int y = SlotStartRow + line * spacing;

            if (i < routines.Count)
            {
                var r = routines[i];
                bool isSel  = selected != null && r.Id == selected.Id;
                bool hovRow = _hover.Y == y && _hover.X >= x && _hover.X < x + ColumnWidth;
                bool hovBox = hovRow && _hover.X < x + CheckboxCols;
                string name = duplicates.Contains(r.Name) ? $"{r.Name} ({r.Time.Label().ToLowerInvariant()})" : r.Name;
                string label = $"{(r.Locked ? "☑" : "☐")} {name}";
                if (label.Length > ColumnWidth) label = label.Substring(0, ColumnWidth - 1) + "…";

                Vector4 fg = isSel ? Config.Colors.BrightYellow
                           : hovRow || r.Locked ? Config.Colors.MediumYellow : Config.Colors.LightGray75;
                Vector4 bg = isSel || hovRow ? SelectedBg : Config.Colors.Black;
                _terminal.FillRect(x, y, ColumnWidth, 1, ' ', fg, bg);
                _terminal.Text(x, y, label, fg, bg);
                if (hovBox)
                    _terminal.Text(x, y, r.Locked ? "☑" : "☐", Config.Colors.BrightYellow, HoverBg);
                _slotRows.Add((y, x, r));
            }
            else if (i < usable)
                _terminal.Text(x, y, "○ empty", Config.Colors.DarkGray35, Config.Colors.Black);
            else
                _terminal.Text(x, y, "× ·····", Config.Colors.DarkGray20, Config.Colors.Black);
        }
    }

    /// <summary>A transparent window onto the always-rendered world, with a small mark at its centre.</summary>
    private void RenderPorthole(int bandTop, int bandBottom)
    {
        int cx = _terminal.Width / 2, cy = _terminal.Height / 2;
        for (int y = bandTop; y <= bandBottom; y++)
            for (int sx = CenterLeft; sx <= CenterRight; sx++)
                _terminal.SetCell(sx, y, ' ', Config.Colors.Transparent, Config.Colors.Transparent);

        _terminal.SetCell(cx - 1, cy - 1, '┌', Config.Colors.BrightYellow, Config.Colors.Transparent);
        _terminal.SetCell(cx + 1, cy - 1, '┐', Config.Colors.BrightYellow, Config.Colors.Transparent);
        _terminal.SetCell(cx - 1, cy + 1, '└', Config.Colors.BrightYellow, Config.Colors.Transparent);
        _terminal.SetCell(cx + 1, cy + 1, '┘', Config.Colors.BrightYellow, Config.Colors.Transparent);
    }

    // ═══════════════════════════════════════════════════════════════
    // Detail (bottom zone)
    // ═══════════════════════════════════════════════════════════════

    private void RenderDetail(Protagonist p, int row)
    {
        int x = CenterLeft;
        var r = Selected(p);
        if (r == null)
        {
            CenterText(x, row, $"No {_category.Label().ToLowerInvariant()} routine learned yet.", Config.Colors.MediumGray60);
            CenterText(x, row + 2, _category.HowLearned(), Config.Colors.DarkGray35);
            return;
        }

        CenterText(x, row++, r.Name, Config.Colors.BrightYellow);
        string where = r.LocationName.Length > 0 ? $"{r.LocationName} · {r.AreaName}" : r.AreaName;
        CenterText(x, row++, $"{where} · {r.Time.Label().ToLowerInvariant()}", Config.Colors.LightGray75);

        string lockText = r.Locked ? "☑ Kept — never forgotten to make room" : "☐ Forgotten first when this kind is full";
        _lockRow = row; _lockWidth = Math.Min(lockText.Length, CenterWidth);
        bool hovLock = OverLock();
        _terminal.FillRect(x, row, _lockWidth, 1, ' ', Config.Colors.MediumGray60, hovLock ? HoverBg : Config.Colors.Black);
        CenterText(x, row++, lockText, hovLock ? Config.Colors.BrightYellow
                                      : r.Locked ? Config.Colors.MediumYellow : Config.Colors.MediumGray60,
                   hovLock ? HoverBg : Config.Colors.Black);
        row++;

        int last = _terminal.Height - 2;
        foreach (var (text, fg) in DetailLines(r, p))
        {
            if (row > last) break;
            CenterText(x, row++, text, fg);
        }
    }

    private IEnumerable<(string, Vector4)> DetailLines(Routine r, Protagonist p)
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
                foreach (var line in trade.Catalogue)
                    yield return (CatalogueLine(line), plain);
                break;

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
                yield return ($"{gather.ItemName} from {gather.SourceName}", head);
                yield return (gather.NeedsTool ? $"  · needs: {gather.ToolItemName}" : "  · needs no implement", plain);
                yield return ($"  · dice today: {GatherYield.Dice(gather, p)} per item", plain);
                yield return ("", plain);
                yield return ("Stay some days and take what grows back;", Config.Colors.DarkGray35);
                yield return ("each item is rolled for, and a failure spoils it.", Config.Colors.DarkGray35);
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

    private static string CoinName(CoinType c) => c switch
    {
        CoinType.Gold   => "gold",
        CoinType.Silver => "silver",
        _               => "copper",
    };

    // ═══════════════════════════════════════════════════════════════
    // Input
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Tracks the cursor; true only when it moved onto a different clickable (or off one).</summary>
    public bool ProcessHover(int x, int y)
    {
        string? before = HoverKey();
        _hover = (x, y);
        return HoverKey() != before;
    }

    private string? HoverKey()
        => HoveredCategory() is { } c ? $"category:{c}"
         : HoveredSlot() is { } s     ? $"slot:{s.Routine.Id}:{_hover.X < s.X + CheckboxCols}"
         : OverLock()                 ? "lock"
         : null;

    /// <summary>
    /// A click on a kind shows it; on a slot's checkbox toggles its lock; on a slot's name selects it
    /// (and refocuses the porthole); on the detail's lock line toggles the selected one's lock.
    /// Returns true when something changed.
    /// </summary>
    public bool ProcessClick(int x, int y, Protagonist protagonist)
    {
        if (CategoryAt(x, y) is { } c)
        {
            if (c == _category) return false;
            SelectCategory(c, protagonist);
            return true;
        }

        if (SlotAt(x, y) is { } slot)
        {
            if (x < slot.X + CheckboxCols)
            {
                slot.Routine.Locked = !slot.Routine.Locked;
                return true;
            }
            _selectedId[_category] = slot.Routine.Id;
            FireFocus(protagonist, force: false);
            return true;
        }

        if (OverLock(x, y) && Selected(protagonist) is { } selected)
        {
            selected.Locked = !selected.Locked;
            return true;
        }
        return false;
    }

    private RoutineCategory? HoveredCategory() => CategoryAt(_hover.X, _hover.Y);
    private (int Row, int X, Routine Routine)? HoveredSlot() => SlotAt(_hover.X, _hover.Y);
    private bool OverLock() => OverLock(_hover.X, _hover.Y);

    private RoutineCategory? CategoryAt(int x, int y)
    {
        if (x < ListX || x >= ListX + ListWidth) return null;
        foreach (var (row, c) in _categoryRows)
            if (row == y) return c;
        return null;
    }

    private (int Row, int X, Routine Routine)? SlotAt(int x, int y)
    {
        foreach (var s in _slotRows)
            if (s.Row == y && x >= s.X && x < s.X + ColumnWidth) return s;
        return null;
    }

    private bool OverLock(int x, int y)
        => _lockRow >= 0 && y == _lockRow && x >= CenterLeft && x < CenterLeft + _lockWidth;

    private void FireFocus(Protagonist protagonist, bool force)
    {
        var r = Selected(protagonist);
        if (r == null) return;
        if (!force && r.LocationId == _lastFocusedLocationId) return;
        _lastFocusedLocationId = r.LocationId;
        OnRoutineFocused?.Invoke(r.LocationId);
    }

    // ═══════════════════════════════════════════════════════════════
    // Drawing helpers
    // ═══════════════════════════════════════════════════════════════

    private void DottedHLine(int y)
    {
        for (int gx = CenterLeft; gx <= CenterRight; gx += 2)
            _terminal.SetCell(gx, y, '─', Config.Colors.DarkGray35, Config.Colors.Black);
    }

    /// <summary>Draws text in the centre, truncated to its width.</summary>
    private void CenterText(int x, int y, string text, Vector4 fg, Vector4? bg = null)
    {
        if (string.IsNullOrEmpty(text)) return;
        if (text.Length > CenterWidth) text = text.Substring(0, CenterWidth - 1) + "…";
        _terminal.Text(x, y, text, fg, bg ?? Config.Colors.Black);
    }
}
