using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Creation;

/// <summary>
/// Reusable component that renders the body art ASCII display on the left
/// and an organ-stats panel on the right. Handles hover highlighting,
/// blink animation, connector arrows, and bounding boxes.
///
/// Used by both ProtagonistCreationRenderer (interactive, with score editing)
/// and ManagementMenuRenderer (read-only viewer).
/// Accepts any <see cref="PartyMember"/> so it works for both protagonist and companions.
/// </summary>
public class BodyArtViewer
{
    private readonly TerminalHUD _terminal;
    private PartyMember _protagonist;  // named _protagonist for minimal diff; holds the currently-displayed member
    private readonly BodyArtData _artData;

    // ── Layout constants ─────────────────────────────────────────
    public const int PanelX = 62;
    public const int PanelWidth = 38;
    public const int PanelContentX = 64;
    public const int PanelContentW = 34;

    // ── Configuration ────────────────────────────────────────────
    /// <summary>Horizontal offset for the body art (default 0, increase to shift art right).</summary>
    public int ArtOffsetX { get; set; } = 0;

    /// <summary>Vertical offset for the body art (default 0).</summary>
    public int ArtOffsetY { get; set; } = 0;

    /// <summary>Row where organ-stat rows begin.</summary>
    public int StatsStartRow { get; set; } = 6;

    /// <summary>When true, ◄/► arrows are rendered next to each score for editing.</summary>
    public bool ShowScoreEditControls { get; set; } = false;

    /// <summary>When true, hovered detail section shows click hint text.</summary>
    public bool ShowClickHints { get; set; } = false;

    // ── Hover state (read by callers) ────────────────────────────
    public string? HoveredOrganPartName => _hoveredOrganPartName;
    public string? HoveredBodyPartId => _hoveredBodyPartId;
    public string? HoveredOrganName => _hoveredOrganName;

    private string? _hoveredOrganPartName;
    private string? _hoveredBodyPartId;
    private string? _hoveredOrganName;
    private string? _hoveredRawPartName;    /// <summary>-1 when hovering the ◄ button, +1 when hovering the ► button, 0 otherwise.</summary>
    private int _hoveredArrowDelta = 0;
    // ── Blink state ──────────────────────────────────────────────
    private readonly Stopwatch _blinkStopwatch = Stopwatch.StartNew();
    private double _lastBlinkTime;
    private bool _blinkOn = true;
    private const double BlinkInterval = 0.35;

    // ── Static constants ─────────────────────────────────────────
    internal static readonly HashSet<string> LimbBodyParts = new() { "upper_limbs", "lower_limbs" };
    internal static readonly Dictionary<(bool isLeft, string bodyPartId), string> LimbSideToRawPart = new()
    {
        { (true,  "upper_limbs"), "left_arm" },
        { (false, "upper_limbs"), "right_arm" },
        { (true,  "lower_limbs"), "left_leg" },
        { (false, "lower_limbs"), "right_leg" },
    };
    private const int BoxPadding = 1;

    // ── Pre-computed mappings ────────────────────────────────────
    private readonly Dictionary<string, char> _organPartNameToChar;
    private readonly List<(string bodyPartId, int startRow)> _bodyPartRows = new();
    private readonly Dictionary<int, string> _rowToOrganPartId = new();
    private readonly Dictionary<int, (int decX, int incX)> _rowToArrowX = new();

    /// <summary>Exposes row→organPartId mapping for callers that need hit-testing.</summary>
    public IReadOnlyDictionary<int, string> RowToOrganPartId => _rowToOrganPartId;

    public BodyArtViewer(TerminalHUD terminal, PartyMember protagonist, BodyArtData artData)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _protagonist = protagonist ?? throw new ArgumentNullException(nameof(protagonist));
        _artData = artData ?? throw new ArgumentNullException(nameof(artData));

