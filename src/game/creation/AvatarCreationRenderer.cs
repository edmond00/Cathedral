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
/// Renders the avatar creation screen: body art on the left with interactive
/// organ-part highlighting, stats panel on the right with score bars.
/// Left-click art cells to +1 organ part score, right-click to -1.
/// </summary>
public class AvatarCreationRenderer
{
    private readonly TerminalHUD _terminal;
    private readonly Avatar _avatar;
    private readonly BodyArtData _artData;

    // Layout constants
    private const int ArtOffsetX = 0;     // Art starts at column 0
    private const int ArtOffsetY = 0;     // Art starts at row 0
    private const int PanelX = 62;        // Right panel starts at column 62
    private const int PanelWidth = 38;    // Cols 62-99
    private const int PanelContentX = 64; // Content inside panel border
    private const int PanelContentW = 34; // Content width

    // Footer
    private const int ContinueButtonY = 96;
    private const int ContinueButtonX = 72;
    private const int ContinueButtonW = 18;

    // State
    private string? _hoveredOrganPartName;  // organ_part id from CSV (e.g. "left_ear")
    private string? _hoveredBodyPartId;     // body part id (e.g. "face")
    private string? _hoveredOrganName;      // organ name (e.g. "ears")
    private string? _hoveredRawPartName;    // raw 7-value part name (e.g. "left_arm") for limb-side boxes
    private int _totalBudget;
    private bool _continueHovered;

    // Blink state for hovered organ part
    private readonly Stopwatch _blinkStopwatch = Stopwatch.StartNew();
    private double _lastBlinkTime;
    private bool _blinkOn = true;
    private const double BlinkInterval = 0.35; // seconds per blink toggle

    // Limb body parts that should use per-side (raw part) bounds
    private static readonly HashSet<string> LimbBodyParts = new() { "upper_limbs", "lower_limbs" };

    // Box drawing padding around body part bounds
    private const int BoxPadding = 1;

    // Mapping from organ part prefix + body part to raw part name
    private static readonly Dictionary<(bool isLeft, string bodyPartId), string> LimbSideToRawPart = new()
    {
        { (true, "upper_limbs"), "left_arm" },
        { (false, "upper_limbs"), "right_arm" },
        { (true, "lower_limbs"), "left_leg" },
        { (false, "lower_limbs"), "right_leg" },
    };

    // Callback for when the player clicks Continue
    public Action? OnContinue { get; set; }

    // Pre-computed: organ part char → organ part id mapping
    private readonly Dictionary<string, char> _organPartNameToChar;

    // Stats panel layout: body-part row positions
    private readonly List<(string bodyPartId, int startRow)> _bodyPartRows = new();
    // Organ part row positions for click detection
    private readonly Dictionary<int, string> _rowToOrganPartId = new();
    // Arrow button X positions per row (decX, incX)
    private readonly Dictionary<int, (int decX, int incX)> _rowToArrowX = new();

    public AvatarCreationRenderer(TerminalHUD terminal, Avatar avatar, BodyArtData artData)
    {
        _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        _avatar = avatar ?? throw new ArgumentNullException(nameof(avatar));
        _artData = artData ?? throw new ArgumentNullException(nameof(artData));

        // Build reverse mapping: organ_part name → char in organs.txt
        _organPartNameToChar = new Dictionary<string, char>();
        foreach (var (c, info) in _artData.OrganPartInfos)
            _organPartNameToChar[info.OrganPartName] = c;

        // Calculate total budget (sum of all current organ part scores)
        _totalBudget = GetTotalScore();

        ComputePanelLayout();
    }

    /// <summary>
    /// Full render of the creation screen.
    /// </summary>
    public void Render()
    {
        _terminal.Fill(' ', Config.Colors.Black, Config.Colors.Black);
        _terminal.Visible = true;

        RenderBodyArt();
        RenderStatsPanel();
        RenderFooter();
    }

