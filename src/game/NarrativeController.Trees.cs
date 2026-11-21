using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Debug;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Tree;
using Cathedral.Game.Npc.Corpse;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.GetUp;

namespace Cathedral.Game;

// The thought trees: where each observed element is now, what the map shows of an area, and how the
// trees are kept coherent with a scene that has changed.
public partial class NarrativeController
{
    /// <summary>
    /// Whether the party has stood in <paramref name="area"/>, which is what a click on its card
    /// walks back to. Outside a real place every area has been.
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
        // What holds the memory at hand is where the remembering is done, whatever scene its own
        // element was of: the fragment last remembered belongs to a memory that has closed.
        if (ReferenceEquals(element, MemoryHolder))
            return NodeForArea(_pov.Where).PossibleOutcomes.OfType<SyntheticObservationObject>()
                       .FirstOrDefault(o => o.PointOfInterest is SelfPointOfInterest) is { } self
                ? (self, _pov.Where) : null;

        // A way on a line is one tree whichever end it was first looked at from: it is seen from
        // the far end while the member stands there, and from the end it was observed at otherwise.
        if (EndsOf(element) is { } ends)
        {
            if (_pov.Where.Id == ends.Far.Id && WayAnchor(element, ends.Far) is { } across) return (across, ends.Far);
            return WayAnchor(element, ends.Near) is { } way ? (way, ends.Near) : null;
        }

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
                var neighbour = NodeForArea(from).PossibleOutcomes.OfType<SyntheticAreaObservationObject>()
                                    .FirstOrDefault(o => ReferenceEquals(o.Area, area));
                return neighbour == null ? null : (neighbour, from);
        }
        return null;
    }

    /// <summary>
    /// Where a waiting act is carried out, and through which anchor. An act on a way was thought of
    /// from one of its ends and is good from that end only — "go to the yard" means nothing in the
    /// yard — so it is looked for at the end the tree is now seen from and then at the other, and
    /// the member is walked to whichever still affords it.
    /// </summary>
    private (NarrativeAnchor Anchor, Area Where)? LocateAct(ThoughtNode act)
    {
        var root = act.ElementNode;
        if (Locate(root) is not { } at) return null;
        if (EndsOf(root) is not { } ends || act.Action?.PreselectedOutcome is not { } goal) return at;
        var identity = ActionProposalLedger.IdentityOf(goal);
        bool Affords(NarrativeAnchor anchor) => GoalsOf(anchor).Any(g => ActionProposalLedger.IdentityOf(g).Equals(identity));
        if (Affords(at.Anchor)) return at;
        var other = at.Where.Id == ends.Near.Id ? ends.Far : ends.Near;
        return WayAnchor(root, other) is { } there && Affords(there) ? (there, other) : at;
    }

    // ── Ways ─────────────────────────────────────────────────────────────────
    //
    // A way the map draws a line for — the plain way between two neighbours, a door, a stair, a
    // path, a climb — has its symbol on that line, not on a card: one line, one symbol, one tree.
    // A view, and a way whose line could not be drawn, stay on the card they are seen from.

    /// <summary>The key of the line <paramref name="anchor"/>, seen from <paramref name="from"/>, is drawn as; null for anything not drawn as one.</summary>
    private string? WayKeyOf(Area from, NarrativeAnchor anchor) => anchor switch
    {
        SyntheticAreaObservationObject { Area: var to } => Drawn(AreaVisits.LinkKey(from, to)),
        SyntheticObservationObject { PointOfInterest: ConnectorPointOfInterest c } => WayKeyOf(c),
        _ => null,
    };

    private string? WayKeyOf(ConnectorPointOfInterest connector)
        => connector is LandscapePointOfInterest || connector.AreaA.Id == connector.AreaB.Id ? null : Drawn(connector.StableKey);

    /// <summary>The key of the line an observed element is drawn on, or null when it hangs under a card.</summary>
    private string? WayKeyOf(ThoughtNode element) => element.Element switch
    {
        Area far when element.SeenFrom is { } near => Drawn(AreaVisits.LinkKey(near, far)),
        ConnectorPointOfInterest c when element.SeenFrom != null => WayKeyOf(c),
        _ => null,
    };

    private string? Drawn(string key) => _map.DrawsWay(_scene, key) ? key : null;

    /// <summary>The two ends of an observed way on a line: the one it was observed from, and the other.</summary>
    private (Area Near, Area Far)? EndsOf(ThoughtNode element)
    {
        if (element.Kind != ThoughtNodeKind.Element || element.SeenFrom is not { } near || WayKeyOf(element) == null) return null;
        return element.Element switch
        {
            Area far => (near, far),
            ConnectorPointOfInterest c when c.Touches(near) => (near, c.Other(near)),
            _ => null,
        };
    }

    /// <summary>The observation object of the way of <paramref name="element"/> as seen from <paramref name="from"/>, one of its ends.</summary>
    private NarrativeAnchor? WayAnchor(ThoughtNode element, Area from)
    {
        if (!_scene.AllAreas.Contains(from)) return null;
        return element.Element switch
        {
            ConnectorPointOfInterest c => WayAnchor(from, c.Other(from), c),
            Area far => WayAnchor(from, from.Id == far.Id ? element.SeenFrom! : far, null),
            _ => null,
        };
    }

    /// <summary>The observation object of a way seen from <paramref name="from"/>: the connector, or the neighbour <paramref name="to"/>.</summary>
    private NarrativeAnchor? WayAnchor(Area from, Area to, ConnectorPointOfInterest? connector)
        => connector != null
            ? NodeForArea(from).PossibleOutcomes.OfType<SyntheticObservationObject>()
                  .FirstOrDefault(o => ReferenceEquals(o.PointOfInterest, connector))
            : NodeForArea(from).PossibleOutcomes.OfType<SyntheticAreaObservationObject>()
                  .FirstOrDefault(o => ReferenceEquals(o.Area, to));

    /// <summary><paramref name="mind"/>'s tree of the way recorded as <paramref name="key"/>, when it has observed it.</summary>
    private ThoughtNode? WayRoot(MemberMind mind, string key) => mind.Roots.FirstOrDefault(r => WayKeyOf(r) == key);

    /// <summary>
    /// The end an unobserved way is looked at from, and the observation object there: where the
    /// member stands when that is an end, else the end fewest known steps away, else either. Null
    /// when neither end shows it — or it is the plain way already gone by, which is walked on the
    /// map and offers nothing to look at.
    /// </summary>
    private (Area From, NarrativeAnchor Anchor)? WaySide(Area a, Area b, ConnectorPointOfInterest? connector)
    {
        (Area From, NarrativeAnchor Anchor)? best = null;
        int bestSteps = int.MaxValue;
        foreach (var (from, to) in new[] { (a, b), (b, a) })
        {
            if (WayAnchor(from, to, connector) is not { } anchor) continue;
            if (connector == null && AreaVisits.IsWayBack(_scene, from, to, Period)) continue;
            int steps = _frameReach.TryGetValue(from.Id, out int n) ? n : int.MaxValue - 1;
            if (steps >= bestSteps) continue;
            best = (from, anchor);
            bestSteps = steps;
        }
        return best;
    }

    /// <summary>
    /// What stands in <paramref name="area"/> that the played member has not observed and that is
    /// shown on its card: each with its kind, and whether it is remembered — named, and a click's to
    /// choose — or anonymous. The ways drawn as lines are not among them (<see cref="WaySide"/>).
    /// </summary>
    private IEnumerable<(NarrativeAnchor Anchor, ElementKind Kind, bool Known)> Unobserved(Area area)
    {
        foreach (var anchor in NodeForArea(area).PossibleOutcomes.OfType<ObservationObject>())
        {
            if (ElementKinds.ElementOf(anchor) is not { } element) continue;
            // A way drawn as a line has its symbol there.
            if (WayKeyOf(area, anchor) != null) continue;
            if (Mind.NodeOf(element, area) != null) continue;
            // What "myself" holds — a memory's fragments, the body's exhaustion — is looked at from
            // there (ContentsOf).
            if (SelfPointOfInterest.Holds(element)) continue;
            // "Myself" is looked at once in a childhood: every memory's scene has one, and the one
            // on the tree is all of them.
            if (element is SelfPointOfInterest && _scene.Phase == NarrationPhase.ChildhoodReminescence
                && Mind.Roots.Any(r => r.Element is SelfPointOfInterest)) continue;
            // The plain way to ground already stood on is walked on the map and offers nothing to
            // look at: its line is drawn, and a click on the card goes there.
            if (anchor is SyntheticAreaObservationObject { Area: var to } && AreaVisits.IsWayBack(_scene, area, to, Period)) continue;
            yield return (anchor, ElementKinds.Of(anchor, _active), ElementMemory.IsKnown(_scene, area, anchor, _active));
        }
    }

    /// <summary>What an observed container holds that the played member has not observed in its turn.</summary>
    private IReadOnlyList<NarrativeAnchor> ContentsOf(ThoughtNode element)
    {
        if (Locate(element) is not { Anchor: SyntheticObservationObject container } held) return Array.Empty<NarrativeAnchor>();
        // "Myself" holds what its phase is about, as a chest holds what is in it — and in the
        // childhood, whatever now holds the memory at hand does. An older holder holds nothing more.
        if (_scene.Phase == NarrationPhase.ChildhoodReminescence && !ReferenceEquals(element, MemoryHolder))
            return Array.Empty<NarrativeAnchor>();
        if (element.Element is SelfPointOfInterest || ReferenceEquals(element, MemoryHolder))
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

        // A will's refusal was its course through the place as it stood: it may be asked again.
        foreach (var mind in _minds.Values) mind.ForgetWillRefusals();

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
            if (LocateAct(act) is not { } at) continue;
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

    /// <summary>
    /// Puts <paramref name="npc"/> on <paramref name="mind"/>'s trees when a conversation was held
    /// with them and nothing of theirs was there to hang it from: somebody who stopped a flight and
    /// spoke before they were ever looked at. Having spoken with them is having seen them.
    /// </summary>
    private ThoughtNode? RootSpokenWith(MemberMind mind, Cathedral.Game.Npc.NpcEntity npc)
    {
        if (_scene.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, npc)) is not { } spoken) return null;
        if (mind.NodeOf(spoken, null) is { } known) return known;
        var anchor = NodeForArea(mind.Pov.Where).PossibleOutcomes.OfType<SyntheticNpcObservationObject>()
                         .FirstOrDefault(o => ReferenceEquals(o.SceneNpc, spoken));
        return anchor == null ? null
             : Root(mind, anchor, mind.Pov.Where, text: null, note: NeutralNarration.SpokenWith());
    }
}
