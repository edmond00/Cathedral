using System;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Creation;

/// <summary>
/// Renders the protagonist creation screen: body art on the left with interactive
/// organ-part highlighting, stats panel on the right with score bars.
/// Left-click art cells to cycle organ part score (increments, wraps back to 0).
///
/// Delegates body art rendering and hover logic to BodyArtViewer,
/// adding score editing controls and the continue button on top.
/// </summary>
public class ProtagonistCreationRenderer
{
    private readonly TerminalHUD _terminal;
    private readonly BodyArtViewer _viewer;

    // Footer layout
    private const int ContinueButtonY = 96;
    private const int ContinueButtonX = 72;
    private const int ContinueButtonW = 18;

    // State
    private bool _continueHovered;

    /// <summary>Callback for when the player clicks Continue.</summary>
    public Action? OnContinue { get; set; }

    public ProtagonistCreationRenderer(TerminalHUD terminal, Protagonist protagonist, BodyArtData artData)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));

        _viewer = new BodyArtViewer(terminal, protagonist, artData)
        {
            StatsStartRow = 6,
            ShowScoreEditControls = true,
            ShowClickHints = true
        };
        _viewer.ComputeLayout();
    }

    /// <summary>Full render of the creation screen.</summary>
    public void Render()
    {
        _terminal.Fill(' ', Config.Colors.Black, Config.Colors.Black);
        _terminal.Visible = true;

        _viewer.RenderBodyArt();
        RenderPanelHeader();
        int lastRow = _viewer.RenderOrganStats();
        _viewer.RenderHoveredDetail(lastRow);
        RenderFooter();
    }

    /// <summary>Called every frame. Handles blink animation for hovered organ part.</summary>
    public void Update()
    {
        if (_viewer.UpdateBlink())
            _viewer.RenderBodyArt();
    }

    /// <summary>Handle mouse hover at terminal coordinates.</summary>
    public void OnMouseMove(int x, int y)
    {
        bool viewerChanged = _viewer.ProcessHover(x, y);
        bool newContinueHovered = IsOnContinueButton(x, y);
        bool continueChanged = newContinueHovered != _continueHovered;

        if (viewerChanged || continueChanged)
        {
            _continueHovered = newContinueHovered;
            Render();
        }
    }

    /// <summary>Handle left click at terminal coordinates (+1 to organ part score).</summary>
    public void OnMouseClick(int x, int y)
    {
        if (IsOnContinueButton(x, y))
        {
            OnContinue?.Invoke();
            return;
        }

        // Check arrow buttons in stats panel
        int arrowDelta = _viewer.GetArrowClickDelta(x, y);
        if (arrowDelta != 0)
        {
            var partId = _viewer.GetOrganPartIdAtRow(y);
            if (partId != null)
            {
                _viewer.AdjustOrganPartScore(partId, arrowDelta);
                Render();
                return;
            }
        }

        var clickedPart = _viewer.GetOrganPartAtPosition(x, y);
        if (clickedPart != null)
        {
            _viewer.CycleOrganPartScore(clickedPart);
            Render();
        }
    }

    /// <summary>Right click has no effect in creation mode.</summary>
    public void OnRightClick(int x, int y) { }

    // ── Panel header ─────────────────────────────────────────────

    private void RenderPanelHeader()
    {
        _terminal.Text(BodyArtViewer.PanelContentX, 1, "A V A T A R", Config.Colors.BrightYellow, Config.Colors.Black);
        _terminal.Text(BodyArtViewer.PanelContentX, 2, "C R E A T I O N", Config.Colors.DarkYellowGrey, Config.Colors.Black);
        _terminal.Text(BodyArtViewer.PanelContentX, 4, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);
    }

    // ── Footer ───────────────────────────────────────────────────

    private void RenderFooter()
    {
        int totalScore = _viewer.GetTotalScore();

        _terminal.Text(BodyArtViewer.PanelContentX, 92, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);
        string pointsText = $"Total Points: {totalScore}";
        _terminal.Text(BodyArtViewer.PanelContentX, 94, pointsText, Config.Colors.LightGray75, Config.Colors.Black);

        Vector4 btnText, btnBg;
        if (_continueHovered)
        {
            btnText = Config.Colors.BrightYellow;
            btnBg = Config.Colors.DarkYellow;
        }
        else
        {
            btnText = Config.Colors.White;
            btnBg = Config.Colors.Black;
        }

        _terminal.FillRect(ContinueButtonX, ContinueButtonY, ContinueButtonW, 1, ' ', btnText, btnBg);
        string btnLabel = "[ CONTINUE ]";
        int lblX = ContinueButtonX + (ContinueButtonW - btnLabel.Length) / 2;
        _terminal.Text(lblX, ContinueButtonY, btnLabel, btnText, btnBg);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private bool IsOnContinueButton(int x, int y)
    {
        return y == ContinueButtonY && x >= ContinueButtonX && x < ContinueButtonX + ContinueButtonW;
    }
}
