using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Tree;

/// <summary>
/// What one party member carries through a visit: where they stand, the noetic points they have
/// left, and their thought trees. Everything here is private to the member — a companion's trees are
/// their own, their pool is their own, and what one mind of theirs declined binds nobody else — and
/// none of it outlasts the visit.
/// </summary>
public sealed class MemberMind
{
    public PartyMember Member { get; }

    /// <summary>
    /// Where this member stands. The hour is the whole party's: the controller writes the same
    /// <see cref="PoV.When"/> to every member's point of view.
    /// </summary>
    public PoV Pov { get; }

    /// <summary>Noetic points left. Refilled when an act of this member's succeeds (<see cref="Refill"/>).</summary>
    public int Noetic { get; set; }

    /// <summary>The elements this member has observed this visit, in the order they were.</summary>
    public List<ThoughtNode> Roots { get; } = new();

    /// <summary>
    /// Refusals that hang on no element: a mind that looked over a kind of thing in an area and
    /// found nothing worth attending to. Kept with the area they were made in.
    /// </summary>
    public List<(Area Area, ThoughtNode Node)> Strays { get; } = new();

    /// <summary>What this member's acts waiting on the trees already cover, and what its minds refused.</summary>
    public ActionProposalLedger Proposals { get; } = new();

    // A mind declines a thing once: asked again about it, it must attend (the player insisting).
    private readonly HashSet<(ModusMentis Mind, object About)> _declined = new();

    public MemberMind(PartyMember member, PoV pov)
    {
        Member = member;
        Pov    = pov;
        Noetic = member.MaxNoeticPoints;
    }

    /// <summary>Whether <paramref name="mind"/> has already declined <paramref name="about"/> (an element, or a kind in an area).</summary>
    public bool HasDeclined(ModusMentis mind, object about) => _declined.Contains((mind, about));

    public void Decline(ModusMentis mind, object about) => _declined.Add((mind, about));

    /// <summary>
    /// The pool back to its full, and with it everything a mind refused or declined: the member
    /// starts over with every mind willing to be asked. Acts still waiting on the trees stay proposed.
    /// </summary>
    public void Refill()
    {
        Noetic = Member.MaxNoeticPoints;
        _declined.Clear();
        Proposals.ForgetRefusals();
    }

    /// <summary>Every element node of this member's trees, containers before their contents.</summary>
    public IEnumerable<ThoughtNode> ElementNodes
        => Roots.SelectMany(r => r.SelfAndDescendants()).Where(n => n.Kind == ThoughtNodeKind.Element);

    /// <summary>Every node of this member's trees and stray refusals.</summary>
    public IEnumerable<ThoughtNode> AllNodes
        => Roots.SelectMany(r => r.SelfAndDescendants()).Concat(Strays.Select(s => s.Node));

    /// <summary>
    /// The node of <paramref name="element"/> as observed from <paramref name="seenFrom"/>, or null
    /// when this member has not observed it. A person is one node wherever they stand.
    /// </summary>
    public ThoughtNode? NodeOf(Element element, Area? seenFrom)
        => ElementNodes.FirstOrDefault(n => ReferenceEquals(n.Element, element)
                                         && (n.SeenFrom == null || seenFrom == null || n.SeenFrom.Id == seenFrom.Id));

    /// <summary>The acts of this member still waiting to be carried out.</summary>
    public IEnumerable<ThoughtNode> WaitingActs => AllNodes.Where(n => n.IsWaitingAct);
}
