using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene;

/// <summary>One action of a path: the action itself, as the scene offers it from that step's standing.</summary>
public sealed record PathStep(VerbAction Action)
{
    public string Verbatim => Action.Verbatim;
}

/// <summary>
/// The shortest path of actions from where a body stands to one its will wants. <see cref="Steps"/>
/// runs from the first action to take now to the wanted action itself, which is always last.
/// </summary>
/// <param name="Steps">At least one: the goal alone when it is offered right here.</param>
/// <param name="ToolStep">The index of the step that obtains the implement the goal needs, or -1 when none is needed.</param>
public sealed record ActionPath(IReadOnlyList<PathStep> Steps, int ToolStep)
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
/// a path never passes through a conversation, a fight or a roll of loot: it stops at them.</para>
///
/// <para><b>Implements.</b> A goal verb that requires a tool (<see cref="Verb.RequiresTool"/>) is
/// reached only holding one of its <see cref="Verb.ReferenceToolIds"/>, or something made for it. When
/// none is held the path goes by one: an action that yields it (taking it from where it lies,
/// gathering it) or a proposal to buy from somebody whose wares include it. The purchase itself is a
/// conversation, so the plan cannot see past it; it is the last step that matters, and the next
/// phase plans again from whatever the trade left in the pack.</para>
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
        typeof(SitAndWaitVerb), typeof(HideAndWaitVerb),
    };

    private readonly record struct State(Guid Area, TimePeriod When, string? Tool);

    private static readonly Dictionary<(Scene, Guid, TimePeriod, WillTarget, string), ActionPath?> _cache = new();

    /// <summary>Drops every remembered path. Called when a narration phase begins, and whenever the scene changes under a plan.</summary>
    public static void Forget() => _cache.Clear();

    /// <summary>
    /// The shortest path from <paramref name="pov"/> to an action <paramref name="will"/> matches, or
    /// null when there is none within this place — no such action here at all, or none the body can
    /// reach (or equip itself for) without leaving.
    /// </summary>
    public static ActionPath? Plan(Scene scene, PoV pov, PartyMember actor, WillTarget will)
    {
        var held = actor.GetAllItems().ToList();
        var key  = (scene, pov.Where.Id, pov.When, will, string.Join(",", held.Select(i => i.ItemId).OrderBy(i => i)));
        if (_cache.TryGetValue(key, out var cached)) return cached;

        var path = Search(scene, pov, actor, will, held);
        _cache[key] = path;
        Console.WriteLine(path == null
            ? $"ActionPathPlanner: no way to '{will.Purpose}' from {pov.Where.DisplayName} at {pov.When} — not in this place."
            : $"ActionPathPlanner: '{will.Purpose}' in {path.Steps.Count} step(s): {string.Join(" | ", path.Steps.Select(s => s.Verbatim))}");
        return path;
    }

    private static ActionPath? Search(Scene scene, PoV pov, PartyMember actor, WillTarget will, List<Item> held)
    {
        var goalVerbs = scene.Verbs.Where(will.Wants).ToList();
        if (goalVerbs.Count == 0) return null;

        bool Holds(Verb verb, string? acquired)
            => verb.ReferenceToolIds.Any(id => id == acquired || held.Any(i => i.ItemId == id))
               || held.Any(i => i.MadeForVerbIds.Contains(verb.VerbId));

        // The implements some goal verb needs and the body does not hold. Empty: no detour for one.
        var wanted = goalVerbs.Where(v => v.RequiresTool && !Holds(v, null))
                              .SelectMany(v => v.ReferenceToolIds).ToHashSet();

        // A transit verb that itself needs an implement is walked only with one already in hand.
        var transit = scene.Verbs.Where(v => TransitVerbTypes.Any(t => t.IsInstanceOfType(v))
                                          && (!v.RequiresTool || Holds(v, null)));
        var narrow  = transit.Concat(goalVerbs).Distinct().ToList();
        var areas   = scene.AllAreas.ToDictionary(a => a.Id);

        var start = new State(pov.Where.Id, pov.When, null);
        var prev  = new Dictionary<State, (State From, VerbAction? Step)> { [start] = (start, null) };
        var queue = new Queue<State>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var s     = queue.Dequeue();
            var here  = new PoV(areas[s.Area], s.When);
            // Every verb is read only while an implement is still to be found: it is the one search
            // in which a grab or a purchase is a step on the way.
            var verbs = wanted.Count > 0 && s.Tool == null ? scene.Verbs : narrow;

            foreach (var entry in scene.View(here, actor, verbs).Entries)
            foreach (var action in entry.ApplicableVerbs)
            {
                if (will.Matches(action) && (!action.Verb.RequiresTool || Holds(action.Verb, s.Tool)))
                    return Unwind(prev, s, action, wanted.Count > 0);

                if (wanted.Count > 0 && s.Tool == null && Obtains(action, wanted) is { } tool)
                    Push(new State(s.Area, s.When, tool), action);

                if (!TransitVerbTypes.Any(t => t.IsInstanceOfType(action.Verb))) continue;

                foreach (var (area, when) in Destinations(scene, here, actor, action))
                    Push(s with { Area = area.Id, When = when }, action);
            }

            void Push(State next, VerbAction step)
            {
                if (prev.ContainsKey(next)) return;
                prev[next] = (s, step);
                queue.Enqueue(next);
            }
        }
        return null;
    }

    /// <summary>
    /// Where a transit action would leave the body: the area and hour its success reports name, read
    /// and never applied. A sit is every other hour of the day, since the sitter chooses.
    /// </summary>
    private static IEnumerable<(Area Area, TimePeriod When)> Destinations(Scene scene, PoV here, PartyMember actor, VerbAction action)
    {
        if (action.Verb is SitAndWaitVerb)
        {
            foreach (var period in Enum.GetValues<TimePeriod>().Where(p => p != here.When))
                yield return (here.Where, period);
            yield break;
        }

        IReadOnlyList<Outcome> reports;
        try { reports = action.Verb.SuccessReports(scene, here, actor, action.Target!, action); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ActionPathPlanner: {action.Verb.VerbId} could not be read ({ex.Message}) — skipped.");
            yield break;
        }
        foreach (var report in reports)
        {
            switch (report)
            {
                case AreaMoveOutcome m:   yield return (m.Destination, here.When); break;
                case DoorUnlockOutcome u: yield return (u.Destination, here.When); break;
                case TimeShiftOutcome t:  yield return (here.Where, t.Destination); break;
            }
        }
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
    public static ActionPath? PathThrough(Scene scene, PoV pov, PartyMember actor, WillTarget will, ActionPath path,
                                          IEnumerable<VerbAction> offered)
    {
        var actions = offered.ToList();

        if (actions.FirstOrDefault(a => SameAction(a, path.First.Action)) is { } own)
            return path with { Steps = path.Steps.Skip(1).Prepend(new PathStep(own)).ToList() };

        var held = actor.GetAllItems().ToList();
        bool Holds(Verb verb)
            => verb.ReferenceToolIds.Any(id => held.Any(i => i.ItemId == id))
               || held.Any(i => i.MadeForVerbIds.Contains(verb.VerbId));

        if (path.Steps.Count == 1
            && actions.FirstOrDefault(a => will.Matches(a) && (!a.Verb.RequiresTool || Holds(a.Verb))) is { } goal)
            return new ActionPath(new[] { new PathStep(goal) }, -1);

        if (path.ToolStep == 0)
        {
            var wanted = scene.Verbs.Where(v => will.Wants(v) && v.RequiresTool && !Holds(v))
                                    .SelectMany(v => v.ReferenceToolIds).ToHashSet();
            if (actions.FirstOrDefault(a => Obtains(a, wanted) != null) is { } obtain)
                return path with { Steps = path.Steps.Skip(1).Prepend(new PathStep(obtain)).ToList() };
        }

        foreach (var action in actions.Where(a => TransitVerbTypes.Any(t => t.IsInstanceOfType(a.Verb))))
            foreach (var (area, when) in Destinations(scene, pov, actor, action))
                if (Plan(scene, new PoV(area, when), actor, will) is { } rest && rest.Steps.Count == path.Steps.Count - 1)
                    return new ActionPath(rest.Steps.Prepend(new PathStep(action)).ToList(),
                                          rest.ToolStep < 0 ? -1 : rest.ToolStep + 1);
        return null;
    }

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

    private static ActionPath Unwind(Dictionary<State, (State From, VerbAction? Step)> prev, State at, VerbAction goal, bool toolWanted)
    {
        var steps = new List<PathStep> { new(goal) };
        int toolStep = -1;
        for (var s = at; prev[s].Step is { } step; s = prev[s].From)
        {
            steps.Add(new PathStep(step));
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
