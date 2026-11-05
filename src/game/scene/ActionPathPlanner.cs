using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>
/// One action of a path: the action itself, as the scene offers it from that step's standing, and
/// what the path needs of a step whose end the player would otherwise choose — the hour a sit lasts
/// until, the area a backtrack goes to (<see cref="Verb.Settle"/>). Either may reach any of several;
/// the path is one of them.
/// </summary>
public sealed record PathStep(VerbAction Action, object? Settled = null)
{
    /// <summary>The action as it would be offered: settled on what the path needs of it, when it needs anything.</summary>
    public VerbAction SettledAction => Settled != null && Action.Variant == null && Action.Verb.Settle(Action, Settled) is { } settled
        ? settled
        : Action;

    public string Verbatim => SettledAction.Verbatim;
}

/// <summary>
/// The shortest path of actions from where a body stands to one its will wants. <see cref="Steps"/>
/// runs from the first action to take now to the wanted action itself, which is always last.
/// </summary>
/// <param name="Steps">At least one: the goal alone when it is offered right here.</param>
/// <param name="ToolStep">The index of the step that obtains the implement the goal needs, or -1 when none is needed.</param>
/// <param name="Target">Which of the will's targets the goal satisfies — the one whose purpose the path is narrated with.</param>
/// <param name="Unarmed">The way needs an implement the body does not hold: it is proposed all the same, and the tool is asked for on the click.</param>
public sealed record ActionPath(IReadOnlyList<PathStep> Steps, int ToolStep, WillTarget? Target = null, bool Unarmed = false)
{
    public PathStep First => Steps[0];
    public PathStep Goal  => Steps[^1];

    /// <summary>The steps between now and the goal, the first included: the way there.</summary>
    public IEnumerable<PathStep> Approach => Steps.Take(Steps.Count - 1);
}

/// <summary>
/// Finds, for a <see cref="WillTarget"/>, the shortest path of actions that reaches it from the body's
/// point of view — the whole mechanism behind the <see cref="ModusMentisFunction.Will"/> function.
///
/// <para><b>What is searched.</b> A breadth-first search over a deliberately small state: the area,
/// the period, and the implement the goal needs, if one was obtained on the way. Successors are read
/// from <see cref="Scene.View(PoV, PartyMember?, IReadOnlyList{Verb})"/> — the live verb gates, so the
/// path never contains an action the scene would not offer — and from the outcomes a success
/// <i>would</i> apply, which are read and never applied: <see cref="AreaMoveOutcome"/> and
/// <see cref="DoorUnlockOutcome"/> move the body, <see cref="TimeShiftOutcome"/> moves the hour. A sit
/// is every other hour of the day, since the sitter chooses. Nothing else a verb does is modelled, so
/// a path never passes through a fight or a roll of loot, and through only the two conversations whose
/// success is known in advance: the leave of a workshop, and a trader leading the way to their premises
/// (<see cref="AskToBeLedVerb"/>, <see cref="TradeGate.NextOnPremises"/>).</para>
///
/// <para><b>Implements.</b> A goal verb that requires a tool (<see cref="Verb.RequiresTool"/>) is
/// reached only holding one of its <see cref="Verb.ReferenceToolIds"/>, or something made for it. When
/// none is held the path goes by one: an action that yields it (taking it from where it lies,
/// gathering it) or a proposal to buy from somebody whose wares include it. The purchase itself is a
/// conversation, so the plan cannot see past it; it is the last step that matters, and the next
/// phase plans again from whatever the trade left in the pack.</para>
///
/// <para><b>An implement never refuses a will.</b> Fetching one is a course the plan prefers, never a
/// condition of having one. When none can be had here the way is planned bare-handed
/// (<see cref="ActionPath.Unarmed"/>), goal and tool-required steps alike, whatever the body carries; and
/// an act the will wants that is offered right here is taken as it stands, even while the plan means to
/// fetch something for it (<see cref="PathThrough"/>, <see cref="WaysThrough"/>). The implement is
/// settled on the click: <c>RequiredToolRule</c> asks for it, and the critic judges what is handed over.</para>
///
/// <para><b>Cost.</b> Measured over every location factory: a path is found in under a millisecond
/// on average and 16 ms at worst, and an exhausted search — the will has nothing to do in this place —
/// takes up to half a second in the largest city. A result is kept until <see cref="Forget"/>, which
/// the narration calls at the start of every phase, so a phase searches each will at most once per
/// standing.</para>
/// </summary>
public static class ActionPathPlanner
{
    /// <summary>
    /// The verbs that move the body or pass the hour, by type. A path is made of these and ends on a
    /// goal; the other verbs are expanded only when an implement must be found. A verb that leaves the
    /// location (<see cref="VoyageTowardVerb"/>) is not here: a path lies within the place.
    /// </summary>
    public static readonly IReadOnlyList<Type> TransitVerbTypes = new[]
    {
        typeof(MoveToAreaVerb), typeof(OpenDoorVerb), typeof(UnlockDoorVerb),
        typeof(GoUpStairsVerb), typeof(GoDownStairsVerb), typeof(ClimbUpVerb), typeof(ClimbDownVerb),
        typeof(ScaleUpVerb), typeof(ScaleDownVerb), typeof(CrossVerb), typeof(SwimAcrossVerb),
        typeof(SlipIntoVerb), typeof(FollowPathVerb), typeof(TrackVerb), typeof(StalkVerb),
        typeof(SitAndWaitVerb), typeof(HideAndWaitVerb), typeof(BacktrackVerb), typeof(AskToBeLedVerb),
    };

