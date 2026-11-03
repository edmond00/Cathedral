using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Crafting;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Management;

/// <summary>
/// The crafting menu, opened by a successful USE at a station. Two columns over one pool of the
/// station's material:
///
/// <list type="bullet">
///   <item><b>Break down</b> (left): every carried item that holds the station's material. Breaking one
///     destroys it and adds its units to the pool — only the station's material; whatever else it held is
///     lost with it.</item>
///   <item><b>Make</b> (right): the trade's recipes. Making one takes its units from the pool and puts the
///     new item in the pack.</item>
/// </list>
///
/// <para>Nothing here is rolled and nothing takes time: the roll was USE's. The pool is not an inventory
/// — <b>whatever is left in it when the menu closes is thrown away</b>, which the footer says plainly.</para>
///
/// <para>Rendered like the trade menu: a centred bordered box, the world visible round it. ESC is the
/// launcher's (it pauses).</para>
/// </summary>
public sealed class CraftingMenuRenderer
{
    private const int BoxW      = 96;
    private const int ColW      = 45;
    private const int LeftOff   = 2;
    private const int RightOff  = 49;
    private const int PageRows  = 14;

    private static readonly Vector4 Outside  = Config.Colors.Transparent;
    private static readonly Vector4 Bg       = Config.Colors.Black;
    private static readonly Vector4 Border   = Config.Colors.DarkYellowGrey;
    private static readonly Vector4 Title    = Config.Colors.BrightYellow;
    private static readonly Vector4 Label    = Config.Colors.MediumGray60;
    private static readonly Vector4 Value    = Config.Colors.LightGray75;
    private static readonly Vector4 Sep      = Config.Colors.DarkGray35;
    private static readonly Vector4 RowHovBg = Config.Colors.DarkGray20;
    private static readonly Vector4 BtnFg    = Config.TravelUI.ClearButtonTextColor;
    private static readonly Vector4 BtnBg    = Config.TravelUI.ClearButtonBackgroundColor;
    private static readonly Vector4 BtnHovFg = Config.TravelUI.ClearButtonHoverTextColor;
    private static readonly Vector4 BtnHovBg = Config.TravelUI.ClearButtonHoverBackgroundColor;
    private static readonly Vector4 OkFg     = Config.TravelUI.TravelButtonTextColor;
    private static readonly Vector4 OkBg     = Config.TravelUI.TravelButtonBackgroundColor;
    private static readonly Vector4 OkHovBg  = Config.TravelUI.TravelButtonHoverBackgroundColor;
    private static readonly Vector4 Disabled = Config.Colors.DarkGray40;
    private static readonly Vector4 Warning  = Config.Colors.OrangeYellow;

    private readonly TerminalHUD         _terminal;
    private readonly PartyMember         _member;
    private readonly PointOfInterest     _station;
    private readonly CraftingModusMentis _trade;
    private readonly List<Item>          _recipes;

    /// <summary>Units of the station's material in hand.</summary>
    public int Pool { get; private set; }

    /// <summary>What was broken down and what was made, in order — for the narration note on leaving.</summary>
    public List<string> BrokenDown { get; } = new();
    public List<string> Made { get; } = new();
    public List<string> Spoiled { get; } = new();

    /// <summary>The last roll made here, for the message line and the CLI.</summary>
    public CraftRoll.Result? LastRoll { get; private set; }

    private string _message = string.Empty;
    private int _hoverX = -1, _hoverY = -1;
    private int _page;

    // Geometry, recomputed each Render and reused by hit-testing.
    private int _boxX, _boxY;
    private int _rowY0 = int.MinValue;
    private int _buttonsRow = int.MinValue, _leaveX0, _leaveX1;
    private int _pagerRow = int.MinValue, _prevX, _nextX;

    public bool IsComplete { get; private set; }

    public CraftingMenuRenderer(TerminalHUD terminal, PartyMember member, PointOfInterest station, CraftingModusMentis trade)
    {
        _terminal = terminal;
        _member   = member;
        _station  = station;
        _trade    = trade;
        _recipes  = trade.RecipePrototypes.ToList();
    }

    public PointOfInterest Station => _station;
    public CraftingModusMentis Trade => _trade;
    public CraftMaterial Material => _trade.Material;

    // ── What the two columns hold ────────────────────────────────────────────

    /// <summary>Carried items that hold the station's material and can be broken down (no full container).</summary>
    public List<Item> Breakables()
        => _member.GetAllItems()
                  .Where(i => ItemMaterials.Holds(i, Material))
                  .Where(i => i is not IContainer c || c.Contents.Count == 0)
                  .ToList();

    public IReadOnlyList<Item> Recipes => _recipes;

    public static int Cost(Item recipe) => CraftingModusMentis.Cost(recipe);

