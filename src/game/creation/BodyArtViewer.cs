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
    private BodyArtData _artData;

    // ── Layout constants ─────────────────────────────────────────
    public const int PanelX = 68;
    public const int PanelWidth = 32;
    public const int PanelContentX = 70;
    public const int PanelContentW = 28;

    // ── Configuration ────────────────────────────────────────────
    /// <summary>Horizontal offset for the body art (default 0, increase to shift art right).</summary>
    public int ArtOffsetX { get; set; } = 0;

    /// <summary>Vertical offset for the body art (default 0).</summary>
    public int ArtOffsetY { get; set; } = 0;

    /// <summary>Row where organ-stat rows begin.</summary>
    public int StatsStartRow { get; set; } = 6;

    /// <summary>When true, wound glyphs and HP bar are rendered (body submenu only).</summary>
    public bool ShowWounds { get; set; } = false;

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
    private char? _hoveredWoundId;          // set when mouse is precisely on a wound ∅ glyph
    private Wound? _hoveredWoundInstance;  // specific wound instance being hovered (used for wildcard position)
    // ── Blink state ──────────────────────────────────────────────
    private readonly Stopwatch _blinkStopwatch = Stopwatch.StartNew();
    private double _lastBlinkTime;
    private bool _blinkOn = true;
    private const double BlinkInterval = 0.35;

    // ── Static constants ─────────────────────────────────────────
    internal static readonly HashSet<string> LimbBodyParts = new() { "upper_limbs", "lower_limbs", "limbs" };
    internal static readonly Dictionary<(bool isLeft, string bodyPartId), string> LimbSideToRawPart = new()
    {
        { (true,  "upper_limbs"), "zone_left_arm" },
        { (false, "upper_limbs"), "zone_right_arm" },
        { (true,  "lower_limbs"), "zone_left_leg" },
        { (false, "lower_limbs"), "zone_right_leg" },
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

    /// <summary>
    /// Swap both the displayed party member and the body art data (used when the new member
    /// has a different anatomy type, e.g. switching from human to beast companion).
    /// </summary>
    public void SwapSubjectWithArt(PartyMember newSubject, BodyArtData newArtData)
    {
        _protagonist = newSubject  ?? throw new ArgumentNullException(nameof(newSubject));
        _artData     = newArtData  ?? throw new ArgumentNullException(nameof(newArtData));
        // Rebuild the organ-part name→char lookup for the new art data
        _organPartNameToChar.Clear();
        foreach (var (c, info) in _artData.OrganPartInfos)
            _organPartNameToChar[info.OrganPartName] = c;
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
        if (_hoveredOrganPartName == null && _protagonist.Wounds.Count == 0) return false;
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
                // Try the static side-lookup first (human anatomy: upper_limbs / lower_limbs).
                // For beast ("limbs") the dict has no entry → keep the zone from GetPartNameAt.
                string? sideRawPart = null;
                if (info.OrganPartName.StartsWith("left_"))
                    sideRawPart = LimbSideToRawPart.GetValueOrDefault((true, newBodyPart!));
                else if (info.OrganPartName.StartsWith("right_"))
                    sideRawPart = LimbSideToRawPart.GetValueOrDefault((false, newBodyPart!));
                if (sideRawPart != null)
                    newRawPartName = sideRawPart;
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
                    if (LimbBodyParts.Contains(op.Value.bodyPartId))
                    {
                        // Try static lookup (human anatomy)
                        string? sideRawPart = null;
                        if (partId.StartsWith("left_"))
                            sideRawPart = LimbSideToRawPart.GetValueOrDefault((true, op.Value.bodyPartId));
                        else if (partId.StartsWith("right_"))
                            sideRawPart = LimbSideToRawPart.GetValueOrDefault((false, op.Value.bodyPartId));

                        if (sideRawPart != null)
                        {
                            newRawPartName = sideRawPart;
                        }
                        else if (_organPartNameToChar.TryGetValue(partId, out char organChar))
                        {
                            // Fallback: derive zone from the art data (e.g. beast limbs)
                            newRawPartName = _artData.GetRawPartNameForOrganPartChar(organChar);
                        }
                    }
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

        // Check if hovering precisely on a wound glyph (only when ShowWounds is active)
        char? newWoundId = null;
        Wound? newWoundInstance = null;
        if (ShowWounds && artX >= 0 && artX < _artData.Width && artY >= 0 && artY < _artData.Height)
        {
            foreach (var wound in _protagonist.Wounds)
            {
                bool hit;
                if (wound.ArtX != null)
                    hit = wound.ArtX.Value == artX && wound.ArtY!.Value == artY;
                else if (_artData.WoundPositions.TryGetValue(wound.WoundId, out var positions))
                    hit = positions.Any(p => p.x == artX && p.y == artY);
                else
                    hit = false;
                if (hit) { newWoundId = wound.WoundId; newWoundInstance = wound; break; }
            }
        }

        changed = changed || newWoundId != _hoveredWoundId || newWoundInstance != _hoveredWoundInstance;

        if (changed)
        {
            _hoveredOrganPartName = newOrganPart;
            _hoveredBodyPartId = newBodyPart;
            _hoveredOrganName = newOrgan;
            _hoveredRawPartName = newRawPartName;
            _hoveredArrowDelta = newArrowDelta;
            _hoveredWoundId = newWoundId;
            _hoveredWoundInstance = newWoundInstance;
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
        _hoveredWoundId = null;
        _hoveredWoundInstance = null;
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
        // Compute bounding box for hovered body part (suppressed when wound glyph is precisely hovered)
        ArtBounds? hoveredBounds = null;
        if (_hoveredBodyPartId != null && _hoveredWoundId == null)
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

        // Clear the full art area (full terminal height to avoid artifacts when art is shorter than 100)
        for (int ty = 0; ty < _terminal.Height; ty++)
            for (int tx = 0; tx < PanelX - 1 && tx < _terminal.Width; tx++)
                _terminal.SetCell(tx, ty, ' ', Config.Colors.Black, Config.Colors.Black);

        // Render all body cells with highlight levels
        for (int ay = 0; ay < _artData.Height; ay++)
        {
            for (int ax = 0; ax < _artData.Width; ax++)
            {
                int tx = ArtOffsetX + ax;
                int ty = ArtOffsetY + ay;

                // Clip: don't let art bleed into the right panel separator
                if (tx >= PanelX - 1) continue;

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
                // Suppress all organ/body-part highlights when hovering precisely on a wound glyph
                bool isHoveredBodyPart;
                if (_hoveredWoundId != null)
                {
                    isHoveredBodyPart = false;
                }
                else if (cellBodyPartId != null && cellBodyPartId == _hoveredBodyPartId
                    && LimbBodyParts.Contains(cellBodyPartId) && _hoveredRawPartName != null)
                    isHoveredBodyPart = cellRawPartName == _hoveredRawPartName;
                else
                    isHoveredBodyPart = cellBodyPartId != null && cellBodyPartId == _hoveredBodyPartId;

                bool isHoveredOrgan = _hoveredWoundId == null && organInfo != null && _hoveredOrganName != null && organInfo.OrganName == _hoveredOrganName;
                bool isHoveredOrganPart = _hoveredWoundId == null && organInfo != null && organInfo.OrganPartName == _hoveredOrganPartName;

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

        // Overlay wound glyphs (∅) on the body art, blinking orange/dark-grey (body submenu only)
        if (ShowWounds)
        {
            // Assign free art positions to any wildcard wounds that haven't been placed yet
            var occupiedByWounds = new HashSet<(int, int)>(
                _artData.WoundPositions.Values.SelectMany(v => v).Select(p => (p.x, p.y)));
            // Also count already-placed wildcard instances
            foreach (var w in _protagonist.Wounds)
                if (w.ArtX != null) occupiedByWounds.Add((w.ArtX.Value, w.ArtY!.Value));
            // Collect all free body cells once, then pick randomly for each unplaced wildcard
            var freeCells = new List<(int x, int y)>();
            for (int fy2 = 0; fy2 < _artData.Height; fy2++)
                for (int fx2 = 0; fx2 < _artData.Width; fx2++)
                    if (_artData.IsBodyCell(fx2, fy2) && !occupiedByWounds.Contains((fx2, fy2)))
                        freeCells.Add((fx2, fy2));
            var rng = new Random();
            foreach (var wound in _protagonist.Wounds.Where(w => w.ArtX == null && w.TargetKind == Cathedral.Game.Narrative.WoundTargetKind.Wildcard))
            {
                if (freeCells.Count == 0) break;
                // Prefer cells in the wound's target zone; fall back to any free cell
                var pool = wound.WildcardZoneHint != null
                    ? freeCells.Where(p => IsWoundCellInZone(p.x, p.y, wound.WildcardZoneHint)).ToList()
                    : freeCells;
                if (pool.Count == 0) pool = freeCells;
                int idx = rng.Next(pool.Count);
                var chosen = pool[idx];
                freeCells.Remove(chosen);
                wound.ArtX = chosen.x;
                wound.ArtY = chosen.y;
                occupiedByWounds.Add(chosen);
            }

            Vector4 woundColor = _blinkOn ? Config.Colors.Orange : Config.Colors.DarkGray35;
            foreach (var wound in _protagonist.Wounds)
            {
                IEnumerable<(int x, int y)> positions;
                if (wound.ArtX != null)
                    positions = new[] { (wound.ArtX.Value, wound.ArtY!.Value) };
                else if (_artData.WoundPositions.TryGetValue(wound.WoundId, out var wpos))
                    positions = wpos;
                else
                    continue;
                foreach (var (wx, wy) in positions)
                {
                    int tx = ArtOffsetX + wx;
                    int ty = ArtOffsetY + wy;
                    if (tx >= 0 && tx < PanelX - 1 && ty >= 0 && ty < _terminal.Height)
                        _terminal.SetCell(tx, ty, '∅', woundColor, Config.Colors.Black);
                }
            }
        }

        // HP bar overlaid in the black space at the top of the art area
        if (ShowWounds)
            RenderHpBar();

        // Separator line between art and panel
        int sepX = PanelX - 1;
        for (int y = 0; y < 100; y++)
            _terminal.SetCell(sepX, y, '│', Config.Colors.DarkGray35, Config.Colors.Black);

        // Connector arrows
        RenderArrows();
    }

    /// <summary>
    /// <summary>
    /// Returns true if the art cell at (x, y) belongs to the given zone hint.
    /// Checks body-part id first, then organ-part name and organ name from organs.csv.
    /// </summary>
    private bool IsWoundCellInZone(int x, int y, string zoneHint)
    {
        if (_artData.GetBodyPartIdAt(x, y) == zoneHint) return true;
        var info = _artData.GetOrganPartInfoAt(x, y);
        return info?.OrganPartName == zoneHint || info?.OrganName == zoneHint;
    }

    /// Renders an HP bar in the black empty space at the top of the body art panel.
    /// One █ per HP point: DarkYellowGrey for remaining, DarkGray35 for lost.
    /// </summary>
    private void RenderHpBar()
    {
        int maxHp = _protagonist.MaxHp;
        int curHp = _protagonist.CurrentHp;
        if (maxHp <= 0) return;

        int barX  = ArtOffsetX + 2;
        int labelRow = ArtOffsetY + 1;
        int barRow   = ArtOffsetY + 2;

        // Label and numeric value
        string label = $"HP  {curHp}/{maxHp}";
        _terminal.Text(barX, labelRow, label, Config.Colors.DarkYellowGrey, Config.Colors.Black);

        // Individual cells — one per max HP
        for (int i = 0; i < maxHp; i++)
        {
            Vector4 c = i < curHp ? Config.Colors.DarkYellowGrey : Config.Colors.DarkGray35;
            _terminal.SetCell(barX + i, barRow, '█', c, Config.Colors.Black);
        }
    }

    private void RenderArrows()
    {
        // Orange arrow when hovering a wound glyph
        if (_hoveredWoundId != null)
        {
            int wx = -1, wy = -1;
            if (_hoveredWoundInstance?.ArtX != null)
            {
                // Wildcard wound: use stored art position
                wx = ArtOffsetX + _hoveredWoundInstance.ArtX.Value;
                wy = ArtOffsetY + _hoveredWoundInstance.ArtY!.Value;
            }
            else if (_artData.WoundPositions.TryGetValue(_hoveredWoundId.Value, out var wpositions) && wpositions.Count > 0)
            {
                // Pick the rightmost wound glyph position as arrow origin
                var rightmost = wpositions.OrderByDescending(p => p.x).First();
                wx = ArtOffsetX + rightmost.x;
                wy = ArtOffsetY + rightmost.y;
            }
            // Arrow target: row 53 = "minRow+1=50, +2 header, +1 WOUND label" i.e. title line of wound detail
            int targetRow = 54;
            if (wx >= 0 && wx < PanelX - 2)
            {
                ArrowRenderer.DrawConnector(_terminal,
                    wx, wy,
                    PanelContentX - 1, targetRow,
                    Config.Colors.Orange, Config.Colors.Black);
            }
            return;
        }

        if (_hoveredOrganPartName == null) return;
        if (!_organPartNameToChar.TryGetValue(_hoveredOrganPartName, out char organChar)) return;

        var cells = _artData.GetOrganPartCells(organChar);
        if (cells.Count == 0) return;

        var rightmostCell = cells.OrderByDescending(c => c.x).First();
        int artEndX = ArtOffsetX + rightmostCell.x;
        int artEndY = ArtOffsetY + rightmostCell.y;

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
            bool isHoveredBP = _hoveredWoundId == null && bp.Id == _hoveredBodyPartId;
            bool bpHasWounds = _protagonist.Wounds.Any(w =>
                w.AffectsBodyPart(bp.Id) ||
                bp.Organs.Any(o => o.Parts.Any(p => w.AffectsOrganPart(p.Id, o.Id, bp.Id))));
            Vector4 headerColor, headerBg, headerScoreColor;
            if (isHoveredBP)
            {
                headerColor      = Config.Colors.BrightYellow;
                headerBg         = new Vector4(0.1f, 0.1f, 0.0f, 1.0f);
                headerScoreColor = Config.Colors.DarkYellowGrey;
            }
            else if (bpHasWounds)
            {
                headerColor      = _blinkOn ? Config.Colors.Orange : Config.Colors.LightGray75;
                headerBg         = Config.Colors.Black;
                headerScoreColor = _blinkOn ? Config.Colors.Orange : Config.Colors.DarkGray35;
            }
            else
            {
                headerColor      = Config.Colors.LightGray75;
                headerBg         = Config.Colors.Black;
                headerScoreColor = Config.Colors.DarkYellowGrey;
            }

            string header = $"▸ {bp.DisplayName.ToUpper()}";
            string scoreStr = $"[{bp.Score}]";
            _terminal.Text(PanelContentX, row, header, headerColor, headerBg);
            _terminal.Text(PanelContentX + PanelContentW - scoreStr.Length, row, scoreStr,
                headerScoreColor, headerBg);
            row++;

            foreach (var organ in bp.Organs)
            {
                foreach (var part in organ.Parts)
                {
                    // ── Wound state (computed once, used for colors and bar rendering) ──
                    var partWounds = _protagonist.GetWoundsForOrganPart(part.Id, organ.Id, bp.Id);
                    bool partIsDisabled = partWounds.Any(w => w.Handicap == Cathedral.Game.Narrative.WoundHandicap.High);
                    bool partIsWounded  = partWounds.Count > 0;
                    int  partEffScore   = partIsDisabled ? 0 :
                        part.Score - partWounds.Count(w => w.Handicap == Cathedral.Game.Narrative.WoundHandicap.Medium);

                    bool isHoveredPart  = _hoveredWoundId == null && part.Id  == _hoveredOrganPartName;
                    bool isHoveredOrgan = _hoveredWoundId == null && organ.Id == _hoveredOrganName;

                    Vector4 nameColor, barFill, barEmpty, scoreFg, bg;
                    if (isHoveredPart)
                    {
                        nameColor = Config.Colors.BrightYellow;
                        barFill   = Config.Colors.GoldYellow;
                        barEmpty  = Config.Colors.DarkYellow;
                        scoreFg   = Config.Colors.BrightYellow;
                        bg        = new Vector4(0.12f, 0.12f, 0.0f, 1.0f);
                    }
                    else if (isHoveredOrgan)
                    {
                        nameColor = Config.Colors.MediumYellow;
                        barFill   = Config.Colors.MediumYellow;
                        barEmpty  = Config.Colors.DarkGray35;
                        scoreFg   = Config.Colors.MediumYellow;
                        bg        = new Vector4(0.06f, 0.06f, 0.0f, 1.0f);
                    }
                    else if (partIsDisabled)
                    {
                        nameColor = _blinkOn ? Config.Colors.Orange : Config.Colors.DarkGray35;
                        barFill   = Config.Colors.DarkGray20;
                        barEmpty  = Config.Colors.DarkGray20;
                        scoreFg   = _blinkOn ? Config.Colors.Orange : Config.Colors.DarkGray35;
                        bg        = Config.Colors.Black;
                    }
                    else if (partIsWounded)
                    {
                        nameColor = _blinkOn ? Config.Colors.Orange : Config.Colors.MediumGray60;
                        barFill   = Config.Colors.MediumGray50;
                        barEmpty  = Config.Colors.DarkGray20;
                        scoreFg   = _blinkOn ? Config.Colors.Orange : Config.Colors.MediumGray60;
                        bg        = Config.Colors.Black;
                    }
                    else
                    {
                        nameColor = Config.Colors.MediumGray60;
                        barFill   = Config.Colors.MediumGray50;
                        barEmpty  = Config.Colors.DarkGray20;
                        scoreFg   = Config.Colors.MediumGray60;
                        bg        = Config.Colors.Black;
                    }

                    string name = FormatPartName(part.DisplayName);
                    int nameWidth = 14;
                    string paddedName = name.Length > nameWidth ? name[..nameWidth] : name.PadRight(nameWidth);

                    _terminal.Text(PanelContentX + 1, row, paddedName, nameColor, bg);

                    int barX    = PanelContentX + 1 + nameWidth + 1;
                    int barWidth = part.MaxScore;

                    // ── Bar rendering ──────────────────────────────────────────────
                    if (partIsDisabled)
                    {
                        // XXX in blinking orange for fully disabled organs
                        for (int i = 0; i < barWidth; i++)
                        {
                            char xc = i switch { 0 => 'X', 1 => 'X', 2 => 'X', _ => ' ' };
                            _terminal.SetCell(barX + i, row, xc,
                                _blinkOn ? Config.Colors.Orange : Config.Colors.DarkGray35, bg);
                        }
                    }
                    else if (partIsWounded && !isHoveredPart && !isHoveredOrgan)
                    {
                        // Filled up to effective score, blinking orange for removed bars, empty after raw score
                        for (int i = 0; i < barWidth; i++)
                        {
                            char barChar; Vector4 barColor;
                            if (i < partEffScore)           { barChar = '█'; barColor = barFill; }
                            else if (i < part.Score)        { barChar = '█'; barColor = _blinkOn ? Config.Colors.Orange : Config.Colors.DarkGray35; }
                            else                            { barChar = '░'; barColor = barEmpty; }
                            _terminal.SetCell(barX + i, row, barChar, barColor, bg);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < barWidth; i++)
                        {
                            char barChar = i < part.Score ? '█' : '░';
                            _terminal.SetCell(barX + i, row, barChar,
                                i < part.Score ? barFill : barEmpty, bg);
                        }
                    }

                    // ── Score digit ───────────────────────────────────────────────
                    char scoreDigit = partIsDisabled ? 'X' :
                        (partIsWounded && !isHoveredPart && !isHoveredOrgan)
                            ? (char)('0' + Math.Max(0, partEffScore))
                            : (char)('0' + part.Score);

                    if (ShowScoreEditControls)
                    {
                        bool isDecHovered = isHoveredPart && _hoveredArrowDelta == -1;
                        bool isIncHovered = isHoveredPart && _hoveredArrowDelta == +1;
                        Vector4 decColor = isDecHovered ? Config.Colors.BrightYellow
                                         : isHoveredPart ? Config.Colors.MediumYellow
                                         : Config.Colors.DarkGray35;
                        Vector4 incColor = isIncHovered ? Config.Colors.BrightYellow
                                         : isHoveredPart ? Config.Colors.MediumYellow
                                         : Config.Colors.DarkGray35;
                        _terminal.SetCell(barX + barWidth,     row, ' ',         Config.Colors.Black, bg);
                        _terminal.SetCell(barX + barWidth + 1, row, '◄',         decColor, bg);
                        _terminal.SetCell(barX + barWidth + 2, row, scoreDigit,  scoreFg,  bg);
                        _terminal.SetCell(barX + barWidth + 3, row, '►',         incColor, bg);
                        _rowToArrowX[row] = (barX + barWidth + 1, barX + barWidth + 3);

                        for (int fx = barX + barWidth + 4; fx < PanelX + PanelWidth; fx++)
                            _terminal.SetCell(fx, row, ' ', Config.Colors.Black, bg);
                    }
                    else
                    {
                        _terminal.SetCell(barX + barWidth,     row, ' ',        Config.Colors.Black, bg);
                        _terminal.SetCell(barX + barWidth + 1, row, scoreDigit, scoreFg,  bg);

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
    /// Split into three sections: organ-part stats, organ stats, body-part stats.
    /// Any <see cref="DerivedStat"/> with a matching relation key is automatically shown
    /// using <see cref="DerivedStat.ShortDisplayName"/> and <see cref="DerivedStat.FormatValue"/>.
    /// </summary>
    public void RenderHoveredDetail(int minRow)
    {
        // ── Exclusive wound detail when hovering precisely on a wound glyph ──
        if (_hoveredWoundId != null)
        {
            var hovered = _hoveredWoundInstance;
            if (hovered != null)
            {
                int wRow = Math.Max(minRow + 1, 50);
                _terminal.Text(PanelContentX, wRow, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);
                wRow += 2;
                _terminal.Text(PanelContentX, wRow, "WOUND", Config.Colors.Orange, Config.Colors.Black);
                wRow += 2;
                _terminal.Text(PanelContentX, wRow, hovered.WoundName, Config.Colors.Black, Config.Colors.Orange);
                wRow += 2;
                string sev = hovered.Handicap == Cathedral.Game.Narrative.WoundHandicap.High ? "Severe (disables)" :
                             hovered.Handicap == Cathedral.Game.Narrative.WoundHandicap.Medium ? "Moderate (-1 penalty)" :
                             "Minor (-1 HP only)";
                _terminal.Text(PanelContentX, wRow, sev, hovered.Handicap == Cathedral.Game.Narrative.WoundHandicap.High
                    ? Config.Colors.Orange : Config.Colors.MediumGray60, Config.Colors.Black);
                wRow++;
                if (!string.IsNullOrEmpty(hovered.TargetId))
                {
                    _terminal.Text(PanelContentX, wRow, $"Affects: {hovered.TargetId}", Config.Colors.MediumGray60, Config.Colors.Black);
                    wRow++;
                }
                if (!string.IsNullOrEmpty(hovered.Description))
                {
                    wRow++;
                    int maxW = PanelContentW;
                    string desc = hovered.Description;
                    while (desc.Length > 0)
                    {
                        string chunk = desc.Length <= maxW ? desc : desc[..maxW];
                        int cut = chunk.Length < desc.Length ? (chunk.LastIndexOf(' ') > 0 ? chunk.LastIndexOf(' ') : chunk.Length) : chunk.Length;
                        _terminal.Text(PanelContentX, wRow, desc[..cut], Config.Colors.LightGray75, Config.Colors.Black);
                        wRow++;
                        desc = desc.Length > cut ? desc[(cut + 1)..] : "";
                    }
                }
            }
            return;
        }

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

            string organPartId      = opInfo.Value.partId;
            string organId          = opInfo.Value.organName;
            string organDisplayName = opInfo.Value.organDisplayName;
            string bodyPartId       = opInfo.Value.bodyPartId;
            string bodyPartDisplay  = opInfo.Value.bodyPartDisplayName;
            bool singlePartOrgan    = organPartId == organId;

            // ── Section 1+2 merged (single-part organs) or split (multi-part) ─
            if (singlePartOrgan)
            {
                // Organ part and organ are the same — collect stats from both relations
                var combined = _protagonist.DerivedStats
                    .Where(s => s.RelatedOrganPartId == organPartId || s.RelatedOrganId == organId)
                    .Distinct()
                    .ToList();
                if (combined.Count > 0)
                {
                    bool isSecretion = HumoralOrganIds.Contains(organId);
                    string header = isSecretion ? $"{organDisplayName} Secretion" : organDisplayName;
                    _terminal.Text(PanelContentX, row, header, Config.Colors.DarkYellowGrey, Config.Colors.Black);
                    row++;
                    foreach (var stat in combined)
                    {
                        int val = stat.CalculateValue(stat.GetSourceScore(_protagonist));
                        string line = $"  {stat.ShortDisplayName,-16} {stat.FormatValue(val)}";
                        _terminal.Text(PanelContentX, row, line, Config.Colors.LightGray75, Config.Colors.Black);
                        row++;
                    }
                    row++;
                }
            }
            else
            {
                // Section 1: Organ Part stats
                var organPartStats = _protagonist.DerivedStats
                    .Where(s => s.RelatedOrganPartId == organPartId)
                    .ToList();
                if (organPartStats.Count > 0)
                {
                    _terminal.Text(PanelContentX, row, opInfo.Value.partDisplayName, Config.Colors.DarkYellowGrey, Config.Colors.Black);
                    row++;
                    foreach (var stat in organPartStats)
                    {
                        int val = stat.CalculateValue(stat.GetSourceScore(_protagonist));
                        string line = $"  {stat.ShortDisplayName,-16} {stat.FormatValue(val)}";
                        _terminal.Text(PanelContentX, row, line, Config.Colors.LightGray75, Config.Colors.Black);
                        row++;
                    }
                    row++;
                }

                // Section 2: Organ stats
                var organStats = _protagonist.DerivedStats
                    .Where(s => s.RelatedOrganId == organId)
                    .ToList();
                if (organStats.Count > 0)
                {
                    bool isSecretion = HumoralOrganIds.Contains(organId);
                    string header = isSecretion ? $"{organDisplayName} Secretion" : organDisplayName;
                    _terminal.Text(PanelContentX, row, header, Config.Colors.DarkYellowGrey, Config.Colors.Black);
                    row++;
                    foreach (var stat in organStats)
                    {
                        int val = stat.CalculateValue(stat.GetSourceScore(_protagonist));
                        string line = $"  {stat.ShortDisplayName,-16} {stat.FormatValue(val)}";
                        _terminal.Text(PanelContentX, row, line, Config.Colors.LightGray75, Config.Colors.Black);
                        row++;
                    }
                    row++;
                }
            }

            // ── Section 3: Body Part stats ───────────────────────────────
            var bodyPartStats = _protagonist.DerivedStats
                .Where(s => s.RelatedBodyPartId == bodyPartId)
                .ToList();
            if (bodyPartStats.Count > 0)
            {
                _terminal.Text(PanelContentX, row, bodyPartDisplay, Config.Colors.DarkYellowGrey, Config.Colors.Black);
                row++;
                foreach (var stat in bodyPartStats)
                {
                    int val = stat.CalculateValue(stat.GetSourceScore(_protagonist));
                    string line = $"  {stat.ShortDisplayName,-16} {stat.FormatValue(val)}";
                    _terminal.Text(PanelContentX, row, line, Config.Colors.LightGray75, Config.Colors.Black);
                    row++;
                }
                row++;
            }

            // ── Section 4: Wounds on this organ part / organ / body part ──
            var wounds = _protagonist.GetWoundsForOrganPart(organPartId, organId, bodyPartId);
            if (wounds.Count > 0)
            {
                _terminal.Text(PanelContentX, row, "Wounds", Config.Colors.Orange, Config.Colors.Black);
                row++;
                foreach (var wound in wounds)
                {
                    string sev = wound.Handicap == Cathedral.Game.Narrative.WoundHandicap.High ? "●" : "◌";
                    string line = $"  {sev} {wound.WoundName}";
                    Vector4 wc = wound.Handicap == Cathedral.Game.Narrative.WoundHandicap.High
                        ? (_blinkOn ? Config.Colors.Orange : Config.Colors.DarkGray35)
                        : Config.Colors.MediumGray60;
                    _terminal.Text(PanelContentX, row, line, wc, Config.Colors.Black);
                    row++;
                }
                row++;
            }

            if (ShowClickHints)
            {
                _terminal.Text(PanelContentX, row, "Click: cycle score", Config.Colors.DarkYellowGrey, Config.Colors.Black);
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

    /// <summary>
    /// Cycles the score of an organ part: increments by 1, wrapping back to 0 after MaxScore.
    /// </summary>
    public void CycleOrganPartScore(string organPartName)
    {
        var organPart = _protagonist.BodyParts
            .SelectMany(bp => bp.Organs)
            .SelectMany(o => o.Parts)
            .FirstOrDefault(p => p.Id == organPartName);

        if (organPart != null)
            organPart.Score = organPart.Score >= organPart.MaxScore ? 0 : organPart.Score + 1;
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