    private readonly record struct State(Guid Area, TimePeriod When, string? Tool, string? Leave = null);

    private static readonly Dictionary<(Scene, Guid, TimePeriod, string, string), ActionPath?> _cache = new();

    /// <summary>
    /// Each verb's actions at a standing, element by element in the view's order, kept as long as the
    /// plans are. Most of a search is the same transit verbs expanded at the same standings for every
    /// will and every replan through an object; scene state changes only where <see cref="Forget"/>
    /// runs, so an expansion is as good as the plan it served.
    /// </summary>
    private static readonly Dictionary<(Scene, Guid, TimePeriod, string?, PartyMember, Verb), IReadOnlyList<VerbAction>[]> _expanded = new();
    private static readonly Dictionary<(Scene, Guid, TimePeriod), IReadOnlyList<Element>> _elements = new();

    /// <summary>Where each kept transit action leaves the body — the same action object, so read once.</summary>
    private static readonly Dictionary<VerbAction, List<(Area Area, TimePeriod When)>> _destinations = new(ReferenceEqualityComparer.Instance);

    /// <summary>Drops every remembered path. Called when a narration phase begins, and whenever the scene changes under a plan.</summary>
    public static void Forget()
    {
        _cache.Clear();
        _expanded.Clear();
        _elements.Clear();
        _destinations.Clear();
    }

    /// <summary>
    /// The actions <paramref name="verbs"/> offer from <paramref name="here"/>, in exactly the order
    /// <see cref="Scene.View(PoV, PartyMember?, IReadOnlyList{Verb})"/> would list them — element by
    /// element, and within one element verb by verb — so the first goal met is the one it always was.
    /// </summary>
    private static IEnumerable<VerbAction> Offered(Scene scene, PoV here, PartyMember actor, IReadOnlyList<Verb> verbs)
    {
        var at = (scene, here.Where.Id, here.When);
        if (!_elements.TryGetValue(at, out var elements)) _elements[at] = elements = scene.ViewElements(here);
        var perVerb = new IReadOnlyList<VerbAction>[verbs.Count][];
        for (int v = 0; v < verbs.Count; v++)
        {
            var key = (scene, here.Where.Id, here.When, here.PlannedLeave, actor, verbs[v]);
            if (!_expanded.TryGetValue(key, out var lists))
            {
                lists = new IReadOnlyList<VerbAction>[elements.Count];
                for (int e = 0; e < elements.Count; e++)
                    lists[e] = verbs[v].ExpandViews(scene, here, elements[e], actor).ToList();
                _expanded[key] = lists;
            }
            perVerb[v] = lists;
        }
        for (int e = 0; e < elements.Count; e++)
            for (int v = 0; v < verbs.Count; v++)
                foreach (var action in perVerb[v][e]) yield return action;
    }

