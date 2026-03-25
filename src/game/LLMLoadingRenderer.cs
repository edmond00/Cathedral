using System;
using OpenTK.Mathematics;
using Cathedral.Terminal;

namespace Cathedral.Game;

/// <summary>
/// Renders a full-screen LLM loading screen on the TerminalHUD.
/// Shown during GameMode.LLMLoading while the AI model is being loaded.
/// Displays a progress bar, animated spinner, and status message.
/// </summary>
public class LLMLoadingRenderer
{
    private readonly TerminalHUD _terminal;
    private float    _progress = 0f;
    private string   _statusMessage = "Initializing...";
    private string   _modelLabel;
    private int      _spinnerFrame = 0;
    private DateTime _lastSpinnerUpdate = DateTime.Now;

    // Layout
    private const int ProgressBarWidth = 42;

    public LLMLoadingRenderer(TerminalHUD terminal, string modelLabel = "AI Model")
    {
        _terminal   = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _modelLabel = modelLabel;
    }

    /// <summary>
    /// Update the progress and status, then re-render.
    /// Safe to call every frame — only the spinner advances per render tick.
    /// </summary>
    public void Update(float progress, string statusMessage)
    {
        _progress      = Math.Clamp(progress, 0f, 1f);
        _statusMessage = statusMessage ?? _statusMessage;
        Render();
    }

    /// <summary>Re-render without changing progress/status (advances spinner animation).</summary>
    public void Update()
    {
        Render();
    }

    private void Render()
    {
        // Advance spinner at ~10 fps
        if ((DateTime.Now - _lastSpinnerUpdate).TotalMilliseconds >= 100)
        {
            _spinnerFrame     = (_spinnerFrame + 1) % Config.Symbols.LoadingSpinner.Length;
            _lastSpinnerUpdate = DateTime.Now;
        }

        int termW = _terminal.Width;
        int termH = _terminal.Height;

        // Fill background
        _terminal.Fill(' ', Config.Colors.Black, Config.Colors.Black);
        _terminal.Visible = true;

        // ── Title block (centred vertically slightly above middle) ──────────
        int titleY = termH / 2 - 10;

        _terminal.CenteredText(titleY,     "C A T H E D R A L",
            Config.Colors.BrightYellow, Config.Colors.Black);
        _terminal.CenteredText(titleY + 1, "─────────────────────",
            Config.Colors.DarkGray35, Config.Colors.Black);

        // ── "Loading model" row with spinner ────────────────────────────────
        string spinner = Config.Symbols.LoadingSpinner[_spinnerFrame];
        _terminal.CenteredText(titleY + 4,
            $"{spinner}  Loading {_modelLabel}  {spinner}",
            Config.Colors.White, Config.Colors.Black);

        // ── Progress bar ─────────────────────────────────────────────────────
        int filled    = (int)(_progress * ProgressBarWidth);
        int remaining = ProgressBarWidth - filled;
        string bar    = "[" + new string('\u2588', filled) + new string('\u2591', remaining) + "]";
        int pct       = (int)(_progress * 100);

        _terminal.CenteredText(titleY + 6, bar,
            Config.NarrativeUI.LoadingColor, Config.Colors.Black);

        string pctText = $"{pct}%";
        _terminal.CenteredText(titleY + 7, pctText,
            Config.Colors.DarkGray35, Config.Colors.Black);

        // ── Status message ───────────────────────────────────────────────────
        string status = _statusMessage.Length > termW - 4
            ? _statusMessage[..(termW - 7)] + "..."
            : _statusMessage;
        _terminal.CenteredText(titleY + 10, status,
            Config.Colors.Gray, Config.Colors.Black);

        // ── Hint ─────────────────────────────────────────────────────────────
        _terminal.CenteredText(titleY + 13,
            "This may take 30–120 seconds on first run",
            Config.Colors.DarkGray35, Config.Colors.Black);
    }
}
