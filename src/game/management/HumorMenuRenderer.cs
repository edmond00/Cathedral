using System;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Management;

/// <summary>
/// Renders the Humors management tab.
///
/// Layout:
///   Row 0-1    : Title "H U M O R S / Q U E U E S"
///   Row 3-62   : Spiral art (64 wide × 60 tall) starting at ArtOffsetX = 18
///   Row 77-99  : Info panel — blank when no hover, humor details when hovering
///
/// The art shows the four interconnected queue spirals. Humor symbols (♉ ♓ ꤁ ☩ ☽)
/// are drawn on top of the base art at the positions defined by the queue position maps.
///
/// Hover detection: moving the mouse over any humor symbol cell shows its details
/// (humor name, symbol, vital heat, transmuting virtue) in the bottom info panel.
/// </summary>
public sealed class HumorMenuRenderer
{
    // ── Dependencies ──────────────────────────────────────────────
    private readonly TerminalHUD _terminal;
    private readonly HumorArtData _artData;
    private readonly HumorQueuePositionMap _heparMap;
    private readonly HumorQueuePositionMap _paunchMap;
    private readonly HumorQueuePositionMap _pulmonesMap;
    private readonly HumorQueuePositionMap _spleenMap;

    // ── Art placement ─────────────────────────────────────────────
    private const int ArtOffsetX = 26;  // centered: 17 + (83 - 64) / 2
    private const int ArtOffsetY = 9;   // centered: 3 + (74 - 61) / 2

    // ── Info panel rows ───────────────────────────────────────────
    private const int InfoPanelStartRow = 77;
    private const int InfoPanelEndRow   = 99;

    // ── Panel background ──────────────────────────────────────────
    private static readonly Vector4 BgColor     = new(0.0f, 0.0f, 0.0f, 1.0f);
    private static readonly Vector4 PanelBg     = new(0.04f, 0.04f, 0.04f, 1.0f);
    private static readonly Vector4 SepColor    = Config.Colors.DarkGray35;
    private static readonly Vector4 TitleColor  = Config.Colors.DarkYellowGrey;
    private static readonly Vector4 HeaderColor = Config.Colors.LightGray75;
    private static readonly Vector4 LabelColor  = Config.Colors.MediumGray60;
    private static readonly Vector4 ValueColor  = Config.Colors.White;

    // ── Hover state ───────────────────────────────────────────────
    private string? _hoveredOrganId;
    private int     _hoveredQueueIndex = -1;

    // ── Constructor ───────────────────────────────────────────────
    public HumorMenuRenderer(
        TerminalHUD terminal,
        HumorArtData artData,
        HumorQueuePositionMap heparMap,
        HumorQueuePositionMap paunchMap,
        HumorQueuePositionMap pulmonesMap,
        HumorQueuePositionMap spleenMap)
    {
        _terminal   = terminal;
        _artData    = artData;
        _heparMap   = heparMap;
        _paunchMap  = paunchMap;
        _pulmonesMap = pulmonesMap;
        _spleenMap  = spleenMap;
    }

    // ─────────────────────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────────────────────

    /// <summary>Full render of the Humors view for the given party member.</summary>
    public void Render(PartyMember member)
    {
        RenderTitle();
        RenderCornerStats(member);
        RenderBaseArt();
        RenderHumorOverlay(member);
        RenderInfoPanel(member);
    }

    /// <summary>
    /// Process mouse hover. Returns true if hover state changed (caller should re-render).
    /// </summary>
    public bool ProcessHover(int screenX, int screenY)
    {
        int artX = screenX - ArtOffsetX;
        int artY = screenY - ArtOffsetY;

        string? newOrgan = null;
        int     newIdx   = -1;

        if (artX >= 0 && artX < _artData.Width && artY >= 0 && artY < _artData.Height)
        {
            (newOrgan, newIdx) = HitTestQueue(artX, artY);
        }

        if (newOrgan == _hoveredOrganId && newIdx == _hoveredQueueIndex) return false;

        _hoveredOrganId    = newOrgan;
        _hoveredQueueIndex = newIdx;
        return true;
    }

    /// <summary>Clear hover state.</summary>
    public void ClearHover()
    {
        _hoveredOrganId    = null;
        _hoveredQueueIndex = -1;
    }

    // ─────────────────────────────────────────────────────────────
    // Private rendering helpers
    // ─────────────────────────────────────────────────────────────

    private void RenderTitle()
    {
        // Row 0: full-width separator
        for (int x = 17; x < Config.Terminal.MainWidth; x++)
            _terminal.SetCell(x, 0, ' ', BgColor, BgColor);

        // Row 1: title centered in available area (cols 17-99 = 83 wide)
        const string title = "H U M O R S   /   Q U E U E S";
        int availW = Config.Terminal.MainWidth - 17;
        int tx = 17 + (availW - title.Length) / 2;
        _terminal.Text(tx, 1, title, TitleColor, BgColor);

        // Row 2: thin separator
        for (int x = 17; x < Config.Terminal.MainWidth; x++)
            _terminal.SetCell(x, 2, '─', SepColor, BgColor);
    }