    /// <summary>
    /// Whether <paramref name="action"/> is an end <paramref name="will"/> would accept from this body:
    /// what it wants, and, for a proposal to sell, a trader who buys something the body carries.
    /// </summary>
    public static bool IsGoal(WillTargets will, VerbAction action, PartyMember actor)
        => will.Matches(action)
           && (action.Verb is not ProposeToSellVerb
               || action.Target is SceneNpc { Entity: NpcEntity npc } && TradeGate.BuysSomethingOf(npc, actor));

    /// <summary>
    /// The shortest path from <paramref name="pov"/> to an action <paramref name="will"/> matches, or
    /// null when there is none within this place — no such action here at all, or none the body can
    /// reach (or equip itself for) without leaving. <paramref name="obtained"/> is an implement taken
    /// on the way and counted as held, for the rest of a path that begins by obtaining it.
    /// </summary>
    public static ActionPath? Plan(Scene scene, PoV pov, PartyMember actor, WillTargets will, string? obtained = null)
    {
        var held = actor.GetAllItems().ToList();
        var key  = (scene, pov.Where.Id, pov.When, will.Key,
                    string.Join(",", held.Select(i => i.ItemId).OrderBy(i => i)) + "+" + obtained + "+" + pov.PlannedLeave);
        if (_cache.TryGetValue(key, out var cached)) return cached;

        var path = Search(scene, pov, actor, will, held, obtained, unarmed: false);
        path ??= Search(scene, pov, actor, will, held, obtained, unarmed: true);
        _cache[key] = path;
        Console.WriteLine(path == null
            ? $"ActionPathPlanner: no way to '{will.Purpose}' from {pov.Where.DisplayName} at {pov.When} — not in this place."
            : $"ActionPathPlanner: '{path.Target?.Purpose ?? will.Purpose}' in {path.Steps.Count} step(s): {string.Join(" | ", path.Steps.Select(s => s.Verbatim))}"
              + (path.Unarmed ? " (no implement for it here: unarmed)" : ""));
        return path;
    }

