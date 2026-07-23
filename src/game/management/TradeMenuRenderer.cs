using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;

namespace Cathedral.Game.Management;

/// <summary>
/// Buy/sell menu, rendered as a centered bordered box (black interior, transparent surround so the
/// 3D world stays visible behind it). Shows the NPC's catalogue for one direction; the player
/// stages quantities with −/+ controls and confirms the exchange. Validates inventory space
/// (buying) and owned quantity (selling), and enforces strict per-denomination affordability.
/// ESC is NOT handled here — the launcher opens the pause menu.
/// </summary>
public sealed class TradeMenuRenderer
{
    // ── Layout ────────────────────────────────────────────────────
    private const int BoxW      = 64;
    private const int ButtonGap = 6;

    // Column offsets relative to the box's left edge.
    private const int NameOff  = 2;
    private const int PriceOff = 34;
    private const int MinusOff = 43;
    private const int QtyOff   = 47;
    private const int PlusOff  = 51;
    private const int OwnedOff = 57;

    // ── Colours ───────────────────────────────────────────────────
    private static readonly Vector4 Outside   = Config.Colors.Transparent;   // world shows through
    private static readonly Vector4 Bg        = Config.Colors.Black;
    private static readonly Vector4 Border    = Config.Colors.DarkYellowGrey;
    private static readonly Vector4 Title     = Config.Colors.BrightYellow;
    private static readonly Vector4 Label     = Config.Colors.MediumGray60;
    private static readonly Vector4 Value     = Config.Colors.LightGray75;
    private static readonly Vector4 Sep       = Config.Colors.DarkGray35;
    private static readonly Vector4 RowHovBg  = Config.Colors.DarkGray20;
    private static readonly Vector4 BtnFg     = Config.TravelUI.ClearButtonTextColor;
    private static readonly Vector4 BtnBg     = Config.TravelUI.ClearButtonBackgroundColor;
    private static readonly Vector4 BtnHovFg  = Config.TravelUI.ClearButtonHoverTextColor;
    private static readonly Vector4 BtnHovBg  = Config.TravelUI.ClearButtonHoverBackgroundColor;
    private static readonly Vector4 OkFg      = Config.TravelUI.TravelButtonTextColor;
    private static readonly Vector4 OkBg      = Config.TravelUI.TravelButtonBackgroundColor;
    private static readonly Vector4 OkHovBg   = Config.TravelUI.TravelButtonHoverBackgroundColor;
    private static readonly Vector4 Disabled  = Config.Colors.DarkGray40;
    private static readonly Vector4 Warning   = Config.Colors.OrangeYellow;

    // ── Dependencies ──────────────────────────────────────────────
    private readonly TerminalHUD     _terminal;
    private readonly PartyMember     _member;
    private readonly Party           _party;
    private readonly NpcEntity       _npc;
    private readonly TradeMode       _mode;
    private readonly NpcTradeCatalog? _catalog;
    private readonly int[]           _staged;

    // ── State ─────────────────────────────────────────────────────
    private string _message = string.Empty;
    private int    _hoverMouseX = -1, _hoverMouseY = -1;

    // Box geometry + hit rects, computed each Render and reused by hit-testing.
    private int _boxX, _boxY, _boxH;
    private int _rowY0 = int.MinValue;
    private int _buttonsRow = int.MinValue;
    private int _leaveX0, _leaveX1;
    private int _confirmX0, _confirmX1;

    /// <summary>Set once the player confirms or leaves the menu.</summary>
    public bool IsComplete { get; private set; }

    public TradeMenuRenderer(TerminalHUD terminal, Protagonist protagonist, NpcEntity npc, TradeMode mode)
    {
        _terminal = terminal;
        _member   = protagonist;
        _party    = protagonist.Party;
        _npc      = npc;
        _mode     = mode;
        _catalog  = mode == TradeMode.Buy ? npc.BuyCatalog : npc.SellCatalog;
        _staged   = new int[_catalog?.Offers.Count ?? 0];
    }

    private IReadOnlyList<TradeOffer> Offers => _catalog?.Offers ?? Array.Empty<TradeOffer>();

    // ═══════════════════════════════════════════════════════════════
    // Input
    // ═══════════════════════════════════════════════════════════════

