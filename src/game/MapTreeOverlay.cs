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

/// <summary>Think about, or speak of, the element of <paramref name="Element"/>.</summary>
public sealed record StubSymbol(ThoughtNode Element, StubKind Kind) : MapSymbol;

/// <summary>Something an observed container holds, disclosed and not yet observed itself.</summary>
public sealed record ContentSymbol(ThoughtNode Container, NarrativeAnchor Part) : MapSymbol;

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
    public MapSymbol? Hovered { get; set; }
    public PartyMember? Active { get; set; }

    /// <summary>The symbol an element node is drawn with (its kind's).</summary>
    public required Func<ThoughtNode, char> ElementGlyph { get; init; }
    /// <summary>Whether the element counts the played member an enemy.</summary>
    public required Func<ThoughtNode, bool> IsHostile { get; init; }
    /// <summary>What an element node holds that is disclosed and not yet observed.</summary>
    public required Func<ThoughtNode, IReadOnlyList<NarrativeAnchor>> Contents { get; init; }
    /// <summary>Which stubs an element node shows.</summary>
    public required Func<ThoughtNode, IReadOnlyList<StubKind>> Stubs { get; init; }
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
/// another, on a panel as wide as the card that covers the map's own lines. A node's first child
/// follows it on the same row; its other children each start a row of their own under it, joined by
/// a line down from the node. So a root reads left to right as observation, thought, act, outcome,
/// and a second thought about the same thing is the next row.</para>
///
/// <para><b>Room.</b> The rows of cards stand a fixed distance apart, so a card has a fixed number
/// of rows to hang its trees in. When they do not all fit, the elements with something left to do
/// are drawn first and the last row says how many finished ones are not shown.</para>
/// </summary>
public sealed class MapTreeOverlay : LocationMap.IMapOverlay
{
    // The game's three hues: grey for what is told, yellow for what a click acts on and for gain,
    // purple for enemies and for what failed.
    private static readonly Vector4 Backdrop     = new(0.040f, 0.038f, 0.026f, 1f);
    private static readonly Vector4 LineInk      = new(0.460f, 0.410f, 0.240f, 1f);
    private static readonly Vector4 DeadInk      = Config.Colors.DarkGray40;
    private static readonly Vector4 DisabledInk  = Config.Colors.DarkGray40;
    private static readonly Vector4 HoverInk     = new(0.04f, 0.04f, 0.04f, 1f);
    private static readonly Vector4 HoverGround  = Config.Colors.GoldYellow;
    private static readonly Vector4 TrayGround   = new(0.075f, 0.075f, 0.080f, 1f);

    public MapFrame Frame { get; set; } = null!;

    public int BandRoom(Area area)
        => Frame?.Cards.FirstOrDefault(c => c.Area.Id == area.Id)?.Tokens.Count ?? 0;

    public void Paint(LocationMap.MapCanvas canvas)
    {
        if (Frame == null) return;
        foreach (var card in Frame.Cards)
            if (canvas.CardOf(card.Area) is { } at)
                PaintCard(canvas, card, at.X, at.Y);
    }

    private void PaintCard(LocationMap.MapCanvas canvas, CardFrame card, int bx, int by)
    {
        // Tokens, right-aligned on the name's first row.
        for (int i = 0; i < card.Tokens.Count; i++)
        {
            var symbol = new TokenSymbol(card.Tokens[i]);
            int x = bx + canvas.CardWidth - 1 - card.Tokens.Count + i;
            bool active = ReferenceEquals(card.Tokens[i], Frame.Active);
            Symbol(canvas, x, by, TokenGlyph(card.Tokens[i]),
                   active ? Config.Colors.BrightYellow : Config.Colors.Ochre, symbol, ground: null);
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

        int top = by + canvas.CardHeight;
        for (int r = 0; r < used + (hidden > 0 ? 1 : 0); r++) Panel(canvas, bx, top + r);

        int y = top, trunk = bx, last = -1;
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
            for (int i = 0; i < more.Length; i++) canvas.Put(trunk + i, y, more[i], DeadInk, Backdrop);
        }
    }

    /// <summary>The rows <paramref name="node"/> and everything under it are drawn on.</summary>
    private int Rows(ThoughtNode node)
    {
        int rows = 0;
        foreach (var item in Hanging(node)) rows += item is ThoughtNode child ? Rows(child) : 1;
        return Math.Max(1, rows);
    }

    /// <summary>Whether anything under <paramref name="root"/> still answers a click: an act waiting, something to think of, something held.</summary>
    private bool HasBusiness(ThoughtNode root)
        => root.SelfAndDescendants().Any(n => n.IsWaitingAct
               || (n.Kind == ThoughtNodeKind.Element && (Frame.Stubs(n).Count > 0 || Frame.Contents(n).Count > 0)));

    /// <summary>One row of the panel the trees hang on, the card's width: it covers the map's lines.</summary>
    private static void Panel(LocationMap.MapCanvas canvas, int bx, int y)
    {
        for (int x = bx; x < bx + canvas.CardWidth; x++) canvas.Put(x, y, ' ', LineInk, Backdrop);
    }