    private static ActionPath? Search(Scene scene, PoV pov, PartyMember actor, WillTargets will, List<Item> held, string? obtained,
                                      bool unarmed)
    {
        var goalVerbs = scene.Verbs.Where(will.Wants).ToList();
        if (goalVerbs.Count == 0) return null;

        // Unarmed, every implement counts as in hand: nothing is fetched, and the click asks for the tool.
        bool Holds(Verb verb, string? acquired, VerbAction? view = null)
            => unarmed
               || verb.ReferenceToolIdsFor(view).Any(id => id == acquired || held.Any(i => i.ItemId == id))
               || held.Any(i => i.MadeForVerbIds.Contains(verb.VerbId));

        // The implements some goal verb needs and the body does not hold. Empty: no detour for one.
        var wanted = goalVerbs.Where(v => v.RequiresTool && !Holds(v, obtained))
                              .SelectMany(ToolIdsFor).ToHashSet();

        // A transit verb that itself needs an implement is walked only with one already in hand.
        var transit = scene.Verbs.Where(v => TransitVerbTypes.Any(t => t.IsInstanceOfType(v))
                                          && (!v.RequiresTool || Holds(v, null)));
        // Asking a master for the use of a workshop is a step on the way too: it opens a conversation,
        // which the planner otherwise never passes, but its success is known in advance — the leave —
        // and that is all a crafting will needs of it. The supposed leave rides in the search state.
        var leaveSteps = scene.Verbs.OfType<AskToUseWorkshopVerb>();
        var narrow  = transit.Concat(leaveSteps).Concat(goalVerbs).Distinct().ToList();
        var areas   = scene.AllAreas.ToDictionary(a => a.Id);

        var start = new State(pov.Where.Id, pov.When, obtained, pov.PlannedLeave);
        var prev  = new Dictionary<State, (State From, VerbAction? Step)> { [start] = (start, null) };
        var depth = new Dictionary<State, int> { [start] = 0 };
        var queue = new Queue<State>();
        queue.Enqueue(start);

        // The nearest goal, and among goals equally near the one whose target is declared first. The
        // search does not stop at the first goal it meets: it finishes that depth, since a target
        // declared earlier may be met later at the same distance. With one target the first goal met
        // is kept, exactly as before.
        (int Index, State At, VerbAction Goal)? best = null;

        while (queue.Count > 0)
        {
            var s     = queue.Dequeue();
            if (best is { } b && depth[s] > depth[b.At]) break;
            var here  = new PoV(areas[s.Area], s.When) { PlannedLeave = s.Leave };
            // Every verb is read only while an implement is still to be found: it is the one search
            // in which a grab or a purchase is a step on the way.
            var verbs = wanted.Count > 0 && s.Tool == null ? scene.Verbs : narrow;

            bool intrusion = PrivacyModel.IsIntrusion(scene, here);
            foreach (var action in Offered(scene, here, actor, verbs))
            {
                // On a master's floor without leave a will asks first: USE is offered there as a crime,
                // and neither it nor an implement lifted on the way is a way a will plans.
                if (intrusion && action.Verb is UseStationVerb) continue;

                if (IsGoal(will, action, actor) && (!action.Verb.RequiresTool || Holds(action.Verb, s.Tool, action)))
                {
                    int index = will.IndexOf(action);
                    if (best == null || index < best.Value.Index) best = (index, s, action);
                    continue;
                }

                if (wanted.Count > 0 && s.Tool == null && !(intrusion && action.Verb.Conduct == VerbConduct.Intrusive)
                    && Obtains(action, wanted) is { } tool)
                    Push(s with { Tool = tool }, action);

                if (action.Verb is AskToUseWorkshopVerb && action.Variant is string workshop && s.Leave == null)
                    Push(s with { Leave = workshop }, action);

                if (!TransitVerbTypes.Any(t => t.IsInstanceOfType(action.Verb))) continue;

                if (!_destinations.TryGetValue(action, out var ends))
                    _destinations[action] = ends = Destinations(scene, here, actor, action).ToList();
                foreach (var (area, when) in ends)
                    Push(s with { Area = area.Id, When = when }, action);
            }

            void Push(State next, VerbAction step)
            {
                if (prev.ContainsKey(next)) return;
                prev[next] = (s, step);
                depth[next] = depth[s] + 1;
                queue.Enqueue(next);
            }
        }
        return best is { } found
            ? Unwind(scene, prev, found.At, found.Goal, wanted.Count > 0) with { Target = will[found.Index], Unarmed = unarmed }
            : null;
    }

    /// <summary>
    /// Where a transit action would leave the body: the area and hour its success reports name, read
    /// and never applied. A sit is every other hour of the day, since the sitter chooses.
    /// </summary>
    private static IEnumerable<(Area Area, TimePeriod When)> Destinations(Scene scene, PoV here, PartyMember actor, VerbAction action)
    {
        if (action.Verb is BacktrackVerb)
        {
            // A backtrack already settled on an area goes there; otherwise anywhere known ways reach.
            if (action.Variant is Area settledTo) { yield return (settledTo, here.When); yield break; }
            foreach (var to in AreaVisits.BackReach(scene, here.Where, here.When).Keys)
                yield return (to, here.When);
            yield break;
        }

        if (action.Verb is SitAndWaitVerb)
        {
            // A wait already settled on an hour lasts until that one; otherwise the sitter chooses.
            if (action.Variant is TimePeriod settled) { yield return (here.Where, settled); yield break; }
            foreach (var period in Enum.GetValues<TimePeriod>().Where(p => p != here.When))
                yield return (here.Where, period);
            yield break;
        }

        // Being led opens a conversation, which reports no move: the walk follows the won conversation
        // (NarrativeController.WalkToPremises), to the place and hour this same question names.
        if (action.Verb is AskToBeLedVerb)
        {
            if (action.Target is SceneNpc trader && TradeGate.NextOnPremises(scene, trader, here.When) is { } visit)
                yield return visit;
            yield break;
        }

        IReadOnlyList<Outcome> reports;
        try { reports = action.Verb.SuccessReports(scene, here, actor, action.Target!, action); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ActionPathPlanner: {action.Verb.VerbId} could not be read ({ex.Message}) — skipped.");
            yield break;
        }
        // One success has one ending: a stalk passes the hour AND moves the body. Read apart they were two
        // ends, and one was a place the follower never stands: still where they began, at the later hour.
        Area? reached = null;
        TimePeriod? until = null;
        foreach (var report in reports)
        {
            switch (report)
            {
                case AreaMoveOutcome m:   reached = m.Destination; break;
                case DoorUnlockOutcome u: reached = u.Destination; break;
                case TimeShiftOutcome t:  until = t.Destination; break;
            }
        }
        if (reached != null || until != null)
            yield return (reached ?? here.Where, until ?? here.When);
    }

