using System;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Bottom-center box displayed during <see cref="GameMode.Traveling"/>.
/// Shows two progress bars: global trip distance and per-location vital-heat,
/// plus a brief flash of each consumed humor.
/// </summary>
public sealed class TravelProgressRenderer
{
    private readonly TerminalHUD _terminal;

    // Layout — reuse TravelUI box dimensions
    private int _boxX, _boxY;
    private const int BoxW = Config.TravelUI.BoxWidth;   // 40
    private const int BoxH = Config.TravelUI.BoxHeight;  // 12

    // Bar geometry (innerW = BoxW - 4 = 36)
    private const int LabelW   = 5;   // "Trip " / "VH   "
    private const int BarW     = 22;  // bar characters
    private const int CounterW = 8;   // right-aligned numeric counter
    // LabelW + BarW + 1 (gap) + CounterW = 5+22+1+8 = 36 == innerW ✓

    // Trip state
    private string   _biomeName     = "—";
    private float    _totalRequired = 1f;

    // Flash animation for the currently consumed humor
    private BodyHumor? _flashHumor = null;
    private float      _flashTimer = 0f;
    private const float FlashDuration = 0.35f;

    // Track painted area for Erase()
    private bool _painted;

    // Colors
    private static readonly Vector4 BgColor          = Config.TravelUI.BackgroundColor;
    private static readonly Vector4 BorderColor      = Config.TravelUI.BorderColor;
    private static readonly Vector4 TitleColor       = Config.TravelUI.TitleColor;
    private static readonly Vector4 LabelColor       = Config.TravelUI.LabelColor;
    private static readonly Vector4 ValueColor       = Config.TravelUI.ValueColor;
    private static readonly Vector4 BarEmptyColor    = Config.Colors.DarkGray35;
    private static readonly Vector4 GlobalBarColor   = new(0.72f, 0.58f, 0.22f, 1.0f); // amber — distance
    private static readonly Vector4 LocalBarColor    = new(0.85f, 0.78f, 0.15f, 1.0f); // golden yellow — vital heat

