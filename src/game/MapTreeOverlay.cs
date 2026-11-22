// MapTreeOverlay.cs — what the narration draws over the location map: on each card the party's
// tokens and a symbol for everything not yet observed there, and under it the thought trees of the
// member being played.
using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Tree;
using Cathedral.Game.Scene;

namespace Cathedral.Game;

/// <summary>
/// Something the pointer can be over on the narration's map. Compared by what it stands for, so the
/// symbol hovered last frame is found again in this one although every frame draws its own.
/// </summary>
public abstract record MapSymbol;

/// <summary>
/// One thing not yet observed in <paramref name="Area"/>: of a kind only, or — once remembered —
/// <paramref name="Known"/>. <paramref name="Index"/> tells one anonymous symbol of a kind from the next.
/// </summary>
public sealed record TraySymbol(Area Area, ElementKind Kind, NarrativeAnchor? Known, int Index) : MapSymbol;

/// <summary>A party member's token.</summary>
public sealed record TokenSymbol(PartyMember Member) : MapSymbol;

/// <summary>A node of a thought tree.</summary>
public sealed record NodeSymbol(ThoughtNode Node) : MapSymbol;

/// <summary>What an observed element offers to do next.</summary>
public enum StubKind { Think, Speak }

/// <summary>
/// Think about the element of <paramref name="Element"/>, or speak of it to <paramref name="Listener"/>:
/// there is one speech mark for each companion within hearing, so whom is said by the symbol clicked.
/// </summary>
public sealed record StubSymbol(ThoughtNode Element, StubKind Kind, PartyMember? Listener = null) : MapSymbol;

/// <summary>What may be done with an act still waiting, besides carrying it out (a click on the act itself).</summary>
public enum ActCommand
{
    /// <summary>Reach for an implement: the act combined with something carried.</summary>
    Tool,
    /// <summary>Think it over again: another mind may keep it and hand it to another means.</summary>
    Reconsider,
}

/// <summary>A command on the waiting act of <paramref name="Act"/>, standing beside it on its row.</summary>
public sealed record CommandSymbol(ThoughtNode Act, ActCommand Command) : MapSymbol;

/// <summary>Something an observed container holds, disclosed and not yet observed itself.</summary>
public sealed record ContentSymbol(ThoughtNode Container, int Index) : MapSymbol;

/// <summary>
/// A way not yet observed, standing on its line: the plain way between two neighbours, a door, a
/// stair, a path, a climb. <paramref name="Key"/> is what the way is recorded under
/// (<see cref="AreaVisits"/>), so one line is one symbol whichever end it is looked at from.
/// </summary>
public sealed record WaySymbol(string Key) : MapSymbol;

/// <summary>The way out of the place: LEAVE, or RUNAWAY while somebody confronts the member played.</summary>
public sealed record ExitSymbol : MapSymbol;

/// <summary>
/// The root of an area's tree, on the first row under its card: every element observed there
/// branches from under it, and a click on it folds the whole of that tree away, or brings it back.
/// </summary>
public sealed record RootSymbol(Area Area) : MapSymbol;

/// <summary>One way drawn on its line this frame: its tree once observed, and the refusals to look at it.</summary>
public sealed class WayFrame
{
    public required string Key { get; init; }
    /// <summary>
    /// The symbol it is drawn as: the way's own while nothing is known of it, and the letter of what
    /// it turned out to be once it is — a door, a stair, a path (<see cref="LocationMap.KindLetter"/>).
    /// </summary>
    public char Glyph { get; init; } = Config.Symbols.MapWay;
    /// <summary>
    /// The end an observed way was observed from. Its tree hangs from the symbol near that card and
    /// stays there: a tree that followed the member along the line read as a second way appearing.
    /// </summary>
    public Area? From { get; init; }
    /// <summary>
    /// The end a way not yet observed would be looked at from: where the played member stands when
    /// the way leaves their area, else the end fewest known steps away.
    /// </summary>
    public Area? LookedFrom { get; init; }
    /// <summary>The observed way's tree, or null while it is only a symbol.</summary>
    public ThoughtNode? Root { get; init; }
    public List<ThoughtNode> Strays { get; } = new();
}

/// <summary>What stands on, and hangs under, one card this frame.</summary>
public sealed class CardFrame
{
    public required Area Area { get; init; }
    /// <summary>The members standing here, the one being played first.</summary>
    public List<PartyMember> Tokens { get; } = new();
    /// <summary>What is not yet observed here, grouped by kind.</summary>
    public List<TraySymbol> Tray { get; } = new();
    /// <summary>The observed elements shown under this card: the played member's, located here.</summary>
    public List<ThoughtNode> Roots { get; } = new();
    /// <summary>The whole tree of this area is folded away: only its root symbol is drawn.</summary>
    public bool Folded { get; set; }
    /// <summary>Somebody confronts a member standing here: the card is framed, and the frame beats.</summary>
    public bool Confronted { get; set; }
    /// <summary>Refusals made here that hang on no element.</summary>
    public List<ThoughtNode> Strays { get; } = new();
}