    /// <summary>The party member's piece: a beast's is another glyph.</summary>
    public static char TokenGlyph(PartyMember member)
        => member is Protagonist || member.Can(Narrative.AnatomyCapability.Speech)
            ? Config.GlyphSphere.ProtagonistChar : Config.Symbols.MapBeast;

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
                if (x < x0 + room) Symbol(canvas, x++, y, ElementKinds.Glyph(known.Kind), TrayInk(known), known, TrayGround);
            var anonymous = group.Where(t => t.Known == null).ToList();
            if (anonymous.Count == 0) continue;
            if (!fold)
            {
                foreach (var t in anonymous)
                    if (x < x0 + room) Symbol(canvas, x++, y, ElementKinds.Glyph(t.Kind), TrayInk(t), t, TrayGround);
                continue;
            }
            // Folded: the symbol, then how many. Every cell of it is the first of them.
            string count = anonymous.Count.ToString();
            if (x + count.Length < x0 + room)
            {
                Symbol(canvas, x++, y, ElementKinds.Glyph(anonymous[0].Kind), TrayInk(anonymous[0]), anonymous[0], TrayGround);
                foreach (char digit in count)
                    Symbol(canvas, x++, y, digit, TrayInk(anonymous[0]), anonymous[0], TrayGround);
            }
        }
    }

    private static int WidthOf(List<TraySymbol> group, bool folded)
    {
        int known = group.Count(t => t.Known != null), anonymous = group.Count - known;
        return !folded || anonymous <= 1 ? group.Count : known + 1 + anonymous.ToString().Length;
    }

    private Vector4 TrayInk(TraySymbol symbol)
    {
        if (!Frame.Enabled(symbol)) return DisabledInk;
        if (symbol.Kind == ElementKind.Hostile) return symbol.Known != null ? Config.Colors.BrightPurple : Config.Colors.Purple;
        return symbol.Known != null ? Config.Colors.GoldYellow : Config.Colors.MediumYellow;
    }

    // ── A tree ───────────────────────────────────────────────────────────────

    /// <summary>
    /// What hangs under a node: its children, and for an element one last row — its shelf — of what
    /// it holds and what it offers: a symbol for each thing in it not yet looked at, then the stubs.
    /// </summary>
    private List<object> Hanging(ThoughtNode node)
    {
        var items = new List<object>(node.Children);
        if (node.Kind != ThoughtNodeKind.Element) return items;
        var shelf = new List<MapSymbol>();
        foreach (var part in Frame.Contents(node)) shelf.Add(new ContentSymbol(node, part));
        foreach (var kind in Frame.Stubs(node)) shelf.Add(new StubSymbol(node, kind));
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
        Symbol(canvas, x, row, NodeGlyph(node), NodeInk(node, live), new NodeSymbol(node), Backdrop);

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
                    // One row, a space between symbols; what is held first, then what is offered.
                    foreach (var symbol in shelf)
                    {
                        char glyph = symbol is StubSymbol stub
                            ? stub.Kind == StubKind.Think ? Config.Symbols.MapThink : Config.Symbols.MapSpeak
                            : Config.Symbols.MapContent;
                        var ink = !Frame.Enabled(symbol) ? DisabledInk
                                : symbol is StubSymbol ? Config.Colors.BrightYellow : Config.Colors.GoldYellow;
                        Symbol(canvas, cx, y, glyph, ink, symbol, Backdrop);
                        canvas.Put(cx + 1, y, ' ', LineInk, Backdrop);
                        cx += 2;
                    }
                    y++;
                    break;
            }
        }
        if (y <= row) y = row + 1;
    }

    private char NodeGlyph(ThoughtNode node) => node.Kind switch
    {
        ThoughtNodeKind.Element => Frame.ElementGlyph(node),
        ThoughtNodeKind.Thought => Config.Symbols.MapThought,
        ThoughtNodeKind.Act     => node.Action?.DifficultyGlyph ?? '>',
        ThoughtNodeKind.Success => Config.Symbols.MapSuccess,
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
        ThoughtNodeKind.Failure or ThoughtNodeKind.Refusal
                                => live ? Config.Colors.Purple : Config.Colors.DarkPurple,
        ThoughtNodeKind.Emotion => Config.Colors.Ochre,
        _                       => live ? Config.Colors.LightGray75 : DeadInk,
    };

    // ── Cells ────────────────────────────────────────────────────────────────

    private void Symbol(LocationMap.MapCanvas canvas, int x, int y, char glyph, Vector4 ink, MapSymbol symbol, Vector4? ground)
    {
        if (Equals(symbol, Frame.Hovered)) canvas.Put(x, y, glyph, HoverInk, HoverGround);
        else canvas.Put(x, y, glyph, ink, ground);
        canvas.Hit(x, y, symbol);
    }

    private static void Line(LocationMap.MapCanvas canvas, int x, int y, char glyph)
        => canvas.Put(x, y, glyph, LineInk, Backdrop);
}
