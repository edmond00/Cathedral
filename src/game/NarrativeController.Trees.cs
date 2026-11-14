using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Debug;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Tree;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.GetUp;

namespace Cathedral.Game;

// The thought trees: where each observed element is now, what the map shows of an area, and how the
// trees are kept coherent with a scene that has changed.
public partial class NarrativeController
{
    /// <summary>
    /// Whether the party knows <paramref name="area"/> well enough to see what stands in it: it has
    /// been stood in. Outside a real place every area is.
    /// </summary>
    private bool IsAreaKnown(Area area) => !Exploring || _scene.VisitedAreas.Contains(area.StableKey);

    /// <summary>
    /// Where the element of <paramref name="element"/> is now, and the observation object to think
    /// about it and act on it through — or null while it is out of sight: a person away at this hour,
    /// a bed somebody sleeps in, a thing taken or destroyed.
    ///
    /// <para>Read from the scene every time rather than kept on the node, because the scene makes its
    /// observation objects afresh as the world changes (<see cref="SyncSpawnedObservations"/>) and
    /// moves a person's from node to node with the hour. The tree is rooted on what does not change:
    /// the element.</para>
    /// </summary>
    private (NarrativeAnchor Anchor, Area Where)? Locate(ThoughtNode element)
    {
        switch (element.Element)
        {
            case SceneNpc npc:
                foreach (var node in _graph.AllNodes.Values.OfType<SyntheticNarrationNode>())
                    foreach (var o in node.PossibleOutcomes.OfType<SyntheticNpcObservationObject>())
                        if (ReferenceEquals(o.SceneNpc, npc)) return (o, node.Area!);
                return null;

            // What a container holds is found through the container.
            case ItemElement item:
                if (element.Parent is not { Kind: ThoughtNodeKind.Element } holder || Locate(holder) is not { } held
                    || held.Anchor is not SyntheticObservationObject container) return null;
                var part = container.Parts.OfType<SyntheticItemObservationObject>()
                                    .FirstOrDefault(p => ReferenceEquals(p.Item, item));
                return part == null ? null : (part, held.Where);

            // A fragment of a memory already remembered is of a scene that is gone: its area is not
            // in this one.
            case PointOfInterest poi when element.SeenFrom is { } from && _scene.AllAreas.Contains(from):
                var thing = NodeForArea(from).PossibleOutcomes.OfType<SyntheticObservationObject>()
                                .FirstOrDefault(o => ReferenceEquals(o.PointOfInterest, poi));
                return thing == null ? null : (thing, from);

            case Area area when element.SeenFrom is { } from && _scene.AllAreas.Contains(from):
                var way = NodeForArea(from).PossibleOutcomes.OfType<SyntheticAreaObservationObject>()
                              .FirstOrDefault(o => ReferenceEquals(o.Area, area));
                return way == null ? null : (way, from);
        }
        return null;
    }

    /// <summary>
    /// What stands in <paramref name="area"/> that the played member has not observed: each with its
    /// kind, and whether it is remembered — named, and a click's to choose — or anonymous.
    /// </summary>
    private IEnumerable<(NarrativeAnchor Anchor, ElementKind Kind, bool Known)> Unobserved(Area area)
    {
        foreach (var anchor in NodeForArea(area).PossibleOutcomes.OfType<ObservationObject>())
        {
            if (ElementKinds.ElementOf(anchor) is not { } element) continue;
            if (Mind.NodeOf(element, area) != null) continue;
            // What "myself" holds — a memory's fragments, the body's exhaustion — is looked at from
            // there (ContentsOf), and "myself" is on the map from the start.
            if (element is SelfPointOfInterest || SelfPointOfInterest.Holds(element)) continue;
            // The plain way to ground already stood on is walked on the map and offers nothing to
            // look at: its line is drawn, and a click on the card goes there.
            if (anchor is SyntheticAreaObservationObject { Area: var to } && AreaVisits.IsWayBack(_scene, area, to, Period)) continue;
            yield return (anchor, ElementKinds.Of(anchor, _active), ElementMemory.IsKnown(_scene, area, anchor));
        }
    }

    /// <summary>What an observed container holds that the played member has not observed in its turn.</summary>
    private IReadOnlyList<NarrativeAnchor> ContentsOf(ThoughtNode element)
    {
        if (Locate(element) is not { Anchor: SyntheticObservationObject container } held) return Array.Empty<NarrativeAnchor>();
        // "Myself" holds what its phase is about, as a chest holds what is in it.
        if (element.Element is SelfPointOfInterest)
            return NodeForArea(held.Where).PossibleOutcomes.OfType<SyntheticObservationObject>()
                .Where(o => SelfPointOfInterest.Holds(o.PointOfInterest) && Mind.NodeOf(o.PointOfInterest, held.Where) == null)
                .ToList<NarrativeAnchor>();
        return container.Parts
            .Where(p => p is SyntheticItemObservationObject item
                     && !element.Children.Any(c => c.Kind == ThoughtNodeKind.Element && ReferenceEquals(c.Element, item.Item)))
            .ToList();
    }

    /// <summary>The acts <paramref name="anchor"/> affords as the world now stands.</summary>
    private static IEnumerable<VerbAction> GoalsOf(NarrativeAnchor anchor)
        => anchor is ObservationObject obs ? obs.SubOutcomes.OfType<VerbAction>() : Enumerable.Empty<VerbAction>();

    // ── Building a tree ──────────────────────────────────────────────────────

    /// <summary>
    /// The element each member's trees took in last. A script on the real model cannot know which
    /// element a mind will choose to look at, and names it "last" (<see cref="CliFindSymbol"/>).
    /// </summary>
    private readonly Dictionary<PartyMember, ThoughtNode> _lastRooted = new();

