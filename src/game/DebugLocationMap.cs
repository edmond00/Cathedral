// DebugLocationMap.cs — a development view of the location the narration is standing in: every area
// as a box, every section as a panel round its areas, every way between them as a routed line. Drawn
// in the narration panel in place of the text, toggled with Tab. Developer keys only.
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Terminal;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game;

/// <summary>
/// The map of a scene, as a debug overlay on the narration panel.
///
/// <para><b>What it draws.</b> An <see cref="Area"/> is a card (name, people present this period,
/// tenure), a <see cref="Section"/> is a tinted, framed panel round its areas with its name on a tab. A
/// <b>solid</b> line is an <see cref="Scene.Scene.AreaGraph"/> edge — the areas are neighbours and
/// <c>move</c> walks it. A <b>dashed</b> line is a <see cref="ConnectorPointOfInterest"/> — a door, a
/// stair, a path, a cliff — with a letter on it naming which. A landscape (a one-way view from a roof
/// or a crown) is drawn only when it touches the area the party stands in, whose card is yellow.</para>
///
/// <para><b>Lines are routed, not drawn.</b> Every line runs on the cell grid, horizontally and
/// vertically only, round the boxes, by a shortest-path search that charges for turns, for running
/// beside a box and for crossing another line. The lines leaving one area are one <i>net</i>: a later
/// line of the net may start from any cell an earlier one already uses, so the ways out of a square
/// leave as one trunk and split at a junction instead of running side by side. Lines of different
/// nets never share a cell except to cross straight over each other. Each cell's glyph is read from
/// which of its four sides the line uses, so corners, junctions and crossings follow by themselves.</para>
///
/// <para><b>Layout.</b> Greedy and deterministic, with no generator at all: areas are placed on a
/// grid within their section, each next to the neighbours already placed, and the sections are then
/// placed on a coarser grid the same way, by how many ways run between them. Nothing here is drawn
/// from <see cref="GameRng"/>, so opening the map changes no seeded run.</para>
///
/// <para><b>Panning.</b> The map is usually larger than the panel. The pad in the panel's top-right
/// corner moves the view a step at a time, and its centre puts the party's area back in the middle;
/// the arrow keys move it too, in smaller steps, and turn no camera while the map is up. The view is
/// centred on the party each time the map is opened or a new scene is shown. The footer button reads
/// RETURN, back to the narration: leaving the location from a map is not a thing to do by accident.</para>
///
/// <para><b>No legend.</b> What a thing on the map is, is said where it is: the pointer over a card,
/// a line or an icon puts a short label just above it (<see cref="LabelAt"/>, drawn by
/// <see cref="ShowLabel"/> in the mouse-following popup) — black on yellow for what a click acts on,
/// black on grey for what is only told.</para>
///
/// <para>Shown or not is static: it stays on from one visit to the next until Tab turns it off.</para>
///
/// <para><b>Walks.</b> Every move in exploration is also shown on it, to every player and whatever
/// the toggle: <see cref="Walk"/> turns the ways a move took into cells, and the narration draws the
/// party's glyph on them a step at a time (<c>NarrativeController.ShowWalk</c>).</para>
/// </summary>
public sealed class DebugLocationMap
{
    /// <summary>Whether the map replaces the narration text. Toggled by Tab or the header's ↹ in exploration; elsewhere by Tab under --developer-keys.</summary>
    public static bool Visible { get; private set; }

    /// <summary>
    /// A walk has ended on the map and waits for its Continue: the pad and the arrow keys pan it, as
    /// they do the map opened by hand.
    /// </summary>
    public static bool WalkPannable { get; set; }

    /// <summary>
    /// This map is a menu's (the mental maps, the routine replay): always panned by its pad, whatever
    /// the Tab toggle says.
    /// </summary>
    public bool AlwaysPannable { get; set; }

    /// <summary>
    /// The ground under everything drawn: the narration's translucent black by default, which lets the
    /// world show faintly through; a modal box over the world sets it opaque.
    /// </summary>
    public Vector4 Canvas { get; init; } = CanvasBg;

    private bool Pannable => Visible || WalkPannable || AlwaysPannable;

    /// <summary>
    /// A routine drawn on a card: a black letter on yellow, in the card's detail row, right-aligned.
    /// <paramref name="Tag"/> is whatever the caller wants back from <see cref="IconAt"/>; one not
    /// <paramref name="Enabled"/> is drawn grey.
    /// </summary>
    public readonly record struct MapIcon(Area Area, char Glyph, bool Enabled, object Tag);

    /// <summary>What the pointer is over, in a few words; <paramref name="Clickable"/> when a click acts on it.</summary>
    public readonly record struct HoverLabel(string Text, bool Clickable);

    // Box and spacing, in cells. An area is a card: two rows of name on a coloured band, one of
    // detail under it. The gaps leave a lane or two for lines between the margin a box keeps clear.
    private const int BoxW = 18, BoxH = 3;
    private const int AreaPitchX = BoxW + 10, AreaPitchY = BoxH + 4;
    private const int SectionPadX = 4, SectionPadTop = 3, SectionPadBottom = 3;
    private const int SectionGapX = 8, SectionGapY = 4;
    private const int CanvasMargin = 4;
    private const int PanStepX = 12, PanStepY = 6;
    private const int KeyStepX = 4,  KeyStepY = 2;

    // Routing costs, in cells of length.
    private const float TurnCost = 3f, BesideBoxCost = 4f, CrossLineCost = 6f, CrossBorderCost = 2f;
    private const float SharedNetCost = 0.25f, RelaxedOverlapCost = 30f, PortOffCentreCost = 0.15f;

    // Directions: bit 1 << d is the side of a cell a line leaves by.
    private const int N = 0, E = 1, S = 2, W = 3;
    private static readonly int[] DX = { 0, 1, 0, -1 };
    private static readonly int[] DY = { -1, 0, 1, 0 };
    private const int Vertical = (1 << N) | (1 << S), Horizontal = (1 << E) | (1 << W);

    // Palette. A debug tool's own, so it lives here rather than in Config.
    //
    // Everything solid is a BACKGROUND colour, never a glyph: the game's cells are wider than its
    // glyphs, so a run of horizontal box-drawing characters shows a seam between every pair of cells
    // and a box border drawn with them reads as dashed. A cell's background fills it edge to edge.
    private static readonly Vector4 CanvasBg        = Config.NarrativeUI.BackgroundColor;
    //
    // A schema chalked on a blackboard: every fill is near black and only just lifted off the
    // canvas, so the light lines and text carry the drawing. The game's own scheme throughout —
    // yellow-grey for open ground, purple for interiors and private rooms, yellow for where you are.
    private static readonly Vector4 OutdoorFill     = new(0.040f, 0.038f, 0.028f, 1f);
    private static readonly Vector4 OutdoorFrame    = new(0.110f, 0.100f, 0.060f, 1f);
    private static readonly Vector4 OutdoorTab      = new(0.200f, 0.180f, 0.090f, 1f);
    private static readonly Vector4 OutdoorTitle    = Config.Colors.DarkYellowGrey;
    private static readonly Vector4 InteriorFill    = new(0.040f, 0.028f, 0.052f, 1f);
    private static readonly Vector4 InteriorFrame   = new(0.100f, 0.065f, 0.135f, 1f);
    private static readonly Vector4 InteriorTab     = new(0.180f, 0.115f, 0.240f, 1f);
    private static readonly Vector4 InteriorTitle   = Config.Colors.LightPurple;
    private static readonly Vector4 DarkText        = new(0.04f, 0.04f, 0.04f, 1f);
    private static readonly Vector4 BoxBand         = new(0.170f, 0.170f, 0.180f, 1f);
    private static readonly Vector4 BoxFill         = new(0.075f, 0.075f, 0.080f, 1f);
    private static readonly Vector4 BoxName         = Config.Colors.LightGray85;
    private static readonly Vector4 PrivateBand     = new(0.200f, 0.125f, 0.270f, 1f);
    private static readonly Vector4 PrivateFill     = new(0.080f, 0.050f, 0.105f, 1f);
    private static readonly Vector4 PrivateName     = Config.Colors.LightPurple;
    private static readonly Vector4 HereBand        = new(0.360f, 0.290f, 0.030f, 1f);
    private static readonly Vector4 HereFill        = new(0.130f, 0.105f, 0.015f, 1f);
    private static readonly Vector4 HereName        = Config.Colors.BrightYellow;
    private static readonly Vector4 UnknownBand     = new(0.050f, 0.050f, 0.055f, 1f);
    private static readonly Vector4 UnknownFill     = new(0.025f, 0.025f, 0.030f, 1f);
    private const float SeenShade = 0.45f, UnknownShade = 0.3f;
    private static readonly Vector4 UnknownInk      = Config.Colors.DarkGray35;
    private static readonly Vector4 UnknownLine     = new(0.16f, 0.16f, 0.17f, 1f);
    private static readonly Vector4 SolidLine       = Config.Colors.LightGray85;
    private static readonly Vector4 DashedLine      = Config.Colors.GoldYellow;
    private static readonly Vector4 ViewLine        = Config.Colors.DarkGray40;
    private static readonly Vector4 PartyInk        = Config.Colors.White;
    public  static readonly Vector4 IconOn          = Config.Colors.GoldYellow;
    private static readonly Vector4 IconOnAlt       = Config.Colors.MediumYellow;
    private static readonly Vector4 IconOff         = Config.Colors.DarkGray40;

    private enum PadArrow { None, Up, Down, Left, Right, Centre }

    /// <summary>What a cell is to the router, before any line is laid.</summary>
    private enum Cell : byte
    {
        Free,
        /// <summary>The ring round a box: passable, but a line there reads as touching the box.</summary>
        BesideBox,
        Box,
        /// <summary>A section's top or bottom edge: crossed vertically, never followed.</summary>
        HBorder,
        /// <summary>A section's side: crossed horizontally, never followed.</summary>
        VBorder,
        /// <summary>A section's corner or title: never entered.</summary>
        Hard,
    }