/// <summary>Everything the overlay draws in one frame, as the controller reads it off the game.</summary>
public sealed class MapFrame
{
    public List<CardFrame> Cards { get; } = new();
    /// <summary>The ways the map draws a line for, each on its line.</summary>
    public List<WayFrame> Ways { get; } = new();
    /// <summary>Whether the way out is drawn (a real place), for the pointer to find.</summary>
    public bool Exit { get; set; }
    /// <summary>
    /// Whether only what hangs from the one root is drawn: no card, no token, no tray, no trunk.
    /// The childhood's screen, where the fragments of a memory are all there is.
    /// </summary>
    public bool Minimal { get; set; }
    /// <summary>An area's tree hangs from a root symbol of its own, which folds it. Not in the get-up, whose screen teaches one click.</summary>
    public bool AreaRoots { get; set; }
    public MapSymbol? Hovered { get; set; }
    /// <summary>
    /// The symbols of whoever confronts a member — in the tray until they are looked at, the root
    /// of their tree after — which beat with the frame round the card they stand on.
    /// </summary>
    public HashSet<MapSymbol> Confronters { get; } = new();
    /// <summary>Which half of the beat this frame is drawn on: the frame and the confronters are lit, or dark.</summary>
    public bool BeatLit { get; set; }
    public PartyMember? Active { get; set; }

    /// <summary>The symbol an element node is drawn with (its kind's).</summary>
    public required Func<ThoughtNode, char> ElementGlyph { get; init; }
    /// <summary>Whether the element counts the played member an enemy.</summary>
    public required Func<ThoughtNode, bool> IsHostile { get; init; }
    /// <summary>What an element node holds that is disclosed and not yet observed.</summary>
    public required Func<ThoughtNode, IReadOnlyList<NarrativeAnchor>> Contents { get; init; }
    /// <summary>Which stubs an element node shows.</summary>
    public required Func<ThoughtNode, IReadOnlyList<StubSymbol>> Stubs { get; init; }
    /// <summary>Which commands a waiting act shows beside it.</summary>
    public required Func<ThoughtNode, IReadOnlyList<ActCommand>> Commands { get; init; }
    /// <summary>Whether a click on the symbol would act now. What would not is drawn grey.</summary>
    public required Func<MapSymbol, bool> Enabled { get; init; }
}

/// <summary>
/// Draws a <see cref="MapFrame"/> over the map.
///
/// <para><b>A card.</b> Tokens stand at the right of the name. The third row holds one symbol for
/// each thing not yet observed, kind by kind; when they do not fit, the kinds that have most are
/// shown once with a count — the symbols of a kind are interchangeable until one is observed.</para>
///
/// <para><b>A tree.</b> Observed elements hang from a trunk down the card's left side, one under
/// another. A node's first child follows it on the same row; its other children each start a row of
/// their own under it, joined by a line down from the node. So a root reads left to right as
/// observation, thought, act, outcome, and a second thought about the same thing is the next row.
/// Only the cells a tree is drawn on are taken from the map: a line running under a card shows
/// wherever the tree does not reach.</para>
///
/// <para><b>A way.</b> Its symbol stands on its line, and once observed is the root of its tree:
/// hung under the symbol where the line runs across, beside it where the line runs down, and above
/// rather than below where the map has more room there. It is never drawn over a card.</para>
///
/// <para><b>What a click acts on stands on a lit ground</b>, whatever its ink says of it; what only
/// tells is drawn on the map's own.</para>
///
/// <para><b>Room.</b> The rows of cards stand a fixed distance apart, so a card has a fixed number
/// of rows to hang its trees in. When they do not all fit, the elements with something left to do
/// are drawn first and the last row says how many finished ones are not shown.</para>
///
/// <para><b>Over the lines.</b> A tree is drawn over the map's lines, and on each of its rows what
/// the map had between its first and last cell is taken off (<see cref="Settle"/>), so no line runs
/// through the middle of one. An element the player folded away (<see cref="ThoughtNode.Folded"/>)
/// is drawn alone, with a mark that there is more.</para>
/// </summary>
public sealed class MapTreeOverlay : LocationMap.IMapOverlay
{
    // The game's three hues: grey for what is told, yellow for what a click acts on and for gain,
    // purple for enemies and for what failed.
    private static readonly Vector4 LineInk      = new(0.460f, 0.410f, 0.240f, 1f);
    private static readonly Vector4 DeadInk      = Config.Colors.DarkGray40;
    private static readonly Vector4 DisabledInk  = Config.Colors.DarkGray40;
    private static readonly Vector4 HoverInk     = new(0.04f, 0.04f, 0.04f, 1f);
    private static readonly Vector4 HoverGround  = Config.Colors.GoldYellow;
    /// <summary>The ground of everything a click acts on: a dark gold, a button's face.</summary>
    private static readonly Vector4 ClickGround  = new(0.300f, 0.225f, 0.030f, 1f);
    // A confrontation: the frame round the card and the confronter's symbol, on the two halves of a beat.
    private static readonly Vector4 AlarmLit     = Config.Colors.BrightPurple;
    private static readonly Vector4 AlarmDark    = Config.Colors.DarkPurple;

