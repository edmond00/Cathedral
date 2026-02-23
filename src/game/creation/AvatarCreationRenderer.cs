using System;
using System.Collections.Generic;
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
    private int _totalBudget;
    private bool _continueHovered;

    // Callback for when the player clicks Continue
    public Action? OnContinue { get; set; }

    // Pre-computed: organ part char → organ part id mapping
    private readonly Dictionary<string, char> _organPartNameToChar;

    // Stats panel layout: body-part row positions
    private readonly List<(string bodyPartId, int startRow)> _bodyPartRows = new();
    // Organ part row positions for click detection
    private readonly Dictionary<int, string> _rowToOrganPartId = new();

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
                }
            }
        }

        bool changed = newOrganPart != _hoveredOrganPartName
                     || newBodyPart != _hoveredBodyPartId
                     || newContinueHovered != _continueHovered;

        if (changed)
        {
            _hoveredOrganPartName = newOrganPart;
            _hoveredBodyPartId = newBodyPart;
            _hoveredOrganName = newOrgan;
            _continueHovered = newContinueHovered;
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

                // Check if this cell is part of the body
                if (!_artData.IsBodyCell(ax, ay)) continue;

                // Apply highlighting based on hover state
                var organInfo = _artData.GetOrganPartInfoAt(ax, ay);
                string? cellBodyPartId = _artData.GetBodyPartIdAt(ax, ay);

                if (organInfo != null && organInfo.OrganPartName == _hoveredOrganPartName)
                {
                    // Hovered organ part: bright yellow
                    baseColor = Config.Colors.BrightYellow;
                    bgColor = new Vector4(0.15f, 0.15f, 0.0f, 1.0f);
                }
                else if (organInfo != null && _hoveredOrganName != null && organInfo.OrganName == _hoveredOrganName)
                {
                    // Same organ, different part: medium yellow
                    baseColor = Config.Colors.MediumYellow;
                    bgColor = new Vector4(0.08f, 0.08f, 0.0f, 1.0f);
                }
                else if (cellBodyPartId != null && cellBodyPartId == _hoveredBodyPartId)
                {
                    // Same body part: subtle dark yellow tint
                    baseColor = TintColor(baseColor, Config.Colors.DarkYellow, 0.3f);
                }

                _terminal.SetCell(tx, ty, artChar, baseColor, bgColor);
            }
        }

        // Draw separator line between art and panel
        int sepX = PanelX - 1;
        for (int y = 0; y < 100; y++)
            _terminal.SetCell(sepX, y, '│', Config.Colors.DarkGray35, Config.Colors.Black);
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

                    // Draw score bar (10 chars wide)
                    int barX = PanelContentX + 1 + nameWidth + 1;
                    for (int i = 0; i < 10; i++)
                    {
                        char barChar = i < part.Score ? '█' : '░';
                        Vector4 barColor = i < part.Score ? barFill : barEmpty;
                        _terminal.SetCell(barX + i, row, barChar, barColor, bg);
                    }

                    // Draw numeric score
                    string scoreText = $" {part.Score,2}";
                    _terminal.Text(barX + 10, row, scoreText, scoreFg, bg);

                    // Fill remaining background
                    for (int fx = barX + 10 + scoreText.Length; fx < PanelX + PanelWidth; fx++)
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

        // Draw connector arrows from hovered organ part cells to stats panel
        if (_hoveredOrganPartName != null && _organPartNameToChar.TryGetValue(_hoveredOrganPartName, out char organChar))
        {
            var cells = _artData.GetOrganPartCells(organChar);
            if (cells.Count > 0)
            {
                // Find the rightmost cell of the organ part
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
                        artEndX + 1, artEndY,
                        PanelContentX - 1, statsRow,
                        Config.Colors.DarkYellowGrey, Config.Colors.Black);
                }
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

    private static Vector4 TintColor(Vector4 baseColor, Vector4 tintColor, float amount)
    {
        return new Vector4(
            baseColor.X + (tintColor.X - baseColor.X) * amount,
            baseColor.Y + (tintColor.Y - baseColor.Y) * amount,
            baseColor.Z + (tintColor.Z - baseColor.Z) * amount,
            1.0f
        );
    }
}