        _organPartNameToChar = new Dictionary<string, char>();
        foreach (var (c, info) in _artData.OrganPartInfos)
            _organPartNameToChar[info.OrganPartName] = c;
    }

    /// <summary>
    /// Re-computes the row layout for the stats panel.
    /// Must be called after changing StatsStartRow or the subject's body parts.
    /// </summary>
    public void ComputeLayout()
    {
        _rowToOrganPartId.Clear();
        _rowToArrowX.Clear();
        _bodyPartRows.Clear();

        int row = StatsStartRow;
        foreach (var bp in _protagonist.BodyParts)
        {
            _bodyPartRows.Add((bp.Id, row));
            row++; // header row

            foreach (var organ in bp.Organs)
                foreach (var part in organ.Parts)
                {
                    _rowToOrganPartId[row] = part.Id;
                    row++;
                }
            row++; // gap between body parts
        }
    }

    /// <summary>
    /// Swap the party member whose body is currently displayed.
    /// Clears hover state and recomputes layout automatically.
    /// </summary>
    public void SwapSubject(PartyMember newSubject)
    {
        _protagonist = newSubject ?? throw new ArgumentNullException(nameof(newSubject));
        ClearHover();
        ComputeLayout();
    }

    // ═══════════════════════════════════════════════════════════════
    // Blink animation
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Updates the blink animation. Returns true if body art needs re-render.
    /// </summary>
    public bool UpdateBlink()
    {
        if (_hoveredOrganPartName == null) return false;
        double now = _blinkStopwatch.Elapsed.TotalSeconds;
        if (now - _lastBlinkTime >= BlinkInterval)
        {
            _lastBlinkTime = now;
            _blinkOn = !_blinkOn;
            return true;
        }
        return false;
    }

    // ═══════════════════════════════════════════════════════════════
    // Hover detection
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Processes mouse hover at terminal coordinates.
    /// Returns true if hover state changed (caller should re-render).
    /// </summary>
    public bool ProcessHover(int x, int y)
    {
        string? newOrganPart = null;
        string? newBodyPart = null;
        string? newOrgan = null;
        string? newRawPartName = null;

        // Check art area
        int artX = x - ArtOffsetX;
        int artY = y - ArtOffsetY;
        if (artX >= 0 && artX < _artData.Width && artY >= 0 && artY < _artData.Height)
        {
            var info = _artData.GetOrganPartInfoAt(artX, artY);
            if (info != null)
            {
                newOrganPart = info.OrganPartName;
                newOrgan = info.OrganName;
                newBodyPart = info.BodyPartName;
            }
            else
            {
                newBodyPart = _artData.GetBodyPartIdAt(artX, artY);
            }
            newRawPartName = _artData.GetPartNameAt(artX, artY);

            if (info != null && LimbBodyParts.Contains(newBodyPart ?? ""))
            {
                if (info.OrganPartName.StartsWith("left_"))
                    newRawPartName = LimbSideToRawPart.GetValueOrDefault((true, newBodyPart!));
                else if (info.OrganPartName.StartsWith("right_"))
                    newRawPartName = LimbSideToRawPart.GetValueOrDefault((false, newBodyPart!));
            }
        }

        // Check stats panel
        if (x >= PanelX && x < PanelX + PanelWidth)
        {
            if (_rowToOrganPartId.TryGetValue(y, out var partId))
            {
                newOrganPart = partId;
                var op = FindOrganPartByName(partId);
                if (op != null)
                {
                    newOrgan = op.Value.organName;
                    newBodyPart = op.Value.bodyPartId;
                    if (LimbBodyParts.Contains(op.Value.bodyPartId) && partId.StartsWith("left_"))
                        newRawPartName = LimbSideToRawPart.GetValueOrDefault((true, op.Value.bodyPartId));
                    else if (LimbBodyParts.Contains(op.Value.bodyPartId) && partId.StartsWith("right_"))
                        newRawPartName = LimbSideToRawPart.GetValueOrDefault((false, op.Value.bodyPartId));
                }
            }
        }

        bool changed = newOrganPart != _hoveredOrganPartName
                     || newBodyPart != _hoveredBodyPartId
                     || newRawPartName != _hoveredRawPartName;

        // Detect arrow hover (only meaningful when ShowScoreEditControls is true)
        int newArrowDelta = 0;
        if (ShowScoreEditControls && _rowToArrowX.TryGetValue(y, out var ax))
        {
            if (x == ax.decX) newArrowDelta = -1;
            else if (x == ax.incX) newArrowDelta = +1;
        }
        changed = changed || newArrowDelta != _hoveredArrowDelta;

        if (changed)
        {
            _hoveredOrganPartName = newOrganPart;
            _hoveredBodyPartId = newBodyPart;
            _hoveredOrganName = newOrgan;
            _hoveredRawPartName = newRawPartName;
            _hoveredArrowDelta = newArrowDelta;
            ResetBlink();
        }

        return changed;
    }

    /// <summary>Clears all hover state.</summary>
    public void ClearHover()
    {
        _hoveredOrganPartName = null;
        _hoveredBodyPartId = null;
        _hoveredOrganName = null;
        _hoveredRawPartName = null;
        _hoveredArrowDelta = 0;
    }

    /// <summary>Resets the blink timer (called when hover target changes).</summary>
    public void ResetBlink()
    {
        _lastBlinkTime = _blinkStopwatch.Elapsed.TotalSeconds;
        _blinkOn = true;
    }

    // ═══════════════════════════════════════════════════════════════
    // Body art rendering (left side)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders the full body art area on the left side of the terminal,
    /// including hover-based highlighting, bounding boxes, separator line,
    /// and connector arrows to the stats panel.
    /// </summary>
    /// <param name="brightness">Multiplier applied to base art colours (1.0 = normal, 0.5 = dimmed).</param>
    public void RenderBodyArt(float brightness = 1.0f)
    {
        // Compute bounding box for hovered body part
        ArtBounds? hoveredBounds = null;
        if (_hoveredBodyPartId != null)
        {
            if (LimbBodyParts.Contains(_hoveredBodyPartId) && _hoveredRawPartName != null)
                hoveredBounds = _artData.GetRawPartBounds(_hoveredRawPartName);
            else
                hoveredBounds = _artData.GetBodyPartBounds(_hoveredBodyPartId);
        }

        // Determine box rectangle in terminal coords (with padding)
        int boxX1 = -1, boxY1 = -1, boxX2 = -1, boxY2 = -1;
        if (hoveredBounds != null)
        {
            boxX1 = ArtOffsetX + hoveredBounds.MinX - BoxPadding;
            boxY1 = ArtOffsetY + hoveredBounds.MinY - BoxPadding;
            boxX2 = ArtOffsetX + hoveredBounds.MaxX + BoxPadding;
            boxY2 = ArtOffsetY + hoveredBounds.MaxY + BoxPadding;
            boxX1 = Math.Max(0, boxX1);
            boxY1 = Math.Max(0, boxY1);
            boxX2 = Math.Min(PanelX - 2, boxX2);
            boxY2 = Math.Min(_terminal.Height - 1, boxY2);
        }

        // Clear the art area
        for (int ty = 0; ty < _artData.Height && ty < _terminal.Height; ty++)
            for (int tx = 0; tx < PanelX - 1 && tx < _terminal.Width; tx++)
                _terminal.SetCell(tx, ty, ' ', Config.Colors.Black, Config.Colors.Black);

        // Render all body cells with highlight levels
        for (int ay = 0; ay < _artData.Height; ay++)
        {
            for (int ax = 0; ax < _artData.Width; ax++)
            {
                int tx = ArtOffsetX + ax;
                int ty = ArtOffsetY + ay;

                char artChar = _artData.ArtGrid[ax, ay];
                if (artChar == ' ' || artChar == '\0') continue;

                Vector4 baseColor = _artData.GetLayerColorAt(ax, ay);
                if (brightness != 1.0f)
                    baseColor = new Vector4(baseColor.X * brightness, baseColor.Y * brightness, baseColor.Z * brightness, baseColor.W);
                Vector4 bgColor = Config.Colors.Black;

                if (!_artData.IsBodyCell(ax, ay)) continue;

                var organInfo = _artData.GetOrganPartInfoAt(ax, ay);
                string? cellBodyPartId = _artData.GetBodyPartIdAt(ax, ay);
                string? cellRawPartName = _artData.GetPartNameAt(ax, ay);

                // For limbs, only highlight cells on the same side
                bool isHoveredBodyPart;
                if (cellBodyPartId != null && cellBodyPartId == _hoveredBodyPartId
                    && LimbBodyParts.Contains(cellBodyPartId) && _hoveredRawPartName != null)
                    isHoveredBodyPart = cellRawPartName == _hoveredRawPartName;
                else
                    isHoveredBodyPart = cellBodyPartId != null && cellBodyPartId == _hoveredBodyPartId;

                bool isHoveredOrgan = organInfo != null && _hoveredOrganName != null && organInfo.OrganName == _hoveredOrganName;
                bool isHoveredOrganPart = organInfo != null && organInfo.OrganPartName == _hoveredOrganPartName;

                if (isHoveredOrganPart)
                {
                    if (_blinkOn)
                    {
                        baseColor = AdjustLuminosity(baseColor, 3.0f);
                        bgColor = new Vector4(0.2f, 0.2f, 0.0f, 1.0f);
                    }
                    else
                    {
                        baseColor = AdjustLuminosity(baseColor, 2.0f);
                        bgColor = new Vector4(0.08f, 0.08f, 0.0f, 1.0f);
                    }
                }
                else if (isHoveredOrgan)
                {
                    baseColor = AdjustLuminosity(baseColor, 2.2f);
                    bgColor = new Vector4(0.12f, 0.12f, 0.0f, 1.0f);
                }
                else if (isHoveredBodyPart && organInfo != null)
                {
                    baseColor = AdjustLuminosity(baseColor, 1.6f);
                    bgColor = new Vector4(0.05f, 0.05f, 0.0f, 1.0f);
                }
                else if (isHoveredBodyPart)
                {
                    baseColor = AdjustLuminosity(baseColor, 1.3f);
                    bgColor = new Vector4(0.04f, 0.04f, 0.0f, 1.0f);
                }

                _terminal.SetCell(tx, ty, artChar, baseColor, bgColor);
            }
        }

        // Draw body part bounding box if hovering
        if (hoveredBounds != null && _hoveredBodyPartId != null)
        {
            Vector4 boxColor = Config.Colors.MediumYellow;
            Vector4 boxBg = Config.Colors.Black;

            // Fill non-body cells inside box with dim highlight
            for (int ty = boxY1 + 1; ty < boxY2; ty++)
            {
                for (int tx = boxX1 + 1; tx < boxX2; tx++)
                {
                    int ax = tx - ArtOffsetX;
                    int ay = ty - ArtOffsetY;
                    bool isBody = ax >= 0 && ax < _artData.Width && ay >= 0 && ay < _artData.Height
                                  && _artData.IsBodyCell(ax, ay);
                    if (!isBody)
                    {
                        _terminal.SetCell(tx, ty, '·', new Vector4(0.15f, 0.15f, 0.05f, 1.0f),
                            new Vector4(0.03f, 0.03f, 0.0f, 1.0f));
                    }
                }
            }

            // Box border
            _terminal.SetCell(boxX1, boxY1, BoxChars.Single.TopLeft, boxColor, boxBg);
            _terminal.SetCell(boxX2, boxY1, BoxChars.Single.TopRight, boxColor, boxBg);
            for (int tx = boxX1 + 1; tx < boxX2; tx++)
                _terminal.SetCell(tx, boxY1, BoxChars.Single.Horizontal, boxColor, boxBg);

            _terminal.SetCell(boxX1, boxY2, BoxChars.Single.BottomLeft, boxColor, boxBg);
            _terminal.SetCell(boxX2, boxY2, BoxChars.Single.BottomRight, boxColor, boxBg);
            for (int tx = boxX1 + 1; tx < boxX2; tx++)
                _terminal.SetCell(tx, boxY2, BoxChars.Single.Horizontal, boxColor, boxBg);

            for (int ty = boxY1 + 1; ty < boxY2; ty++)
                _terminal.SetCell(boxX1, ty, BoxChars.Single.Vertical, boxColor, boxBg);
            for (int ty = boxY1 + 1; ty < boxY2; ty++)
                _terminal.SetCell(boxX2, ty, BoxChars.Single.Vertical, boxColor, boxBg);
        }

        // Separator line between art and panel
        int sepX = PanelX - 1;
        for (int y = 0; y < 100; y++)
            _terminal.SetCell(sepX, y, '│', Config.Colors.DarkGray35, Config.Colors.Black);

        // Connector arrows
        RenderArrows();
    }

    private void RenderArrows()
    {
        if (_hoveredOrganPartName == null) return;
        if (!_organPartNameToChar.TryGetValue(_hoveredOrganPartName, out char organChar)) return;

        var cells = _artData.GetOrganPartCells(organChar);
        if (cells.Count == 0) return;

        var rightmost = cells.OrderByDescending(c => c.x).First();
        int artEndX = ArtOffsetX + rightmost.x;
        int artEndY = ArtOffsetY + rightmost.y;

        int statsRow = _rowToOrganPartId
            .Where(kvp => kvp.Value == _hoveredOrganPartName)
            .Select(kvp => kvp.Key)
            .FirstOrDefault(-1);

        if (statsRow >= 0 && artEndX < PanelX - 2)
        {
            ArrowRenderer.DrawConnector(_terminal,
                artEndX, artEndY,
                PanelContentX - 1, statsRow,
                Config.Colors.MediumYellow, Config.Colors.Black);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Organ stats panel rendering (right side)
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Renders the organ stat rows in the right panel starting at StatsStartRow.
    /// Returns the row index after the last rendered content.
    /// </summary>
    public int RenderOrganStats()
    {
        int row = StatsStartRow;
        foreach (var bp in _protagonist.BodyParts)
        {
            bool isHoveredBP = bp.Id == _hoveredBodyPartId;
            Vector4 headerColor = isHoveredBP ? Config.Colors.BrightYellow : Config.Colors.LightGray75;
            Vector4 headerBg = isHoveredBP ? new Vector4(0.1f, 0.1f, 0.0f, 1.0f) : Config.Colors.Black;

            string header = $"▸ {bp.DisplayName.ToUpper()}";
            string scoreStr = $"[{bp.Score}]";
            _terminal.Text(PanelContentX, row, header, headerColor, headerBg);
            _terminal.Text(PanelContentX + PanelContentW - scoreStr.Length, row, scoreStr,
                Config.Colors.DarkYellowGrey, headerBg);
            row++;

            foreach (var organ in bp.Organs)
            {
                foreach (var part in organ.Parts)
                {
                    bool isHoveredPart = part.Id == _hoveredOrganPartName;
                    bool isHoveredOrgan = organ.Id == _hoveredOrganName;

                    Vector4 nameColor, barFill, barEmpty, scoreFg, bg;
                    if (isHoveredPart)
                    {
                        nameColor = Config.Colors.BrightYellow;
                        barFill = Config.Colors.GoldYellow;
                        barEmpty = Config.Colors.DarkYellow;
                        scoreFg = Config.Colors.BrightYellow;
                        bg = new Vector4(0.12f, 0.12f, 0.0f, 1.0f);
                    }
                    else if (isHoveredOrgan)
                    {
                        nameColor = Config.Colors.MediumYellow;
                        barFill = Config.Colors.MediumYellow;
                        barEmpty = Config.Colors.DarkGray35;
                        scoreFg = Config.Colors.MediumYellow;
                        bg = new Vector4(0.06f, 0.06f, 0.0f, 1.0f);
                    }
                    else
                    {
                        nameColor = Config.Colors.MediumGray60;
                        barFill = Config.Colors.MediumGray50;
                        barEmpty = Config.Colors.DarkGray20;
                        scoreFg = Config.Colors.MediumGray60;
                        bg = Config.Colors.Black;
                    }

                    string name = FormatPartName(part.DisplayName);
                    int nameWidth = 14;
                    string paddedName = name.Length > nameWidth ? name[..nameWidth] : name.PadRight(nameWidth);

                    _terminal.Text(PanelContentX + 1, row, paddedName, nameColor, bg);

                    int barX = PanelContentX + 1 + nameWidth + 1;
                    int barWidth = part.MaxScore;
                    for (int i = 0; i < barWidth; i++)
                    {
                        char barChar = i < part.Score ? '█' : '░';
                        Vector4 barColor = i < part.Score ? barFill : barEmpty;
                        _terminal.SetCell(barX + i, row, barChar, barColor, bg);
                    }

                    if (ShowScoreEditControls)
                    {
                        // Editable: ◄ score ► arrows
                        // Each arrow is highlighted individually when directly hovered
                        bool isDecHovered = isHoveredPart && _hoveredArrowDelta == -1;
                        bool isIncHovered = isHoveredPart && _hoveredArrowDelta == +1;
                        Vector4 decColor = isDecHovered ? Config.Colors.BrightYellow
                                         : isHoveredPart ? Config.Colors.MediumYellow
                                         : Config.Colors.DarkGray35;
                        Vector4 incColor = isIncHovered ? Config.Colors.BrightYellow
                                         : isHoveredPart ? Config.Colors.MediumYellow
                                         : Config.Colors.DarkGray35;
                        _terminal.SetCell(barX + barWidth, row, ' ', Config.Colors.Black, bg);
                        _terminal.SetCell(barX + barWidth + 1, row, '◄', decColor, bg);
                        _terminal.SetCell(barX + barWidth + 2, row, (char)('0' + part.Score), scoreFg, bg);
                        _terminal.SetCell(barX + barWidth + 3, row, '►', incColor, bg);
                        _rowToArrowX[row] = (barX + barWidth + 1, barX + barWidth + 3);

                        for (int fx = barX + barWidth + 4; fx < PanelX + PanelWidth; fx++)
                            _terminal.SetCell(fx, row, ' ', Config.Colors.Black, bg);
                    }
                    else
                    {
                        // Read-only: just the score number
                        _terminal.SetCell(barX + barWidth, row, ' ', Config.Colors.Black, bg);
                        _terminal.SetCell(barX + barWidth + 1, row, (char)('0' + part.Score), scoreFg, bg);

                        for (int fx = barX + barWidth + 2; fx < PanelX + PanelWidth; fx++)
                            _terminal.SetCell(fx, row, ' ', Config.Colors.Black, bg);
                    }

                    for (int fx = PanelContentX; fx < PanelContentX + 1; fx++)
                        _terminal.SetCell(fx, row, ' ', Config.Colors.Black, bg);

                    row++;
                }
            }

            row++; // Gap between body parts
        }

        return row;
    }

    // ── Humoral organ IDs ────────────────────────────────────────
    private static readonly System.Collections.Generic.HashSet<string> HumoralOrganIds =
        new() { "hepar", "paunch", "pulmones", "spleen" };

    /// <summary>
    /// Renders the hovered organ detail section below the stats.
    /// </summary>
    public void RenderHoveredDetail(int minRow)
    {
        if (_hoveredOrganPartName == null) return;

        int row = Math.Max(minRow + 1, 50);
        _terminal.Text(PanelContentX, row, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);
        row += 2;

        var opInfo = FindOrganPartByName(_hoveredOrganPartName);
        if (opInfo != null)
        {
            _terminal.Text(PanelContentX, row, opInfo.Value.partDisplayName, Config.Colors.BrightYellow, Config.Colors.Black);
            row++;
            _terminal.Text(PanelContentX, row, $"Organ: {opInfo.Value.organDisplayName}", Config.Colors.MediumGray60, Config.Colors.Black);
            row++;
            _terminal.Text(PanelContentX, row, $"Region: {opInfo.Value.bodyPartDisplayName}", Config.Colors.MediumGray60, Config.Colors.Black);
            row += 2;

            string organId   = opInfo.Value.organName;
            string bodyPartId = opInfo.Value.bodyPartId;

            // ── Secretion stats — humoral organs only ─────────────────
            if (HumoralOrganIds.Contains(organId))
            {
                var secretionStats = _protagonist.DerivedStats
                    .Where(s => s.RelatedOrganId == organId)
                    .ToList();
                if (secretionStats.Count > 0)
                {
                    _terminal.Text(PanelContentX, row, "Secretion", Config.Colors.DarkYellowGrey, Config.Colors.Black);
                    row++;
                    foreach (var stat in secretionStats)
                    {
                        int val = stat.CalculateValue(stat.GetSourceScore(_protagonist));
                        string label = stat.DisplayName;
                        int spaceIdx = label.IndexOf(' ');
                        if (spaceIdx >= 0) label = label[(spaceIdx + 1)..];
                        _terminal.Text(PanelContentX, row, $"  {label,-16}{val,3}%", Config.Colors.LightGray75, Config.Colors.Black);
                        row++;
                    }
                    row++;
                }
            }

            // ── General derived stats — non-humoral organ-linked + body-part-linked ──
            var generalStats = _protagonist.DerivedStats
                .Where(s => (s.RelatedOrganId == organId   && !HumoralOrganIds.Contains(organId))
                         || (s.RelatedBodyPartId == bodyPartId && s.RelatedBodyPartId != null))
                .ToList();
            if (generalStats.Count > 0)
            {
                _terminal.Text(PanelContentX, row, "Stats", Config.Colors.DarkYellowGrey, Config.Colors.Black);
                row++;
                foreach (var stat in generalStats)
                {
                    int val = stat.CalculateValue(stat.GetSourceScore(_protagonist));
                    _terminal.Text(PanelContentX, row, $"  {stat.DisplayName,-16}{val,4}", Config.Colors.LightGray75, Config.Colors.Black);
                    row++;
                }
                row++;
            }

            if (ShowClickHints)
            {
                _terminal.Text(PanelContentX, row, "Left-click: +1", Config.Colors.DarkYellowGrey, Config.Colors.Black);
                row++;
                _terminal.Text(PanelContentX, row, "Right-click: -1", Config.Colors.DarkYellowGrey, Config.Colors.Black);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Click helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the organ part id at the given terminal position (art area or stats panel).
    /// </summary>
    public string? GetOrganPartAtPosition(int x, int y)
    {
        int artX = x - ArtOffsetX;
        int artY = y - ArtOffsetY;
        if (artX >= 0 && artX < _artData.Width && artY >= 0 && artY < _artData.Height)
        {
            var info = _artData.GetOrganPartInfoAt(artX, artY);
            if (info != null) return info.OrganPartName;
        }

        if (x >= PanelX && x < PanelX + PanelWidth)
        {
            if (_rowToOrganPartId.TryGetValue(y, out var partId))
                return partId;
        }

        return null;
    }

    /// <summary>
    /// Returns -1 for ◄ click, +1 for ► click, 0 if not on an arrow button.
    /// Only meaningful when ShowScoreEditControls is true.
    /// </summary>
    public int GetArrowClickDelta(int x, int y)
    {
        if (!ShowScoreEditControls) return 0;
        if (_rowToArrowX.TryGetValue(y, out var arrows))
        {
            if (x == arrows.decX) return -1;
            if (x == arrows.incX) return +1;
        }
        return 0;
    }

    /// <summary>
    /// Returns the organ part id mapped to the given stats row, or null.
    /// </summary>
    public string? GetOrganPartIdAtRow(int y)
    {
        return _rowToOrganPartId.TryGetValue(y, out var partId) ? partId : null;
    }

    /// <summary>
    /// Adjusts the score of an organ part on the protagonist.
    /// </summary>
    public void AdjustOrganPartScore(string organPartName, int delta)
    {
        var organPart = _protagonist.BodyParts
            .SelectMany(bp => bp.Organs)
            .SelectMany(o => o.Parts)
            .FirstOrDefault(p => p.Id == organPartName);

        if (organPart != null)
            organPart.Score = Math.Clamp(organPart.Score + delta, 0, organPart.MaxScore);
    }

    // ═══════════════════════════════════════════════════════════════
    // Utility
    // ═══════════════════════════════════════════════════════════════

    /// <summary>Total score across all organ parts.</summary>
    public int GetTotalScore()
    {
        return _protagonist.BodyParts
            .SelectMany(bp => bp.Organs)
            .SelectMany(o => o.Parts)
            .Sum(p => p.Score);
    }

    /// <summary>Finds organ part info by its string id.</summary>
    public (string bodyPartId, string bodyPartDisplayName, string organName, string organDisplayName, string partId, string partDisplayName)?
        FindOrganPartByName(string organPartName)
    {
        foreach (var bp in _protagonist.BodyParts)
            foreach (var organ in bp.Organs)
                foreach (var part in organ.Parts)
                    if (part.Id == organPartName)
                        return (bp.Id, bp.DisplayName, organ.Id, organ.DisplayName, part.Id, part.DisplayName);
        return null;
    }

    internal static string FormatPartName(string displayName)
    {
        return displayName
            .Replace("Left ", "L.")
            .Replace("Right ", "R.");
    }

    internal static Vector4 AdjustLuminosity(Vector4 color, float multiplier)
    {
        return new Vector4(
            Math.Clamp(color.X * multiplier, 0f, 1f),
            Math.Clamp(color.Y * multiplier, 0f, 1f),
            Math.Clamp(color.Z * multiplier, 0f, 1f),
            1.0f
        );
    }
}
