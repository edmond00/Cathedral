using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Tree;

/// <summary>What a symbol of a thought tree stands for.</summary>
public enum ThoughtNodeKind
{
    /// <summary>An observed element: the root of a tree, or a container's content observed in its turn.</summary>
    Element,
    /// <summary>A thinking mind's reasoning (or an acting mind's, over an implement).</summary>
    Thought,
    /// <summary>A mind that would not: to observe, to carry an act out, a coded rule, a refused implement.</summary>
    Refusal,
    /// <summary>An act proposed, waiting to be carried out or carried out already.</summary>
    Act,
    /// <summary>The account of an act that succeeded.</summary>
    Success,
    /// <summary>The account of an act that failed.</summary>
    Failure,
    /// <summary>What the outcome stirred in the one who caused it.</summary>
    Emotion,
    /// <summary>What was said of the element to a companion.</summary>
    Speech,
}

/// <summary>Where an act node stands.</summary>
public enum ActState
{
    /// <summary>Proposed and not yet carried out: the one state a click executes.</summary>
    Waiting,
    /// <summary>Carried out, whatever came of it.</summary>
    Done,
    /// <summary>Reconsidered: another act, by another means, took its place.</summary>
    Superseded,
    /// <summary>No longer possible as the world now stands.</summary>
    Lapsed,
}

/// <summary>
/// One symbol of a thought tree: an observed element at the root, and under it every text a mind
/// wrote about it and every act those texts came to. The tree is the modus mentis chain made
/// visible — observation, thinking, action — and each node keeps the <see cref="NarrationBlock"/> or
/// <see cref="ParsedNarrativeAction"/> the dice read (<see cref="ModusMentisChainElement"/>), so the
/// roll needs nothing the tree does not already hold.
///
/// <para>A tree belongs to one party member and lasts one visit. It is rooted on the scene
/// <see cref="Element"/>, not on the observation object, which the scene may make afresh.</para>
/// </summary>
public sealed class ThoughtNode
{
    public ThoughtNodeKind Kind { get; }

    /// <summary>The text of this node and the mind that wrote it. Null for an element nobody observed (a corpse just made).</summary>
    public NarrationBlock? Block { get; init; }

    /// <summary>The act, for an <see cref="ThoughtNodeKind.Act"/> node.</summary>
    public ParsedNarrativeAction? Action { get; init; }

    /// <summary>The element observed, for an <see cref="ThoughtNodeKind.Element"/> node.</summary>
    public Element? Element { get; init; }

    /// <summary>
    /// The area an element was observed from, when that tells one observation from another: a door
    /// is one thing seen from the street and another from the hall, and a neighbouring area is a way
    /// only from somewhere. Null for what is the same from anywhere (a person, who moves besides).
    /// </summary>
    public Area? SeenFrom { get; init; }

    /// <summary>
    /// What is said of a node no mind wrote: a body just made, somebody who came at a noise. Read
    /// where the text of a mind would be.
    /// </summary>
    public string? Note { get; init; }

    public ActState State { get; set; } = ActState.Waiting;

    public ThoughtNode? Parent { get; private set; }

    private readonly List<ThoughtNode> _children = new();
    public IReadOnlyList<ThoughtNode> Children => _children;

    public ThoughtNode(ThoughtNodeKind kind) => Kind = kind;

    /// <summary>Hangs <paramref name="child"/> under this node and returns it.</summary>
    public ThoughtNode Add(ThoughtNode child)
    {
        child.Parent = this;
        _children.Add(child);
        return child;
    }

    public void Remove(ThoughtNode child)
    {
        if (_children.Remove(child)) child.Parent = null;
    }

    /// <summary>The mind that wrote this node, when one did.</summary>
    public ModusMentis? Mind => Kind == ThoughtNodeKind.Act ? Action?.ChainModusMentis : Block?.ModusMentis;

    /// <summary>The element node this node hangs from: itself, for one.</summary>
    public ThoughtNode ElementNode
    {
        get
        {
            var n = this;
            while (n.Kind != ThoughtNodeKind.Element && n.Parent != null) n = n.Parent;
            return n;
        }
    }

    /// <summary>Whether this is an act a click may still carry out.</summary>
    public bool IsWaitingAct => Kind == ThoughtNodeKind.Act && State == ActState.Waiting;

    /// <summary>This node and everything under it, parents first.</summary>
    public IEnumerable<ThoughtNode> SelfAndDescendants()
    {
        yield return this;
        foreach (var child in _children)
            foreach (var n in child.SelfAndDescendants())
                yield return n;
    }

    /// <summary>
    /// Whether anything under this node is still to be done: an act waiting. A branch with nothing
    /// left is drawn dimmed, and stays as the record of what was thought.
    /// </summary>
    public bool IsLive => SelfAndDescendants().Any(n => n.IsWaitingAct);
}