    /// <summary>
    /// Called every frame. Handles blink animation for hovered organ part.
    /// </summary>
    public void Update()
    {
        if (_hoveredOrganPartName == null) return;

        double now = _blinkStopwatch.Elapsed.TotalSeconds;
        if (now - _lastBlinkTime >= BlinkInterval)
        {
            _lastBlinkTime = now;
            _blinkOn = !_blinkOn;
            // Only re-render the body art area (not the whole screen) for performance
            RenderBodyArt();
        }
    }

    /// <summary>
    /// Handle mouse hover at terminal coordinates.
    /// </summary>
    public void OnMouseMove(int x, int y)
    {
        string? newOrganPart = null;
        string? newBodyPart = null;
        string? newOrgan = null;
        bool newContinueHovered = IsOnContinueButton(x, y);

        // Check if hovering over art area
        int artX = x - ArtOffsetX;
        int artY = y - ArtOffsetY;
        string? newRawPartName = null;
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
                // Might be on a body-structure cell (not organ-specific)
                newBodyPart = _artData.GetBodyPartIdAt(artX, artY);
            }
            // Capture raw part name for limb-side detection.
            // Organ map has priority over body part map when they conflict.
            newRawPartName = _artData.GetPartNameAt(artX, artY);

            // If organ info provides a limb body part, infer raw part from organ part name
            // (handles both missing raw part and conflicting body part map)
            if (info != null && LimbBodyParts.Contains(newBodyPart ?? ""))
            {
                if (info.OrganPartName.StartsWith("left_"))
                    newRawPartName = LimbSideToRawPart.GetValueOrDefault((true, newBodyPart!));
                else if (info.OrganPartName.StartsWith("right_"))
                    newRawPartName = LimbSideToRawPart.GetValueOrDefault((false, newBodyPart!));
            }
        }

        // Check if hovering over stats panel organ part rows
        if (x >= PanelX && x < PanelX + PanelWidth)
        {
            if (_rowToOrganPartId.TryGetValue(y, out var partId))
            {
                newOrganPart = partId;
                // Find the organ and body part for this organ part
                var op = FindOrganPartByName(partId);
                if (op != null)
                {
                    newOrgan = op.Value.organName;
                    newBodyPart = op.Value.bodyPartId;
                    // Determine limb side from organ part name for stats panel hover
                    if (LimbBodyParts.Contains(op.Value.bodyPartId) && partId.StartsWith("left_"))
                        newRawPartName = LimbSideToRawPart.GetValueOrDefault((true, op.Value.bodyPartId));
                    else if (LimbBodyParts.Contains(op.Value.bodyPartId) && partId.StartsWith("right_"))
                        newRawPartName = LimbSideToRawPart.GetValueOrDefault((false, op.Value.bodyPartId));
                }
            }
        }

        bool changed = newOrganPart != _hoveredOrganPartName
                     || newBodyPart != _hoveredBodyPartId
                     || newRawPartName != _hoveredRawPartName
                     || newContinueHovered != _continueHovered;

        if (changed)
        {
            _hoveredOrganPartName = newOrganPart;
            _hoveredBodyPartId = newBodyPart;
            _hoveredOrganName = newOrgan;
            _hoveredRawPartName = newRawPartName;
            _continueHovered = newContinueHovered;
            // Reset blink state when hover target changes
            _lastBlinkTime = _blinkStopwatch.Elapsed.TotalSeconds;
            _blinkOn = true;
            Render(); // Re-render with new highlights
        }
    }

    /// <summary>
    /// Handle left click at terminal coordinates (+1 to organ part score).
    /// </summary>
    public void OnMouseClick(int x, int y)
    {
        // Check continue button
        if (IsOnContinueButton(x, y))
        {
            OnContinue?.Invoke();
            return;
        }

        // Check arrow buttons in stats panel
        int arrowDelta = GetArrowClickDelta(x, y);
        if (arrowDelta != 0)
        {
            if (_rowToOrganPartId.TryGetValue(y, out var arrowPartId))
            {
                AdjustOrganPartScore(arrowPartId, arrowDelta);
                Render();
                return;
            }
        }

        var partId = GetOrganPartAtPosition(x, y);
        if (partId != null)
        {
            AdjustOrganPartScore(partId, +1);
            Render();
        }
    }

    /// <summary>
    /// Handle right click at terminal coordinates (-1 to organ part score).
    /// </summary>
    public void OnRightClick(int x, int y)
    {
        // Check arrow buttons in stats panel (right-click on arrows also works)
        int arrowDelta = GetArrowClickDelta(x, y);
        if (arrowDelta != 0)
        {
            if (_rowToOrganPartId.TryGetValue(y, out var arrowPartId))
            {
                AdjustOrganPartScore(arrowPartId, arrowDelta);
                Render();
                return;
            }
        }

        var partId = GetOrganPartAtPosition(x, y);
        if (partId != null)
        {
            AdjustOrganPartScore(partId, -1);
            Render();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Rendering
    // ═══════════════════════════════════════════════════════════════

    private void RenderBodyArt()
    {
        // Compute bounding box for hovered body part (if any)
        // For limbs (upper_limbs, lower_limbs), use per-side raw part bounds
        ArtBounds? hoveredBounds = null;
        if (_hoveredBodyPartId != null)
        {
            if (LimbBodyParts.Contains(_hoveredBodyPartId) && _hoveredRawPartName != null)
            {
                // Use raw part bounds for the specific limb side
                hoveredBounds = _artData.GetRawPartBounds(_hoveredRawPartName);
            }
            else
            {
                hoveredBounds = _artData.GetBodyPartBounds(_hoveredBodyPartId);
            }
        }

        // Determine box rectangle in terminal coords (with padding)
        int boxX1 = -1, boxY1 = -1, boxX2 = -1, boxY2 = -1;
        if (hoveredBounds != null)
        {
            boxX1 = ArtOffsetX + hoveredBounds.MinX - BoxPadding;
            boxY1 = ArtOffsetY + hoveredBounds.MinY - BoxPadding;
            boxX2 = ArtOffsetX + hoveredBounds.MaxX + BoxPadding;
            boxY2 = ArtOffsetY + hoveredBounds.MaxY + BoxPadding;
            // Clamp to art area (leave room for separator)
            boxX1 = Math.Max(0, boxX1);
            boxY1 = Math.Max(0, boxY1);
            boxX2 = Math.Min(PanelX - 2, boxX2);
            boxY2 = Math.Min(_terminal.Height - 1, boxY2);
        }

        // Clear the art area first
        for (int ty = 0; ty < _artData.Height && ty < _terminal.Height; ty++)
            for (int tx = 0; tx < PanelX - 1 && tx < _terminal.Width; tx++)
                _terminal.SetCell(tx, ty, ' ', Config.Colors.Black, Config.Colors.Black);

        // Render all body cells
        for (int ay = 0; ay < _artData.Height; ay++)
        {
            for (int ax = 0; ax < _artData.Width; ax++)
            {
                int tx = ArtOffsetX + ax;
                int ty = ArtOffsetY + ay;

                char artChar = _artData.ArtGrid[ax, ay];
                if (artChar == ' ' || artChar == '\0') continue;

                // Get base color from layer
                Vector4 baseColor = _artData.GetLayerColorAt(ax, ay);
                Vector4 bgColor = Config.Colors.Black;

                bool isBodyCell = _artData.IsBodyCell(ax, ay);
                if (!isBodyCell) continue;

                // Determine cell category for highlighting
                var organInfo = _artData.GetOrganPartInfoAt(ax, ay);
                string? cellBodyPartId = _artData.GetBodyPartIdAt(ax, ay);
                string? cellRawPartName = _artData.GetPartNameAt(ax, ay);
                
                // For limbs, only highlight cells on the same side
                bool isHoveredBodyPart;
                if (cellBodyPartId != null && cellBodyPartId == _hoveredBodyPartId
                    && LimbBodyParts.Contains(cellBodyPartId) && _hoveredRawPartName != null)
                {
                    isHoveredBodyPart = cellRawPartName == _hoveredRawPartName;
                }
                else
                {
                    isHoveredBodyPart = cellBodyPartId != null && cellBodyPartId == _hoveredBodyPartId;
                }
                bool isHoveredOrgan = organInfo != null && _hoveredOrganName != null && organInfo.OrganName == _hoveredOrganName;
                bool isHoveredOrganPart = organInfo != null && organInfo.OrganPartName == _hoveredOrganPartName;

                if (isHoveredOrganPart)
                {
                    // Level 4: Hovered organ part — strongest luminosity boost + blinking
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
                    // Level 3: Same organ, different part — strong luminosity boost
                    baseColor = AdjustLuminosity(baseColor, 2.2f);
                    bgColor = new Vector4(0.12f, 0.12f, 0.0f, 1.0f);
                }
                else if (isHoveredBodyPart && organInfo != null)
                {
                    // Level 2b: Body part cell that IS an organ — medium luminosity boost
                    baseColor = AdjustLuminosity(baseColor, 1.6f);
                    bgColor = new Vector4(0.05f, 0.05f, 0.0f, 1.0f);
                }
                else if (isHoveredBodyPart)
                {
                    // Level 2a: Body part cell not belonging to a specific organ — slight boost
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

            // Level 1: Fill cells inside box that are NOT body cells with a slight highlight
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
                        // Slight highlight: very dim yellow background, dot pattern
                        _terminal.SetCell(tx, ty, '·', new Vector4(0.15f, 0.15f, 0.05f, 1.0f),
                            new Vector4(0.03f, 0.03f, 0.0f, 1.0f));
                    }
                }
            }

            // Draw the box border
            // Top edge
            _terminal.SetCell(boxX1, boxY1, BoxChars.Single.TopLeft, boxColor, boxBg);
            _terminal.SetCell(boxX2, boxY1, BoxChars.Single.TopRight, boxColor, boxBg);
            for (int tx = boxX1 + 1; tx < boxX2; tx++)
                _terminal.SetCell(tx, boxY1, BoxChars.Single.Horizontal, boxColor, boxBg);
            // Bottom edge
            _terminal.SetCell(boxX1, boxY2, BoxChars.Single.BottomLeft, boxColor, boxBg);
            _terminal.SetCell(boxX2, boxY2, BoxChars.Single.BottomRight, boxColor, boxBg);
            for (int tx = boxX1 + 1; tx < boxX2; tx++)
                _terminal.SetCell(tx, boxY2, BoxChars.Single.Horizontal, boxColor, boxBg);
            // Left edge
            for (int ty = boxY1 + 1; ty < boxY2; ty++)
                _terminal.SetCell(boxX1, ty, BoxChars.Single.Vertical, boxColor, boxBg);
            // Right edge
            for (int ty = boxY1 + 1; ty < boxY2; ty++)
                _terminal.SetCell(boxX2, ty, BoxChars.Single.Vertical, boxColor, boxBg);
        }

        // Draw separator line between art and panel
        int sepX = PanelX - 1;
        for (int y = 0; y < 100; y++)
            _terminal.SetCell(sepX, y, '│', Config.Colors.DarkGray35, Config.Colors.Black);

        // Draw connector arrows (must be after body art so they aren't cleared by blink redraws)
        RenderArrows();
    }

    private void RenderArrows()
    {
        if (_hoveredOrganPartName == null) return;
        if (!_organPartNameToChar.TryGetValue(_hoveredOrganPartName, out char organChar)) return;

        var cells = _artData.GetOrganPartCells(organChar);
        if (cells.Count == 0) return;

        // Find the rightmost cell of the organ part (arrow starts ON this cell)
        var rightmost = cells.OrderByDescending(c => c.x).First();
        int artEndX = ArtOffsetX + rightmost.x;
        int artEndY = ArtOffsetY + rightmost.y;

        // Find the stats row for this organ part
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

    private void RenderStatsPanel()
    {
        // Title
        _terminal.Text(PanelContentX, 1, "A V A T A R", Config.Colors.BrightYellow, Config.Colors.Black);
        _terminal.Text(PanelContentX, 2, "C R E A T I O N", Config.Colors.DarkYellowGrey, Config.Colors.Black);
        _terminal.Text(PanelContentX, 4, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);

        // Render each body part section
        int row = 6;
        foreach (var bp in _avatar.BodyParts)
        {
            bool isHoveredBP = bp.Id == _hoveredBodyPartId;
            Vector4 headerColor = isHoveredBP ? Config.Colors.BrightYellow : Config.Colors.LightGray75;
            Vector4 headerBg = isHoveredBP ? new Vector4(0.1f, 0.1f, 0.0f, 1.0f) : Config.Colors.Black;

            // Body part header with total score
            string header = $"▸ {bp.DisplayName.ToUpper()}";
            string scoreStr = $"[{bp.Score}]";
            _terminal.Text(PanelContentX, row, header, headerColor, headerBg);
            _terminal.Text(PanelContentX + PanelContentW - scoreStr.Length, row, scoreStr, 
                Config.Colors.DarkYellowGrey, headerBg);
            row++;

            // Organs and their parts
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

                    // Format: "  PartName     ██████░░░░ 6"
                    string name = FormatPartName(part.DisplayName);
                    int nameWidth = 14;
                    string paddedName = name.Length > nameWidth
                        ? name[..nameWidth]
                        : name.PadRight(nameWidth);

                    // Draw name
                    _terminal.Text(PanelContentX + 1, row, paddedName, nameColor, bg);

                    // Draw score bar (width = MaxScore)
                    int barX = PanelContentX + 1 + nameWidth + 1;
                    int barWidth = part.MaxScore;
                    for (int i = 0; i < barWidth; i++)
                    {
                        char barChar = i < part.Score ? '█' : '░';
                        Vector4 barColor = i < part.Score ? barFill : barEmpty;
                        _terminal.SetCell(barX + i, row, barChar, barColor, bg);
                    }

                    // Draw ◄ score ► layout
                    Vector4 arrowColor = isHoveredPart ? Config.Colors.MediumYellow : Config.Colors.DarkGray35;
                    _terminal.SetCell(barX + barWidth, row, ' ', Config.Colors.Black, bg);
                    _terminal.SetCell(barX + barWidth + 1, row, '◄', arrowColor, bg);
                    _terminal.SetCell(barX + barWidth + 2, row, (char)('0' + part.Score), scoreFg, bg);
                    _terminal.SetCell(barX + barWidth + 3, row, '►', arrowColor, bg);

                    // Store arrow positions for click detection
                    _rowToArrowX[row] = (barX + barWidth + 1, barX + barWidth + 3);

                    // Fill remaining background
                    for (int fx = barX + barWidth + 4; fx < PanelX + PanelWidth; fx++)
                        _terminal.SetCell(fx, row, ' ', Config.Colors.Black, bg);
                    for (int fx = PanelContentX; fx < PanelContentX + 1; fx++)
                        _terminal.SetCell(fx, row, ' ', Config.Colors.Black, bg);

                    row++;
                }
            }

            row++; // Gap between body parts
        }

        // Hovered organ detail (if any)
        if (_hoveredOrganPartName != null)
        {
            row = Math.Max(row + 1, 50);
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
                _terminal.Text(PanelContentX, row, "Left-click: +1", Config.Colors.DarkYellowGrey, Config.Colors.Black);
                row++;
                _terminal.Text(PanelContentX, row, "Right-click: -1", Config.Colors.DarkYellowGrey, Config.Colors.Black);
            }
        }
    }

    private void RenderFooter()
    {
        int totalScore = GetTotalScore();

        // Points display
        _terminal.Text(PanelContentX, 92, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);
        string pointsText = $"Total Points: {totalScore}";
        _terminal.Text(PanelContentX, 94, pointsText, Config.Colors.LightGray75, Config.Colors.Black);

        // Continue button
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

    // ═══════════════════════════════════════════════════════════════
    // Input helpers
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns -1 if clicking ◄, +1 if clicking ►, 0 if not on an arrow.
    /// </summary>
    private int GetArrowClickDelta(int x, int y)
    {
        if (_rowToArrowX.TryGetValue(y, out var arrows))
        {
            if (x == arrows.decX) return -1;
            if (x == arrows.incX) return +1;
        }
        return 0;
    }

    private string? GetOrganPartAtPosition(int x, int y)
    {
        // Check art area
        int artX = x - ArtOffsetX;
        int artY = y - ArtOffsetY;
        if (artX >= 0 && artX < _artData.Width && artY >= 0 && artY < _artData.Height)
        {
            var info = _artData.GetOrganPartInfoAt(artX, artY);
            if (info != null) return info.OrganPartName;
        }

        // Check stats panel rows
        if (x >= PanelX && x < PanelX + PanelWidth)
        {
            if (_rowToOrganPartId.TryGetValue(y, out var partId))
                return partId;
        }

        return null;
    }

    private bool IsOnContinueButton(int x, int y)
    {
        return y == ContinueButtonY && x >= ContinueButtonX && x < ContinueButtonX + ContinueButtonW;
    }

    private void AdjustOrganPartScore(string organPartName, int delta)
    {
        var organPart = _avatar.BodyParts
            .SelectMany(bp => bp.Organs)
            .SelectMany(o => o.Parts)
            .FirstOrDefault(p => p.Id == organPartName);

        if (organPart != null)
        {
            organPart.Score = Math.Clamp(organPart.Score + delta, 0, organPart.MaxScore);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Layout computation
    // ═══════════════════════════════════════════════════════════════

    private void ComputePanelLayout()
    {
        _rowToOrganPartId.Clear();
        _rowToArrowX.Clear();
        _bodyPartRows.Clear();

        int row = 6;
        foreach (var bp in _avatar.BodyParts)
        {
            _bodyPartRows.Add((bp.Id, row));
            row++; // header row

            foreach (var organ in bp.Organs)
            {
                foreach (var part in organ.Parts)
                {
                    _rowToOrganPartId[row] = part.Id;
                    row++;
                }
            }
            row++; // gap
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // Utility
    // ═══════════════════════════════════════════════════════════════

    private int GetTotalScore()
    {
        return _avatar.BodyParts
            .SelectMany(bp => bp.Organs)
            .SelectMany(o => o.Parts)
            .Sum(p => p.Score);
    }

    private (string bodyPartId, string bodyPartDisplayName, string organName, string organDisplayName, string partId, string partDisplayName)?
        FindOrganPartByName(string organPartName)
    {
        foreach (var bp in _avatar.BodyParts)
            foreach (var organ in bp.Organs)
                foreach (var part in organ.Parts)
                    if (part.Id == organPartName)
                        return (bp.Id, bp.DisplayName, organ.Id, organ.DisplayName, part.Id, part.DisplayName);
        return null;
    }

    private static string FormatPartName(string displayName)
    {
        // Shorten "Left" → "L." and "Right" → "R." for compact display
        return displayName
            .Replace("Left ", "L.")
            .Replace("Right ", "R.");
    }

    private static Vector4 AdjustLuminosity(Vector4 color, float multiplier)
    {
        return new Vector4(
            Math.Clamp(color.X * multiplier, 0f, 1f),
            Math.Clamp(color.Y * multiplier, 0f, 1f),
            Math.Clamp(color.Z * multiplier, 0f, 1f),
            1.0f
        );
    }
}