    public MapFrame Frame { get; set; } = null!;

    // Set while a way's tree is drawn: it hangs in the open map, and stops at a card.
    private bool _keepOffCards;

    // The cells of the tree being drawn, and on each of its rows the first and the last of them.
    // A tree is drawn over the map's lines: once it is down, whatever the map had between two of
    // its cells on a row is taken off (Settle), so a line never runs through the middle of one.
    private readonly HashSet<(int X, int Y)> _treeCells = new();
    private readonly Dictionary<int, (int Min, int Max)> _treeSpans = new();

    // While somebody is confronted, the cells left lit: the framed cards, the trees under them and
    // whatever still answers a click. Everything else on the map goes into the shade.
    private readonly HashSet<(int X, int Y)> _lit = new();
    private bool _alarm;
    // The tree being drawn hangs under a confronted card: all of it stays lit, records included.
    private bool _litTree;
    private const float ShadeInk = 0.42f, ShadeGround = 0.55f;
    private bool _tracking;

    public int BandRoom(Area area)
        => Frame?.Cards.FirstOrDefault(c => c.Area.Id == area.Id)?.Tokens.Count ?? 0;

    public void Paint(LocationMap.MapCanvas canvas)
    {
        if (Frame == null) return;
        _lit.Clear();
        _alarm = Frame.Cards.Any(c => c.Confronted);
        foreach (var card in Frame.Cards)
            if (canvas.CardOf(card.Area) is { } at)
            {
                if (Frame.Minimal) PaintMinimal(canvas, card, at.X, at.Y);
                else PaintCard(canvas, card, at.X, at.Y);
            }

        // The ways after the cards' trees: theirs are the smaller, and where the pointer goes for one.
        _keepOffCards = true;
        foreach (var way in Frame.Ways)
        {
            var spots = canvas.WaySpots(way.Key);
            if (spots.Count == 0) continue;
            // A long line carries the symbol near each card while the way is unobserved, so it is
            // marked wherever it is looked at from. Observed, it is drawn once, where it was
            // observed from, and its tree does not move.
            var hung = spots.FirstOrDefault(p => p.Near != null && p.Near.Id == way.From?.Id, spots[0]);
            foreach (var spot in spots)
                if (way.Root == null || spot == hung)
                {
                    // Under a confrontation a way is looked at from where the member is held, and
                    // only its symbol at that end answers. The copy a long line carries near its
                    // far card stayed lit and gold in the middle of a greyed map, as though
                    // something over there could be clicked: it is drawn spent, and tagged nothing.
                    if (_alarm && way.Root == null && way.LookedFrom != null && spot.Near != null
                        && spot.Near.Id != way.LookedFrom.Id)
                    {
                        if (!canvas.IsCard(spot.X, spot.Y)) canvas.Put(spot.X, spot.Y, way.Glyph, DisabledInk);
                        continue;
                    }
                    Track();
                    PaintWay(canvas, way, spot.X, spot.Y, spot.Vertical, spot.Up, hang: spot == hung);
                    Settle(canvas);
                }
        }
        _keepOffCards = false;

        // The way out is the map's own glyph; under the pointer it is drawn as any symbol is.
        if (Frame.Exit && canvas.Exit is { } exit)
        {
            canvas.Hit(exit.X, exit.Y, new ExitSymbol());
            if (Frame.Hovered is ExitSymbol) canvas.Put(exit.X, exit.Y, LocationMap.ExitGlyph, HoverInk, HoverGround);
            _lit.Add((exit.X, exit.Y));
        }

        // A confrontation takes the rest of the map out of the light: only the framed cards, what
        // hangs under them and what a click still acts on keep their colours. Nothing else answers
        // while it lasts, and the shade says so before the pointer has to.
        if (_alarm) canvas.Shade((x, y) => _lit.Contains((x, y)), ShadeInk, ShadeGround);
    }