    /// <summary>
    /// The first of <paramref name="offered"/> that begins <b>a</b> shortest path to the will, and that
    /// path — not only the one <see cref="Plan"/> happened to keep. When several things are equally
    /// near (two people to strike, two bushes to pick), the search commits to whichever it reached
    /// first, and a will that accepted only that one would refuse the other, called "not on the way",
    /// though it is exactly as much what the will wants.
    ///
    /// <para>An offered action qualifies when it is the plan's own first step; when the goal is within
    /// reach right here and the action is itself one the will wants; when the plan's first step is
    /// obtaining an implement and the action obtains one too, in the same place; or when it is a move
    /// or a wait after which the goal is exactly as near as the plan says. Null when none does.</para>
    /// </summary>
    public static ActionPath? PathThrough(Scene scene, PoV pov, PartyMember actor, WillTargets will, ActionPath path,
                                          IEnumerable<VerbAction> offered)
    {
        var actions = offered.ToList();

        if (actions.FirstOrDefault(a => SameAction(a, path.First.Action)) is { } own)
            return path with { Steps = path.Steps.Skip(1).Prepend(new PathStep(own, path.First.Settled)).ToList() };

        var held = actor.GetAllItems().ToList();
        bool Holds(Verb verb, VerbAction? view = null)
            => verb.ReferenceToolIdsFor(view).Any(id => held.Any(i => i.ItemId == id))
               || held.Any(i => i.MadeForVerbIds.Contains(verb.VerbId));

        // A goal offered here is as near as a goal can be, whatever implement the plan means to fetch:
        // the implement is the click's to ask for (RequiredToolRule).
        if (actions.FirstOrDefault(a => IsGoal(will, a, actor)) is { } goal)
            return new ActionPath(new[] { new PathStep(goal) }, -1, will.TargetOf(goal), goal.Verb.RequiresTool && !Holds(goal.Verb, goal));

        if (path.ToolStep == 0)
        {
            var wanted = scene.Verbs.Where(v => will.Wants(v) && v.RequiresTool && !Holds(v))
                                    .SelectMany(ToolIdsFor).ToHashSet();
            if (actions.FirstOrDefault(a => Obtains(a, wanted) != null) is { } obtain)
                return path with { Steps = path.Steps.Skip(1).Prepend(new PathStep(obtain)).ToList() };
        }

        foreach (var action in actions.Where(a => a.Verb is AskToUseWorkshopVerb && a.Variant is string && pov.PlannedLeave == null))
            if (Plan(scene, new PoV(pov.Where, pov.When) { PlannedLeave = (string)action.Variant! }, actor, will) is { } rest
                && rest.Steps.Count == path.Steps.Count - 1)
                return new ActionPath(rest.Steps.Prepend(new PathStep(action)).ToList(),
                                      rest.ToolStep < 0 ? -1 : rest.ToolStep + 1, rest.Target);

        foreach (var action in actions.Where(a => TransitVerbTypes.Any(t => t.IsInstanceOfType(a.Verb))))
            foreach (var (area, when) in Destinations(scene, pov, actor, action))
                if (Plan(scene, new PoV(area, when) { PlannedLeave = pov.PlannedLeave }, actor, will) is { } rest && rest.Steps.Count == path.Steps.Count - 1)
                    return new ActionPath(rest.Steps.Prepend(new PathStep(action, SettledOf(action, area, when))).ToList(),
                                          rest.ToolStep < 0 ? -1 : rest.ToolStep + 1, rest.Target);
        return null;
    }