    private void RenderCornerStats(PartyMember member)
    {
        // Top-right corner, rows 4-5. Art starts at row 9 so these rows are free.
        // Layout: label at col 74, value right-aligned at col 98.
        const int labelCol = 74;
        const int valueCol = 87;
        const int row1 = 4;
        const int row2 = 6;

        var qs = member.HumorQueues;

        // ── Vital heat ─────────────────────────────────────────────
        int vitalHeat = qs.TotalVitalHeat;
        string vhStr = (vitalHeat > 0 ? "+" : "") + vitalHeat;
        Vector4 vhColor = vitalHeat > 0 ? Config.Colors.BrightYellow
                        : vitalHeat < 0 ? Config.Colors.MediumGray60
                        : LabelColor;

        _terminal.Text(labelCol, row1, "VITAL HEAT", LabelColor, BgColor);
        _terminal.Text(valueCol, row1, vhStr, vhColor, BgColor);

        // ── Black bile % ──────────────────────────────────────────
        int totalSlots = HumorQueue.Capacity * 4;
        int bbPct = qs.TotalBlackBileCount * 100 / totalSlots;
        string bbStr = bbPct + "%";
        Vector4 bbColor = bbPct >= 40 ? Config.Colors.BrightYellow
                        : bbPct >= 20 ? Config.Colors.DarkYellowGrey
                        : LabelColor;

        _terminal.Text(labelCol, row2, "BLACK BILE", LabelColor, BgColor);
        _terminal.Text(valueCol, row2, bbStr, bbColor, BgColor);
    }

    private void RenderBaseArt()
    {
        // Clear art area
        for (int ty = ArtOffsetY; ty < ArtOffsetY + _artData.Height && ty < Config.Terminal.MainHeight; ty++)
            for (int tx = ArtOffsetX; tx < ArtOffsetX + _artData.Width && tx < Config.Terminal.MainWidth; tx++)
                _terminal.SetCell(tx, ty, ' ', BgColor, BgColor);

        // Draw art cells
        for (int ay = 0; ay < _artData.Height; ay++)
        {
            for (int ax = 0; ax < _artData.Width; ax++)
            {
                char glyph = _artData.ArtGrid[ax, ay];
                if (glyph == ' ' || glyph == '\0') continue;

                int layerIdx = _artData.LayerGrid[ax, ay];
                if (layerIdx < 0) continue; // layer -1 means background

                var color = _artData.GetLayerColorAt(ax, ay);

                int tx = ArtOffsetX + ax;
                int ty = ArtOffsetY + ay;
                if (tx < Config.Terminal.MainWidth && ty < Config.Terminal.MainHeight)
                    _terminal.SetCell(tx, ty, glyph, color, BgColor);
            }
        }
    }

    private void RenderHumorOverlay(PartyMember member)
    {
        RenderQueueOverlay(member.HumorQueues.Hepar,    _heparMap);
        RenderQueueOverlay(member.HumorQueues.Paunch,   _paunchMap);
        RenderQueueOverlay(member.HumorQueues.Pulmones, _pulmonesMap);
        RenderQueueOverlay(member.HumorQueues.Spleen,   _spleenMap);
    }

    private void RenderQueueOverlay(HumorQueue queue, HumorQueuePositionMap posMap)
    {
        for (int i = 0; i < HumorQueue.Capacity; i++)
        {
            if (!posMap.TryGetPosition(i, out int ax, out int ay)) continue;

            var humor = queue.Items[i];
            int tx = ArtOffsetX + ax;
            int ty = ArtOffsetY + ay;
            if (tx < 0 || ty < 0 || tx >= Config.Terminal.MainWidth || ty >= Config.Terminal.MainHeight) continue;

            bool isHovered = _hoveredOrganId == queue.OrganId && _hoveredQueueIndex == i;
            var  fgColor   = isHovered ? BgColor : humor.Color;
            var  bgCell    = isHovered ? humor.Color : BgColor;

            _terminal.SetCell(tx, ty, humor.Symbol, fgColor, bgCell);
        }
    }