    /// <summary>
    /// The minimal screen: one tree in the dark, drawn from where its card would stand. Before
    /// anything is looked at it is a single symbol — whoever is remembering, drawn as the
    /// protagonist is everywhere — and once that is looked at, the tree that grows from it: each
    /// memory hangs from the fragment remembered before it, so the tree gets a row deeper and a
    /// cell further right with every memory that comes back.
    /// </summary>
    private void PaintMinimal(LocationMap.MapCanvas canvas, CardFrame card, int bx, int by)
    {
        // A little up and left of the middle: the tree grows down and to the right.
        int x = bx + 1, y = Math.Max(0, by - 3);
        if (card.Roots.Count == 0)
        {
            foreach (var symbol in card.Tray)
            {
                char glyph = symbol.Kind == ElementKind.Person ? Config.GlyphSphere.ProtagonistChar : ElementKinds.Glyph(symbol.Kind);
                Symbol(canvas, x, y, glyph, TrayInk(symbol), symbol);
                x += 2;
            }
            return;
        }
        foreach (var root in card.Roots)
            Place(canvas, root, x, ref y, live: true);
    }

    private void PaintCard(LocationMap.MapCanvas canvas, CardFrame card, int bx, int by)
    {
        // Tokens, right-aligned on the name's first row.
        for (int i = 0; i < card.Tokens.Count; i++)
        {
            var symbol = new TokenSymbol(card.Tokens[i]);
            int x = bx + canvas.CardWidth - 1 - card.Tokens.Count + i;
            bool active = ReferenceEquals(card.Tokens[i], Frame.Active);
            // Every member wears the party's face, a beast as a person does.
            Symbol(canvas, x, by, Config.GlyphSphere.ProtagonistChar,
                   active ? Config.Colors.BrightYellow : Config.Colors.Ochre, symbol);
        }

        // A confrontation frames the card, under everything else drawn on those cells. The card
        // and its frame stay lit while the rest of the map is shaded.
        if (card.Confronted)
        {
            PaintAlarm(canvas, bx, by);
            for (int y = by - 1; y <= by + canvas.CardHeight; y++)
                for (int x = bx - 1; x <= bx + canvas.CardWidth; x++)
                    _lit.Add((x, y));
        }

        PaintTray(canvas, card, bx + 1, by + 2, canvas.CardWidth - 2);

        // The trunk, and each observed element on a row of its own under the card.
        var hanging = card.Roots.Concat(card.Strays).ToList();
        if (hanging.Count == 0) return;
        _litTree = card.Confronted;
        try { PaintTrees(canvas, card, hanging, bx, by); }
        finally { _litTree = false; }
    }

    /// <summary>The trees under a card: its root symbol, then each observed element on rows of its own.</summary>
    private void PaintTrees(LocationMap.MapCanvas canvas, CardFrame card, List<ThoughtNode> hanging, int bx, int by)
    {

        Track();
        int y = by + canvas.CardHeight, trunk = bx, last = -1;
        // The tree's own root, over everything observed here: the elements branch from under it,
        // and a click on it folds the whole tree away. Folded, it stands alone with the mark that
        // there is more.
        if (Frame.AreaRoots)
        {
            var root = new RootSymbol(card.Area);
            // In the trunk's own ink: the root is where the tree's lines start, not one more thing
            // standing in the place. What says it can be clicked is the pointer over it.
            Symbol(canvas, trunk, y, Config.Symbols.MapRoot, LineInk, root);
            if (card.Folded)
            {
                Symbol(canvas, trunk + 1, y, Config.Symbols.MapFolded, Config.Colors.BrightYellow, root);
                Settle(canvas);
                return;
            }
            last = y++;
        }
        // What does not fit is what is finished: the elements with something left to do come first.
        int room = canvas.TreeRows - (Frame.AreaRoots ? 1 : 0);
        int shown = hanging.Count, used = hanging.Sum(Rows);
        if (used > room)
        {
            hanging = hanging.OrderBy(n => HasBusiness(n) ? 0 : 1).ToList();
            // One row is kept to say how many are not shown.
            shown = 0; used = 0;
            while (shown < hanging.Count && used + Rows(hanging[shown]) <= room - 1)
                used += Rows(hanging[shown++]);
        }
        int hidden = hanging.Count - shown;

        for (int i = 0; i < shown; i++)
        {
            for (int r = last + 1; last >= 0 && r < y; r++) Line(canvas, trunk, r, '│');
            Line(canvas, trunk, y, i == shown - 1 && hidden == 0 ? '└' : '├');
            Line(canvas, trunk + 1, y, '─');
            last = y;
            Place(canvas, hanging[i], trunk + 2, ref y, live: hanging[i].Kind == ThoughtNodeKind.Element || hanging[i].IsLive);
        }
        if (hidden > 0)
        {
            for (int r = last + 1; last >= 0 && r < y; r++) Line(canvas, trunk, r, '│');
            string more = $"└ +{hidden}";
            for (int i = 0; i < more.Length; i++)
            {
                canvas.Put(trunk + i, y, more[i], DeadInk);
                Took(trunk + i, y);
            }
        }
        Settle(canvas);
    }