    public TravelProgressRenderer(TerminalHUD terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Called when travel begins to initialise trip totals.</summary>
    public void StartTrip(float totalVhRequired)
    {
        _totalRequired = MathF.Max(1f, totalVhRequired);
        _flashHumor    = null;
        _flashTimer    = 0f;
    }

    /// <summary>Called once per frame when a humor is consumed.</summary>
    public void RegisterConsumption(string biomeName, BodyHumor humor, float tripVhConsumedNet)
    {
        _biomeName  = biomeName;
        _flashHumor = humor;
        _flashTimer = FlashDuration;
    }

    /// <summary>Advance the flash timer each frame.</summary>
    public void Update(float deltaTime)
    {
        if (_flashTimer > 0f)
            _flashTimer = MathF.Max(0f, _flashTimer - deltaTime);
    }

    /// <summary>Render the travel-progress box.</summary>
    public void Draw(int cellsTraveled, int totalCells, float locationVhConsumed, float locationVhRequired)
    {
        _boxX = (_terminal.Width  - BoxW) / 2;
        _boxY =  _terminal.Height - BoxH - Config.TravelUI.BoxBottomMargin;

        FillRect(_boxX, _boxY, BoxW, BoxH, BgColor);
        DrawBox(_boxX, _boxY, BoxW, BoxH, BorderColor, BgColor);

        int innerL   = _boxX + 2;
        int contentY = _boxY + 1;

        // Title
        const string title = "── TRAVELING ──";
        _terminal.Text(_boxX + (BoxW - title.Length) / 2, contentY, title, TitleColor, BgColor);

        // Biome row
        _terminal.Text(innerL, contentY + 2, "Biome:", LabelColor, BgColor);
        _terminal.Text(innerL + 7, contentY + 2, Truncate(_biomeName, BoxW - 4 - 8), ValueColor, BgColor);

        // Global progress bar — cells traveled / total cells
        float globalProgress = totalCells > 0 ? (float)cellsTraveled / totalCells : 0f;
        string globalCounter = $"{cellsTraveled}/{totalCells}".PadLeft(CounterW);
        DrawBarRow(innerL, contentY + 4, "Trip ", globalProgress, globalCounter, GlobalBarColor);

        // Local progress bar — location VH consumed / required (can retreat, clamped 0..1)
        float localProgress = locationVhRequired > 0f
            ? Math.Clamp(locationVhConsumed / locationVhRequired, 0f, 1f)
            : 0f;
        string localCounter = $"{locationVhConsumed:F1}/{locationVhRequired:F1}".PadLeft(CounterW);
        DrawBarRow(innerL, contentY + 5, "VH   ", localProgress, localCounter, LocalBarColor, '═', '─');

        // Humor flash
        if (_flashTimer > 0f && _flashHumor != null)
        {
            string sign  = _flashHumor.VitalHeat >= 0 ? "+" : "";
            string flash = Truncate($"{_flashHumor.Symbol} {_flashHumor.Name} {sign}{_flashHumor.VitalHeat}",
                                    BoxW - 4);
            _terminal.Text(innerL, contentY + 7, flash, _flashHumor.Color, BgColor);
        }

        _painted = true;
    }

    /// <summary>Erase the box (restore transparent cells).</summary>
    public void Erase()
    {
        if (!_painted) return;
        int bx = (_terminal.Width  - BoxW) / 2;
        int by =  _terminal.Height - BoxH - Config.TravelUI.BoxBottomMargin;
        for (int dy = 0; dy < BoxH; dy++)
            for (int dx = 0; dx < BoxW; dx++)
                _terminal.SetCell(bx + dx, by + dy, ' ', Config.Colors.Transparent, Config.Colors.Transparent);
        _painted = false;
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws one labeled progress bar row.
    /// Layout: [label LabelW][bar BarW][gap 1][counter CounterW]
    /// </summary>
    private void DrawBarRow(int x, int y, string label, float progress, string counter,
        Vector4 fillColor, char filledChar = '█', char emptyChar = '░')
    {
        // Label
        _terminal.Text(x, y, label, LabelColor, BgColor);

        // Bar
        int filled = (int)(Math.Clamp(progress, 0f, 1f) * BarW);
        for (int i = 0; i < BarW; i++)
        {
            bool isFilled = i < filled;
            _terminal.SetCell(x + LabelW + i, y,
                isFilled ? filledChar : emptyChar,
                isFilled ? fillColor : BarEmptyColor,
                BgColor);
        }

        // Counter
        _terminal.Text(x + LabelW + BarW + 1, y, counter, ValueColor, BgColor);
    }

    private void FillRect(int x, int y, int w, int h, Vector4 bg)
    {
        for (int dy = 0; dy < h; dy++)
            for (int dx = 0; dx < w; dx++)
                _terminal.SetCell(x + dx, y + dy, ' ', bg, bg);
    }

    private void DrawBox(int x, int y, int w, int h, Vector4 border, Vector4 bg)
    {
        _terminal.SetCell(x,         y,         '┌', border, bg);
        _terminal.SetCell(x + w - 1, y,         '┐', border, bg);
        _terminal.SetCell(x,         y + h - 1, '└', border, bg);
        _terminal.SetCell(x + w - 1, y + h - 1, '┘', border, bg);
        for (int dx = 1; dx < w - 1; dx++)
        {
            _terminal.SetCell(x + dx, y,         '─', border, bg);
            _terminal.SetCell(x + dx, y + h - 1, '─', border, bg);
        }
        for (int dy = 1; dy < h - 1; dy++)
        {
            _terminal.SetCell(x,         y + dy, '│', border, bg);
            _terminal.SetCell(x + w - 1, y + dy, '│', border, bg);
        }
    }

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max];
}
