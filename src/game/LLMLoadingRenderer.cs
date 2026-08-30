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

    public LLMLoadingRenderer(TerminalHUD terminal, string modelLabel = "language model")
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

        string title = Config.Name.GameTitle;
        _terminal.CenteredText(titleY,     title,
            Config.Colors.BrightYellow, Config.Colors.Black);
        _terminal.CenteredText(titleY + 1, new string('─', title.Length),
            Config.Colors.DarkGray35, Config.Colors.Black);

        // ── "Loading model" row with spinner ────────────────────────────────
        string spinner = Config.Symbols.LoadingSpinner[_spinnerFrame];
        _terminal.CenteredText(titleY + 4,
            $"{spinner}  Loading {_modelLabel}  {spinner}",
            Config.Colors.White, Config.Colors.Black);

        // ── Progress bar ─────────────────────────────────────────────────────
        // Drawn in four pieces rather than as one centred string: the filled part is yellow and
        // the remainder grey, so the bar reads as a track being consumed rather than as one block.
        int filled    = (int)(_progress * ProgressBarWidth);
        int remaining = ProgressBarWidth - filled;
        int pct       = (int)(_progress * 100);

        int barY = titleY + 6;
        int barX = termW / 2 - (ProgressBarWidth + 2) / 2;

        _terminal.Text(barX, barY, "[",
            Config.Colors.DarkGray35, Config.Colors.Black);
        _terminal.Text(barX + 1, barY, new string('\u2588', filled),
            Config.NarrativeUI.LoadingColor, Config.Colors.Black);
        _terminal.Text(barX + 1 + filled, barY, new string('\u2591', remaining),
            Config.Colors.DarkGray35, Config.Colors.Black);
        _terminal.Text(barX + 1 + ProgressBarWidth, barY, "]",
            Config.Colors.DarkGray35, Config.Colors.Black);

        // One blank row between the bar and the percentage, which otherwise crowd each other.
        string pctText = $"{pct}%";
        _terminal.CenteredText(titleY + 8, pctText,
            Config.Colors.DarkGray35, Config.Colors.Black);

        // ── Status message ───────────────────────────────────────────────────
        string status = _statusMessage.Length > termW - 4
            ? _statusMessage[..(termW - 7)] + "..."
            : _statusMessage;
        _terminal.CenteredText(titleY + 11, status,
            Config.Colors.Gray, Config.Colors.Black);

        // ── Hint ─────────────────────────────────────────────────────────────
        _terminal.CenteredText(titleY + 14,
            "This may take 30–120 seconds on first run",
            Config.Colors.DarkGray35, Config.Colors.Black);

        // Edge rules against the sphere, drawn last so nothing overwrites them. This screen is
        // opaque black to the terminal's edges exactly like the main menu and the settings screen,
        // so without them the panel bleeds into the skybox — and this is the FIRST screen a player
        // ever sees. The two transparent-surround screens (trade, work) deliberately have none.
        _terminal.DrawSideRails();
    }
}