    public bool CanMake(Item recipe) => Pool >= Cost(recipe);

    /// <summary>Sixes needed to make <paramref name="recipe"/>.</summary>
    public static int Difficulty(Item recipe) => CraftRoll.Difficulty(recipe);

    /// <summary>The dice every make here rolls: every held trade for this station and every carried implement of one.</summary>
    public int Dice => CraftRoll.PoolFor(_member, _station);

    public IReadOnlyList<CraftRoll.Contribution> Contributions => CraftRoll.ContributionsFor(_member, _station);

    // ── Actions (mouse and CLI alike) ────────────────────────────────────────

    /// <summary>Destroys <paramref name="item"/> and adds its units of the station's material to the pool.</summary>
    public bool BreakDown(Item item)
    {
        if (!ItemMaterials.Holds(item, Material)) { _message = $"There is no {Material.Label()} in that."; return false; }
        if (item is IContainer c && c.Contents.Count > 0) { _message = $"Empty the {item.DisplayName.ToLowerInvariant()} first."; return false; }
        if (!_member.RemoveItem(item)) { _message = "You cannot get at that."; return false; }
        int units = ItemMaterials.Units(item);
        Pool += units;
        BrokenDown.Add(item.DisplayName);
        _message = $"Broke down {item.WithArticle()}: +{units} {Material.Label()}.";
        Console.WriteLine($"CraftingMenu: broke down {item.ItemId} → +{units} {Material} (pool {Pool})");
        return true;
    }

    public enum MakeResult { Refused, Spoiled, Made }

    /// <summary>
    /// Pays for one <paramref name="recipe"/> and rolls for it (<see cref="CraftRoll"/>). Made, it goes into
    /// the pack; spoiled, the material is gone and nothing is made. Refused — not enough in hand, or no room
    /// for the thing — nothing is paid and nothing rolled.
    /// </summary>
    public MakeResult Make(Item recipe)
    {
        int cost = Cost(recipe);
        if (Pool < cost) { _message = $"Not enough {Material.Label()}: {recipe.DisplayName.ToLowerInvariant()} takes {cost}."; return MakeResult.Refused; }
        var made = ItemRegistry.NewInstance(recipe);
        if (!_member.CanAcquireItem(made)) { _message = $"No room in your pack for {made.WithArticle()}."; return MakeResult.Refused; }

        var roll = CraftRoll.Roll(Dice, Difficulty(recipe));
        LastRoll = roll;
        Pool -= cost;
        string dice = $"{roll.Sixes} {(roll.Sixes == 1 ? "six" : "sixes")} on {roll.Dice} {(roll.Dice == 1 ? "die" : "dice")}, {roll.Difficulty} needed";
        if (!roll.Succeeded)
        {
            Spoiled.Add(made.DisplayName);
            _message = $"Spoiled {made.WithArticle()} ({dice}): -{cost} {Material.Label()} lost.";
            Console.WriteLine($"CraftingMenu: spoiled {made.ItemId} ← -{cost} {Material} ({dice}; pool {Pool})");
            return MakeResult.Spoiled;
        }
        if (!_member.AcquireItem(made))
        {
            Pool += cost;   // the room was there a moment ago; nothing is lost to a placement that changed under us
            _message = $"No room in your pack for {made.WithArticle()}.";
            return MakeResult.Refused;
        }
        Made.Add(made.DisplayName);
        _message = $"Made {made.WithArticle()} ({dice}): -{cost} {Material.Label()}.";
        Console.WriteLine($"CraftingMenu: made {made.ItemId} ← -{cost} {Material} ({dice}; pool {Pool})");
        return MakeResult.Made;
    }

    public void Leave()
    {
        if (Pool > 0) Console.WriteLine($"CraftingMenu: {Pool} {Material} left over and thrown away");
        IsComplete = true;
    }

    // ── Input ────────────────────────────────────────────────────────────────

    public void OnMouseMove(int x, int y) { _hoverX = x; _hoverY = y; }

    public string? GetHoveredControlId(int x, int y)
    {
        if (y == _buttonsRow && x >= _leaveX0 && x < _leaveX1) return "craft:leave";
        if (y == _pagerRow && (Near(x, _prevX) || Near(x, _nextX))) return Near(x, _prevX) ? "craft:prev" : "craft:next";
        if (RowAt(y) is { } row)
        {
            if (row.Left != null && x >= _boxX + LeftOff && x < _boxX + LeftOff + ColW) return $"craft:break:{row.Index}";
            if (row.Right != null && x >= _boxX + RightOff && x < _boxX + RightOff + ColW) return $"craft:make:{row.Index}";
        }
        return null;
    }

