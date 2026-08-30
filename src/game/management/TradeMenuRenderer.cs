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

    /// <summary>
    /// Stable identity of the clickable element under (x, y), or null when there is none — the same
    /// contract <c>SettingsMenuRenderer.GetHoveredControlId</c> has, so the controller's one
    /// "tick when the hovered element changes" rule covers this screen too. It had no tick at all:
    /// every button here highlighted in silence, which reads as an unresponsive screen next to the
    /// menus either side of it.
    ///
    /// <para>Mirrors <see cref="OnMouseClick"/>'s regions exactly. Anything that grows a click target
    /// there needs a case here, or that target goes quiet.</para>
    /// </summary>
    public string? GetHoveredControlId(int x, int y)
    {
        if (y == _buttonsRow)
        {
            if (_confirmX0 != int.MinValue && x >= _confirmX0 && x < _confirmX1) return "trade:confirm";
            if (x >= _leaveX0 && x < _leaveX1) return "trade:leave";
        }

        for (int i = 0; i < Offers.Count; i++)
        {
            if (y != _rowY0 + i) continue;
            if (x >= _boxX + MinusOff && x < _boxX + MinusOff + 3) return $"trade:minus:{i}";
            if (x >= _boxX + PlusOff  && x < _boxX + PlusOff  + 3) return $"trade:plus:{i}";
        }

        return null;
    }

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
                if (!v.TryPlaceOffer(Offers[i])) return false;
        return true;
    }

    /// <summary>
    /// Every owned instance matching this offer, ready to hand over. A plain offer matches items of
    /// the same type that are not full containers; a bundle matches a vessel of the right type that
    /// actually holds the right liquid, and hands over both — selling "a bottle of ale" means
    /// parting with the bottle too.
    /// </summary>
    private List<(Item Primary, Item? Vessel)> MatchingStock(TradeOffer offer)
    {
        var held = _member.GetAllItems();

        if (offer.VesselPrototype is null)
        {
            var type = offer.Prototype.GetType();
            return held.Where(it => it.GetType() == type
                                 && !(it is IContainer c && c.Contents.Count > 0))
                       .Select(it => (it, (Item?)null))
                       .ToList();
        }

        var vesselType  = offer.VesselPrototype.GetType();
        var liquidType  = offer.Prototype.GetType();
        var matches = new List<(Item, Item?)>();
        foreach (var candidate in held.Where(it => it.GetType() == vesselType))
        {
            if (candidate is not IContainer vessel) continue;
            var liquid = vessel.Contents.FirstOrDefault(c => c.GetType() == liquidType);
            if (liquid != null) matches.Add((liquid, candidate));
        }
        return matches;
    }

    /// <summary>(sellable units, total owned of this type, whether any owned instance is a non-empty container).</summary>
    private (int sellable, int total, bool anyFull) OwnedInfo(TradeOffer offer)
    {
        if (offer.VesselPrototype is not null)
        {
            int bundles = MatchingStock(offer).Count;
            return (bundles, bundles, false);
        }

        var type = offer.Prototype.GetType();
        var owned = _member.GetAllItems().Where(it => it.GetType() == type).ToList();
        int total = owned.Count;
        int full  = owned.Count(it => it is IContainer c && c.Contents.Count > 0);
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

    /// <summary>
    /// Delivers one unit of an offer. For a bundle the vessel is filled before it is handed over,
    /// so the liquid never has to find a home on its own — placing the bottle first and pouring
    /// afterwards would fail whenever the buyer owns no other vessel.
    /// </summary>
    private bool BuyOne(TradeOffer offer)
    {
        if (offer.VesselPrototype is null)
            return _member.AcquireItem(ItemRegistry.NewInstance(offer.Prototype));

        var vessel = ItemRegistry.NewInstance(offer.VesselPrototype);
        var liquid = ItemRegistry.NewInstance(offer.Prototype);

        if (vessel is not IContainer container || !container.TryAdd(liquid)) return false;
        return _member.AcquireItem(vessel);
    }

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
                    if (!BuyOne(offer)) { _party.Add(offer.Coin, offer.UnitPrice); break; } // space guard → refund
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

                foreach (var (primary, vessel) in MatchingStock(offer).Take(toSell))
                {
                    // Remove the contents first: removing the vessel first would take its contents
                    // out of GetAllItems' reach and strand the liquid.
                    if (!_member.RemoveItem(primary)) continue;
                    if (vessel != null) _member.RemoveItem(vessel);
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
            // (blank) title <break> notice <break> buttons (blank)
            DrawBox(11);
            int ey = _boxY + 2;
            CenteredInBox(ey++, Truncate(title, BoxW - 4), Title, Bg);
            DrawSectionBreak(ref ey);
            CenteredInBox(ey++, Truncate($"{_npc.DisplayName} has nothing to trade.", BoxW - 4), Label, Bg);
            DrawSectionBreak(ref ey);
            DrawButtons(ey, withConfirm: false);
            return;
        }

        // (blank) title / wallet <break> header / rule / blank / items… <break> [total] / message / rule / blank / buttons (blank)
        bool showTotal = _mode == TradeMode.Buy;
        DrawBox(17 + Offers.Count + (showTotal ? 1 : 0));

        int y = _boxY + 2;   // one blank row under the top border
        CenteredInBox(y++, Truncate(title, BoxW - 4), Title, Bg);
        RenderWallet(y++);
        DrawSectionBreak(ref y);

        // Column headers, underlined by their own rule (a table head, not a section break).
        _terminal.Text(_boxX + NameOff,  y, "Item",  Label, Bg);
        _terminal.Text(_boxX + PriceOff, y, "Price", Label, Bg);
        _terminal.Text(_boxX + QtyOff - 1, y, "Qty", Label, Bg);
        if (_mode == TradeMode.Sell) _terminal.Text(_boxX + OwnedOff, y, "Have", Label, Bg);
        y++;
        DrawSeparator(y++);
        y++;

        _rowY0 = y;
        for (int i = 0; i < Offers.Count; i++)
            RenderOfferRow(i, y++);

        DrawSectionBreak(ref y);
        if (showTotal) RenderTotals(y++);
        // Reserved even when empty — this row also serves as the breathing row above the rule below,
        // so the totals never sit two blank rows away from it.
        CenteredInBox(y, Truncate(_message, BoxW - 4), Warning, Bg);
        y++;
        DrawSeparator(y++);
        y++;
        DrawButtons(y, withConfirm: true);
    }

    private void RenderOfferRow(int i, int y)
    {
        var offer = Offers[i];

        // Hovering anywhere on the row highlights it.
        bool rowHov = _hoverMouseY == y && _hoverMouseX > _boxX && _hoverMouseX < _boxX + BoxW - 1;
        Vector4 bg = rowHov ? RowHovBg : Bg;
        _terminal.FillRect(_boxX + 1, y, BoxW - 2, 1, ' ', Value, bg);

        // The offer's own name, not the item's: a bundle is sold as "bottle of ale".
        _terminal.Text(_boxX + NameOff, y, Truncate(offer.DisplayName, PriceOff - NameOff - 1), Value, bg);

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

    /// <summary>Section rule with one blank row of breathing room either side; advances <paramref name="y"/> past all three.</summary>
    private void DrawSectionBreak(ref int y, string? caption = null)
    {
        y++;                        // blank above
        DrawSeparator(y++, caption);
        y++;                        // blank below
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
        private readonly List<(IContainer c, int free)> _containers = new();

        // Deliberately no weight budget: buying is an acquisition like any other, and weight does
        // not gate acquisition — it gates travel. Overspending on heavy goods is allowed, and the
        // consequence is discovered at the travel panel, not at the merchant's stall.
        public VirtualInventory(PartyMember m)
        {
            foreach (EquipmentAnchor a in Enum.GetValues<EquipmentAnchor>())
                _anchorFree[a] = m.AvailableSlots(a);

            foreach (var it in m.GetAllItems())
                if (it is IContainer c)
                    _containers.Add((c, c.AvailableSlots));
        }

        /// <summary>
        /// Places a whole catalogue line. For a bundle the vessel must be placed first and then
        /// registered as available space, because the liquid's only home is the bottle arriving
        /// alongside it — checking the liquid against the player's existing vessels would wrongly
        /// reject a bundle bought by someone carrying nothing.
        /// </summary>
        public bool TryPlaceOffer(TradeOffer offer)
        {
            if (offer.VesselPrototype is null) return TryPlace(offer.Prototype);

            if (!TryPlace(offer.VesselPrototype)) return false;
            if (offer.VesselPrototype is IContainer vessel) AddPendingVessel(vessel);
            return TryPlaceLiquid(offer.Prototype);
        }

        public bool TryPlace(Item item)
        {
            int need = item.SlotCount;
            if (item.IsLiquid) return TryPlaceLiquid(item);

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

        /// <summary>A liquid only ever goes into a vessel — no anchor will take it.</summary>
        private bool TryPlaceLiquid(Item liquid)
        {
            int need = liquid.SlotCount;

            for (int i = 0; i < _containers.Count; i++)
            {
                var (c, free) = _containers[i];
                if (c.Kind == ContainerKind.Vessel && c.CanContain(liquid) && free >= need)
                {
                    _containers[i] = (c, free - need);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Registers a vessel the player does not own yet but is buying in the same transaction, so
        /// a bundle's liquid can be placed into the bottle that arrives with it.
        /// </summary>
        public void AddPendingVessel(IContainer vessel) => _containers.Add((vessel, vessel.ContentSlots));
    }
}