    /// <summary>One way between two areas, and how it was laid.</summary>
    private sealed class Link
    {
        public required Area From;
        public required Area To;
        public required bool Dashed;
        public required bool View;
        public required char Kind;
        /// <summary>What the way is recorded under once taken (<see cref="AreaVisits"/>).</summary>
        public required string Key;
        /// <summary>Cells this line laid where no line was before: where its letter may go.</summary>
        public readonly List<int> Fresh = new();
        /// <summary>
        /// The cells this line laid or joined. A branch's list starts where it left its net's trunk,
        /// so the way back to the source box is the net's, not the line's: walk it by <see cref="Net"/>.
        /// </summary>
        public readonly List<int> Cells = new();
        /// <summary>The net the line was laid in, or -1 when it could not be routed.</summary>
        public int Net = -1;
        /// <summary>
        /// Every line cell between the two cards, the trunk it shares included: what is lit when the
        /// way is known. <see cref="Cells"/> stops at the junction, and a way taken left the stretch
        /// back to its card dim.
        /// </summary>
        public List<int> Path = new();
    }

    /// <summary>
    /// One cell of a walk shown on the map: where the party is drawn, the area it counts as in (the
    /// one it left, until it reaches the next box), and which of the walk's ways it is on (-1 for the
    /// cell it starts from).
    /// </summary>
    public readonly record struct WalkStep(int X, int Y, Area Here, int Hop);

    // The icons as painted last, by canvas cell, for hit-testing.
    private readonly List<(int X, int Y, object Tag)> _iconCells = new();

    // What the last paint knew, for the pointer's label: each card's knowledge, the line on each
    // canvas cell (the best-known one through it) and each line's, where the party stood, and which
    // cards a click would choose.
    private readonly Dictionary<Area, Known> _paintedKnown = new();
    private readonly Dictionary<Link, Known> _paintedLinkKnown = new();
    private Link?[] _lineAt = Array.Empty<Link?>();
    private Area? _paintedHere;
    private IReadOnlySet<Area>? _paintedChoosable;

    /// <summary>How much the party knows of an area or a way, which is how it is drawn.</summary>
    private enum Known
    {
        /// <summary>Never near it: a dark card reading ???, a dark plain line, a ? for a connector.</summary>
        Unknown,
        /// <summary>Taken in by an observation, never stood in or gone by: dimmed, but named and lettered.</summary>
        Seen,
        /// <summary>Stood in, or gone by: drawn in full.</summary>
        Visited,
    }

    /// <summary>
    /// The routing grid: what each cell is, and which nets' lines pass through it with which sides.
    /// At most two nets share a cell, and then only crossing straight over each other.
    /// </summary>
    private sealed class RouteGrid
    {
        public readonly int W, H;
        public readonly Cell[] Kind;
        public readonly int[] NetA, MaskA, NetB, MaskB;

        public RouteGrid(int w, int h)
        {
            W = w; H = h;
            Kind = new Cell[w * h];
            NetA = Enumerable.Repeat(-1, w * h).ToArray();
            NetB = Enumerable.Repeat(-1, w * h).ToArray();
            MaskA = new int[w * h];
            MaskB = new int[w * h];
        }

        private RouteGrid(RouteGrid o)
        {
            W = o.W; H = o.H;
            Kind = o.Kind;   // never written after layout, so shared
            NetA = (int[])o.NetA.Clone(); MaskA = (int[])o.MaskA.Clone();
            NetB = (int[])o.NetB.Clone(); MaskB = (int[])o.MaskB.Clone();
        }

        public RouteGrid Clone() => new(this);

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < W && y < H;

        /// <summary>Adds sides to a net's mask in a cell. Returns true when the cell had no line before.</summary>
        public bool Add(int c, int net, int bits)
        {
            if (NetA[c] == -1)   { NetA[c] = net; MaskA[c] = bits; return true; }
            if (NetA[c] == net)  { MaskA[c] |= bits; return false; }
            if (NetB[c] == -1)   { NetB[c] = net; MaskB[c] = bits; return false; }
            if (NetB[c] == net)  { MaskB[c] |= bits; return false; }
            MaskA[c] |= bits;    // a relaxed route forced through a full cell; rare
            return false;
        }
    }

    // The layout of the scene last shown, rebuilt when the scene changes.
    private Scene.Scene? _laidOut;
    private readonly Dictionary<Area, (int X, int Y)> _boxAt = new();
    private readonly List<(Section Section, int X, int Y, int W, int H, string Title)> _sectionBoxes = new();
    private readonly List<Link> _links = new();          // solid and dashed, routed once per scene
    private readonly List<Link> _viewLinks = new();      // landscapes, routed per area stood in
    private readonly List<bool> _netDashed = new();      // net id → drawn dashed
    private readonly List<bool> _netView = new();        // net id → a landscape net
    private int _layoutNets;                             // nets the layout made; views' come after
    private RouteGrid _grid = new(1, 1);
    private int _canvasW, _canvasH;

    // Landscapes from the area stood in, laid over a copy of the grid.
    private Area? _overlayFor;
    private RouteGrid? _overlay;
    private readonly List<Link> _overlayLinks = new();

    // Search scratch, reused between routes.
    private float[] _g = Array.Empty<float>();
    private int[] _parent = Array.Empty<int>();
    private int[] _stamp = Array.Empty<int>();
    private int _stampNow;

    // View: the canvas cell at the viewport's top-left.
    private int _viewX, _viewY;
    private bool _recentre = true;

    // The pad, as drawn last frame, for hit-testing.
    private readonly List<(int X, int Y, PadArrow Arrow)> _padCells = new();
    private PadArrow _hovered = PadArrow.None;

    /// <summary>Turns the map on or off. Turning it on re-centres it on the party.</summary>
    public void Toggle()
    {
        Visible = !Visible;
        if (Visible) _recentre = true;
        Console.WriteLine($"DebugLocationMap: {(Visible ? "shown" : "hidden")}");
    }

    /// <summary>Centres the view on the next render's <c>focus</c> (or <c>here</c>), as the pad's centre does.</summary>
    public void Recentre() => _recentre = true;

    /// <summary>
    /// Records the cursor. Returns true when the pad arm under it changed, the caller's cue to tick.
    /// </summary>
    public bool SetHover(int cellX, int cellY)
    {
        var arrow = Pannable ? ArrowAt(cellX, cellY) : PadArrow.None;
        if (arrow == _hovered) return false;
        _hovered = arrow;
        return arrow != PadArrow.None;
    }

    /// <summary>Whether the cursor is on an arm of the pad, as last recorded by <see cref="SetHover"/>.</summary>
    public bool PadArrowHovered => _hovered != PadArrow.None;

    /// <summary>
    /// A click on the pad. Returns true when it landed on an arm and was consumed, so the narration
    /// does not also act on it.
    /// </summary>
    public bool HandleClick(int cellX, int cellY)
    {
        if (!Pannable) return false;
        switch (ArrowAt(cellX, cellY))
        {
            case PadArrow.Up:     Pan(0, -PanStepY); break;
            case PadArrow.Down:   Pan(0,  PanStepY); break;
            case PadArrow.Left:   Pan(-PanStepX, 0); break;
            case PadArrow.Right:  Pan( PanStepX, 0); break;
            case PadArrow.Centre: _recentre = true;  break;
            default: return false;
        }
        return true;
    }

    /// <summary>
    /// An arrow key while the map is up: a smaller step than the pad's, since a held key repeats.
    /// Returns false for any other key, or with the map hidden, so the key is not swallowed.
    /// </summary>
    public bool HandleKey(OpenTK.Windowing.GraphicsLibraryFramework.Keys key)
    {
        if (!Pannable) return false;
        switch (key)
        {
            case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Up:    Pan(0, -KeyStepY); return true;
            case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Down:  Pan(0,  KeyStepY); return true;
            case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Left:  Pan(-KeyStepX, 0); return true;
            case OpenTK.Windowing.GraphicsLibraryFramework.Keys.Right: Pan( KeyStepX, 0); return true;
            default: return false;
        }
    }

    /// <summary>Moves the view; the next render clamps it to the canvas.</summary>
    private void Pan(int dx, int dy)
    {
        _viewX += dx;
        _viewY += dy;
    }

    private PadArrow ArrowAt(int x, int y)
    {
        foreach (var c in _padCells)
            if (c.X == x && c.Y == y) return c.Arrow;
        return PadArrow.None;
    }

    // ── Rendering ────────────────────────────────────────────────────────────

    /// <summary>
    /// Draws the map into the rectangle [<paramref name="left"/>, <paramref name="left"/> +
    /// <paramref name="width"/>) × [<paramref name="top"/>, <paramref name="top"/> +
    /// <paramref name="height"/>), which the caller has already cleared.
    /// </summary>
    /// <para>A menu passes <paramref name="icons"/> (its routines), the one under the cursor and the
    /// one selected, and <paramref name="focus"/> to centre on where nobody stands. The selected icon
    /// blinks when the caller alternates <paramref name="selectedLit"/>: off, it is drawn inverted.</para>
    /// <para>With <paramref name="party"/>, a walk is being shown: the party's glyph is drawn on that
    /// canvas cell, and the view follows it while <paramref name="follow"/> (once the walk has ended
    /// it is left where it is, for the pad and the arrows). What the walk has yet to reach is drawn as
    /// it was before the move: <paramref name="withheld"/> are the visit keys it recorded and has not
    /// come to yet.</para>
    public void Render(TerminalHUD terminal, Scene.Scene? scene, Area? here, TimePeriod period,
                       int left, int top, int width, int height, (int X, int Y)? party = null,
                       bool follow = true, IReadOnlySet<string>? withheld = null,
                       IReadOnlySet<Area>? choosable = null, Area? hovered = null,
                       IReadOnlyList<MapIcon>? icons = null, object? hoveredIcon = null,
                       object? selectedIcon = null, Area? focus = null,
                       int footRows = 3, bool selectedLit = true)
    {
        _padCells.Clear();
        if (scene == null || scene.Sections.Count == 0)
        {
            terminal.Text(left + 2, top + 1, "(no scene to map)", Config.Colors.MediumGray50, Canvas);
            return;
        }

        EnsureLayout(scene);
        if (_overlay == null || !ReferenceEquals(_overlayFor, here))
            RouteViewsFrom(here);

        // The pad floats over the map's top-right corner; the footer button (LEAVE / CONTINUE) is
        // drawn over the narration panel's last rows, so the map stops short of them there
        // (footRows; a menu has no footer).
        int mapTop = top;
        int mapH = height - footRows;

        if (party is { } p)
        {
            if (follow)
            {
                _viewX = p.X - width / 2;
                _viewY = p.Y - mapH / 2;
            }
            _recentre = true;   // the map opened after a walk is centred on where it ended
        }
        else if (_recentre && (here ?? focus) is { } anchor && _boxAt.TryGetValue(anchor, out var hp))
        {
            _viewX = hp.X + BoxW / 2 - width / 2;
            _viewY = hp.Y + BoxH / 2 - mapH / 2;
            _recentre = false;
        }
        _viewX = ClampView(_viewX, _canvasW, width);
        _viewY = ClampView(_viewY, _canvasH, mapH);

        var (glyphs, fgs, bgs) = Paint(scene, here, period, withheld, choosable, hovered, icons, hoveredIcon, selectedIcon, selectedLit);
        _shownAt = (left, mapTop, width, mapH);

        for (int y = 0; y < mapH; y++)
        {
            int cy = _viewY + y;
            if (cy < 0 || cy >= _canvasH) continue;
            for (int x = 0; x < width; x++)
            {
                int cx = _viewX + x;
                if (cx < 0 || cx >= _canvasW) continue;
                int c = cy * _canvasW + cx;
                terminal.SetCell(left + x, mapTop + y, glyphs[c], fgs[c], bgs[c]);
            }
        }

        if (party is { } at)
        {
            int px = at.X - _viewX, py = at.Y - _viewY;
            if (px >= 0 && px < width && py >= 0 && py < mapH)
                terminal.SetCell(left + px, mapTop + py, Config.GlyphSphere.ProtagonistChar, PartyInk,
                                 bgs[at.Y * _canvasW + at.X]);
        }

        RenderPad(terminal, left + width - 6, top);
    }