    public void OnMouseClick(int x, int y)
    {
        if (y == _buttonsRow && x >= _leaveX0 && x < _leaveX1) { Leave(); return; }
        if (y == _pagerRow)
        {
            int pages = Pages();
            if (Near(x, _prevX) && _page > 0) _page--;
            if (Near(x, _nextX) && _page < pages - 1) _page++;
            return;
        }
        if (RowAt(y) is not { } row) return;
        if (row.Left != null && x >= _boxX + LeftOff && x < _boxX + LeftOff + ColW) BreakDown(row.Left);
        else if (row.Right != null && x >= _boxX + RightOff && x < _boxX + RightOff + ColW && CanMake(row.Right)) Make(row.Right);
    }

    private static bool Near(int x, int x0) => x0 != int.MinValue && x >= x0 && x < x0 + 3;

    private int Pages() => Math.Max(1, (Breakables().Count + PageRows - 1) / PageRows);

    private (int Index, Item? Left, Item? Right)? RowAt(int y)
    {
        if (_rowY0 == int.MinValue || y < _rowY0 || y >= _rowY0 + PageRows) return null;
        int i = y - _rowY0;
        var left  = Breakables().Skip(_page * PageRows).Take(PageRows).ToList();
        return (i, i < left.Count ? left[i] : null, i < _recipes.Count ? _recipes[i] : null);
    }

    // ── Render ───────────────────────────────────────────────────────────────

    public void Render()
    {
        _terminal.Fill(' ', Config.Colors.White, Outside);

        var left = Breakables();
        _page = Math.Min(_page, Pages() - 1);
        var shown = left.Skip(_page * PageRows).Take(PageRows).ToList();

        int inner = 31;   // the dice line takes the blank row under the pool   // blank, 9 header rows, the page, 6 footer rows, blank
        DrawBox(inner);

        int y = _boxY + 2;
        Centered(y++, Truncate($"{_trade.DisplayName} at the {_station.DisplayName}", BoxW - 4), Title);
        Centered(y++, $"Works {Material.Label()} with the {_trade.ToolPrototype?.DisplayName.ToLowerInvariant() ?? "tool"}", Label);
        y++;
        Separator(y++);
        y++;

        // The pool, the one number everything here turns on — and the dice every make rolls, with whence they come.
        Centered(y++, $"In hand: {Pool} {Material.Label()}", Pool > 0 ? Title : Value);
        Centered(y++, Truncate($"Dice: {Dice}  ({string.Join(" · ", Contributions.Select(c => $"{c.Label} {c.Dice}"))})", BoxW - 4), Label);

        _terminal.Text(_boxX + LeftOff,  y, $"Break down ({left.Count})", Label, Bg);
        _terminal.Text(_boxX + RightOff, y, "Make", Label, Bg);
        _terminal.Text(_boxX + RightOff + ColW - 22, y, "sixes", Label, Bg);
        y++;
        for (int x = _boxX + 1; x < _boxX + BoxW - 1; x++) _terminal.SetCell(x, y, '─', Sep, Bg);
        y++;

        _rowY0 = y;
        for (int i = 0; i < PageRows; i++, y++)
        {
            if (i < shown.Count)    RenderBreakRow(shown[i], y);
            if (i < _recipes.Count) RenderMakeRow(_recipes[i], y);
        }
        for (int r = _rowY0; r < _rowY0 + PageRows; r++) _terminal.SetCell(_boxX + RightOff - 2, r, '│', Sep, Bg);

        // Pager for a long pack.
        _pagerRow = y;
        int pages = Pages();
        if (pages > 1)
        {
            _prevX = _boxX + LeftOff; _nextX = _prevX + 10;
            _terminal.Text(_prevX, y, "[<]", _page > 0 ? Value : Disabled, Bg);
            _terminal.Text(_prevX + 4, y, $"{_page + 1}/{pages}", Label, Bg);
            _terminal.Text(_nextX, y, "[>]", _page < pages - 1 ? Value : Disabled, Bg);
        }
        else { _prevX = _nextX = int.MinValue; }
        y++;

        Separator(y++);
        Centered(y++, Truncate(_message, BoxW - 4), Warning);
        Centered(y++, Pool > 0 ? $"Leaving throws away the {Pool} {Material.Label()} still in hand." : "", Disabled);
        y++;

        const string leave = "[ Leave ]";
        _buttonsRow = y;
        _leaveX0 = _boxX + (BoxW - leave.Length) / 2; _leaveX1 = _leaveX0 + leave.Length;
        bool hov = _hoverY == y && _hoverX >= _leaveX0 && _hoverX < _leaveX1;
        _terminal.Text(_leaveX0, y, leave, hov ? BtnHovFg : BtnFg, hov ? BtnHovBg : BtnBg);
    }

