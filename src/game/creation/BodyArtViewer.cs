using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Terminal.Utils;
using Cathedral.Game.Narrative;
using Cathedral.Fight;

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
    public const int MaxBarWidth = 5;

    // ── Creation point budget ─────────────────────────────────────
    public const int PointBudget = 25;

    // ── Configuration ────────────────────────────────────────────
    /// <summary>Horizontal offset for the body art (default 0, increase to shift art right).</summary>
    public int ArtOffsetX { get; set; } = 0;

    /// <summary>Vertical offset for the body art (default 0).</summary>
    public int ArtOffsetY { get; set; } = 0;

    /// <summary>Row where organ-stat rows begin.</summary>
    public int StatsStartRow { get; set; } = 6;

    /// <summary>When true, wound glyphs and HP bar are rendered (body submenu only).</summary>
    public bool ShowWounds { get; set; } = false;

    /// <summary>When true, the age / remaining-lifetime readout is rendered (body submenu only).</summary>
    public bool ShowAge { get; set; } = false;

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
    private readonly Dictionary<int, string> _rowToBodyPartId = new();
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
        _rowToBodyPartId.Clear();
        _rowToArrowX.Clear();
        _bodyPartRows.Clear();

        int row = StatsStartRow;
        foreach (var bp in _protagonist.BodyParts)
        {
            _bodyPartRows.Add((bp.Id, row));
            _rowToBodyPartId[row] = bp.Id;
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
            else if (_rowToBodyPartId.TryGetValue(y, out var hoverBpId))
            {
                // Hovering a region header row → region selected (no organ, whole-region box).
                newBodyPart = hoverBpId;
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

            Vector4 woundColor = _blinkOn ? Config.Colors.Purple : Config.Colors.DarkGray35;
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

        // Age readout in the opposite (top-right) corner of the art area
        if (ShowAge)
            RenderAgeReadout();

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

    /// <summary>
    /// Age readout in the top-right of the art area, mirroring the HP bar opposite it.
    ///   Row 1: current age / lifetime, in days.
    ///   Row 2: a short bar and percentage showing how much life is left.
    /// Both are derived — age from the member's birth time against the global clock, lifetime from
    /// the heart — so nothing here needs refreshing when either changes.
    /// </summary>
    private void RenderAgeReadout()
    {
        int lifetime = _protagonist.GetLifetimeDays();
        if (lifetime <= 0) return;

        int age       = (int)Math.Round(_protagonist.GetAgeDays());
        int remaining = (int)Math.Round(_protagonist.GetRemainingLifeDays());
        float fraction = Math.Clamp((float)remaining / lifetime, 0f, 1f);
        int percent    = (int)Math.Round(fraction * 100f);

        // "Age " mirrors the "HP  " prefix on the opposite corner — both four cells wide.
        string label = $"Age {age}/{lifetime} d";
        string pct   = $"{percent}%";

        // Right-align both rows against the art area's right edge, kept clear of the separator.
        int rightEdge = Math.Min(ArtOffsetX + _artData.Width - 1, PanelX - 3);
        int labelRow  = ArtOffsetY + 1;
        int barRow    = ArtOffsetY + 2;

        int rowWidth = Math.Max(label.Length, AgeBarWidth + 1 + pct.Length);
        int x0       = rightEdge - rowWidth + 1;
        if (x0 < 0) return;   // art area too narrow to hold the readout

        // Row 1 — age / lifetime in days, right-aligned.
        _terminal.Text(rightEdge - label.Length + 1, labelRow, label,
            Config.Colors.DarkYellowGrey, Config.Colors.Black);

        // Row 2 — remaining-lifetime bar, then the percentage.
        int filled = (int)Math.Round(fraction * AgeBarWidth);
        Vector4 barColor = AgeBarColor(fraction);
        for (int i = 0; i < AgeBarWidth; i++)
        {
            Vector4 c = i < filled ? barColor : Config.Colors.DarkGray35;
            _terminal.SetCell(x0 + i, barRow, '█', c, Config.Colors.Black);
        }
        _terminal.Text(x0 + AgeBarWidth + 1, barRow, pct, barColor, Config.Colors.Black);
    }

    /// <summary>Width in cells of the remaining-lifetime bar.</summary>
    private const int AgeBarWidth = 10;

    /// <summary>Bar colour by remaining life: it sours as the end approaches.</summary>
    private static Vector4 AgeBarColor(float remainingFraction) => remainingFraction switch
    {
        <= 0.10f => Config.Colors.BrightPurple,   // on the threshold
        <= 0.25f => Config.Colors.Purple,         // failing
        _        => Config.Colors.DarkYellowGrey, // matches the HP bar opposite
    };

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
                    Config.Colors.Purple, Config.Colors.Black);
            }
            return;
        }

        // Region arrow: a region is hovered with no organ part (region name text or a non-organ
        // body cell). Connect the region's art box to its header row, without any organ highlight.
        if (_hoveredOrganPartName == null && _hoveredBodyPartId != null)
        {
            ArtBounds? regionBounds =
                (LimbBodyParts.Contains(_hoveredBodyPartId) && _hoveredRawPartName != null)
                    ? _artData.GetRawPartBounds(_hoveredRawPartName)
                    : _artData.GetBodyPartBounds(_hoveredBodyPartId);

            int headerRow = -1;
            foreach (var (bpId, startRow) in _bodyPartRows)
                if (bpId == _hoveredBodyPartId) { headerRow = startRow; break; }

            if (regionBounds != null && headerRow >= 0)
            {
                int ox = ArtOffsetX + regionBounds.MaxX;
                int oy = ArtOffsetY + (regionBounds.MinY + regionBounds.MaxY) / 2;
                if (ox < PanelX - 2)
                    ArrowRenderer.DrawConnector(_terminal,
                        ox, oy,
                        PanelContentX - 1, headerRow,
                        Config.Colors.MediumYellow, Config.Colors.Black);
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
                headerColor      = _blinkOn ? Config.Colors.Purple : Config.Colors.LightGray75;
                headerBg         = Config.Colors.Black;
                headerScoreColor = _blinkOn ? Config.Colors.Purple : Config.Colors.DarkGray35;
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
                        nameColor = _blinkOn ? Config.Colors.BrightPurple : Config.Colors.DarkGray35;
                        barFill   = Config.Colors.DarkGray20;
                        barEmpty  = Config.Colors.DarkGray20;
                        scoreFg   = _blinkOn ? Config.Colors.BrightPurple : Config.Colors.DarkGray35;
                        bg        = Config.Colors.Black;
                    }
                    else if (partIsWounded)
                    {
                        nameColor = _blinkOn ? Config.Colors.Purple : Config.Colors.MediumGray60;
                        barFill   = Config.Colors.MediumGray50;
                        barEmpty  = Config.Colors.DarkGray20;
                        scoreFg   = _blinkOn ? Config.Colors.Purple : Config.Colors.MediumGray60;
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
                        // XXX in blinking purple for fully disabled organs
                        for (int i = 0; i < barWidth; i++)
                        {
                            char xc = i switch { 0 => 'X', 1 => 'X', 2 => 'X', _ => ' ' };
                            _terminal.SetCell(barX + i, row, xc,
                                _blinkOn ? Config.Colors.BrightPurple : Config.Colors.DarkGray35, bg);
                        }
                    }
                    else if (partIsWounded && !isHoveredPart && !isHoveredOrgan)
                    {
                        // Filled up to effective score, blinking purple for removed bars, empty after raw score
                        for (int i = 0; i < barWidth; i++)
                        {
                            char barChar; Vector4 barColor;
                            if (i < partEffScore)           { barChar = '█'; barColor = barFill; }
                            else if (i < part.Score)        { barChar = '█'; barColor = _blinkOn ? Config.Colors.Purple : Config.Colors.DarkGray35; }
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

                    // ── Padding: align arrows to fixed column ──────────────────────
                    for (int i = barWidth; i < MaxBarWidth; i++)
                        _terminal.SetCell(barX + i, row, ' ', Config.Colors.Black, bg);

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
                        _terminal.SetCell(barX + MaxBarWidth,     row, ' ',         Config.Colors.Black, bg);
                        _terminal.SetCell(barX + MaxBarWidth + 1, row, '◄',         decColor, bg);
                        _terminal.SetCell(barX + MaxBarWidth + 2, row, scoreDigit,  scoreFg,  bg);
                        _terminal.SetCell(barX + MaxBarWidth + 3, row, '►',         incColor, bg);
                        _rowToArrowX[row] = (barX + MaxBarWidth + 1, barX + MaxBarWidth + 3);

                        for (int fx = barX + MaxBarWidth + 4; fx < PanelX + PanelWidth; fx++)
                            _terminal.SetCell(fx, row, ' ', Config.Colors.Black, bg);
                    }
                    else
                    {
                        _terminal.SetCell(barX + MaxBarWidth,     row, ' ',        Config.Colors.Black, bg);
                        _terminal.SetCell(barX + MaxBarWidth + 1, row, scoreDigit, scoreFg,  bg);

                        for (int fx = barX + MaxBarWidth + 2; fx < PanelX + PanelWidth; fx++)
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

    /// <summary>
    /// Renders the hovered organ's flavour description in the band between the organ list
    /// and the derived-stat detail. Returns the row after the description so the caller can
    /// anchor the detail section below it. Renders nothing and returns <paramref name="minRow"/>
    /// unchanged when no organ is hovered (or a wound glyph is hovered), preserving the detail
    /// section's default position.
    /// </summary>
    public int RenderHoveredOrganDescription(int minRow)
    {
        if (_hoveredWoundId != null || _hoveredOrganPartName == null) return minRow;

        var opInfo = FindOrganPartByName(_hoveredOrganPartName);
        if (opInfo == null) return minRow;

        var organ = _protagonist.GetOrganById(opInfo.Value.organName);
        if (organ == null || string.IsNullOrWhiteSpace(organ.Description)) return minRow;

        int row = Math.Max(minRow + 1, StatsStartRow);
        _terminal.Text(PanelContentX, row, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);
        row += 2;
        _terminal.Text(PanelContentX, row, opInfo.Value.organDisplayName.ToUpper(), Config.Colors.MediumYellow, Config.Colors.Black);
        row += 2;

        foreach (var line in WrapText(organ.Description, PanelContentW))
        {
            _terminal.Text(PanelContentX, row, line, Config.Colors.LightGray75, Config.Colors.Black);
            row++;
        }
        return row;
    }

    /// <summary>Greedy word-wrap of <paramref name="text"/> into lines no wider than <paramref name="maxWidth"/>.</summary>
    private static IEnumerable<string> WrapText(string text, int maxWidth)
    {
        var sb = new StringBuilder();
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (sb.Length == 0)
                sb.Append(word);
            else if (sb.Length + 1 + word.Length <= maxWidth)
                sb.Append(' ').Append(word);
            else
            {
                yield return sb.ToString();
                sb.Clear();
                sb.Append(word);
            }
        }
        if (sb.Length > 0) yield return sb.ToString();
    }

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
                _terminal.Text(PanelContentX, wRow, "WOUND", Config.Colors.BrightPurple, Config.Colors.Black);
                wRow += 2;
                _terminal.Text(PanelContentX, wRow, hovered.WoundName, Config.Colors.Black, Config.Colors.Purple);
                wRow += 2;
                string sev = hovered.Handicap == Cathedral.Game.Narrative.WoundHandicap.High ? "Severe (disables)" :
                             hovered.Handicap == Cathedral.Game.Narrative.WoundHandicap.Medium ? "Moderate (-1 penalty)" :
                             "Minor (-1 HP only)";
                _terminal.Text(PanelContentX, wRow, sev, hovered.Handicap == Cathedral.Game.Narrative.WoundHandicap.High
                    ? Config.Colors.BrightPurple : Config.Colors.MediumGray60, Config.Colors.Black);
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
            string organPartId      = opInfo.Value.partId;
            string organId          = opInfo.Value.organName;
            string organDisplayName = opInfo.Value.organDisplayName;
            string bodyPartId       = opInfo.Value.bodyPartId;
            string bodyPartDisplay  = opInfo.Value.bodyPartDisplayName;
            bool singlePartOrgan    = organPartId == organId;

            // Colour scheme (foreground only — no background chips):
            //   organ part      → yellow (the focused entity: title + its own stats)
            //   belonging lines → grey ("Organ: …", "Region: …")
            //   scope headers   → dark-yellow text for every scope ("Left Foot (part)",
            //                     "Legs (organ)", "Lower Limbs (region)")
            //   fighting medium → white text
            Vector4 partColor      = Config.Colors.BrightYellow;
            Vector4 belongGrey     = Config.Colors.MediumGray60;
            Vector4 statGrey       = Config.Colors.LightGray75;
            Vector4 scopeHeadColor = Config.Colors.DarkYellow;

            var organObj = _protagonist.GetOrganById(organId);
            // Whether each part is its own fighting medium (e.g. hands) decides where the
            // Fighting Medium block hangs and which level it shows.
            bool perPartMedium = organObj is { PartsAreIndependentMediums: true } && organObj.Parts.Count > 1;
            int organScore = organObj?.Score ?? 0;
            int partScore  = _protagonist.GetOrganPartById(organPartId)?.Score ?? 0;

            // ── Title + belonging ──
            _terminal.Text(PanelContentX, row, opInfo.Value.partDisplayName, partColor, Config.Colors.Black);
            row++;
            _terminal.Text(PanelContentX, row, $"Organ: {organDisplayName}", belongGrey, Config.Colors.Black);
            row++;
            _terminal.Text(PanelContentX, row, $"Region: {bodyPartDisplay}", belongGrey, Config.Colors.Black);
            row += 2;

            // ── Local renderers (share `row`) ──
            void RenderScopeHeader(string title, string scope, Vector4 fg, Vector4 bg)
            {
                // "▸" prefix makes scope headers stand out; a blank line always separates the
                // header from its first info line.
                _terminal.Text(PanelContentX, row, $"▸ {title}  ({scope})", fg, bg);
                row += 2;
            }
            // Each derived stat is rendered on two lines: the label on the first line and the
            // value (with its unit) on the second, indented one extra space and drawn in a
            // slightly darker grey so the number reads as secondary detail. Because the label
            // no longer shares a row with the value it may use the full width of the right panel.
            // extraIndent shifts both lines further right so a stat can nest under a group
            // sub-header (e.g. secretion percentages beneath the "Secretion" label).
            void RenderStat(DerivedStat stat, Vector4 labelColor, int extraIndent = 0)
            {
                int val = stat.GetRawValue(_protagonist);
                Vector4 valueColor = AdjustLuminosity(labelColor, 0.65f);
                string pad = new string(' ', extraIndent);
                _terminal.Text(PanelContentX, row, $"{pad}  {stat.ShortDisplayName}", labelColor, Config.Colors.Black);
                row++;
                _terminal.Text(PanelContentX, row, $"{pad}    {stat.FormatValue(val)}", valueColor, Config.Colors.Black);
                row++;
            }
            // Renders a "Fighting Medium lv.X" block followed by its skill list (learned skills
            // bright, the next learnable one dim). Shared by organ and body-part mediums.
            void RenderFightingMediumSkills(IReadOnlyList<string> skillIds, int level)
            {
                _terminal.Text(PanelContentX, row, $"  Fighting Medium  lv.{level}", Config.Colors.White, Config.Colors.Black);
                row++;
                bool nextLearnable = false;
                foreach (var skillId in skillIds)
                {
                    var skill = FightingSkillRegistry.Instance.GetById(skillId);
                    if (skill == null) continue;
                    bool isLearned = _protagonist.LearnedModiMentis
                        .Any(m => m.ModusMentisId == skill.RequiredModusMentisId);
                    if (isLearned)
                    {
                        _terminal.Text(PanelContentX, row, $"    · {skill.DisplayName}", Config.Colors.LightGray75, Config.Colors.Black);
                        row++;
                    }
                    else if (!nextLearnable)
                    {
                        _terminal.Text(PanelContentX, row, $"    · {skill.DisplayName}", Config.Colors.DarkGray35, Config.Colors.Black);
                        row++;
                        nextLearnable = true;
                    }
                }
                row++;
            }
            // White "Secretion" sub-label shown beneath a humoral organ's scope header,
            // mirroring how the white "Fighting Medium" label sits beneath its scope header.
            void RenderSecretionLabel()
            {
                _terminal.Text(PanelContentX, row, "  Secretion", Config.Colors.White, Config.Colors.Black);
                row++;
            }

            // Renders a scope's derived stats: ordinary stats first, then the humoral "Secretion"
            // group as special info (white label) separated by a blank line. Returns true if any
            // stat row was emitted (so the caller can blank-line before a following medium block).
            bool RenderScopeStats(List<DerivedStat> stats, Vector4 normalColor)
            {
                var secretionStats = stats.Where(s => s is HumoralSecretionStat).ToList();
                var normalStats    = stats.Where(s => s is not HumoralSecretionStat).ToList();
                foreach (var stat in normalStats) RenderStat(stat, normalColor);
                if (secretionStats.Count > 0)
                {
                    if (normalStats.Count > 0) row++; // blank line between normal stats and secretion info
                    RenderSecretionLabel();
                    foreach (var stat in secretionStats) RenderStat(stat, statGrey, extraIndent: 2);
                }
                return stats.Count > 0;
            }

            // Organ fighting-medium category (null if this organ has no combat skills).
            var organCategory = OrganMediumRegistry.GetById(organId);

            // ── Organ-part / organ stats, with the fighting medium hung off its owner ──
            // Every scope that has content (stats and/or a fighting medium) renders its scope
            // header first, so a header always precedes the info regardless of scope. The white
            // Secretion / Fighting Medium labels sit beneath the (dark-yellow) scope header.
            if (singlePartOrgan)
            {
                // Part and organ coincide — one combined block owned by the organ.
                var combined = _protagonist.DerivedStats
                    .Where(s => s.RelatedOrganPartId == organPartId || s.RelatedOrganId == organId)
                    .Distinct()
                    .ToList();
                if (combined.Count > 0 || organCategory != null)
                {
                    RenderScopeHeader(organDisplayName, "organ", scopeHeadColor, Config.Colors.Black);
                    bool anyStat = RenderScopeStats(combined, statGrey);
                    // Single-part organs are whole-organ mediums → owned by the organ.
                    if (organCategory != null)
                    {
                        if (anyStat) row++; // blank line between stats and special info
                        RenderFightingMediumSkills(organCategory.SkillIds, organScore);
                    }
                    row++;
                }
            }
            else
            {
                // ── Part scope: part stats and/or the per-part medium (e.g. hands, feet) ──
                var organPartStats = _protagonist.DerivedStats
                    .Where(s => s.RelatedOrganPartId == organPartId)
                    .ToList();
                bool partOwnsMedium = perPartMedium && organCategory != null;
                if (organPartStats.Count > 0 || partOwnsMedium)
                {
                    RenderScopeHeader(opInfo.Value.partDisplayName, "part", scopeHeadColor, Config.Colors.Black);
                    bool anyStat = RenderScopeStats(organPartStats, partColor);
                    // Per-part medium → owned by THIS part, level = this part's score.
                    if (partOwnsMedium)
                    {
                        if (anyStat) row++; // blank line between stats and special info
                        RenderFightingMediumSkills(organCategory!.SkillIds, partScore);
                    }
                    row++;
                }

                // ── Organ scope: organ stats and/or the whole-organ medium (e.g. legs) ──
                var organStats = _protagonist.DerivedStats
                    .Where(s => s.RelatedOrganId == organId)
                    .ToList();
                bool organOwnsMedium = !perPartMedium && organCategory != null;
                if (organStats.Count > 0 || organOwnsMedium)
                {
                    RenderScopeHeader(organDisplayName, "organ", scopeHeadColor, Config.Colors.Black);
                    bool anyStat = RenderScopeStats(organStats, statGrey);
                    if (organOwnsMedium)
                    {
                        if (anyStat) row++; // blank line between stats and special info
                        RenderFightingMediumSkills(organCategory!.SkillIds, organScore);
                    }
                    row++;
                }
            }

            // ── Body-part (region) stats + region fighting medium (e.g. upper limbs) ──
            var bodyPartStats = _protagonist.DerivedStats
                .Where(s => s.RelatedBodyPartId == bodyPartId)
                .ToList();
            var bodyPartCategory = BodyPartMediumRegistry.GetById(bodyPartId);
            if (bodyPartStats.Count > 0 || bodyPartCategory != null)
            {
                RenderScopeHeader(bodyPartDisplay, "region", scopeHeadColor, Config.Colors.Black);
                bool anyStat = RenderScopeStats(bodyPartStats, statGrey);
                // A body part that is itself a fighting medium (level = region total score).
                if (bodyPartCategory != null)
                {
                    if (anyStat) row++; // blank line between stats and special info
                    RenderFightingMediumSkills(bodyPartCategory.SkillIds,
                        _protagonist.GetBodyPartById(bodyPartId)?.Score ?? 0);
                }
                row++;
            }

            // ── Wounds on this organ part / organ / body part ──
            var wounds = _protagonist.GetWoundsForOrganPart(organPartId, organId, bodyPartId);
            if (wounds.Count > 0)
            {
                _terminal.Text(PanelContentX, row, "Wounds", Config.Colors.BrightPurple, Config.Colors.Black);
                row++;
                foreach (var wound in wounds)
                {
                    string sev = wound.Handicap == Cathedral.Game.Narrative.WoundHandicap.High ? "●" : "◌";
                    string line = $"  {sev} {wound.WoundName}";
                    Vector4 wc = wound.Handicap == Cathedral.Game.Narrative.WoundHandicap.High
                        ? (_blinkOn ? Config.Colors.BrightPurple : Config.Colors.DarkGray35)
                        : Config.Colors.MediumGray60;
                    _terminal.Text(PanelContentX, row, line, wc, Config.Colors.Black);
                    row++;
                }
                row++;
            }

        }
    }

    /// <summary>
    /// Renders the hovered region (body part) detail: its flavour description in the middle band
    /// (right below the organ list), followed by the derived stats that belong specifically to the
    /// region (not to its organs) and any region fighting medium. No-ops unless a region is hovered
    /// with no organ part and no wound glyph.
    /// </summary>
    public void RenderHoveredRegionDetail(int minRow)
    {
        if (_hoveredWoundId != null || _hoveredOrganPartName != null || _hoveredBodyPartId == null) return;

        var bodyPart = _protagonist.GetBodyPartById(_hoveredBodyPartId);
        if (bodyPart == null) return;

        // ── Description block (middle band) ──
        int row = Math.Max(minRow + 1, StatsStartRow);
        _terminal.Text(PanelContentX, row, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);
        row += 2;
        _terminal.Text(PanelContentX, row, bodyPart.DisplayName.ToUpper(), Config.Colors.MediumYellow, Config.Colors.Black);
        row += 2;
        if (!string.IsNullOrWhiteSpace(bodyPart.Description))
            foreach (var line in WrapText(bodyPart.Description, PanelContentW))
            {
                _terminal.Text(PanelContentX, row, line, Config.Colors.LightGray75, Config.Colors.Black);
                row++;
            }

        // ── Region-specific derived stats + region fighting medium (the info section) ──
        var regionStats = _protagonist.DerivedStats
            .Where(s => s.RelatedBodyPartId == _hoveredBodyPartId)
            .ToList();
        var regionCategory = BodyPartMediumRegistry.GetById(_hoveredBodyPartId);
        if (regionStats.Count == 0 && regionCategory == null) return;

        int detailRow = Math.Max(row + 1, 50);
        _terminal.Text(PanelContentX, detailRow, "──────────────────────────────", Config.Colors.DarkGray35, Config.Colors.Black);
        detailRow += 2;
        _terminal.Text(PanelContentX, detailRow, $"▸ {bodyPart.DisplayName}  (region)", Config.Colors.DarkYellow, Config.Colors.Black);
        detailRow += 2;

        Vector4 statGrey = Config.Colors.LightGray75;
        foreach (var stat in regionStats)
        {
            int val = stat.GetRawValue(_protagonist);
            _terminal.Text(PanelContentX, detailRow, $"  {stat.ShortDisplayName}", statGrey, Config.Colors.Black);
            detailRow++;
            _terminal.Text(PanelContentX, detailRow, $"    {stat.FormatValue(val)}", AdjustLuminosity(statGrey, 0.65f), Config.Colors.Black);
            detailRow++;
        }

        if (regionCategory != null)
        {
            if (regionStats.Count > 0) detailRow++; // blank line between stats and medium
            _terminal.Text(PanelContentX, detailRow, $"  Fighting Medium  lv.{bodyPart.Score}", Config.Colors.White, Config.Colors.Black);
            detailRow++;
            bool nextLearnable = false;
            foreach (var skillId in regionCategory.SkillIds)
            {
                var skill = FightingSkillRegistry.Instance.GetById(skillId);
                if (skill == null) continue;
                bool isLearned = _protagonist.LearnedModiMentis.Any(m => m.ModusMentisId == skill.RequiredModusMentisId);
                if (isLearned)
                {
                    _terminal.Text(PanelContentX, detailRow, $"    · {skill.DisplayName}", Config.Colors.LightGray75, Config.Colors.Black);
                    detailRow++;
                }
                else if (!nextLearnable)
                {
                    _terminal.Text(PanelContentX, detailRow, $"    · {skill.DisplayName}", Config.Colors.DarkGray35, Config.Colors.Black);
                    detailRow++;
                    nextLearnable = true;
                }
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
    /// Increases are gated by the remaining creation point budget.
    /// </summary>
    public void AdjustOrganPartScore(string organPartName, int delta)
    {
        var organPart = _protagonist.BodyParts
            .SelectMany(bp => bp.Organs)
            .SelectMany(o => o.Parts)
            .FirstOrDefault(p => p.Id == organPartName);

        if (organPart == null) return;
        if (delta > 0 && GetRemainingPoints() <= 0) return;
        organPart.Score = Math.Clamp(organPart.Score + delta, 0, organPart.MaxScore);
    }

    /// <summary>
    /// Cycles the score of an organ part: increments by 1, wrapping back to 1 after MaxScore.
    /// Increments are gated by the remaining creation point budget.
    /// </summary>
    public void CycleOrganPartScore(string organPartName)
    {
        var organPart = _protagonist.BodyParts
            .SelectMany(bp => bp.Organs)
            .SelectMany(o => o.Parts)
            .FirstOrDefault(p => p.Id == organPartName);

        if (organPart == null) return;
        if (organPart.Score >= organPart.MaxScore) return;  // already at max, do nothing
        if (GetRemainingPoints() > 0)
            organPart.Score++;             // spend one point
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

    /// <summary>
    /// Points remaining in the creation budget (each organ starts at 1; budget covers extras).
    /// </summary>
    public int GetRemainingPoints()
    {
        int organCount = _protagonist.BodyParts
            .SelectMany(bp => bp.Organs)
            .SelectMany(o => o.Parts)
            .Count();
        return PointBudget - (GetTotalScore() - organCount);
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