    /// <summary>
    /// The frame round a card somebody is confronted on: a double line one cell outside it, in the
    /// purple of a danger, lit and dark on the two halves of a beat. Only its ink changes with the
    /// beat — the glyphs stand — so the screen reads the same on either half. Where a line of the
    /// map already runs, the line is kept and takes the frame's ink: the frame is crossed, not cut.
    /// </summary>
    private void PaintAlarm(LocationMap.MapCanvas canvas, int bx, int by)
    {
        var ink = Frame.BeatLit ? AlarmLit : AlarmDark;
        int x0 = bx - 1, x1 = bx + canvas.CardWidth, y0 = by - 1, y1 = by + canvas.CardHeight;
        void Edge(int x, int y, char glyph)
        {
            if (!canvas.InBounds(x, y) || canvas.IsCard(x, y)) return;
            if (canvas.GlyphAt(x, y) == ' ') canvas.Put(x, y, glyph, ink);
            else canvas.Ink(x, y, ink);
        }
        for (int x = x0 + 1; x < x1; x++) { Edge(x, y0, '═'); Edge(x, y1, '═'); }
        for (int y = y0 + 1; y < y1; y++) { Edge(x0, y, '║'); Edge(x1, y, '║'); }
        Edge(x0, y0, '╔'); Edge(x1, y0, '╗'); Edge(x0, y1, '╚'); Edge(x1, y1, '╝');
    }

    /// <summary>Starts taking note of the cells a tree is drawn on.</summary>
    private void Track()
    {
        _treeCells.Clear();
        _treeSpans.Clear();
        _tracking = true;
    }

    private void Took(int x, int y)
    {
        if (_litTree) _lit.Add((x, y));
        if (!_tracking) return;
        _treeCells.Add((x, y));
        _treeSpans[y] = _treeSpans.TryGetValue(y, out var span) ? (Math.Min(span.Min, x), Math.Max(span.Max, x)) : (x, x);
    }

    /// <summary>
    /// The tree is down: on each of its rows, what the map had between its first and its last cell
    /// is taken off. Only there — a line beside a tree, or under a card where no tree reaches, is
    /// left as it was.
    /// </summary>
    private void Settle(LocationMap.MapCanvas canvas)
    {
        _tracking = false;
        foreach (var (y, (min, max)) in _treeSpans)
            for (int x = min + 1; x < max; x++)
                if (!_treeCells.Contains((x, y))) canvas.Blank(x, y);
    }

    /// <summary>The rows <paramref name="node"/> and everything under it are drawn on.</summary>
    private int Rows(ThoughtNode node)
    {
        int rows = Hanging(node).Sum(RowsOf);
        return Math.Max(1, rows);
    }

    /// <summary>The rows one hanging thing takes: a node its tree's, a shelf one.</summary>
    private int RowsOf(object item) => item is ThoughtNode child ? Rows(child) : 1;

    /// <summary>Whether anything under <paramref name="root"/> still answers a click: an act waiting, something to think of, something held.</summary>
    private bool HasBusiness(ThoughtNode root)
        => root.SelfAndDescendants().Any(n => n.IsWaitingAct
               || (n.Kind == ThoughtNodeKind.Element && (Frame.Stubs(n).Count > 0 || Frame.Contents(n).Count > 0)));

    // ── The tray ─────────────────────────────────────────────────────────────