    private void RenderInfoPanel(PartyMember member)
    {
        // Clear info area
        for (int r = InfoPanelStartRow; r <= InfoPanelEndRow; r++)
            for (int x = 17; x < Config.Terminal.MainWidth; x++)
                _terminal.SetCell(x, r, ' ', BgColor, PanelBg);

        // Top separator of info panel
        for (int x = 17; x < Config.Terminal.MainWidth; x++)
            _terminal.SetCell(x, InfoPanelStartRow, '─', SepColor, PanelBg);

        if (_hoveredOrganId == null || _hoveredQueueIndex < 0)
        {
            // Show hint
            const string hint = "Hover over a humor symbol to view its properties.";
            _terminal.Text(19, InfoPanelStartRow + 2, hint, LabelColor, PanelBg);
            return;
        }

        var queue = member.HumorQueues.GetByOrganId(_hoveredOrganId);
        if (queue == null) return;

        int idx   = _hoveredQueueIndex;
        var humor = queue.Items[idx];

        int row = InfoPanelStartRow + 1;

        // Header: ORGAN / position tag
        string organLabel = char.ToUpper(_hoveredOrganId[0]) + _hoveredOrganId[1..];
        string headerLine = $"{organLabel.ToUpper()}  ·  Position {idx + 1} / {HumorQueue.Capacity}";
        _terminal.Text(19, row, headerLine, HeaderColor, PanelBg);
        row++;

        // Thin divider
        for (int x = 19; x < 80; x++)
            _terminal.SetCell(x, row, '─', SepColor, PanelBg);
        row++;

        // Humor symbol + name (large display)
        string nameDisplay = $" {humor.Symbol}  {humor.Name.ToUpper()} ";
        _terminal.Text(19, row, nameDisplay, humor.Color, PanelBg);
        row += 2;

        // Special warning for black bile
        if (humor.IsBlackBile)
        {
            _terminal.Text(19, row, "CORRUPTION — cannot be consumed. Purgation required.",
                Config.Colors.DarkYellowGrey, PanelBg);
            row++;
            _terminal.Text(19, row, "Queue fills toward death when uncleansed.",
                Config.Colors.MediumGray60, PanelBg);
            return;
        }

        // Vital heat
        string vhSign  = humor.VitalHeat > 0 ? "+" : "";
        string vhColor = humor.VitalHeat > 0 ? "positive" : humor.VitalHeat < 0 ? "negative" : "neutral";
        Vector4 vhFg   = humor.VitalHeat > 0
            ? Config.Colors.BrightYellow
            : humor.VitalHeat < 0
                ? Config.Colors.MediumGray60
                : LabelColor;

        _terminal.Text(19, row, "Vital Heat:", LabelColor, PanelBg);
        _terminal.Text(32, row, $"{vhSign}{humor.VitalHeat}  ({vhColor})", vhFg, PanelBg);
        row++;

        // Transmuting virtue
        _terminal.Text(19, row, "Transmutation:", LabelColor, PanelBg);
        if (humor.TransmutingVirtue != null)
        {
            _terminal.Text(35, row, humor.TransmutingVirtue.Description, ValueColor, PanelBg);

            // Explain the virtue type
            row++;
            string explanation = humor.TransmutingVirtue switch
            {
                NumericModVirtue nmv => nmv.Modifier < 0
                    ? "Reduces the dice result by a fixed amount on each invocation."
                    : "Increases the dice result by a fixed amount on each invocation.",
                DigitConversionVirtue dcv when dcv.SourceDigit == -1 =>
                    $"Converts any dice face to {dcv.TargetDigit} (worst-case lock).",
                DigitConversionVirtue dcv =>
                    $"Converts face {dcv.SourceDigit} to {dcv.TargetDigit} when that face is rolled.",
                _ => ""
            };
            if (explanation.Length > 0)
                _terminal.Text(21, row, explanation, LabelColor, PanelBg);
        }
        else
        {
            _terminal.Text(35, row, "none", LabelColor, PanelBg);
        }

        // Black bile stack info (if any black bile is present near the back of this queue)
        row += 2;
        int stackDepth = queue.BlackBileStackDepth;
        if (stackDepth > 0)
        {
            Vector4 warningColor = stackDepth >= 20
                ? Config.Colors.BrightYellow
                : Config.Colors.DarkYellowGrey;
            _terminal.Text(19, row, $"Black Bile stack at edge: {stackDepth} / {HumorQueue.Capacity}",
                warningColor, PanelBg);
        }
    }

    // ─────────────────────────────────────────────────────────────
    // Hit testing
    // ─────────────────────────────────────────────────────────────

    private (string? organId, int index) HitTestQueue(int artX, int artY)
    {
        int idx;

        idx = _heparMap.GetIndexAt(artX, artY);
        if (idx >= 0) return ("hepar", idx);

        idx = _paunchMap.GetIndexAt(artX, artY);
        if (idx >= 0) return ("paunch", idx);

        idx = _pulmonesMap.GetIndexAt(artX, artY);
        if (idx >= 0) return ("pulmones", idx);

        idx = _spleenMap.GetIndexAt(artX, artY);
        if (idx >= 0) return ("spleen", idx);

        return (null, -1);
    }

    // ─────────────────────────────────────────────────────────────
    // Color utilities
    // ─────────────────────────────────────────────────────────────

    private static Vector4 BrightenColor(Vector4 c, float factor) =>
        new(Math.Min(1f, c.X * factor),
            Math.Min(1f, c.Y * factor),
            Math.Min(1f, c.Z * factor),
            c.W);
}
