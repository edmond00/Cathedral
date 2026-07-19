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
    private readonly Protagonist _protagonist;

    // Footer layout
    private const int ContinueButtonY = 96;
    private const int ContinueButtonX = 72;
    private const int ContinueButtonW = 18;

    // Name banner layout (top of the body-art pane): [⟲] reroll button, then the name to its right.
    private const int NameRow = 2;
    private const int NameX = 2;
    private const string RegenLabel = "[⟲]";

    // State
    private bool _continueHovered;
    private bool _regenHovered;
    private int _regenButtonX = -1;   // computed each Render (depends on name length)

    /// <summary>Callback for when the player clicks Continue.</summary>
    public Action? OnContinue { get; set; }

    public ProtagonistCreationRenderer(TerminalHUD terminal, Protagonist protagonist, BodyArtData artData)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _protagonist = protagonist ?? throw new ArgumentNullException(nameof(protagonist));

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
        RenderNameBanner();
        RenderPanelHeader();
        int lastRow = _viewer.RenderOrganStats();
        int descRow = _viewer.RenderHoveredOrganDescription(lastRow);
        _viewer.RenderHoveredDetail(descRow);
        _viewer.RenderHoveredRegionDetail(lastRow);
        RenderFooter();
    }

    /// <summary>Called every frame. Handles blink animation for hovered organ part.</summary>
    public void Update()
    {
        if (_viewer.UpdateBlink())
        {
            // RenderBodyArt repaints (and clears) the whole left pane, so the name banner must be
            // redrawn on top of it or it would vanish on blink frames.
            _viewer.RenderBodyArt();
            RenderNameBanner();
        }
    }

    /// <summary>Handle mouse hover at terminal coordinates.</summary>
    public void OnMouseMove(int x, int y)
    {
        bool viewerChanged = _viewer.ProcessHover(x, y);
        bool newContinueHovered = IsOnContinueButton(x, y);
        bool newRegenHovered = IsOnRegenButton(x, y);
        bool buttonsChanged = newContinueHovered != _continueHovered || newRegenHovered != _regenHovered;

        if (viewerChanged || buttonsChanged)
        {
            _continueHovered = newContinueHovered;
            _regenHovered = newRegenHovered;
            Render();
        }
    }

    /// <summary>Handle left click at terminal coordinates (+1 to organ part score).</summary>
    public void OnMouseClick(int x, int y)
    {
        if (IsOnRegenButton(x, y))
        {
            _protagonist.RegenerateName();
            Render();
            return;
        }

        if (IsOnContinueButton(x, y))
        {
            OnContinue?.Invoke();
            return;
        }

        // A gender change (genitories score crossing 0) rerolls the name to match the new sex.
        bool maleBefore = IsMale();

        // Check arrow buttons in stats panel
        int arrowDelta = _viewer.GetArrowClickDelta(x, y);
        if (arrowDelta != 0)
        {
            var partId = _viewer.GetOrganPartIdAtRow(y);
            if (partId != null)
            {
                _viewer.AdjustOrganPartScore(partId, arrowDelta);
                RegenerateIfGenderChanged(maleBefore);
                Render();
                return;
            }
        }

        var clickedPart = _viewer.GetOrganPartAtPosition(x, y);
        if (clickedPart != null)
        {
            _viewer.CycleOrganPartScore(clickedPart);
            RegenerateIfGenderChanged(maleBefore);
            Render();
        }
    }

    /// <summary>True when the protagonist's current genitories score reads as male (&gt; 0).</summary>
    private bool IsMale() => (_protagonist.GetOrganPartById("genitories")?.Score ?? 1) > 0;

    /// <summary>Rerolls the name when the last score edit flipped the gender.</summary>
    private void RegenerateIfGenderChanged(bool maleBefore)
    {
        if (IsMale() != maleBefore)
            _protagonist.RegenerateName();
    }

    /// <summary>Right click has no effect in creation mode.</summary>
    public void OnRightClick(int x, int y) { }

    // ── Panel header ─────────────────────────────────────────────

    /// <summary>
    /// Draws the protagonist's generated name at the top of the body-art pane, with a "[⟲]" reroll
    /// button to its right. The button's x depends on the name length, so it is recomputed and stored
    /// here for hit-testing (<see cref="IsOnRegenButton"/>).
    /// </summary>
    private void RenderNameBanner()
    {
        _regenButtonX = NameX;
        Vector4 fg = _regenHovered ? Config.Colors.BrightYellow  : Config.Colors.DarkYellowGrey;
        Vector4 bg = _regenHovered ? Config.Colors.DarkYellow    : Config.Colors.Black;
        _terminal.Text(_regenButtonX, NameRow, RegenLabel, fg, bg);

        int nameX = _regenButtonX + RegenLabel.Length + 1;
        _terminal.Text(nameX, NameRow, _protagonist.DisplayName, Config.Colors.BrightYellow, Config.Colors.Black);
    }

    private void RenderPanelHeader()
    {
        _terminal.Text(BodyArtViewer.PanelContentX, 1, "P R O T A G O N I S T", Config.Colors.BrightYellow, Config.Colors.Black);
        _terminal.Text(BodyArtViewer.PanelContentX, 2, "C R E A T I O N", Config.Colors.DarkYellowGrey, Config.Colors.Black);
        _terminal.Text(BodyArtViewer.PanelContentX, 4, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);
    }

    // ── Footer ───────────────────────────────────────────────────

    private void RenderFooter()
    {
        int remaining = _viewer.GetRemainingPoints();

        _terminal.Text(BodyArtViewer.PanelContentX, 92, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);
        Vector4 pointsColor = remaining > 0 ? Config.Colors.BrightYellow : Config.Colors.DarkGray35;
        string pointsText = $"Points: {remaining}/{BodyArtViewer.PointBudget} remaining";
        _terminal.Text(BodyArtViewer.PanelContentX, 94, pointsText, pointsColor, Config.Colors.Black);

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

    /// <summary>True when (x, y) is on the "[⟲]" reroll button next to the name banner.</summary>
    private bool IsOnRegenButton(int x, int y)
        => y == NameRow && _regenButtonX >= 0 && x >= _regenButtonX && x < _regenButtonX + RegenLabel.Length;

    /// <summary>Returns a stable element id for the UI element at (x, y), or null if none.</summary>
    public string? GetHoveredElementId(int x, int y)
    {
        if (IsOnRegenButton(x, y)) return "regen";
        if (IsOnContinueButton(x, y)) return "continue";
        int arrowDelta = _viewer.GetArrowClickDelta(x, y);
        if (arrowDelta != 0)
        {
            var partId = _viewer.GetOrganPartIdAtRow(y);
            return partId != null ? $"arrow:{partId}:{arrowDelta}" : null;
        }
        return _viewer.GetOrganPartAtPosition(x, y) is { } part ? $"organ:{part}" : null;
    }

    /// <summary>Returns true if the cell at (x, y) is a clickable/hoverable UI element.</summary>
    public bool IsInteractiveCell(int x, int y) => GetHoveredElementId(x, y) != null;
}