    private void PaintTray(LocationMap.MapCanvas canvas, CardFrame card, int x0, int y, int room)
    {
        var byKind = ElementKinds.All
            .Select(k => card.Tray.Where(t => t.Kind == k).ToList())
            .Where(l => l.Count > 0).ToList();
        if (byKind.Count == 0) return;

        // One symbol each while they fit, a space between kinds. When they do not, the anonymous
        // symbols of the fullest kinds fold into one with a count, fullest first, until they do.
        var folded = new HashSet<ElementKind>();
        int Width() => byKind.Sum(l => WidthOf(l, folded.Contains(l[0].Kind))) + byKind.Count - 1;
        while (Width() > room)
        {
            var fullest = byKind.Where(l => !folded.Contains(l[0].Kind) && l.Count(t => t.Known == null) > 1)
                                .OrderByDescending(l => l.Count(t => t.Known == null)).FirstOrDefault();
            if (fullest == null) break;
            folded.Add(fullest[0].Kind);
        }

        int x = x0;
        foreach (var group in byKind)
        {
            if (x > x0) x++;
            bool fold = folded.Contains(group[0].Kind);
            // The remembered first: each is its own symbol, a shade brighter than the anonymous.
            foreach (var known in group.Where(t => t.Known != null))
                if (x < x0 + room) Symbol(canvas, x++, y, ElementKinds.Glyph(known.Kind), TrayInk(known), known);
            var anonymous = group.Where(t => t.Known == null).ToList();
            if (anonymous.Count == 0) continue;
            if (!fold)
            {
                foreach (var t in anonymous)
                    if (x < x0 + room) Symbol(canvas, x++, y, ElementKinds.Glyph(t.Kind), TrayInk(t), t);
                continue;
            }
            // Folded: the symbol, then how many. Every cell of it is the first of them.
            string count = anonymous.Count.ToString();
            if (x + count.Length < x0 + room)
            {
                Symbol(canvas, x++, y, ElementKinds.Glyph(anonymous[0].Kind), TrayInk(anonymous[0]), anonymous[0]);
                foreach (char digit in count)
                    Symbol(canvas, x++, y, digit, TrayInk(anonymous[0]), anonymous[0]);
            }
        }
    }

    private static int WidthOf(List<TraySymbol> group, bool folded)
    {
        int known = group.Count(t => t.Known != null), anonymous = group.Count - known;
        return !folded || anonymous <= 1 ? group.Count : known + 1 + anonymous.ToString().Length;
    }

    /// <summary>An enemy is purple whether or not it can be looked at from here; the rest are gold, or grey out of reach.</summary>
    private Vector4 TrayInk(TraySymbol symbol)
    {
        bool enabled = Frame.Enabled(symbol);
        if (symbol.Kind == ElementKind.Hostile)
            return !enabled ? Config.Colors.DarkPurple : symbol.Known != null ? Config.Colors.BrightPurple : Config.Colors.LightPurple;
        return !enabled ? DisabledInk : symbol.Known != null ? Config.Colors.BrightYellow : Config.Colors.GoldYellow;
    }

    // ── A way ────────────────────────────────────────────────────────────────

    /// <summary>
    /// A way's symbol on its line and what hangs from it: its tree once observed, and any refusal to
    /// look at it. Under the symbol where the line runs across — every row a branch off a trunk down
    /// from it — and beside it where the line runs down, the first branch on the symbol's own row.
    /// Above instead of below when <paramref name="up"/>. Only the symbol when not <paramref name="hang"/>:
    /// the far one of a long line's two.
    /// </summary>
    private void PaintWay(LocationMap.MapCanvas canvas, WayFrame way, int x, int y, bool vertical, bool up, bool hang)
    {
        var items = new List<object>();
        if (way.Root is { } root)
        {
            Symbol(canvas, x, y, way.Glyph, NodeInk(root, live: true), new NodeSymbol(root));
            if (root.Folded) FoldMark(canvas, root, x + 1, y);
            items.AddRange(Hanging(root));
        }
        else
        {
            var symbol = new WaySymbol(way.Key);
            Symbol(canvas, x, y, way.Glyph, Frame.Enabled(symbol) ? Config.Colors.BrightYellow : DisabledInk, symbol);
        }
        // The refusals to look at the way hang from the same symbol as its tree, and are folded
        // away with it: folding left them standing, a tree put away with its refusals still out.
        if (way.Root is not { Folded: true }) items.AddRange(way.Strays);
        if (!hang || items.Count == 0) return;

        // Where each branch starts: one under another, from the top.
        int total = items.Sum(RowsOf);
        int top = vertical ? (up ? y - total + 1 : y) : (up ? y - total : y + 1);
        int trunk = vertical ? x + 1 : x;
        var starts = new List<int>();
        for (int i = 0, r = top; i < items.Count; r += RowsOf(items[i]), i++) starts.Add(r);

        // The trunk, from the symbol to the furthest branch.
        int first = Math.Min(starts[0], vertical ? y : y + 1), last = Math.Max(starts[^1], vertical ? y : y - 1);
        for (int r = first; r <= last; r++)
        {
            // Under or over the symbol, the trunk runs on to it; beside it, it joins it on its row.
            bool branch = starts.Contains(r);
            bool above = r > first || (!vertical && !up), below = r < last || (!vertical && up);
            char glyph = vertical && r == y
                ? (branch ? (above && below ? '┼' : above ? '┴' : below ? '┬' : '─') : (above && below ? '┤' : above ? '┘' : '┐'))
                : (branch ? (above && below ? '├' : above ? '└' : below ? '┌' : '─') : '│');
            Line(canvas, trunk, r, glyph);
        }

        for (int i = 0; i < items.Count; i++)
        {
            int row = starts[i];
            switch (items[i])
            {
                case ThoughtNode child:
                    Place(canvas, child, trunk + 1, ref row,
                          live: child.IsLive || child.Kind == ThoughtNodeKind.Element);
                    break;
                case List<MapSymbol> shelf:
                    PaintShelf(canvas, shelf, trunk + 1, row);
                    break;
            }
        }
    }

