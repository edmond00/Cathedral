using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Tree;

/// <summary>What a mind is asked to do from the popup: the offices a refusal is remembered by.</summary>
public enum MindOffice { Observe, Think, Reconsider, Speak }

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
    /// The areas whose whole tree the player has folded away: only the tree's root symbol is drawn
    /// under those cards. The screen's and not the game's, like an element's own
    /// <see cref="ThoughtNode.Folded"/>: nothing under it is lost or changed.
    /// </summary>
    public HashSet<System.Guid> FoldedAreas { get; } = new();

    /// <summary>
    /// Refusals that hang on no element: a mind that looked over a kind of thing in an area and
    /// found nothing worth attending to. Kept with the area they were made in.
    /// </summary>
    public List<(Area Area, ThoughtNode Node)> Strays { get; } = new();

    /// <summary>
    /// The element a stray refusal was about, when it was about one: a mind asked to look at a
    /// thing already known by name, which would not. Once that element is observed the refusal
    /// hangs under it (see the controller's <c>Root</c>), so it folds with the tree it belongs to.
    /// </summary>
    public Dictionary<ThoughtNode, Element> StrayAbout { get; } = new();

    /// <summary>
    /// Refusals to look at a way, which hang from the way's symbol on its line: kept with the key
    /// the way is recorded under (<see cref="AreaVisits"/>).
    /// </summary>
    public List<(string Way, ThoughtNode Node)> WayStrays { get; } = new();

    /// <summary>What this member's acts waiting on the trees already cover, and what its minds refused.</summary>
    public ActionProposalLedger Proposals { get; } = new();

    // A thing is declined once: asked about it again, whichever mind is asked must attend (the
    // player insisting). The mind that declined is not asked again at all (_refused).
    private readonly HashSet<object> _declined = new();

    // The minds that refused a thing asked of them, by what was asked: they are greyed in the
    // popup the next time the same is asked about the same thing.
    private readonly HashSet<(ModusMentis Mind, MindOffice Office, object About)> _refused = new();

    public MemberMind(PartyMember member, PoV pov)
    {
        Member = member;
        Pov    = pov;
        Noetic = member.MaxNoeticPoints;
    }

    /// <summary>Whether a mind has already declined <paramref name="about"/> (an element, a way, or a kind in an area).</summary>
    public bool HasDeclined(object about) => _declined.Contains(about);

    public void Decline(object about) => _declined.Add(about);

    /// <summary>
    /// <paramref name="mind"/> refused <paramref name="office"/> about <paramref name="about"/>: a
    /// decline, a will with no way through it, a faculty the body can no longer carry. Asking the
    /// same mind the same thing again would spend a point to hear it twice.
    /// </summary>
    public void Refuse(ModusMentis mind, MindOffice office, object about) => _refused.Add((mind, office, about));

    public bool HasRefused(ModusMentis mind, MindOffice office, object about) => _refused.Contains((mind, office, about));

    /// <summary>
    /// The world changed: a will's refusal was its course through the place as it stood, and is asked
    /// again. Any other mind's stands until the pool refills.
    /// </summary>
    public void ForgetWillRefusals() => _refused.RemoveWhere(r => r.Mind.IsWill);

    /// <summary>
    /// The pool back to its full, and with it everything a mind refused or declined: the member
    /// starts over with every mind willing to be asked. Acts still waiting on the trees stay proposed.
    /// </summary>
    public void Refill()
    {
        Noetic = Member.MaxNoeticPoints;
        _declined.Clear();
        _refused.Clear();
        Proposals.ForgetRefusals();
    }

    /// <summary>Every element node of this member's trees, containers before their contents.</summary>
    public IEnumerable<ThoughtNode> ElementNodes
        => Roots.SelectMany(r => r.SelfAndDescendants()).Where(n => n.Kind == ThoughtNodeKind.Element);

    /// <summary>Every node of this member's trees and stray refusals.</summary>
    public IEnumerable<ThoughtNode> AllNodes
        => Roots.SelectMany(r => r.SelfAndDescendants()).Concat(Strays.Select(s => s.Node)).Concat(WayStrays.Select(s => s.Node));

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