    /// <summary>
    /// The ways to the will a thinking mind may take through what one object offers, the shortest only:
    /// for each offered action, the shortest path that begins with it, kept when it is no longer than
    /// <see cref="Config.Narrative.WillMaxPathSteps"/> — or no longer than <paramref name="path"/>, the
    /// shortest this place offers, however long that is. Of those kept, every one of the least length
    /// is returned, one per distinct first action: more than one is a choice the will makes itself.
    /// Empty when nothing offered is on such a way.
    ///
    /// <para>Wider than <see cref="PathThrough"/>, which accepts only a way as short as the plan's and
    /// is what observation attends by: a will looking about turns to the object on its nearest road,
    /// but asked about an object two steps from its goal, it does not refuse it for being one step
    /// further than another.</para>
    /// </summary>
    public static IReadOnlyList<ActionPath> WaysThrough(Scene scene, PoV pov, PartyMember actor, WillTargets will, ActionPath path,
                                                        IEnumerable<VerbAction> offered, int? reachCap = null)
    {
        // reachCap: the most steps a way may take whatever the place's shortest — the will's other
        // targets during a visit made for one of them (WillPriority.WaysOn).
        int reach = reachCap ?? Math.Max(Config.Narrative.WillMaxPathSteps, path.Steps.Count);
        var ways  = new List<ActionPath>();
        foreach (var action in offered)
            if (!ways.Any(w => SameAction(action, w.First.Action))
                && ShortestThrough(scene, pov, actor, will, action) is { } way && way.Steps.Count <= reach)
                ways.Add(way);
        if (ways.Count == 0) return ways;
        int least = ways.Min(w => w.Steps.Count);
        return ways.Where(w => w.Steps.Count == least).ToList();
    }

    /// <summary>
    /// The shortest path to the will that begins with <paramref name="action"/>, or null when none
    /// does: the action itself when the will wants it (with the implement in hand); an implement
    /// obtained, then the rest planned holding it; a move or a wait, then the rest planned from where
    /// it leaves the body, the nearest of its ends. Any other action leads nowhere the plan can see.
    /// </summary>
    private static ActionPath? ShortestThrough(Scene scene, PoV pov, PartyMember actor, WillTargets will, VerbAction action)
    {
        var held = actor.GetAllItems().ToList();
        bool Holds(Verb verb, VerbAction? view = null)
            => verb.ReferenceToolIdsFor(view).Any(id => held.Any(i => i.ItemId == id))
               || held.Any(i => i.MadeForVerbIds.Contains(verb.VerbId));

        // No implement is asked for here: the goal and a tool-required step on the way are the click's
        // to refuse (RequiredToolRule), never the will's.
        if (IsGoal(will, action, actor))
            return new ActionPath(new[] { new PathStep(action) }, -1, will.TargetOf(action), action.Verb.RequiresTool && !Holds(action.Verb, action));

        var wanted = scene.Verbs.Where(v => will.Wants(v) && v.RequiresTool && !Holds(v))
                                .SelectMany(ToolIdsFor).ToHashSet();
        if (wanted.Count > 0 && Obtains(action, wanted) is { } tool)
            return Plan(scene, pov, actor, will, tool) is { } armed
                ? new ActionPath(armed.Steps.Prepend(new PathStep(action)).ToList(), 0, armed.Target)
                : null;

        // The workshop's leave, asked of its master, then whatever the will wants there.
        if (action.Verb is AskToUseWorkshopVerb && action.Variant is string workshop && pov.PlannedLeave == null)
            return Plan(scene, new PoV(pov.Where, pov.When) { PlannedLeave = workshop }, actor, will) is { } granted
                ? new ActionPath(granted.Steps.Prepend(new PathStep(action)).ToList(),
                                 granted.ToolStep < 0 ? -1 : granted.ToolStep + 1, granted.Target)
                : null;

        if (!TransitVerbTypes.Any(t => t.IsInstanceOfType(action.Verb))) return null;

        ActionPath? best = null;
        foreach (var (area, when) in Destinations(scene, pov, actor, action))
            if (Plan(scene, new PoV(area, when) { PlannedLeave = pov.PlannedLeave }, actor, will) is { } rest
                && (best == null || rest.Steps.Count + 1 < best.Steps.Count))
                best = new ActionPath(rest.Steps.Prepend(new PathStep(action, SettledOf(action, area, when))).ToList(),
                                      rest.ToolStep < 0 ? -1 : rest.ToolStep + 1, rest.Target);
        return best;
    }

