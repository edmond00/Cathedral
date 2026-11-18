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
public sealed record ContentSymbol(ThoughtNode Container, NarrativeAnchor Part) : MapSymbol;

/// <summary>
/// A way not yet observed, standing on its line: the plain way between two neighbours, a door, a
/// stair, a path, a climb. <paramref name="Key"/> is what the way is recorded under
/// (<see cref="AreaVisits"/>), so one line is one symbol whichever end it is looked at from.
/// </summary>
public sealed record WaySymbol(string Key) : MapSymbol;

/// <summary>The way out of the place: LEAVE, or RUNAWAY while somebody confronts the member played.</summary>
public sealed record ExitSymbol : MapSymbol;

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
    public MapSymbol? Hovered { get; set; }
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

    public MapFrame Frame { get; set; } = null!;

    // Set while a way's tree is drawn: it hangs in the open map, and stops at a card.
    private bool _keepOffCards;

    public int BandRoom(Area area)
        => Frame?.Cards.FirstOrDefault(c => c.Area.Id == area.Id)?.Tokens.Count ?? 0;

    public void Paint(LocationMap.MapCanvas canvas)
    {
        if (Frame == null) return;
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
                    PaintWay(canvas, way, spot.X, spot.Y, spot.Vertical, spot.Up, hang: spot == hung);
        }
        _keepOffCards = false;

        // The way out is the map's own glyph; under the pointer it is drawn as any symbol is.
        if (Frame.Exit && canvas.Exit is { } exit)
        {
            canvas.Hit(exit.X, exit.Y, new ExitSymbol());
            if (Frame.Hovered is ExitSymbol) canvas.Put(exit.X, exit.Y, LocationMap.ExitGlyph, HoverInk, HoverGround);
        }
    }

    /// <summary>
    /// The minimal screen: what the one root holds and what was made of it, from where its card
    /// would stand. Each thing looked at is a row — the thing, the thought, the act — and the last
    /// row is what is still to be looked at. The root itself is not drawn: it is whoever is looking.
    /// </summary>
    private void PaintMinimal(LocationMap.MapCanvas canvas, CardFrame card, int bx, int by)
    {
        if (card.Roots.FirstOrDefault() is not { } holder) return;
        int x = bx + 1, y = by;
        foreach (var item in Hanging(holder))
            switch (item)
            {
                case ThoughtNode child:
                    Place(canvas, child, x, ref y, live: child.IsLive || child.Kind == ThoughtNodeKind.Element);
                    y++;
                    break;
                case List<MapSymbol> shelf:
                    PaintShelf(canvas, shelf, x, y);
                    y += 2;
                    break;
            }
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

        PaintTray(canvas, card, bx + 1, by + 2, canvas.CardWidth - 2);

        // The trunk, and each observed element on a row of its own under the card.
        var hanging = card.Roots.Concat(card.Strays).ToList();
        if (hanging.Count == 0) return;

        // What does not fit is what is finished: the elements with something left to do come first.
        int room = canvas.TreeRows;
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

        int y = by + canvas.CardHeight, trunk = bx, last = -1;
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
            for (int i = 0; i < more.Length; i++) canvas.Put(trunk + i, y, more[i], DeadInk);
        }
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
            items.AddRange(Hanging(root));
        }
        else
        {
            var symbol = new WaySymbol(way.Key);
            Symbol(canvas, x, y, way.Glyph, Frame.Enabled(symbol) ? Config.Colors.BrightYellow : DisabledInk, symbol);
        }
        items.AddRange(way.Strays);
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
        foreach (var part in Frame.Contents(node)) shelf.Add(new ContentSymbol(node, part));
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
    /// A shelf: one row, a space between symbols. An element's is what it holds, then what it
    /// offers; a waiting act's is what may be done with it besides carrying it out.
    /// </summary>
    private void PaintShelf(LocationMap.MapCanvas canvas, List<MapSymbol> shelf, int x, int y)
    {
        foreach (var symbol in shelf)
        {
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
        // Under the pointer a symbol is inverted when a click would act on it or it has a text to
        // read; one that answers nothing stays as it is, and only its label says why.
        if (Equals(symbol, Frame.Hovered) && (enabled || symbol is NodeSymbol)) canvas.Put(x, y, glyph, HoverInk, HoverGround);
        else canvas.Put(x, y, glyph, ink, enabled ? ClickGround : null);
        canvas.Hit(x, y, symbol);
    }

    private void Line(LocationMap.MapCanvas canvas, int x, int y, char glyph)
    {
        if (_keepOffCards && canvas.IsCard(x, y)) return;
        canvas.Put(x, y, glyph, LineInk);
    }
}