    private void RenderBreakRow(Item item, int y)
    {
        bool hov = _hoverY == y && _hoverX >= _boxX + LeftOff && _hoverX < _boxX + LeftOff + ColW;
        var bg = hov ? RowHovBg : Bg;
        _terminal.FillRect(_boxX + LeftOff, y, ColW - 2, 1, ' ', Value, bg);
        _terminal.Text(_boxX + LeftOff, y, Truncate(item.DisplayName, ColW - 16), Value, bg);
        _terminal.Text(_boxX + LeftOff + ColW - 15, y, $"+{ItemMaterials.Units(item)}", Label, bg);
        _terminal.Text(_boxX + LeftOff + ColW - 10, y, "[Break]", hov ? OkFg : Value, hov ? OkHovBg : bg);
    }

    private void RenderMakeRow(Item recipe, int y)
    {
        bool can = CanMake(recipe);
        bool hov = can && _hoverY == y && _hoverX >= _boxX + RightOff && _hoverX < _boxX + RightOff + ColW;
        var bg = hov ? RowHovBg : Bg;
        _terminal.FillRect(_boxX + RightOff, y, ColW - 2, 1, ' ', Value, bg);
        _terminal.Text(_boxX + RightOff, y, Truncate(recipe.DisplayName, ColW - 24), can ? Value : Disabled, bg);
        _terminal.Text(_boxX + RightOff + ColW - 22, y, new string('◆', Difficulty(recipe)), can ? Title : Disabled, bg);
        _terminal.Text(_boxX + RightOff + ColW - 15, y, $"-{Cost(recipe)}", can ? Label : Disabled, bg);
        _terminal.Text(_boxX + RightOff + ColW - 10, y, "[Make]", can ? (hov ? OkFg : Value) : Disabled, can ? (hov ? OkHovBg : OkBg) : bg);
    }

    private void DrawBox(int innerRows)
    {
        int h = innerRows + 2;
        _boxX = Math.Max(0, (_terminal.Width - BoxW) / 2);
        _boxY = Math.Max(0, (_terminal.Height - h) / 2);
        _terminal.FillRect(_boxX, _boxY, BoxW, h, ' ', Value, Bg);
        int x1 = _boxX + BoxW - 1, y1 = _boxY + h - 1;
        for (int x = _boxX; x <= x1; x++) { _terminal.SetCell(x, _boxY, '─', Border, Bg); _terminal.SetCell(x, y1, '─', Border, Bg); }
        for (int yy = _boxY; yy <= y1; yy++) { _terminal.SetCell(_boxX, yy, '│', Border, Bg); _terminal.SetCell(x1, yy, '│', Border, Bg); }
        _terminal.SetCell(_boxX, _boxY, '┌', Border, Bg); _terminal.SetCell(x1, _boxY, '┐', Border, Bg);
        _terminal.SetCell(_boxX, y1, '└', Border, Bg);    _terminal.SetCell(x1, y1, '┘', Border, Bg);
    }

    private void Separator(int y)
    {
        for (int x = _boxX + 1; x < _boxX + BoxW - 1; x++) _terminal.SetCell(x, y, '─', Sep, Bg);
        _terminal.SetCell(_boxX, y, '├', Border, Bg);
        _terminal.SetCell(_boxX + BoxW - 1, y, '┤', Border, Bg);
    }

    private void Centered(int y, string text, Vector4 fg)
    {
        if (text.Length == 0) return;
        _terminal.Text(_boxX + (BoxW - text.Length) / 2, y, text, fg, Bg);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : (max <= 1 ? s.Substring(0, Math.Max(0, max)) : s.Substring(0, max - 1) + "…");

    /// <summary>The menu as text lines, for <c>inspect crafting</c>.</summary>
    public IEnumerable<string> CliLines()
    {
        yield return $"crafting trade={_trade.ModusMentisId} material={Material.Label()} pool={Pool} dice={Dice} station=\"{_station.DisplayName}\"";
        foreach (var c in Contributions)
            yield return $"crafting dice-from {(c.IsTool ? "tool" : "trade")} dice={c.Dice} name=\"{c.Label}\"";
        if (LastRoll is { } roll)
            yield return $"crafting last-roll dice={roll.Dice} sixes={roll.Sixes} needed={roll.Difficulty} result={(roll.Succeeded ? "made" : "spoiled")}";
        foreach (var i in Breakables())
            yield return $"crafting break item={i.ItemId} units={ItemMaterials.Units(i)} name=\"{i.DisplayName}\"";
        foreach (var r in _recipes)
            yield return $"crafting make item={r.ItemId} cost={Cost(r)} difficulty={Difficulty(r)} price={r.PriceReference} can={(CanMake(r) ? "yes" : "no")} name=\"{r.DisplayName}\"";
        if (_message.Length > 0) yield return $"crafting message=\"{_message}\"";
    }
}