    /// <summary>
    /// Roots a tree on <paramref name="anchor"/> for <paramref name="mind"/>: under the container that
    /// holds it, for what a container holds; else among the member's roots. <paramref name="text"/> is
    /// the observation (null for what nobody observed, with <paramref name="note"/> said in its place).
    /// </summary>
    private ThoughtNode Root(MemberMind mind, NarrativeAnchor anchor, Area seenFrom, NarrationBlock? text,
                             ThoughtNode? container = null, string? note = null)
    {
        var element = ElementKinds.ElementOf(anchor)
                      ?? throw new InvalidOperationException($"'{anchor.DisplayName}' is no element of the scene");
        var node = new ThoughtNode(ThoughtNodeKind.Element)
        {
            Element  = element,
            // A person is the same person wherever they stand.
            SeenFrom = element is SceneNpc ? null : seenFrom,
            Block    = text,
            Note     = note,
        };
        if (container != null) container.Add(node);
        else mind.Roots.Add(node);
        _lastRooted[mind.Member] = node;
        return node;
    }

    // ── Keeping the trees true ───────────────────────────────────────────────

    /// <summary>
    /// Brings every tree into line with the scene as it now stands. Called after anything that may
    /// have changed it: an outcome, a change of hour, a fight, a conversation, a change of the member
    /// played.
    ///
    /// <para>The scene is the truth and the trees are an account of it, so nothing here decides
    /// anything about the world: a person killed loses their tree, a body made in front of a member
    /// is on their map already observed, somebody who came at a noise is noticed without being looked
    /// for, and an act that the world no longer allows is no longer offered. A tree whose element is
    /// only out of sight is kept and not drawn (<see cref="Locate"/>): a person walks back in with
    /// everything thought about them.</para>
    /// </summary>
    private void ReconcileTrees()
    {
        SyncRoster();
        // Where everybody is now: the hour moves people, and so does a noise that draws one in, a
        // householder coming to bed, a death, a beast tamed.
        _npcPlacement.PlaceForPeriod(Period);
        // What the world now allows is planned afresh: a will's path was found in the scene as it was.
        ActionPathPlanner.Forget();
        RefreshSceneVerbs();

        foreach (var mind in _minds.Values)
            foreach (var dead in mind.Roots.Where(r => r.Element is SceneNpc { IsAlive: false }).ToList())
            {
                foreach (var act in dead.SelfAndDescendants().Where(n => n.IsWaitingAct))
                    mind.Proposals.Withdraw(act.Action!.PreselectedOutcome);
                mind.Roots.Remove(dead);
            }

        // Acts are checked for the member played only: every verb list was just expanded for them.
        foreach (var act in Mind.WaitingActs.ToList())
        {
            if (Locate(act.ElementNode) is not { } at) continue;
            var goal = act.Action!.PreselectedOutcome;
            var identity = ActionProposalLedger.IdentityOf(goal);
            if (GoalsOf(at.Anchor).Any(g => ActionProposalLedger.IdentityOf(g).Equals(identity))) continue;
            act.State = ActState.Lapsed;
            Mind.Proposals.Withdraw(goal);
            Console.WriteLine($"NarrativeController: '{act.Action.ActionText}' is no longer possible — withdrawn");
        }

        AdoptCorpses();
        AdoptArrivals();
        SceneDebugManager.UpdatePoV(_pov);
    }

    /// <summary>
    /// The bodies made since the trees were last reconciled are on the map already observed, for
    /// every member standing where they fell: nobody has to look for what they have just killed. No
    /// mind observed them, so none is counted in a roll made on them.
    /// </summary>
    private void AdoptCorpses()
    {
        var corpses = _scene.PendingCorpseObservations.ToList();
        _scene.PendingCorpseObservations.Clear();
        foreach (var corpse in corpses)
        {
            if (_scene.AllAreas.FirstOrDefault(a => a.PointsOfInterest.Contains(corpse)) is not { } area) continue;
            var anchor = NodeForArea(area).PossibleOutcomes.OfType<SyntheticObservationObject>()
                             .FirstOrDefault(o => ReferenceEquals(o.PointOfInterest, corpse));
            if (anchor == null) continue;
            foreach (var mind in _minds.Values.Where(m => m.Pov.Where.Id == area.Id))
                if (mind.NodeOf(corpse, area) == null)
                    Root(mind, anchor, area, text: null, note: $"{NeutralNarration.NounPhrase(corpse.DisplayName.ToLowerInvariant())}, where it fell");
        }
    }

    /// <summary>
    /// Somebody who heard an act go wrong and has walked in is noticed by every member standing
    /// there, as a body is: they are the one thing in the room that will not wait to be looked at.
    /// </summary>
    private void AdoptArrivals()
    {
        var arrivals = _scene.PendingArrivalObservations.ToList();
        _scene.PendingArrivalObservations.Clear();
        foreach (var arrival in arrivals.Where(a => a.IsAlive))
        {
            if (_scene.GetAreaOf(arrival, Period) is not { } area) continue;
            var anchor = NodeForArea(area).PossibleOutcomes.OfType<SyntheticNpcObservationObject>()
                             .FirstOrDefault(o => ReferenceEquals(o.SceneNpc, arrival));
            if (anchor == null) continue;
            foreach (var mind in _minds.Values.Where(m => m.Pov.Where.Id == area.Id))
            {
                if (mind.NodeOf(arrival, null) != null) continue;
                bool enemy = ElementKinds.IsHostile(arrival, mind.Member);
                Root(mind, anchor, area, text: null,
                     note: enemy ? NeutralNarration.ArrivalEnemyCaution() : NeutralNarration.ArrivalWitnessNotice());
            }
        }
    }
}