    /// <summary>
    /// Keeps the canvas on screen: a canvas narrower than the view is centred in it, a wider one may
    /// be panned until its edge meets the view's and no further.
    /// </summary>
    private static int ClampView(int view, int canvas, int viewport)
        => canvas <= viewport ? (canvas - viewport) / 2 : Math.Clamp(view, 0, canvas - viewport);

    private (char[], Vector4[], Vector4[]) Paint(Scene.Scene scene, Area? here, TimePeriod period,
                                                 IReadOnlySet<string>? withheld,
                                                 IReadOnlySet<Area>? choosable, Area? hovered,
                                                 IReadOnlyList<MapIcon>? icons, object? hoveredIcon,
                                                 object? selectedIcon, bool selectedLit)
    {
        _iconCells.Clear();
        _paintedHere = here;
        _paintedChoosable = choosable;
        var iconsOf = (icons ?? Array.Empty<MapIcon>()).GroupBy(i => i.Area)
                                                        .ToDictionary(g => g.Key, g => g.ToList());
        var grid = _overlay ?? _grid;
        int w = _canvasW, h = _canvasH;
        var glyphs = new char[w * h];
        var fgs = new Vector4[w * h];
        var bgs = new Vector4[w * h];
        Array.Fill(glyphs, ' ');
        Array.Fill(bgs, Canvas);

        void Put(int x, int y, char ch, Vector4 fg, Vector4? bg = null)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int c = y * w + x;
            glyphs[c] = ch; fgs[c] = fg;
            if (bg is { } b) bgs[c] = b;
        }

        // What the party knows (AreaVisits): stood in or gone by, else taken in by an observation.
        // A walk being shown holds back what it has not reached yet.
        bool Held(string key) => withheld != null && withheld.Contains(key);
        var visited = new HashSet<Area>(_boxAt.Keys.Where(a => scene.VisitedAreas.Contains(a.StableKey) && !Held(a.StableKey)));
        if (here != null) visited.Add(here);
        Known LinkKnown(Link l) => scene.TakenLinks.Contains(l.Key) && !Held(l.Key) ? Known.Visited
                                 : scene.SeenLinks.Contains(l.Key) ? Known.Seen
                                 : Known.Unknown;
        Known AreaKnown(Area a) => visited.Contains(a) ? Known.Visited
                                 : scene.SeenAreas.Contains(a.StableKey) ? Known.Seen
                                 : Known.Unknown;

        // A cell is as known as the best-known line through it: a trunk the party walked stays lit
        // where an unknown branch leaves it.
        var cellKnown = new Known[w * h];
        _lineAt = new Link?[w * h];
        _paintedLinkKnown.Clear();
        foreach (var link in _links.Concat(_overlayLinks))
        {
            var k = LinkKnown(link);
            _paintedLinkKnown[link] = k;
            foreach (int c in link.Path.Count > 0 ? link.Path : link.Cells)
                if (_lineAt[c] == null || k > cellKnown[c])
                {
                    cellKnown[c] = k;
                    _lineAt[c] = link;
                }
        }
        _paintedKnown.Clear();
        foreach (var a in _boxAt.Keys) _paintedKnown[a] = AreaKnown(a);

        // Sections: a tinted panel inside a one-cell frame of a deeper tint, the name on a tab.
        foreach (var (section, sx, sy, sw, sh, title) in _sectionBoxes)
        {
            var fill  = section.IsInterior ? InteriorFill  : OutdoorFill;
            var frame = section.IsInterior ? InteriorFrame : OutdoorFrame;
            var tab   = section.IsInterior ? InteriorTab   : OutdoorTab;
            var ink   = section.IsInterior ? InteriorTitle : OutdoorTitle;
            // As known as its best-known area: a section only seen into is dimmed, one nothing is
            // known of is barely lifted off the canvas.
            var sectionKnown = section.Areas.Where(a => _boxAt.ContainsKey(a))
                .Select(AreaKnown).DefaultIfEmpty(Known.Unknown).Max();
            float k = KnownShade(sectionKnown);
            fill = Shade(fill, k); frame = Shade(frame, k); tab = Shade(tab, k); ink = Shade(ink, k);
            for (int y = sy; y < sy + sh; y++)
                for (int x = sx; x < sx + sw; x++)
                {
                    bool edge = y == sy || y == sy + sh - 1 || x == sx || x == sx + sw - 1;
                    Put(x, y, ' ', fill, edge ? frame : fill);
                }
            // A building nobody has seen into or up to keeps its name.
            bool named = sectionKnown != Known.Unknown;
            string shown = named ? title : " ??? ".PadRight(title.Length);
            for (int i = 0; i < shown.Length; i++)
                Put(sx + 2 + i, sy, shown[i], named ? ink : UnknownInk, tab);
        }

        // Lines, from what each cell's sides are.
        for (int c = 0; c < w * h; c++)
        {
            if (grid.NetA[c] == -1) continue;
            int x = c % w, y = c / w;
            var kind = grid.Kind[c];
            var known = cellKnown[c];
            if (grid.NetB[c] != -1)
            {
                // Two nets crossing: the horizontal one runs heavy over a light vertical.
                int horizontalNet = (grid.MaskA[c] & Horizontal) != 0 ? grid.NetA[c] : grid.NetB[c];
                Put(x, y, '┿', LineColor(horizontalNet, known));
            }
            else
                // An unknown way is drawn plain whatever it is: its kind is not known either.
                Put(x, y, LineGlyph(grid.MaskA[c], _netDashed[grid.NetA[c]] && known != Known.Unknown),
                    LineColor(grid.NetA[c], known));
        }

        PaintExit(Put);

        // Areas over everything: a card, the name on a coloured band, the detail under it.
        foreach (var (area, (bx, by)) in _boxAt)
        {
            bool isHere = here != null && area.Id == here.Id;
            var known = AreaKnown(area);
            var band = isHere ? HereBand : area.IsPrivate ? PrivateBand : BoxBand;
            var fill = isHere ? HereFill : area.IsPrivate ? PrivateFill : BoxFill;
            var ink  = isHere ? HereName : area.IsPrivate ? PrivateName : BoxName;
            if (known == Known.Seen)
            {
                band = Shade(band, SeenShade); fill = Shade(fill, SeenShade); ink = Shade(ink, SeenShade);
            }
            else if (known == Known.Unknown)
            {
                // Not even whose ground it is: an unknown private room looks like any other.
                band = UnknownBand; fill = UnknownFill; ink = UnknownInk;
            }
            // While a way back is chosen: what may be gone back to lit, the one under the cursor
            // as a keyword is when hovered.
            if (choosable != null && choosable.Contains(area))
            {
                bool hot = hovered != null && area.Id == hovered.Id;
                band = hot ? Config.NarrativeUI.KeywordHoverBackgroundColor : Lift(band, 0.35f);
                fill = hot ? Lift(Config.NarrativeUI.KeywordHoverBackgroundColor, -0.4f) : Lift(fill, 0.25f);
                ink  = hot ? DarkText : Config.Colors.White;
            }

            for (int y = by; y < by + BoxH; y++)
                for (int x = bx; x < bx + BoxW; x++)
                    Put(x, y, ' ', DarkText, y < by + 2 ? band : fill);

            var name = known == Known.Unknown ? new List<string> { "???" } : Wrap(area.DisplayName, BoxW - 2, 2);
            for (int line = 0; line < name.Count; line++)
                for (int i = 0; i < name[line].Length; i++)
                    Put(bx + 1 + i, by + line, name[line][i], ink);

            // Who is about is only known where you stand: a count anywhere else would be a sighting
            // nobody made.
            string tenure = area.IsPrivate ? "private" : area.IsWild ? "wild" : "public";
            string detail = isHere ? $"you · {scene.GetNpcsAt(area, period).Count} npc"
                          : known == Known.Visited ? tenure
                          : known == Known.Seen ? "not visited"
                          : "";
            // The routines learned here take the right of the detail row; the detail keeps what is left.
            var mine = iconsOf.TryGetValue(area, out var list) ? list : null;
            int iconRoom = mine == null ? 0 : Math.Min(mine.Count, BoxW - 2);
            int detailRoom = BoxW - 2 - (iconRoom > 0 ? iconRoom + 1 : 0);
            if (detail.Length > detailRoom) detail = detail[..Math.Max(0, detailRoom)];
            for (int i = 0; i < detail.Length; i++)
                Put(bx + 1 + i, by + 2, detail[i], choosable != null && choosable.Contains(area) ? ink
                                                  : isHere ? Config.Colors.DarkYellowGrey
                                                  : known == Known.Seen ? UnknownInk : Config.Colors.MediumGray50);

            for (int i = 0; i < iconRoom; i++)
            {
                var icon = mine![i];
                int ix = bx + BoxW - 1 - iconRoom + i, iy = by + 2;
                bool more = i == iconRoom - 1 && mine.Count > iconRoom;
                bool hot = Equals(icon.Tag, hoveredIcon), picked = Equals(icon.Tag, selectedIcon);
                // Neighbours alternate two yellows, so a row of icons reads as so many and not as a word.
                var back = !icon.Enabled ? IconOff
                         : hot ? Config.Colors.White
                         : picked ? Config.Colors.BrightYellow
                         : i % 2 == 0 ? IconOn : IconOnAlt;
                if (picked && !hot && !more && !selectedLit)
                    Put(ix, iy, icon.Glyph, icon.Enabled ? Config.Colors.BrightYellow : IconOff, DarkText);
                else
                    Put(ix, iy, more ? '+' : icon.Glyph, DarkText, back);
                if (!more) _iconCells.Add((ix, iy, icon.Tag));
            }
        }

