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
/// Full-screen buy/sell menu. Shows the NPC's catalogue for one direction; the player stages
/// quantities with −/+ controls and confirms the exchange. Validates inventory space (buying)
/// and owned quantity (selling), and enforces strict per-denomination affordability.
/// </summary>
public sealed class TradeMenuRenderer
{
    // ── Layout ────────────────────────────────────────────────────
    private const int TitleRow   = 6;
    private const int WalletRow  = 9;
    private const int HeaderRow  = 12;
    private const int ListRow    = 14;
    private const int RowStride  = 2;
    private const int MessageRow = 90;
    private const int ConfirmRow = 93;
    private const int HintRow    = 96;

    private const int NameX  = 14;
    private const int PriceX = 50;
    private const int MinusX = 62;
    private const int QtyX   = 66;
    private const int PlusX  = 70;
    private const int OwnedX = 74;

    // ── Colours ───────────────────────────────────────────────────
    private static readonly Vector4 Bg        = Config.Colors.Black;
    private static readonly Vector4 Title     = Config.Colors.BrightYellow;
    private static readonly Vector4 Label     = Config.Colors.MediumGray60;
    private static readonly Vector4 Value     = Config.Colors.LightGray75;
    private static readonly Vector4 Sep       = Config.Colors.DarkGray35;
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

    public void OnKeyEscape() => IsComplete = true;

