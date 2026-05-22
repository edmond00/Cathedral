using System;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Bottom-center box displayed during <see cref="GameMode.Traveling"/>.
/// Shows the current biome, a progress bar for trip vital-heat, and a brief
/// flash of each humor symbol as it is consumed.
/// </summary>
public sealed class TravelProgressRenderer
{
    private readonly TerminalHUD _terminal;

    // Layout — reuse TravelUI box dimensions
    private int _boxX, _boxY;
    private const int BoxW = Config.TravelUI.BoxWidth;   // 40
    private const int BoxH = Config.TravelUI.BoxHeight;  // 12

    // Trip state (set by controller before/during travel)
    private string  _biomeName      = "—";
    private float   _totalRequired  = 1f;
    private float   _consumedNet    = 0f;

    // Flash animation for the currently consumed humor
    private BodyHumor? _flashHumor  = null;
    private float      _flashTimer  = 0f;
    private const float FlashDuration = 0.35f;

    // Track painted area for Erase()
    private bool _painted;

    // Colors
    private static readonly Vector4 BgColor        = Config.TravelUI.BackgroundColor;
    private static readonly Vector4 BorderColor    = Config.TravelUI.BorderColor;
    private static readonly Vector4 TitleColor     = Config.TravelUI.TitleColor;
    private static readonly Vector4 LabelColor     = Config.TravelUI.LabelColor;
    private static readonly Vector4 ValueColor     = Config.TravelUI.ValueColor;
    private static readonly Vector4 BarFillColor   = new(0.72f, 0.58f, 0.22f, 1.0f); // blood amber
    private static readonly Vector4 BarEmptyColor  = Config.Colors.DarkGray35;

    public TravelProgressRenderer(TerminalHUD terminal)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Called when travel begins to initialise trip totals.</summary>
    public void StartTrip(float totalVhRequired)
    {
        _totalRequired = MathF.Max(1f, totalVhRequired);
        _consumedNet   = 0f;
        _flashHumor    = null;
        _flashTimer    = 0f;
    }

    /// <summary>
    /// Called by the controller on each travel step to update biome and register
    /// a consumed humor for the flash animation.
    /// </summary>
    /// <summary>
    /// Called once per frame by the controller when a humor is consumed.
    /// Each call replaces the current flash so each humor is visible for one frame.
    /// </summary>
    public void RegisterConsumption(string biomeName, BodyHumor humor, float tripVhConsumedNet)
    {
        _biomeName   = biomeName;
        _consumedNet = tripVhConsumedNet;
        _flashHumor  = humor;
        _flashTimer  = FlashDuration;
    }

    /// <summary>Advance the flash timer each frame.</summary>
    public void Update(float deltaTime)
    {
        if (_flashTimer > 0f)
            _flashTimer = MathF.Max(0f, _flashTimer - deltaTime);
    }

    /// <summary>Render the travel-progress box.</summary>
    public void Draw()
    {
        _boxX = (_terminal.Width  - BoxW) / 2;
        _boxY =  _terminal.Height - BoxH - Config.TravelUI.BoxBottomMargin;

        // Background + border
        FillRect(_boxX, _boxY, BoxW, BoxH, BgColor);
        DrawBox(_boxX, _boxY, BoxW, BoxH, BorderColor, BgColor);

        int innerL   = _boxX + 2;
        int innerW   = BoxW - 4;     // usable text width inside border + padding
        int contentY = _boxY + 1;

        // Title
        const string title = "── TRAVELING ──";
        int titleX = _boxX + (BoxW - title.Length) / 2;
        _terminal.Text(titleX, contentY, title, TitleColor, BgColor);

        // Biome row
        _terminal.Text(innerL, contentY + 2, "Biome:", LabelColor, BgColor);
        string biomeTrunc = Truncate(_biomeName, innerW - 8);
        _terminal.Text(innerL + 7, contentY + 2, biomeTrunc, ValueColor, BgColor);

        // Progress bar (row contentY+4)
        DrawProgressBar(innerL, contentY + 4, innerW);

        // VH counter row (contentY+6, with an empty line at contentY+5)
        string vhText = $"{_consumedNet:F1} / {_totalRequired:F1} VH";
        _terminal.Text(innerL, contentY + 6, vhText, ValueColor, BgColor);

        // Humor flash (right-aligned on the same row, contentY+6)
        if (_flashTimer > 0f && _flashHumor != null)
        {
            string sign   = _flashHumor.VitalHeat >= 0 ? "+" : "";
            string flash  = $"{_flashHumor.Symbol} {_flashHumor.Name} {sign}{_flashHumor.VitalHeat}";
            flash = Truncate(flash, innerW - vhText.Length - 1);
            int fx = _boxX + BoxW - 2 - flash.Length;
            if (fx > innerL + vhText.Length + 1)
                _terminal.Text(fx, contentY + 6, flash, _flashHumor.Color, BgColor);
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

    private void DrawProgressBar(int x, int y, int width)
    {
        float progress = _totalRequired > 0f
            ? Math.Clamp(_consumedNet / _totalRequired, 0f, 1f)
            : 0f;
        int filled = (int)(progress * width);

        for (int i = 0; i < width; i++)
        {
            bool isFilled = i < filled;
            _terminal.SetCell(x + i, y,
                isFilled ? '█' : '░',
                isFilled ? BarFillColor : BarEmptyColor,
                BgColor);
        }
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