    // ── A tree ───────────────────────────────────────────────────────────────

    /// <summary>
    /// What hangs under a node: its children, and for an element one last row — its shelf — of what
    /// it holds and what it offers: a symbol for each thing in it not yet looked at, then the stubs.
    /// A waiting act's commands come first instead, so they stand beside it on its own row: the act
    /// is what a click carries out, and what else may be done with it is at its elbow.
    /// </summary>
    private List<object> Hanging(ThoughtNode node)
    {
        var items = new List<object>();
        // Folded away: the element alone, whatever it holds and whatever was made of it.
        if (node.Folded) return items;
        var shelf = new List<MapSymbol>();
        if (node.IsWaitingAct)
        {
            foreach (var command in Frame.Commands(node)) shelf.Add(new CommandSymbol(node, command));
            if (shelf.Count > 0) items.Add(shelf);
            items.AddRange(node.Children);
            return items;
        }
        items.AddRange(node.Children);
        if (node.Kind != ThoughtNodeKind.Element) return items;
        // One symbol for each thing it holds not yet looked at, and none of them any one of them:
        // which is looked at is the observing mind's to choose.
        for (int i = 0, held = Frame.Contents(node).Count; i < held; i++) shelf.Add(new ContentSymbol(node, i));
        shelf.AddRange(Frame.Stubs(node));
        if (shelf.Count > 0) items.Add(shelf);
        return items;
    }

    /// <summary>
    /// Draws <paramref name="node"/> at (<paramref name="x"/>, <paramref name="y"/>) and everything
    /// under it, leaving <paramref name="y"/> on the first row below what was drawn.
    /// </summary>
    private void Place(LocationMap.MapCanvas canvas, ThoughtNode node, int x, ref int y, bool live)
    {
        int row = y;
        Symbol(canvas, x, row, NodeGlyph(node), NodeInk(node, live), new NodeSymbol(node));
        if (node.Folded) FoldMark(canvas, node, x + 1, row);

        var hanging = Hanging(node);
        if (hanging.Count == 0) { y = row + 1; return; }

        int lastCorner = row;
        for (int i = 0; i < hanging.Count; i++)
        {
            int cx;
            if (i == 0)
            {
                // The first follows on the node's own row.
                Line(canvas, x + 1, row, '─');
                cx = x + 2;
            }
            else
            {
                // The others each start a row under it, joined by a line down from the node.
                for (int r = lastCorner + 1; r < y; r++) Line(canvas, x, r, '│');
                Line(canvas, x, y, i == hanging.Count - 1 ? '└' : '├');
                lastCorner = y;
                cx = x + 1;
            }

            switch (hanging[i])
            {
                case ThoughtNode child:
                    // A branch with no act left waiting is the record of a thought, and is dimmed.
                    Place(canvas, child, cx, ref y,
                          live: node.Kind == ThoughtNodeKind.Element ? child.IsLive || child.Kind == ThoughtNodeKind.Element : live);
                    break;
                case List<MapSymbol> shelf:
                    PaintShelf(canvas, shelf, cx, y);
                    y++;
                    break;
            }
        }
        if (y <= row) y = row + 1;
    }

    /// <summary>
    /// A shelf: one row, every symbol on it joined to the next by a line. An element's is what it
    /// holds, then what it offers; a waiting act's is what may be done with it besides carrying it
    /// out.
    /// </summary>
    private void PaintShelf(LocationMap.MapCanvas canvas, List<MapSymbol> shelf, int x, int y)
    {
        MapSymbol? before = null;
        foreach (var symbol in shelf)
        {
            // Everything on a shelf is joined by a line, one symbol to the next, as a tree's nodes
            // are: what a container holds, what an element offers, what may be done with a waiting
            // act. A gap left a symbol standing beside the tree and not on it — the mark that has an
            // act thought over again read as belonging to nothing.
            if (before != null) Line(canvas, x - 1, y, '─');
            before = symbol;
            char glyph = symbol switch
            {
                StubSymbol { Kind: StubKind.Think }             => Config.Symbols.MapThink,
                StubSymbol                                      => Config.Symbols.MapSpeak,
                CommandSymbol { Command: ActCommand.Tool }      => Config.Symbols.MapTool,
                CommandSymbol                                   => Config.Symbols.MapReconsider,
                _                                               => Config.Symbols.MapContent,
            };
            var ink = !Frame.Enabled(symbol) ? DisabledInk
                    : symbol is ContentSymbol ? Config.Colors.GoldYellow : Config.Colors.BrightYellow;
            Symbol(canvas, x, y, glyph, ink, symbol);
            x += 2;
        }
    }