        // A connector's letter, as a badge on a straight stretch only it uses.
        foreach (var link in _links.Concat(_overlayLinks))
        {
            if (!link.Dashed) continue;
            var straight = link.Fresh.Where(c => grid.NetB[c] == -1 && grid.Kind[c] is Cell.Free or Cell.BesideBox
                                              && (grid.MaskA[c] == Vertical || grid.MaskA[c] == Horizontal)).ToList();
            if (straight.Count == 0) continue;
            int at = straight[straight.Count / 2];
            var known = LinkKnown(link);
            var badge = link.View ? ViewLine : DashedLine;
            if (known == Known.Unknown)
                Put(at % w, at / w, '?', UnknownInk, UnknownBand);
            else
                Put(at % w, at / w, link.Kind, DarkText, known == Known.Seen ? Shade(badge, 0.5f) : badge);
        }

        return (glyphs, fgs, bgs);
    }

    private Vector4 NetColor(int net) => _netView[net] ? ViewLine : _netDashed[net] ? DashedLine : SolidLine;

    private Vector4 LineColor(int net, Known known) => known switch
    {
        Known.Visited => NetColor(net),
        Known.Seen    => Shade(NetColor(net), 0.45f),
        _             => UnknownLine,
    };

    /// <summary>How much of its colour a thing keeps at each degree of knowledge.</summary>
    private static float KnownShade(Known known) => known switch
    {
        Known.Visited => 1f,
        Known.Seen    => SeenShade,
        _             => UnknownShade,
    };

    private static Vector4 Shade(Vector4 c, float k) => new(c.X * k, c.Y * k, c.Z * k, 1f);

    /// <summary>The heavy box-drawing glyph for the sides a line uses; dashed on straight runs.</summary>
    private static char LineGlyph(int mask, bool dashed)
    {
        const int n = 1 << N, e = 1 << E, s = 1 << S, wv = 1 << W;
        return mask switch
        {
            n or s or (n | s)       => dashed ? '┇' : '┃',
            e or wv or (e | wv)     => dashed ? '┅' : '━',
            n | e                   => '┗',
            e | s                   => '┏',
            s | wv                  => '┓',
            wv | n                  => '┛',
            n | e | s               => '┣',
            e | s | wv              => '┳',
            s | wv | n              => '┫',
            wv | n | e              => '┻',
            _                       => '╋',
        };
    }

    private static List<string> Wrap(string text, int width, int maxLines)
    {
        var lines = new List<string>();
        string line = "";
        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            string w = word.Length > width ? word[..width] : word;
            if (line.Length == 0) line = w;
            else if (line.Length + 1 + w.Length <= width) line += " " + w;
            else { lines.Add(line); line = w; }
        }
        if (line.Length > 0) lines.Add(line);
        if (lines.Count > maxLines)
        {
            lines = lines.Take(maxLines).ToList();
            string last = lines[^1];
            lines[^1] = (last.Length >= width ? last[..(width - 1)] : last) + "…";
        }
        return lines;
    }

    /// <summary>
    /// The pan pad, five cells by three:
    /// <code>
    ///   ▲
    /// ◄ ● ►
    ///   ▼
    /// </code>
    /// The centre re-centres on the party.
    /// </summary>
    private void RenderPad(TerminalHUD terminal, int x, int y)
    {
        void Arm(int ax, int ay, char ch, PadArrow arrow)
        {
            var fg = _hovered == arrow ? Config.NarrativeUI.KeywordHoverColor : Config.NarrativeUI.KeywordNormalColor;
            var back = _hovered == arrow ? Config.NarrativeUI.KeywordHoverBackgroundColor : Canvas;
            terminal.SetCell(ax, ay, ch, fg, back);
            _padCells.Add((ax, ay, arrow));
        }
        Arm(x + 2, y,     '▲', PadArrow.Up);
        Arm(x,     y + 1, '◄', PadArrow.Left);
        Arm(x + 2, y + 1, '●', PadArrow.Centre);
        Arm(x + 4, y + 1, '►', PadArrow.Right);
        Arm(x + 2, y + 2, '▼', PadArrow.Down);
    }

    // ── Layout ───────────────────────────────────────────────────────────────

    private void Layout(Scene.Scene scene)
    {
        _boxAt.Clear();
        _sectionBoxes.Clear();
        _links.Clear();
        _viewLinks.Clear();
        _netDashed.Clear();
        _netView.Clear();

        var areas = scene.AllAreas;
        var sectionOf = new Dictionary<Area, Section>();
        foreach (var s in scene.Sections)
            foreach (var a in s.Areas)
                sectionOf.TryAdd(a, s);

        // Solid: the area graph, undirected and deduplicated.
        var seen = new HashSet<(Guid, Guid)>();
        foreach (var (from, tos) in scene.AreaGraph)
            foreach (var to in tos)
            {
                var a = scene.GetArea(from);
                var b = scene.GetArea(to);
                if (a == null || b == null || a == b || !sectionOf.ContainsKey(a) || !sectionOf.ContainsKey(b)) continue;
                if (seen.Add(Key(a, b)))
                    _links.Add(new Link { From = a, To = b, Dashed = false, View = false, Kind = ' ',
                                          Key = AreaVisits.LinkKey(a, b) });
            }

        // Dashed: every connector, once (each is listed in both its areas' PoIs).
        var connectorsSeen = new HashSet<ConnectorPointOfInterest>();
        foreach (var area in areas)
            foreach (var c in area.PointsOfInterest.OfType<ConnectorPointOfInterest>())
                if (connectorsSeen.Add(c) && c.AreaA != c.AreaB
                    && sectionOf.ContainsKey(c.AreaA) && sectionOf.ContainsKey(c.AreaB))
                {
                    bool view = c is LandscapePointOfInterest;
                    var link = new Link { From = c.AreaA, To = c.AreaB, Dashed = true, View = view,
                                          Kind = KindLetter(c), Key = c.StableKey };
                    (view ? _viewLinks : _links).Add(link);
                }

        // Weighted undirected links between areas, for placement. Views are left out: a roof sees
        // every outside area, and pulling it towards all of them would place it nowhere in particular.
        var weights = new Dictionary<Area, Dictionary<Area, int>>();
        foreach (var link in _links)
            foreach (var (p, q) in new[] { (link.From, link.To), (link.To, link.From) })
            {
                if (!weights.TryGetValue(p, out var m)) weights[p] = m = new();
                m[q] = m.GetValueOrDefault(q) + 1;
            }
        IReadOnlyDictionary<Area, int> WeightsOf(Area a)
            => weights.TryGetValue(a, out var m) ? m : new Dictionary<Area, int>();

        // 1. Areas on a grid within each section.
        var cellsOf = new Dictionary<Section, Dictionary<Area, (int C, int R)>>();
        foreach (var section in scene.Sections)
        {
            var members = section.Areas.Where(a => sectionOf[a] == section).ToList();
            if (members.Count == 0) continue;
            var placed = PlaceOnGrid(members, a => WeightsOf(a).Where(kv => members.Contains(kv.Key)));
            cellsOf[section] = Normalise(placed);
        }

        // Each section's own size in cells, from its grid.
        var sizeOf = new Dictionary<Section, (int W, int H)>();
        var titleOf = new Dictionary<Section, string>();
        foreach (var (section, cells) in cellsOf)
        {
            int cols = cells.Values.Max(c => c.C) + 1, rows = cells.Values.Max(c => c.R) + 1;
            int innerW = cols * AreaPitchX - (AreaPitchX - BoxW);
            int innerH = rows * AreaPitchY - (AreaPitchY - BoxH);
            string title = $" {section.DisplayName}{(section.IsInterior ? " · interior" : "")} ";
            int w = Math.Max(innerW + 2 * SectionPadX, title.Length + 6);
            if (title.Length > w - 4) title = title[..(w - 4)];
            titleOf[section] = title;
            sizeOf[section] = (w, innerH + SectionPadTop + SectionPadBottom + 2);
        }

        // 2. Sections on a coarser grid, weighted by the ways between them.
        var sections = cellsOf.Keys.ToList();
        var sectionWeights = new Dictionary<Section, Dictionary<Section, int>>();
        foreach (var s in sections) sectionWeights[s] = new();
        foreach (var (a, m) in weights)
            foreach (var (b, n) in m)
            {
                var sa = sectionOf[a]; var sb = sectionOf[b];
                if (sa == sb || !sectionWeights.ContainsKey(sa) || !sectionWeights.ContainsKey(sb)) continue;
                sectionWeights[sa][sb] = sectionWeights[sa].GetValueOrDefault(sb) + n;
            }
        var slots = Normalise(PlaceOnGrid(sections, s => sectionWeights[s]));

        int slotCols = slots.Values.Max(c => c.C) + 1, slotRows = slots.Values.Max(c => c.R) + 1;
        var colW = new int[slotCols];
        var rowH = new int[slotRows];
        foreach (var (s, (c, r)) in slots)
        {
            colW[c] = Math.Max(colW[c], sizeOf[s].W);
            rowH[r] = Math.Max(rowH[r], sizeOf[s].H);
        }
        var colX = new int[slotCols];
        var rowY = new int[slotRows];
        for (int c = 1; c < slotCols; c++) colX[c] = colX[c - 1] + colW[c - 1] + SectionGapX;
        for (int r = 1; r < slotRows; r++) rowY[r] = rowY[r - 1] + rowH[r - 1] + SectionGapY;

        // 3. Cells to canvas coordinates, each section centred in its slot.
        foreach (var (s, (c, r)) in slots)
        {
            var (w, h) = sizeOf[s];
            int sx = CanvasMargin + colX[c] + (colW[c] - w) / 2;
            int sy = CanvasMargin + rowY[r] + (rowH[r] - h) / 2;
            _sectionBoxes.Add((s, sx, sy, w, h, titleOf[s]));

            var cells = cellsOf[s];
            int cols = cells.Values.Max(v => v.C) + 1;
            int innerW = cols * AreaPitchX - (AreaPitchX - BoxW);
            int ox = sx + (w - innerW) / 2;
            int oy = sy + 1 + SectionPadTop;
            foreach (var (area, (ac, ar)) in cells)
                _boxAt[area] = (ox + ac * AreaPitchX, oy + ar * AreaPitchY);
        }

        _canvasW = CanvasMargin * 2 + colX[^1] + colW[^1];
        _canvasH = CanvasMargin * 2 + rowY[^1] + rowH[^1];

        // 4. Route every solid and dashed line; views wait for the area stood in.
        _grid = BuildGrid();
        _links.RemoveAll(l => !_boxAt.ContainsKey(l.From) || !_boxAt.ContainsKey(l.To));
        _viewLinks.RemoveAll(l => !_boxAt.ContainsKey(l.From) || !_boxAt.ContainsKey(l.To));
        int failed = RouteAll(_grid, _links, weights);
        _layoutNets = _netDashed.Count;
        foreach (var link in _links) SetPath(_grid, link);
        LayExit(scene);

        Console.WriteLine($"DebugLocationMap: laid out {_boxAt.Count} areas in {_sectionBoxes.Count} sections, " +
                          $"{_links.Count} lines ({failed} unroutable), {_viewLinks.Count} views, canvas {_canvasW}x{_canvasH}");
    }

    /// <summary>The routing grid before any line: boxes, the ring round them, section edges and tabs.</summary>
    private RouteGrid BuildGrid()
    {
        var g = new RouteGrid(_canvasW, _canvasH);
        foreach (var (_, sx, sy, sw, sh, title) in _sectionBoxes)
        {
            for (int x = sx + 1; x < sx + sw - 1; x++)
            {
                g.Kind[sy * g.W + x] = Cell.HBorder;
                g.Kind[(sy + sh - 1) * g.W + x] = Cell.HBorder;
            }
            for (int y = sy + 1; y < sy + sh - 1; y++)
            {
                g.Kind[y * g.W + sx] = Cell.VBorder;
                g.Kind[y * g.W + sx + sw - 1] = Cell.VBorder;
            }
            foreach (var (x, y) in new[] { (sx, sy), (sx + sw - 1, sy), (sx, sy + sh - 1), (sx + sw - 1, sy + sh - 1) })
                g.Kind[y * g.W + x] = Cell.Hard;
            // The tab, and a cell either side so no line runs flush against the name.
            for (int i = 1; i <= title.Length + 2; i++)
                g.Kind[sy * g.W + sx + i] = Cell.Hard;
        }
        foreach (var (bx, by) in _boxAt.Values)
            for (int y = by - 1; y <= by + BoxH; y++)
                for (int x = bx - 1; x <= bx + BoxW; x++)
                {
                    if (!g.InBounds(x, y)) continue;
                    bool inside = x >= bx && x < bx + BoxW && y >= by && y < by + BoxH;
                    int c = y * g.W + x;
                    if (inside) g.Kind[c] = Cell.Box;
                    else if (g.Kind[c] == Cell.Free) g.Kind[c] = Cell.BesideBox;
                }
        return g;
    }

    /// <summary>
    /// Routes links onto a grid, shortest first, each in the net of its busier end. Returns how many
    /// could not be routed at all.
    /// </summary>
    private int RouteAll(RouteGrid grid, List<Link> links, Dictionary<Area, Dictionary<Area, int>> weights)
    {
        int Degree(Area a) => weights.TryGetValue(a, out var m) ? m.Values.Sum() : 0;
        var nets = new Dictionary<(Area, bool, bool), int>();
        int failed = 0;

        foreach (var link in links.OrderBy(l => Distance(l.From, l.To)))
        {
            // The busier end is the source, so the ways out of a hub share its trunk.
            var (src, dst) = Degree(link.To) > Degree(link.From) ? (link.To, link.From) : (link.From, link.To);
            var key = (src, link.Dashed, link.View);
            if (!nets.TryGetValue(key, out int net))
            {
                net = _netDashed.Count;
                nets[key] = net;
                _netDashed.Add(link.Dashed);
                _netView.Add(link.View);
            }
            if (!Route(grid, link, src, dst, net, relaxed: false) && !Route(grid, link, src, dst, net, relaxed: true))
                failed++;
        }
        return failed;
    }

    private int Distance(Area a, Area b)
    {
        var (ax, ay) = _boxAt[a];
        var (bx, by) = _boxAt[b];
        return Math.Abs(ax - bx) + Math.Abs(ay - by);
    }

    /// <summary>The landscapes from <paramref name="here"/>, routed over a copy of the grid as one net.</summary>
    private void RouteViewsFrom(Area? here)
    {
        _overlayFor = here;
        _overlayLinks.Clear();
        _overlay = _grid.Clone();
        if (here == null) return;
        // Views are drawn from where the party stands, whichever end the connector names first.
        foreach (var view in _viewLinks.Where(v => v.From.Id == here.Id || v.To.Id == here.Id))
        {
            var link = new Link { From = view.From, To = view.To, Dashed = true, View = true,
                                  Kind = view.Kind, Key = view.Key };
            _overlayLinks.Add(link);
        }
        if (_overlayLinks.Count == 0) return;
        // The views' net from the last area stood in goes; the layout's nets stay.
        _netDashed.RemoveRange(_layoutNets, _netDashed.Count - _layoutNets);
        _netView.RemoveRange(_layoutNets, _netView.Count - _layoutNets);
        int net = _netDashed.Count;
        _netDashed.Add(true);
        _netView.Add(true);
        foreach (var link in _overlayLinks.OrderBy(l => Distance(l.From, l.To)))
        {
            var other = link.From.Id == here.Id ? link.To : link.From;
            if (!Route(_overlay, link, here, other, net, relaxed: false))
                Route(_overlay, link, here, other, net, relaxed: true);
        }
        foreach (var link in _overlayLinks) SetPath(_overlay, link);
    }

    /// <summary>A routed line's whole run between its cards, for <see cref="Link.Path"/>.</summary>
    private void SetPath(RouteGrid g, Link link)
    {
        link.Path.Clear();
        if (link.Net < 0) return;
        var cells = NetPath(g, link.Net, link.From, link.To);
        if (cells == null) return;
        foreach (var (x, y) in cells)
            if (g.Kind[y * g.W + x] != Cell.Box) link.Path.Add(y * g.W + x);
    }

    /// <summary>Lays the scene out when it is not the one laid out last.</summary>
    private void EnsureLayout(Scene.Scene scene)
    {
        if (ReferenceEquals(scene, _laidOut)) return;
        Layout(scene);
        _laidOut = scene;
        _overlayFor = null;
        _overlay = null;
        _recentre = true;
    }

    // ── Walks ────────────────────────────────────────────────────────────────

    /// <summary>
    /// The cells a walk passes on the map, one step a cell: from the middle of
    /// <paramref name="from"/>'s card, along the line of each way taken (<paramref name="hops"/>, as
    /// <see cref="AreaVisits.Move"/> returns them), into the middle of the next card. A move with no
    /// way the map draws (no walk was found, or the line could not be routed) is a jump: one step from
    /// card to card, rather than a line drawn through whatever lies between. Empty when nothing can be
    /// drawn.
    /// </summary>
    public List<WalkStep> Walk(Scene.Scene scene, Area from, IReadOnlyList<(Area To, string? Link)> hops)
    {
        var steps = new List<WalkStep>();
        if (scene.Sections.Count == 0) return steps;
        EnsureLayout(scene);
        if (!_boxAt.ContainsKey(from)) return steps;

        var a = from;
        steps.Add(new WalkStep(Middle(a).X, Middle(a).Y, a, -1));
        for (int hop = 0; hop < hops.Count; hop++)
        {
            var (to, key) = hops[hop];
            if (!_boxAt.ContainsKey(to)) continue;
            var cells = (key == null ? null : AlongLine(a, to, key)) ?? new List<(int X, int Y)> { Middle(a), Middle(to) };
            var (tx, ty) = _boxAt[to];
            foreach (var (x, y) in cells.Skip(1))
            {
                bool inside = x >= tx && x < tx + BoxW && y >= ty && y < ty + BoxH;
                steps.Add(new WalkStep(x, y, inside ? to : a, hop));
            }
            a = to;
        }
        // Views are routed for the area stood in: leave the overlay to be routed afresh.
        _overlayFor = null;
        _overlay = null;
        return steps;
    }

    // ── The way in and out ───────────────────────────────────────────────────

    /// <summary>
    /// Where the location is left and entered: the glyph a fight arena's exit uses, out in the margin
    /// past every section, with a line from the card of the area the location is entered by.
    /// </summary>
    public const char ExitGlyph = '⎆';   // ⎆

    private static readonly Vector4 ExitInk     = Config.Colors.GoldYellow;
    private static readonly Vector4 ExitInkDim  = new(0.44f, 0.36f, 0.06f, 1f);
    private static readonly Vector4 ExitGround  = new(0.16f, 0.11f, 0.0f, 1f);
    private static readonly Vector4 ExitLine    = new(0.62f, 0.50f, 0.10f, 1f);
    private const int ExitBlinkMs = 400;

    /// <summary>The area the location is entered by, as the layout found it.</summary>
    private Area? _entryArea;

    /// <summary>The way out, cell by cell: from the entry card's middle to the exit glyph, both included.</summary>
    private readonly List<(int X, int Y)> _exitPath = new();

    /// <summary>The way out's line cells outside the card, by canvas index, for the pointer's label.</summary>
    private readonly HashSet<int> _exitCells = new();

    /// <summary>The canvas cell of the exit glyph, or null when no way out could be laid.</summary>
    private (int X, int Y)? ExitCell => _exitPath.Count > 1 ? _exitPath[^1] : null;

    /// <summary>The exit glyph's canvas cell for <paramref name="scene"/>, laying it out first; null when it has none.</summary>
    public (int X, int Y)? ExitCellOf(Scene.Scene scene)
    {
        if (scene.Sections.Count == 0) return null;
        EnsureLayout(scene);
        return ExitCell;
    }

    /// <summary>The area the location is entered and left by, as the map has it.</summary>
    public Area? EntryAreaOf(Scene.Scene scene)
    {
        if (scene.Sections.Count == 0) return null;
        EnsureLayout(scene);
        return _entryArea;
    }

    /// <summary>
    /// Lays the way out: the shortest run on the grid from the middle of the entry card to the
    /// margin round the canvas, outside every section. It crosses a section's edge straight, crosses a
    /// line straight over and never runs along one, and turns as little as it can — the same rules
    /// the lines keep, so it reads as one more of them. Nothing is laid when no such run exists.
    /// </summary>
    private void LayExit(Scene.Scene scene)
    {
        _exitPath.Clear();
        _exitCells.Clear();
        _entryArea = Scene.SceneSyntheticGraphFactory.ResolveEntryArea(scene);
        if (_entryArea == null || !_boxAt.TryGetValue(_entryArea, out var box)) return;

        var g = _grid;
        int w = g.W, h = g.H;
        bool OwnBox(int x, int y) => x >= box.X && x < box.X + BoxW && y >= box.Y && y < box.Y + BoxH;
        bool Goal(int x, int y) => x == 1 || y == 1 || x == w - 2 || y == h - 2;

        // Dijkstra over (cell, direction entered by): turning and crossing are charged, so the state
        // has to know which way it came.
        int states = w * h * 4;
        var dist = new float[states];
        var parent = new int[states];
        Array.Fill(dist, float.MaxValue);
        Array.Fill(parent, -1);
        var queue = new PriorityQueue<int, float>();
        var (mx, my) = Middle(_entryArea);
        for (int d = 0; d < 4; d++)
        {
            int st = (my * w + mx) * 4 + d;
            dist[st] = 0;
            queue.Enqueue(st, 0);
        }

        int found = -1;
        while (queue.TryDequeue(out int st, out float cost))
        {
            if (cost > dist[st]) continue;
            int c = st / 4, dir = st % 4, x = c % w, y = c / w;
            if (!OwnBox(x, y) && Goal(x, y)) { found = st; break; }
            // A line crossed is crossed straight over: no turn on it.
            bool onLine = !OwnBox(x, y) && g.NetA[c] != -1;
            for (int d = 0; d < 4; d++)
            {
                if (onLine && d != dir) continue;
                if (d == (dir + 2) % 4 && cost > 0) continue;
                int nx = x + DX[d], ny = y + DY[d];
                if (!g.InBounds(nx, ny)) continue;
                int nc = ny * w + nx;
                float step = 1f;
                if (!OwnBox(nx, ny))
                {
                    bool vertical = d is N or S;
                    switch (g.Kind[nc])
                    {
                        case Cell.Box:
                        case Cell.Hard:
                            continue;
                        case Cell.HBorder when !vertical:
                        case Cell.VBorder when vertical:
                            continue;
                        case Cell.HBorder:
                        case Cell.VBorder:
                            step += CrossBorderCost;
                            break;
                        case Cell.BesideBox:
                            if (!OwnBoxRing(nx, ny)) step += BesideBoxCost;
                            break;
                    }
                    if (g.NetA[nc] != -1)
                    {
                        // Only straight over a single straight line.
                        int across = vertical ? Horizontal : Vertical;
                        if (g.NetB[nc] != -1 || g.MaskA[nc] != across) continue;
                        step += CrossLineCost;
                    }
                }
                if (cost > 0 && d != dir) step += TurnCost;
                int ns = nc * 4 + d;
                float nd = cost + step;
                if (nd < dist[ns])
                {
                    dist[ns] = nd;
                    parent[ns] = st;
                    queue.Enqueue(ns, nd);
                }
            }
        }
        if (found < 0)
        {
            Console.WriteLine($"DebugLocationMap: no way out could be laid from '{_entryArea.DisplayName}'");
            return;
        }

        var cells = new List<(int X, int Y)>();
        for (int st = found; st >= 0; st = parent[st])
        {
            int c = st / 4;
            if (cells.Count == 0 || cells[^1] != (c % w, c / w)) cells.Add((c % w, c / w));
        }
        cells.Reverse();
        _exitPath.AddRange(cells);
        foreach (var (x, y) in cells)
            if (!OwnBox(x, y)) _exitCells.Add(y * w + x);

        // Laid on the grid as a net of its own, so the views routed later cross it as they cross any
        // line rather than run along it.
        int net = _netDashed.Count;
        _netDashed.Add(false);
        _netView.Add(false);
        for (int i = 0; i < cells.Count; i++)
        {
            var (x, y) = cells[i];
            if (OwnBox(x, y)) continue;
            int bits = 0;
            foreach (int j in new[] { i - 1, i + 1 })
            {
                if (j < 0 || j >= cells.Count) continue;
                for (int d = 0; d < 4; d++)
                    if (cells[j].X == x + DX[d] && cells[j].Y == y + DY[d]) bits |= 1 << d;
            }
            g.Add(y * w + x, net, bits);
        }
        _layoutNets = _netDashed.Count;

        bool OwnBoxRing(int x, int y) => x >= box.X - 1 && x <= box.X + BoxW && y >= box.Y - 1 && y <= box.Y + BoxH;
    }

    /// <summary>The way out's line and the blinking exit glyph at its end.</summary>
    private void PaintExit(Action<int, int, char, Vector4, Vector4?> put)
    {
        if (ExitCell is not { } exit) return;
        for (int i = 0; i < _exitPath.Count - 1; i++)
        {
            var (x, y) = _exitPath[i];
            int c = y * _canvasW + x;
            if (!_exitCells.Contains(c)) continue;
            int mask = 0;
            foreach (int j in new[] { i - 1, i + 1 })
            {
                if (j < 0) continue;
                var (ox, oy) = _exitPath[j];
                for (int d = 0; d < 4; d++)
                    if (ox == x + DX[d] && oy == y + DY[d]) mask |= 1 << d;
            }
            // A line already there is crossed: the crossing glyph, in the way out's colour.
            var grid = _overlay ?? _grid;
            char glyph = grid.NetB[c] != -1 ? '╋' : LineGlyph(mask, dashed: false);
            put(x, y, glyph, ExitLine, null);
        }
        bool lit = Environment.TickCount64 / ExitBlinkMs % 2 == 0;
        put(exit.X, exit.Y, ExitGlyph, lit ? ExitInk : ExitInkDim, ExitGround);
    }

    /// <summary>
    /// The party coming in: from the exit glyph along the way in to the middle of the entry card.
    /// Empty when the map has no way out laid, or <paramref name="entry"/> is not the area it starts from.
    /// </summary>
    public List<WalkStep> WalkIn(Scene.Scene scene, Area entry)
    {
        var steps = new List<WalkStep>();
        if (scene.Sections.Count == 0) return steps;
        EnsureLayout(scene);
        if (ExitCell == null || _entryArea == null || entry.Id != _entryArea.Id) return steps;
        for (int i = _exitPath.Count - 1; i >= 0; i--)
            steps.Add(new WalkStep(_exitPath[i].X, _exitPath[i].Y, entry, -1));
        return steps;
    }

    /// <summary>
    /// The party leaving: from <paramref name="from"/> along <paramref name="hops"/> (as
    /// <see cref="Walk"/> takes them) and on from the entry card out to the exit glyph. When the walk
    /// does not end on the entry card (no known way to it), the last step is a jump to the glyph.
    /// Empty when the map has no way out laid.
    /// </summary>
    public List<WalkStep> WalkOut(Scene.Scene scene, Area from, IReadOnlyList<(Area To, string? Link)> hops)
    {
        if (scene.Sections.Count == 0) return new();
        EnsureLayout(scene);
        if (ExitCell is not { } exit || _entryArea == null || !_boxAt.ContainsKey(from)) return new();
        var steps = hops.Count > 0 ? Walk(scene, from, hops) : new List<WalkStep>();
        if (steps.Count == 0) steps.Add(new WalkStep(Middle(from).X, Middle(from).Y, from, -1));
        var last = steps[^1];
        if (last.Here.Id == _entryArea.Id)
            foreach (var (x, y) in _exitPath.Skip(1))
                steps.Add(new WalkStep(x, y, last.Here, last.Hop));
        else
            steps.Add(new WalkStep(exit.X, exit.Y, last.Here, last.Hop));
        return steps;
    }

    /// <summary>Towards white by <paramref name="k"/> (towards black when negative).</summary>
    private static Vector4 Lift(Vector4 c, float k)
        => k >= 0 ? new(c.X + (1 - c.X) * k, c.Y + (1 - c.Y) * k, c.Z + (1 - c.Z) * k, 1f)
                  : Shade(c, 1 + k);

    // Where the map was last drawn on screen, for hit-testing a card: panel left, map top, size.
    private (int Left, int Top, int W, int H) _shownAt;

    /// <summary>The area whose card is under screen cell (<paramref name="x"/>, <paramref name="y"/>) as last drawn, or null.</summary>
    public Area? AreaAt(int x, int y)
    {
        var (left, top, w, h) = _shownAt;
        if (x < left || y < top || x >= left + w || y >= top + h) return null;
        int cx = _viewX + x - left, cy = _viewY + y - top;
        foreach (var (area, (bx, by)) in _boxAt)
            if (cx >= bx && cx < bx + BoxW && cy >= by && cy < by + BoxH) return area;
        return null;
    }

    /// <summary>The tag of the routine icon under screen cell (<paramref name="x"/>, <paramref name="y"/>) as last drawn, or null.</summary>
    public object? IconAt(int x, int y)
    {
        var (left, top, w, h) = _shownAt;
        if (x < left || y < top || x >= left + w || y >= top + h) return null;
        int cx = _viewX + x - left, cy = _viewY + y - top;
        foreach (var (ix, iy, tag) in _iconCells)
            if (ix == cx && iy == cy) return tag;
        return null;
    }

    /// <summary>
    /// What is under screen cell (<paramref name="x"/>, <paramref name="y"/>) as last drawn, in a few
    /// words, or null over bare canvas. An icon is the caller's to name (<paramref name="iconLabel"/>,
    /// handed its tag); the pad, a card and a line are named here.
    /// </summary>
    public HoverLabel? LabelAt(int x, int y, Func<object, HoverLabel?>? iconLabel = null)
    {
        switch (Pannable ? ArrowAt(x, y) : PadArrow.None)
        {
            case PadArrow.Up:     return new("pan up", true);
            case PadArrow.Down:   return new("pan down", true);
            case PadArrow.Left:   return new("pan left", true);
            case PadArrow.Right:  return new("pan right", true);
            case PadArrow.Centre: return new("centre the map", true);
        }
        var (left, top, w, h) = _shownAt;
        if (x < left || y < top || x >= left + w || y >= top + h) return null;
        if (IconAt(x, y) is { } tag) return iconLabel?.Invoke(tag);
        if (AreaAt(x, y) is { } area) return AreaLabel(area);
        int cx = _viewX + x - left, cy = _viewY + y - top;
        if (cx < 0 || cy < 0 || cx >= _canvasW || cy >= _canvasH) return null;
        int c = cy * _canvasW + cx;
        if (ExitCell is { } exit && exit.X == cx && exit.Y == cy)
            return new("the way out of this place", false);
        if (_exitCells.Contains(c)) return new($"the way in: {_entryArea?.DisplayName}", false);
        return c < _lineAt.Length && _lineAt[c] is { } link ? LinkLabel(link) : null;
    }

    private HoverLabel AreaLabel(Area area)
    {
        var known = _paintedKnown.TryGetValue(area, out var k) ? k : Known.Unknown;
        if (_paintedChoosable != null && _paintedChoosable.Contains(area))
            return new($"go back to {area.DisplayName}", true);
        if (known == Known.Unknown) return new("an unknown area", false);
        string tenure = area.IsPrivate ? "private" : area.IsWild ? "wild" : "public";
        string state = _paintedHere != null && area.Id == _paintedHere.Id ? "you are here"
                     : known == Known.Visited ? "visited" : "seen, not visited";
        return new($"{area.DisplayName} · {tenure} · {state}", false);
    }

    private HoverLabel LinkLabel(Link link)
    {
        var known = _paintedLinkKnown.TryGetValue(link, out var k) ? k : Known.Unknown;
        if (known == Known.Unknown) return new("an unknown way", false);
        string Name(Area a) => _paintedKnown.TryGetValue(a, out var ak) && ak != Known.Unknown ? a.DisplayName : "???";
        string what = link.View ? $"view from {Name(link.From)} over {Name(link.To)}"
                    : $"{KindName(link.Kind)}: {Name(link.From)} – {Name(link.To)}";
        return new(known == Known.Seen ? $"{what} · seen" : what, false);
    }

    /// <summary>A line's letter in words; a solid line (no letter) joins neighbours.</summary>
    private static string KindName(char kind) => kind switch
    {
        'D' => "door",
        'S' => "stair",
        'P' => "path",
        'C' => "cliff",
        'W' => "water crossing",
        'X' => "crossing",
        '^' => "scale",
        'I' => "slip-in",
        'V' => "view",
        ' ' => "adjacent",
        _   => "way",
    };

    /// <summary>The label's ground when a click acts on what it names, and when it only tells.</summary>
    private static Vector4 LabelClickBg => Config.ExplorationPopup.LocationNameBackgroundColor;
    private static Vector4 LabelInfoBg  => Config.Colors.DarkGray40;

    /// <summary>
    /// Puts <paramref name="label"/> in the mouse-following popup, as the world map's location name is,
    /// but lifted just above the pointer so it does not hide the cell it names. Null clears the popup.
    /// </summary>
    public static void ShowLabel(PopupTerminalHUD popup, HoverLabel? label)
    {
        popup.Clear();
        if (label is not { } l) return;
        string text = $" {l.Text} ";
        if (text.Length > popup.Width) text = text[..(popup.Width - 2)] + "… ";
        popup.LiftCells = 1.5f;
        popup.DrawCenteredText(popup.Height / 2, text, DarkText, l.Clickable ? LabelClickBg : LabelInfoBg);
    }

    /// <summary>Puts <paramref name="area"/>'s card in the view at the next render, when it is not in it already.</summary>
    public void Reveal(Area area)
    {
        if (ScreenCellOf(area) == null) CentreOn(area);
    }

    /// <summary>The screen cell of <paramref name="area"/>'s card middle as last drawn, or null when it is off the view.</summary>
    public (int X, int Y)? ScreenCellOf(Area area)
    {
        if (!_boxAt.ContainsKey(area)) return null;
        var (mx, my) = Middle(area);
        var (left, top, w, h) = _shownAt;
        int x = left + mx - _viewX, y = top + my - _viewY;
        return x >= left && y >= top && x < left + w && y < top + h ? (x, y) : null;
    }

    /// <summary>The canvas cell of the middle of <paramref name="area"/>'s card, laying the scene out first; null when it has none.</summary>
    public (int X, int Y)? CardMiddle(Scene.Scene scene, Area area)
    {
        EnsureLayout(scene);
        return _boxAt.ContainsKey(area) ? Middle(area) : null;
    }

    /// <summary>Puts <paramref name="area"/>'s card in the middle of the view at the next render.</summary>
    public void CentreOn(Area area)
    {
        if (!_boxAt.ContainsKey(area)) return;
        var (mx, my) = Middle(area);
        _viewX = ClampView(mx - _shownAt.W / 2, _canvasW, _shownAt.W);
        _viewY = ClampView(my - _shownAt.H / 2, _canvasH, _shownAt.H);
    }

    private (int X, int Y) Middle(Area area)
    {
        var (bx, by) = _boxAt[area];
        return (bx + BoxW / 2, by + BoxH / 2);
    }

    /// <summary>
    /// The shortest way from <paramref name="a"/>'s middle to <paramref name="b"/>'s along the line
    /// recorded as <paramref name="key"/>: across the two cards, and along every cell of the line's net
    /// by the sides the line uses. Null when the map has no such line.
    /// </summary>
    private List<(int X, int Y)>? AlongLine(Area a, Area b, string key)
    {
        RouteGrid g = _grid;
        var link = _links.FirstOrDefault(l => l.Key == key && Joins(l, a, b));
        if (link == null && _viewLinks.Any(l => l.Key == key && Joins(l, a, b)))
        {
            RouteViewsFrom(a);
            g = _overlay!;
            link = _overlayLinks.FirstOrDefault(l => l.Key == key);
        }
        if (link == null || link.Net < 0) return null;
        return NetPath(g, link.Net, a, b);
    }

    /// <summary>
    /// The shortest way from <paramref name="a"/>'s middle to <paramref name="b"/>'s over the cells of
    /// <paramref name="net"/>, by the sides its lines use, and across the two cards. Null when the net
    /// does not join them.
    /// </summary>
    private List<(int X, int Y)>? NetPath(RouteGrid g, int net, Area a, Area b)
    {
        int Mask(int c) => g.NetA[c] == net ? g.MaskA[c] : g.NetB[c] == net ? g.MaskB[c] : 0;
        bool InBox(Area area, int x, int y)
        {
            var (bx, by) = _boxAt[area];
            return x >= bx && x < bx + BoxW && y >= by && y < by + BoxH;
        }

        var (sx, sy) = Middle(a);
        var (gx, gy) = Middle(b);
        int start = sy * g.W + sx, goal = gy * g.W + gx;
        var cameFrom = new Dictionary<int, int> { [start] = -1 };
        var queue = new Queue<int>();
        queue.Enqueue(start);
        while (queue.Count > 0 && !cameFrom.ContainsKey(goal))
        {
            int c = queue.Dequeue();
            int x = c % g.W, y = c / g.W;
            bool inA = InBox(a, x, y), inB = InBox(b, x, y);
            for (int d = 0; d < 4; d++)
            {
                int nx = x + DX[d], ny = y + DY[d];
                if (!g.InBounds(nx, ny)) continue;
                int nc = ny * g.W + nx;
                if (cameFrom.ContainsKey(nc)) continue;
                bool nA = InBox(a, nx, ny), nB = InBox(b, nx, ny);
                bool ok;
                if (inA || inB)
                    // Across the card, or out of it onto the line's stub.
                    ok = (inA && nA) || (inB && nB) || (Mask(nc) & (1 << ((d + 2) % 4))) != 0;
                else
                    // Along the line by a side it uses, onward or into a card.
                    ok = (Mask(c) & (1 << d)) != 0
                         && (nA || nB || (Mask(nc) & (1 << ((d + 2) % 4))) != 0);
                if (!ok) continue;
                cameFrom[nc] = c;
                queue.Enqueue(nc);
            }
        }
        if (!cameFrom.ContainsKey(goal)) return null;

        var path = new List<(int, int)>();
        for (int c = goal; c != -1; c = cameFrom[c]) path.Add((c % g.W, c / g.W));
        path.Reverse();
        return path;
    }

    private static bool Joins(Link l, Area a, Area b)
        => (l.From == a && l.To == b) || (l.From == b && l.To == a);

    // ── Routing ──────────────────────────────────────────────────────────────

    /// <summary>The cells a line may leave or reach a box by: beside each side, off its corners.</summary>
    private IEnumerable<(int Cell, int IntoBox, float Cost)> Ports(RouteGrid g, Area area)
    {
        var (bx, by) = _boxAt[area];
        int midX = bx + BoxW / 2, midY = by + BoxH / 2;
        for (int x = bx + 2; x <= bx + BoxW - 3; x++)
        {
            float off = Math.Abs(x - midX) * PortOffCentreCost;
            if (g.InBounds(x, by - 1))   yield return ((by - 1) * g.W + x, S, off);
            if (g.InBounds(x, by + BoxH)) yield return ((by + BoxH) * g.W + x, N, off);
        }
        for (int y = by + 1; y <= by + BoxH - 2; y++)
        {
            float off = Math.Abs(y - midY) * PortOffCentreCost;
            if (g.InBounds(bx - 1, y))    yield return (y * g.W + bx - 1, E, off);
            if (g.InBounds(bx + BoxW, y)) yield return (y * g.W + bx + BoxW, W, off);
        }
    }

    /// <summary>Whether a line of <paramref name="net"/> may only pass straight through this cell.</summary>
    private static bool PassStraight(RouteGrid g, int c, int net)
        => g.Kind[c] is Cell.HBorder or Cell.VBorder
        || (g.NetA[c] != -1 && g.NetA[c] != net)
        || g.NetB[c] != -1;

    /// <summary>What it costs a line of <paramref name="net"/> to step into a cell going <paramref name="d"/>, or null.</summary>
    private static float? EnterCost(RouteGrid g, int c, int d, int net, bool relaxed)
    {
        bool vertical = d is N or S;
        float cost = 1f;
        switch (g.Kind[c])
        {
            case Cell.Box: case Cell.Hard: return null;
            case Cell.HBorder: if (!vertical) return null; cost += CrossBorderCost; break;
            case Cell.VBorder: if (vertical) return null; cost += CrossBorderCost; break;
            case Cell.BesideBox: cost += BesideBoxCost; break;
        }

        if (g.NetA[c] == -1) return cost;
        if (g.NetA[c] == net && g.NetB[c] == -1) return g.Kind[c] is Cell.HBorder or Cell.VBorder ? cost : SharedNetCost;
        // Another net: crossed straight over a straight run of it, on open ground.
        if (g.NetB[c] == -1 && g.Kind[c] is Cell.Free or Cell.BesideBox
            && g.MaskA[c] == (vertical ? Horizontal : Vertical))
            return cost + CrossLineCost;
        return relaxed ? cost + RelaxedOverlapCost : null;
    }

    /// <summary>
    /// A* from <paramref name="src"/>'s ports (and every cell its net already holds) to
    /// <paramref name="dst"/>'s ports, over states of (cell, direction arrived in). Lays the line on
    /// the grid and returns true, or returns false and lays nothing.
    /// </summary>
    private bool Route(RouteGrid g, Link link, Area src, Area dst, int net, bool relaxed)
    {
        int cells = g.W * g.H, states = cells * 5;
        if (_g.Length < states)
        {
            _g = new float[states];
            _parent = new int[states];
            _stamp = new int[states];
            _stampNow = 0;
        }
        _stampNow++;

        var (dx0, dy0) = _boxAt[dst];
        int rx0 = dx0 - 1, rx1 = dx0 + BoxW, ry0 = dy0 - 1, ry1 = dy0 + BoxH;
        float Heuristic(int c)
        {
            int x = c % g.W, y = c / g.W;
            return Math.Max(0, Math.Max(rx0 - x, x - rx1)) + Math.Max(0, Math.Max(ry0 - y, y - ry1));
        }

        bool Usable(int c) => g.Kind[c] is Cell.Free or Cell.BesideBox
                           && (g.NetA[c] == -1 || (g.NetA[c] == net && g.NetB[c] == -1));

        var goals = new Dictionary<int, (int IntoBox, float Cost)>();
        foreach (var (c, into, cost) in Ports(g, dst))
            if (Usable(c)) goals.TryAdd(c, (into, cost));
        if (goals.Count == 0) return false;

        var open = new PriorityQueue<int, float>();
        var startInto = new Dictionary<int, int>();
        void Seed(int c, float cost, int into)
        {
            int st = c * 5 + 4;
            if (_stamp[st] == _stampNow && _g[st] <= cost) return;
            _stamp[st] = _stampNow; _g[st] = cost; _parent[st] = -1;
            if (into >= 0) startInto[c] = into; else startInto.Remove(c);
            open.Enqueue(st, cost + Heuristic(c));
        }
        foreach (var (c, into, cost) in Ports(g, src))
            if (Usable(c)) Seed(c, cost, into);
        // A branch may leave the net's trunk anywhere on open ground.
        for (int c = 0; c < cells; c++)
            if (g.NetA[c] == net && g.NetB[c] == -1 && g.Kind[c] is Cell.Free or Cell.BesideBox)
                Seed(c, 0f, -1);

        int found = -1;
        while (open.Count > 0)
        {
            int st = open.Dequeue();
            int c = st / 5, from = st % 5;
            float gc = _g[st];

            if (goals.ContainsKey(c) && from != 4) { found = st; break; }
            if (goals.ContainsKey(c) && from == 4 && !startInto.ContainsKey(c)) { found = st; break; }

            bool straightOnly = from != 4 && PassStraight(g, c, net);
            int x = c % g.W, y = c / g.W;
            for (int d = 0; d < 4; d++)
            {
                if (from != 4 && d == (from + 2) % 4) continue;          // no doubling back
                if (straightOnly && d != from) continue;
                int nx = x + DX[d], ny = y + DY[d];
                if (!g.InBounds(nx, ny)) continue;
                int nc = ny * g.W + nx;
                float? step = EnterCost(g, nc, d, net, relaxed);
                if (step == null) continue;
                float ng = gc + step.Value + (from != 4 && d != from ? TurnCost : 0f);
                if (goals.TryGetValue(nc, out var goal)) ng += goal.Cost;
                int ns = nc * 5 + d;
                if (_stamp[ns] == _stampNow && _g[ns] <= ng) continue;
                _stamp[ns] = _stampNow; _g[ns] = ng; _parent[ns] = st;
                open.Enqueue(ns, ng + Heuristic(nc));
            }
        }
        if (found < 0) return false;

        // Walk back, then lay the line: each step joins the two cells' facing sides.
        var path = new List<int>();
        for (int st = found; st >= 0; st = _parent[st]) path.Add(st);
        path.Reverse();

        int first = path[0] / 5, last = path[^1] / 5;
        if (startInto.TryGetValue(first, out int outInto)) Stub(first, outInto);
        for (int i = 1; i < path.Count; i++)
        {
            int prev = path[i - 1] / 5, cur = path[i] / 5, d = path[i] % 5;
            Lay(prev, 1 << d);
            Lay(cur, 1 << ((d + 2) % 4));
        }
        Stub(last, goals[last].IntoBox);
        link.Net = net;
        return true;

        void Lay(int c, int bits)
        {
            if (g.Add(c, net, bits)) link.Fresh.Add(c);
            link.Cells.Add(c);
        }
        // The last half-step, from the cell beside the box to its edge.
        void Stub(int c, int into) => Lay(c, 1 << into);
    }

    // ── Placement ────────────────────────────────────────────────────────────

    private static (Guid, Guid) Key(Area a, Area b)
        => a.Id.CompareTo(b.Id) < 0 ? (a.Id, b.Id) : (b.Id, a.Id);

    /// <summary>The letter on a connector's line, by its type; <see cref="KindName"/> says it in words.</summary>
    private static char KindLetter(ConnectorPointOfInterest c) => c switch
    {
        DoorPointOfInterest          => 'D',
        StairPointOfInterest         => 'S',
        PathPointOfInterest          => 'P',
        CliffPointOfInterest         => 'C',
        WaterCrossingPointOfInterest => 'W',
        CrossingPointOfInterest      => 'X',
        ScalePointOfInterest         => '^',
        SlipIntoPointOfInterest      => 'I',
        LandscapePointOfInterest     => 'V',
        _                            => '?',
    };

    /// <summary>
    /// Places nodes on an unbounded integer grid, each in the free cell nearest the neighbours
    /// already placed (squared distance, weighted by how many ways join them), so an orthogonal
    /// neighbour is preferred to a diagonal one. Order is breadth-first from the best-connected node,
    /// so each node mostly arrives with a neighbour already down. Deterministic: ties go to the
    /// first candidate in a fixed scan order, and the inputs are iterated in the scene's own order.
    /// </summary>
    private static Dictionary<T, (int C, int R)> PlaceOnGrid<T>(
        List<T> nodes, Func<T, IEnumerable<KeyValuePair<T, int>>> neighbours) where T : notnull
    {
        var order = new List<T>();
        var queued = new HashSet<T>();
        foreach (var start in nodes.OrderByDescending(n => neighbours(n).Sum(kv => kv.Value)))
        {
            if (!queued.Add(start)) continue;
            var queue = new Queue<T>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                var n = queue.Dequeue();
                order.Add(n);
                foreach (var (m, _) in neighbours(n).OrderByDescending(kv => kv.Value))
                    if (nodes.Contains(m) && queued.Add(m)) queue.Enqueue(m);
            }
        }

        var at = new Dictionary<T, (int C, int R)>();
        var taken = new HashSet<(int, int)>();
        foreach (var n in order)
        {
            if (at.Count == 0) { at[n] = (0, 0); taken.Add((0, 0)); continue; }

            int minC = at.Values.Min(p => p.C) - 1, maxC = at.Values.Max(p => p.C) + 1;
            int minR = at.Values.Min(p => p.R) - 1, maxR = at.Values.Max(p => p.R) + 1;
            var placedNeighbours = neighbours(n).Where(kv => at.ContainsKey(kv.Key))
                                                .Select(kv => (Pos: at[kv.Key], W: kv.Value)).ToList();

            (int C, int R) best = default;
            double bestCost = double.MaxValue;
            for (int r = minR; r <= maxR; r++)
                for (int c = minC; c <= maxC; c++)
                {
                    if (taken.Contains((c, r))) continue;
                    double cost;
                    if (placedNeighbours.Count > 0)
                        cost = placedNeighbours.Sum(p => p.W * (Sq(c - p.Pos.C) + Sq(r - p.Pos.R)));
                    else
                        // Unconnected: anywhere touching the rest, nearest the middle.
                        cost = 1000 + Sq(c) + Sq(r);
                    // Keep the whole compact, and a little wider than tall, since the panel is.
                    cost += 0.01 * (Sq(c) + 2 * Sq(r));
                    if (cost < bestCost) { bestCost = cost; best = (c, r); }
                }
            at[n] = best;
            taken.Add(best);
        }
        return at;

        static double Sq(int v) => (double)v * v;
    }

    private static Dictionary<T, (int C, int R)> Normalise<T>(Dictionary<T, (int C, int R)> cells) where T : notnull
    {
        int minC = cells.Values.Min(p => p.C), minR = cells.Values.Min(p => p.R);
        return cells.ToDictionary(kv => kv.Key, kv => (kv.Value.C - minC, kv.Value.R - minR));
    }
}