    public void OnMouseMove(int x, int y) { _hoverMouseX = x; _hoverMouseY = y; }

    public void OnMouseClick(int x, int y)
    {
        // Buttons row.
        if (y == _buttonsRow)
        {
            if (x >= _confirmX0 && x < _confirmX1)
            {
                if (CanConfirm()) ApplyAndClose();
                return;
            }
            if (x >= _leaveX0 && x < _leaveX1) { IsComplete = true; return; }
        }

        // −/+ per row.
        for (int i = 0; i < Offers.Count; i++)
        {
            if (y != _rowY0 + i) continue;

            if (x >= _boxX + MinusOff && x < _boxX + MinusOff + 3) { Decrement(i); return; }
            if (x >= _boxX + PlusOff  && x < _boxX + PlusOff  + 3) { Increment(i); return; }
        }
    }

    private void Decrement(int i)
    {
        if (_staged[i] > 0) _staged[i]--;
        _message = string.Empty;
    }

    private void Increment(int i)
    {
        var offer = Offers[i];
        if (_mode == TradeMode.Buy)
        {
            if (!AffordableWith(WithExtra(i)))
            {
                _message = $"Not enough {offer.Coin.ToString().ToLowerInvariant()} coins.";
                return;
            }
            if (!FitsWith(WithExtra(i)))
            {
                _message = $"No room in your inventory for another {offer.Prototype.DisplayName.ToLowerInvariant()}.";
                return;
            }
            _staged[i]++;
            _message = string.Empty;
        }
        else // Sell
        {
            var (sellable, total, anyFull) = OwnedInfo(offer);
            if (_staged[i] >= sellable)
            {
                _message = sellable == 0 && anyFull
                    ? $"Cannot sell a {offer.Prototype.DisplayName.ToLowerInvariant()} that still holds something."
                    : total == 0
                        ? $"You carry no {offer.Prototype.DisplayName.ToLowerInvariant()} to sell."
                        : $"You only carry {sellable} {offer.Prototype.DisplayName.ToLowerInvariant()}.";
                return;
            }
            _staged[i]++;
            _message = string.Empty;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Validation
    // ═══════════════════════════════════════════════════════════════

    private int[] WithExtra(int i)
    {
        var q = (int[])_staged.Clone();
        q[i]++;
        return q;
    }

    /// <summary>True when the party can afford the staged buys, grouped by coin denomination.</summary>
    private bool AffordableWith(int[] q)
    {
        var sums = new Dictionary<CoinType, int>();
        for (int i = 0; i < Offers.Count; i++)
        {
            if (q[i] <= 0) continue;
            var c = Offers[i].Coin;
            sums[c] = sums.GetValueOrDefault(c) + q[i] * Offers[i].UnitPrice;
        }
        return sums.All(kv => _party.CanAfford(kv.Key, kv.Value));
    }

    /// <summary>True when every staged bought unit can be placed (virtual inventory simulation).</summary>
    private bool FitsWith(int[] q)
    {
        var v = new VirtualInventory(_member);
        for (int i = 0; i < Offers.Count; i++)
            for (int k = 0; k < q[i]; k++)
                if (!v.TryPlace(Offers[i].Prototype)) return false;
        return true;
    }

    /// <summary>(sellable units, total owned of this type, whether any owned instance is a non-empty container).</summary>
    private (int sellable, int total, bool anyFull) OwnedInfo(TradeOffer offer)
    {
        var type = offer.Prototype.GetType();
        var owned = _member.GetAllItems().Where(it => it.GetType() == type).ToList();
        int total = owned.Count;
        int full  = owned.Count(it => it is ContainerItem c && c.Contents.Count > 0);
        return (total - full, total, full > 0);
    }

    private bool CanConfirm()
    {
        if (Offers.Count == 0) return false;
        if (_staged.All(q => q == 0)) return false;
        if (_mode == TradeMode.Buy)
            return AffordableWith(_staged) && FitsWith(_staged);
        return true; // selling: capped at increment time; always valid
    }

    // ═══════════════════════════════════════════════════════════════
    // Apply
    // ═══════════════════════════════════════════════════════════════

    private void ApplyAndClose()
    {
        if (_mode == TradeMode.Buy)
        {
            for (int i = 0; i < Offers.Count; i++)
            {
                var offer = Offers[i];
                for (int k = 0; k < _staged[i]; k++)
                {
                    if (!_party.TrySpend(offer.Coin, offer.UnitPrice)) break;        // funds guard
                    var fresh = ItemRegistry.NewInstance(offer.Prototype);
                    if (!_member.AcquireItem(fresh)) { _party.Add(offer.Coin, offer.UnitPrice); break; } // space guard → refund
                }
            }
        }
        else // Sell
        {
            for (int i = 0; i < Offers.Count; i++)
            {
                var offer = Offers[i];
                int toSell = _staged[i];
                if (toSell <= 0) continue;

                var type = offer.Prototype.GetType();
                var removable = _member.GetAllItems()
                    .Where(it => it.GetType() == type && !(it is ContainerItem c && c.Contents.Count > 0))
                    .Take(toSell)
                    .ToList();

                foreach (var item in removable)
                {
                    if (_member.RemoveItem(item))
                        _party.Add(offer.Coin, offer.UnitPrice);
                }
            }
        }

        IsComplete = true;
    }

    // ═══════════════════════════════════════════════════════════════
    // Render
    // ═══════════════════════════════════════════════════════════════

    public void Render()
    {
        // Transparent surround: the (dimmed) 3D world stays visible outside the box.
        _terminal.Fill(' ', Config.Colors.White, Outside);

        string title = _mode == TradeMode.Buy
            ? $"Buying from {_npc.DisplayName}"
            : $"Selling to {_npc.DisplayName}";

        if (Offers.Count == 0)
        {
            // title / sep / notice / sep / buttons
            DrawBox(5);
            int ey = _boxY + 1;
            CenteredInBox(ey++, Truncate(title, BoxW - 4), Title, Bg);
            DrawSeparator(ey++);
            CenteredInBox(ey++, Truncate($"{_npc.DisplayName} has nothing to trade.", BoxW - 4), Label, Bg);
            DrawSeparator(ey++);
            DrawButtons(ey, withConfirm: false);
            return;
        }

        // title / wallet / sep / header / sep / items… / sep / [total] / message / sep / buttons
        bool showTotal = _mode == TradeMode.Buy;
        DrawBox(9 + Offers.Count + (showTotal ? 1 : 0));

        int y = _boxY + 1;
        CenteredInBox(y++, Truncate(title, BoxW - 4), Title, Bg);
        RenderWallet(y++);
        DrawSeparator(y++);

        // Column headers.
        _terminal.Text(_boxX + NameOff,  y, "Item",  Label, Bg);
        _terminal.Text(_boxX + PriceOff, y, "Price", Label, Bg);
        _terminal.Text(_boxX + QtyOff - 1, y, "Qty", Label, Bg);
        if (_mode == TradeMode.Sell) _terminal.Text(_boxX + OwnedOff, y, "Have", Label, Bg);
        y++;
        DrawSeparator(y++);

        _rowY0 = y;
        for (int i = 0; i < Offers.Count; i++)
            RenderOfferRow(i, y++);

        DrawSeparator(y++);
        if (showTotal) RenderTotals(y++);
        CenteredInBox(y, Truncate(_message, BoxW - 4), Warning, Bg);   // reserved even when empty
        y++;
        DrawSeparator(y++);
        DrawButtons(y, withConfirm: true);
    }

    private void RenderOfferRow(int i, int y)
    {
        var offer = Offers[i];

        // Hovering anywhere on the row highlights it.
        bool rowHov = _hoverMouseY == y && _hoverMouseX > _boxX && _hoverMouseX < _boxX + BoxW - 1;
        Vector4 bg = rowHov ? RowHovBg : Bg;
        _terminal.FillRect(_boxX + 1, y, BoxW - 2, 1, ' ', Value, bg);

        _terminal.Text(_boxX + NameOff, y, Truncate(offer.Prototype.DisplayName, PriceOff - NameOff - 1), Value, bg);

        _terminal.Text(_boxX + PriceOff, y, $"{offer.UnitPrice}", Value, bg);
        _terminal.SetCell(_boxX + PriceOff + $"{offer.UnitPrice}".Length, y, CoinGlyph(offer.Coin), CoinColor(offer.Coin), bg);

        // − qty +
        bool canDec = _staged[i] > 0;
        bool canInc = _mode == TradeMode.Buy
            ? AffordableWith(WithExtra(i)) && FitsWith(WithExtra(i))
            : _staged[i] < OwnedInfo(offer).sellable;

        DrawStepButton(_boxX + MinusOff, y, "[-]", canDec, bg);
        _terminal.Text(_boxX + QtyOff, y, $"{_staged[i]}", _staged[i] > 0 ? Title : Value, bg);
        DrawStepButton(_boxX + PlusOff, y, "[+]", canInc, bg);

        if (_mode == TradeMode.Sell)
            _terminal.Text(_boxX + OwnedOff, y, $"{OwnedInfo(offer).sellable}", Label, bg);
    }

    private void RenderTotals(int y)
    {
        var sums = new Dictionary<CoinType, int>();
        for (int i = 0; i < Offers.Count; i++)
        {
            if (_staged[i] <= 0) continue;
            var c = Offers[i].Coin;
            sums[c] = sums.GetValueOrDefault(c) + _staged[i] * Offers[i].UnitPrice;
        }
        if (sums.Count == 0)
        {
            CenteredInBox(y, "Total: —", Label, Bg);
            return;
        }

        string total = string.Join("  ", sums.Select(kv => $"{kv.Value}{CoinGlyph(kv.Key)}"));
        CenteredInBox(y, $"Total: {total}", Value, Bg);
    }

    /// <summary>Centered party purse, one colored segment per denomination.</summary>
    private void RenderWallet(int y)
    {
        (string text, Vector4 color)[] segs =
        {
            ($"{_party.Gold}{Config.Symbols.GoldCoinSymbol}",     Config.Colors.CoinGold),
            ($"{_party.Silver}{Config.Symbols.SilverCoinSymbol}", Config.Colors.CoinSilver),
            ($"{_party.Copper}{Config.Symbols.CopperCoinSymbol}", Config.Colors.CoinCopper),
        };
        const int gap = 2;
        int len = segs.Sum(s => s.text.Length) + gap * (segs.Length - 1);
        int x = _boxX + (BoxW - len) / 2;
        foreach (var s in segs)
        {
            _terminal.Text(x, y, s.text, s.color, Bg);
            x += s.text.Length + gap;
        }
    }

    private void DrawButtons(int y, bool withConfirm)
    {
        const string leaveLabel = "[ Leave ]";
        _buttonsRow = y;

        if (!withConfirm)
        {
            _leaveX0 = _boxX + (BoxW - leaveLabel.Length) / 2;
            _leaveX1 = _leaveX0 + leaveLabel.Length;
            _confirmX0 = _confirmX1 = int.MinValue;
            bool h = _hoverMouseY == y && _hoverMouseX >= _leaveX0 && _hoverMouseX < _leaveX1;
            _terminal.Text(_leaveX0, y, leaveLabel, h ? BtnHovFg : BtnFg, h ? BtnHovBg : BtnBg);
            return;
        }

        string confirmLabel = _mode == TradeMode.Buy ? "[ Confirm purchase ]" : "[ Confirm sale ]";
        int total = leaveLabel.Length + ButtonGap + confirmLabel.Length;
        int x = _boxX + (BoxW - total) / 2;

        _leaveX0 = x; _leaveX1 = x + leaveLabel.Length;
        bool hovLeave = _hoverMouseY == y && _hoverMouseX >= _leaveX0 && _hoverMouseX < _leaveX1;
        _terminal.Text(_leaveX0, y, leaveLabel, hovLeave ? BtnHovFg : BtnFg, hovLeave ? BtnHovBg : BtnBg);

        bool enabled = CanConfirm();
        _confirmX0 = _leaveX1 + ButtonGap; _confirmX1 = _confirmX0 + confirmLabel.Length;
        bool hovOk = enabled && _hoverMouseY == y && _hoverMouseX >= _confirmX0 && _hoverMouseX < _confirmX1;
        Vector4 fg = enabled ? OkFg : Disabled;
        Vector4 bg = enabled ? (hovOk ? OkHovBg : OkBg) : Bg;
        _terminal.Text(_confirmX0, y, confirmLabel, fg, bg);
    }

    private void DrawStepButton(int x, int y, string label, bool enabled, Vector4 rowBg)
    {
        bool hov = enabled && _hoverMouseY == y && _hoverMouseX >= x && _hoverMouseX < x + label.Length;
        Vector4 fg = enabled ? (hov ? BtnHovFg : Value) : Disabled;
        Vector4 bg = hov ? BtnHovBg : rowBg;
        _terminal.Text(x, y, label, fg, bg);
    }

    // ═══════════════════════════════════════════════════════════════
    // Box widgets
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Fills and frames the centered box; <paramref name="innerRows"/> excludes the borders.</summary>
    private void DrawBox(int innerRows)
    {
        _boxH = innerRows + 2;
        _boxX = Math.Max(0, (_terminal.Width  - BoxW)  / 2);
        _boxY = Math.Max(0, (_terminal.Height - _boxH) / 2);

        _terminal.FillRect(_boxX, _boxY, BoxW, _boxH, ' ', Value, Bg);

        int x1 = _boxX + BoxW - 1, y1 = _boxY + _boxH - 1;
        for (int x = _boxX; x <= x1; x++)
        {
            _terminal.SetCell(x, _boxY, '─', Border, Bg);
            _terminal.SetCell(x, y1,    '─', Border, Bg);
        }
        for (int y = _boxY; y <= y1; y++)
        {
            _terminal.SetCell(_boxX, y, '│', Border, Bg);
            _terminal.SetCell(x1,    y, '│', Border, Bg);
        }
        _terminal.SetCell(_boxX, _boxY, '┌', Border, Bg);
        _terminal.SetCell(x1,    _boxY, '┐', Border, Bg);
        _terminal.SetCell(_boxX, y1,    '└', Border, Bg);
        _terminal.SetCell(x1,    y1,    '┘', Border, Bg);
    }

    /// <summary>Horizontal rule across the box, optionally with a centered caption.</summary>
    private void DrawSeparator(int y, string? caption = null)
    {
        for (int x = _boxX + 1; x < _boxX + BoxW - 1; x++)
            _terminal.SetCell(x, y, '─', Sep, Bg);
        _terminal.SetCell(_boxX, y,            '├', Border, Bg);
        _terminal.SetCell(_boxX + BoxW - 1, y, '┤', Border, Bg);
        if (caption != null)
            CenteredInBox(y, $" {caption} ", Label, Bg);
    }

    private void CenteredInBox(int y, string text, Vector4 fg, Vector4 bg)
    {
        if (text.Length == 0) return;
        int x = _boxX + (BoxW - text.Length) / 2;
        _terminal.Text(x, y, text, fg, bg);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : (max <= 1 ? s.Substring(0, Math.Max(0, max)) : s.Substring(0, max - 1) + "…");

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

    // ═══════════════════════════════════════════════════════════════
    // Virtual inventory — mirrors PartyMember.TryAcquireItem placement priority
    // (preferred anchor → any compatible anchor → equipped container) against a
    // snapshot of free capacity, so multiple staged buys are validated together.
    // ═══════════════════════════════════════════════════════════════
    private sealed class VirtualInventory
    {
        private readonly Dictionary<EquipmentAnchor, int> _anchorFree = new();
        private readonly List<(ContainerItem c, int free)> _containers = new();

        public VirtualInventory(PartyMember m)
        {
            foreach (EquipmentAnchor a in Enum.GetValues<EquipmentAnchor>())
                _anchorFree[a] = m.AvailableSlots(a);

            foreach (var list in m.EquippedItems.Values)
                foreach (var it in list)
                    if (it is ContainerItem c)
                        _containers.Add((c, c.AvailableSlots));
        }

        public bool TryPlace(Item item)
        {
            int need = item.SlotCount;

            if (item.PreferredAnchor is { } pref
                && pref.CanAccept(item) && _anchorFree[pref] >= need)
            {
                _anchorFree[pref] -= need;
                return true;
            }

            foreach (EquipmentAnchor a in Enum.GetValues<EquipmentAnchor>())
                if (a.CanAccept(item) && _anchorFree[a] >= need)
                {
                    _anchorFree[a] -= need;
                    return true;
                }

            for (int i = 0; i < _containers.Count; i++)
            {
                var (c, free) = _containers[i];
                if (c.CanContain(item) && free >= need)
                {
                    _containers[i] = (c, free - need);
                    return true;
                }
            }

            return false;
        }
    }
}