    private char NodeGlyph(ThoughtNode node) => node.Kind switch
    {
        ThoughtNodeKind.Element => Frame.ElementGlyph(node),
        ThoughtNodeKind.Thought => Config.Symbols.MapThought,
        ThoughtNodeKind.Act     => node.Action?.DifficultyGlyph ?? '>',
        ThoughtNodeKind.Success => Config.Symbols.MapSuccess,
        ThoughtNodeKind.Failure => Config.Symbols.MapFailure,
        ThoughtNodeKind.Emotion => Config.Symbols.MapEmotion,
        ThoughtNodeKind.Speech  => Config.Symbols.MapSpeak,
        ThoughtNodeKind.Dialogue => Config.Symbols.MapDialogue,
        _                       => Config.Symbols.MapRefusal,
    };

    private Vector4 NodeInk(ThoughtNode node, bool live) => node.Kind switch
    {
        ThoughtNodeKind.Element => Frame.IsHostile(node) ? Config.Colors.LightPurple : Config.Colors.White,
        ThoughtNodeKind.Act when node.State == ActState.Waiting
                                => Frame.Enabled(new NodeSymbol(node)) ? Config.Colors.BrightYellow : Config.Colors.DarkYellow,
        ThoughtNodeKind.Act     => node.State == ActState.Done ? Config.Colors.MediumGray50 : DeadInk,
        ThoughtNodeKind.Success => Config.Colors.GoldYellow,
        // The light purple: the standard one is too dark to read on the map's near-black ground.
        ThoughtNodeKind.Failure or ThoughtNodeKind.Refusal
                                => live ? Config.Colors.LightPurple : Config.Colors.Purple,
        ThoughtNodeKind.Emotion => Config.Colors.Ochre,
        // A conversation is a record to read back, whatever became of the branch it hangs on: it
        // keeps its ink, gold when it was won and purple when it was lost or walked away from.
        ThoughtNodeKind.Dialogue => node.Dialogue?.Ending switch
        {
            Cathedral.Game.Dialogue.Runtime.DialogueEnding.Success => Config.Colors.GoldYellow,
            Cathedral.Game.Dialogue.Runtime.DialogueEnding.Failure or
            Cathedral.Game.Dialogue.Runtime.DialogueEnding.Interrupted => Config.Colors.LightPurple,
            _ => Config.Colors.LightGray75,
        },
        _                       => live ? Config.Colors.LightGray75 : DeadInk,
    };

    // ── Cells ────────────────────────────────────────────────────────────────

    /// <summary>
    /// One symbol: inverted under the pointer, on the lit ground when a click would act on it, and
    /// otherwise on whatever the map has there.
    /// </summary>
    private void Symbol(LocationMap.MapCanvas canvas, int x, int y, char glyph, Vector4 ink, MapSymbol symbol)
    {
        if (_keepOffCards && canvas.IsCard(x, y)) return;
        bool enabled = Frame.Enabled(symbol);
        // Whoever confronts a member beats with the frame round the card, whatever their own ink.
        if (Frame.Confronters.Contains(symbol)) ink = Frame.BeatLit ? AlarmLit : AlarmDark;
        // Under the pointer a symbol is inverted when a click would act on it or it has a text to
        // read; one that answers nothing stays as it is, and only its label says why.
        if (_alarm && enabled) _lit.Add((x, y));
        if (Equals(symbol, Frame.Hovered) && (enabled || symbol is NodeSymbol)) canvas.Put(x, y, glyph, HoverInk, HoverGround);
        // The root of an area's tree is drawn as the trunk is, with no ground of its own: on the
        // lit ground its line-coloured glyph was a brown square with nothing legible in it. It
        // still answers the pointer (inverted above) and the click.
        else canvas.Put(x, y, glyph, ink, enabled && symbol is not RootSymbol ? ClickGround : null);
        canvas.Hit(x, y, symbol);
        Took(x, y);
    }

    /// <summary>The mark beside a folded element: there is more under it, and it is part of the same click.</summary>
    private void FoldMark(LocationMap.MapCanvas canvas, ThoughtNode node, int x, int y)
        => Symbol(canvas, x, y, Config.Symbols.MapFolded, Config.Colors.BrightYellow, new NodeSymbol(node));

    private void Line(LocationMap.MapCanvas canvas, int x, int y, char glyph)
    {
        if (_keepOffCards && canvas.IsCard(x, y)) return;
        canvas.Put(x, y, glyph, LineInk);
        Took(x, y);
    }
}