    /// <summary>
    /// Every implement that would let <paramref name="verb"/> be done: its reference tools, and every
    /// item made for it (<see cref="Item.MadeForVerbIds"/>) — a ring of keys opens a lock as surely as a
    /// pick does, and a will to unlock should go for the keys hanging by the door.
    /// </summary>
    private static IEnumerable<string> ToolIdsFor(Verb verb)
    {
        if (!_toolIds.TryGetValue(verb.VerbId, out var ids))
            _toolIds[verb.VerbId] = ids = verb.ReferenceToolIds
                .Concat(ItemRegistry.Instance.All.Where(i => i.MadeForVerbIds.Contains(verb.VerbId)).Select(i => i.ItemId))
                .Distinct().ToList();
        return ids;
    }

    private static readonly Dictionary<string, List<string>> _toolIds = new();

    /// <summary>
    /// The implement <paramref name="action"/> would put in the pack, when it is one of
    /// <paramref name="wanted"/>: an action that yields it, or a proposal to buy from somebody whose
    /// wares include it.
    /// </summary>
    private static string? Obtains(VerbAction action, HashSet<string> wanted)
    {
        if (action.Verb.AcquiredItem(action.Target) is { } item && wanted.Contains(item.ItemId))
            return item.ItemId;
        if (action.Verb is ProposeToBuyVerb && action.Target is SceneNpc { Entity: NpcEntity npc }
            && npc.BuyCatalog?.Offers.FirstOrDefault(o => wanted.Contains(o.Prototype.ItemId)) is { } offer)
            return offer.Prototype.ItemId;
        return null;
    }

    /// <summary>What a step on the path is settled on: the hour a sit reaches, the area a backtrack does.</summary>
    private static object? SettledOf(VerbAction step, Area reached, TimePeriod when) => step.Verb switch
    {
        SitAndWaitVerb => when,
        BacktrackVerb  => reached,
        _              => null,
    };

    private static ActionPath Unwind(Scene scene, Dictionary<State, (State From, VerbAction? Step)> prev, State at, VerbAction goal, bool toolWanted)
    {
        var steps = new List<PathStep> { new(goal) };
        int toolStep = -1;
        for (var s = at; prev[s].Step is { } step; s = prev[s].From)
        {
            steps.Add(new PathStep(step, SettledOf(step, scene.GetArea(s.Area)!, s.When)));
            if (toolWanted && s.Tool != null && prev[s].From.Tool == null) toolStep = steps.Count - 1;
        }
        steps.Reverse();
        if (toolStep >= 0) toolStep = steps.Count - 1 - toolStep;
        return new ActionPath(steps, toolStep);
    }

    /// <summary>
    /// Whether <paramref name="candidate"/> is the same action as <paramref name="step"/>: the same
    /// verb, on the same element, with the same payload. Observation objects and the planner each
    /// expand their own <see cref="VerbAction"/> instances, so identity is never by reference.
    /// </summary>
    public static bool SameAction(VerbAction candidate, VerbAction step)
        => candidate.Verb.GetType() == step.Verb.GetType()
           && candidate.Target?.Id == step.Target?.Id
           && Equals(candidate.Variant, step.Variant);
}