    public void OnMouseClick(int x, int y)
    {
        // Confirm button.
        if (y == ConfirmRow && InConfirmHit(x))
        {
            if (CanConfirm()) ApplyAndClose();
            return;
        }

        // Close button (hint row "[ Leave ]").
        if (y == HintRow && x >= NameX && x < NameX + LeaveLabel.Length + 4)
        {
            IsComplete = true;
            return;
        }

        // −/+ per row.
        for (int i = 0; i < Offers.Count; i++)
        {
            int rowY = ListRow + i * RowStride;
            if (y != rowY) continue;

            if (x >= MinusX && x < MinusX + 3) { Decrement(i); return; }
            if (x >= PlusX  && x < PlusX  + 3) { Increment(i); return; }
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

    private const string LeaveLabel = "Leave";

    public void Render()
    {
        _terminal.Clear();

        string title = _mode == TradeMode.Buy
            ? $"Buying from {_npc.DisplayName}"
            : $"Selling to {_npc.DisplayName}";
        _terminal.CenteredText(TitleRow, title, Title, Bg);

        RenderWallet();

        if (Offers.Count == 0)
        {
            _terminal.CenteredText(ListRow, $"{_npc.DisplayName} has nothing to trade.", Label, Bg);
            DrawLeave();
            return;
        }

        // Column headers.
        _terminal.Text(NameX,  HeaderRow, "Item",  Label, Bg);
        _terminal.Text(PriceX, HeaderRow, "Price", Label, Bg);
        _terminal.Text(MinusX, HeaderRow, "Qty",   Label, Bg);
        if (_mode == TradeMode.Sell) _terminal.Text(OwnedX, HeaderRow, "Have", Label, Bg);
        _terminal.Text(NameX, HeaderRow + 1, new string('─', OwnedX + 6 - NameX), Sep, Bg);

        for (int i = 0; i < Offers.Count; i++)
            RenderOfferRow(i);

        // Totals + message.
        RenderTotals();
        if (!string.IsNullOrEmpty(_message))
            _terminal.CenteredText(MessageRow, _message, Warning, Bg);

        RenderConfirm();
        DrawLeave();
    }

    private void RenderOfferRow(int i)
    {
        var offer = Offers[i];
        int y = ListRow + i * RowStride;

        _terminal.Text(NameX, y, Trunc(offer.Prototype.DisplayName, MinusX - NameX - 1), Value, Bg);

        _terminal.Text(PriceX, y, $"{offer.UnitPrice}", Value, Bg);
        _terminal.SetCell(PriceX + $"{offer.UnitPrice}".Length, y, CoinGlyph(offer.Coin), CoinColor(offer.Coin), Bg);

        // − qty +
        bool canDec = _staged[i] > 0;
        bool canInc = _mode == TradeMode.Buy
            ? AffordableWith(WithExtra(i)) && FitsWith(WithExtra(i))
            : _staged[i] < OwnedInfo(offer).sellable;

        DrawButton(MinusX, y, "[-]", canDec);
        _terminal.Text(QtyX, y, $"{_staged[i]}", _staged[i] > 0 ? Title : Value, Bg);
        DrawButton(PlusX, y, "[+]", canInc);

        if (_mode == TradeMode.Sell)
            _terminal.Text(OwnedX, y, $"{OwnedInfo(offer).sellable}", Label, Bg);
    }

    private void RenderTotals()
    {
        if (_mode != TradeMode.Buy) return;
        var sums = new Dictionary<CoinType, int>();
        for (int i = 0; i < Offers.Count; i++)
        {
            if (_staged[i] <= 0) continue;
            var c = Offers[i].Coin;
            sums[c] = sums.GetValueOrDefault(c) + _staged[i] * Offers[i].UnitPrice;
        }
        if (sums.Count == 0) return;

        string total = string.Join("  ", sums.Select(kv => $"{kv.Value}{CoinGlyph(kv.Key)}"));
        _terminal.CenteredText(MessageRow - 2, $"Total: {total}", Value, Bg);
    }

    private void RenderConfirm()
    {
        bool enabled = CanConfirm();
        string label = _mode == TradeMode.Buy ? "[ Confirm purchase ]" : "[ Confirm sale ]";
        int x = (Config.Terminal.MainWidth - label.Length) / 2;
        bool hov = _hoverMouseY == ConfirmRow && InConfirmHit(_hoverMouseX);

        Vector4 fg = enabled ? OkFg : Disabled;
        Vector4 bg = enabled ? (hov ? OkHovBg : OkBg) : Bg;
        _terminal.Text(x, ConfirmRow, label, fg, bg);
        _confirmX0 = x;
        _confirmX1 = x + label.Length;
    }

    private int _confirmX0, _confirmX1;
    private bool InConfirmHit(int x) => x >= _confirmX0 && x < _confirmX1;

    private void DrawLeave()
    {
        string label = $"[ {LeaveLabel} ]";
        bool hov = _hoverMouseY == HintRow && _hoverMouseX >= NameX && _hoverMouseX < NameX + label.Length;
        _terminal.Text(NameX, HintRow, label, hov ? BtnHovFg : BtnFg, hov ? BtnHovBg : BtnBg);
        _terminal.Text(NameX + label.Length + 2, HintRow, "(or press Esc)", Label, Bg);
    }

    private void DrawButton(int x, int y, string label, bool enabled)
    {
        bool hov = enabled && _hoverMouseY == y && _hoverMouseX >= x && _hoverMouseX < x + label.Length;
        Vector4 fg = enabled ? (hov ? BtnHovFg : Value) : Disabled;
        Vector4 bg = hov ? BtnHovBg : Bg;
        _terminal.Text(x, y, label, fg, bg);
    }

    private void RenderWallet()
    {
        (string text, Vector4 color)[] segs =
        {
            ($"{_party.Gold}{Config.Symbols.GoldCoinSymbol}",   Config.Colors.CoinGold),
            ($"{_party.Silver}{Config.Symbols.SilverCoinSymbol}", Config.Colors.CoinSilver),
            ($"{_party.Copper}{Config.Symbols.CopperCoinSymbol}", Config.Colors.CoinCopper),
        };
        const int gap = 2;
        int len = segs.Sum(s => s.text.Length) + gap * (segs.Length - 1);
        int x = (Config.Terminal.MainWidth - len) / 2;
        foreach (var s in segs)
        {
            _terminal.Text(x, WalletRow, s.text, s.color, Bg);
            x += s.text.Length + gap;
        }
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

    private static string Trunc(string s, int max)
        => s.Length <= max ? s : s[..Math.Max(0, max - 1)] + "…";

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
