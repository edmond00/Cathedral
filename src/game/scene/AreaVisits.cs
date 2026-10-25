using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene;

/// <summary>
/// What the party knows of a location's layout: the areas it has stood in and the ways it has gone
/// by. Written to <see cref="Scene.VisitedAreas"/> and <see cref="Scene.TakenLinks"/>, which are the
/// stores of the protagonist's <c>MentalMap</c> of the location, so it outlives the visit for as long
/// as the place is remembered. What an observation
/// took in from where the party stood (<see cref="See"/>) is kept beside them. The location map draws
/// the visited in full, the only seen dimmed, and the rest as unknown.
///
/// <para><b>Keys, not instances.</b> A scene is rebuilt on every arrival, so an area is recorded by
/// its <see cref="Element.StableKey"/>, a connector by its own, and a way between neighbours — which
/// has no element of its own — by the two areas' keys in a fixed order (<see cref="LinkKey"/>).</para>
///
/// <para><b>A move far away is walked, not jumped.</b> Following somebody, being led to a trader's
/// premises or presented to a reeve, tracking a quarry: each of these lands the party some areas
/// away, and in the world it walked there. <see cref="Route"/> finds the walk — the shortest way over
/// neighbours, stairs and doors open from this side — and every area and way on it is recorded.
/// A private room is walked through like any other (a temple's chapel may lie past a private
/// corridor); only a locked door stops the walk. When there is no such walk (a roof, a far bank, a
/// house locked from outside), only the arrival is recorded. A path is a named way past the areas between its ends, so
/// following one records those areas as well. A landscape's voyage records the view taken and,
/// where one exists, the walk.</para>
///
/// <para>Recording only: none of this decides whether a move may happen. The verbs do that.</para>
/// </summary>
public static class AreaVisits
{
    /// <summary>The key of the way between two neighbouring areas, the same whichever end is named first.</summary>
    public static string LinkKey(Area a, Area b)
    {
        string first = a.StableKey, second = b.StableKey;
        if (string.CompareOrdinal(first, second) > 0) (first, second) = (second, first);
        return $"edge|{first}|{second}";
    }

    /// <summary>The party stands in <paramref name="area"/>.</summary>
    public static void Arrive(Scene scene, Area area) => scene.VisitedAreas.Add(area.StableKey);

    /// <summary>
    /// An observation made from <paramref name="here"/> took in <paramref name="observed"/>. A
    /// neighbouring area looked at is seen, and so is the way to it; a door, a stair, a path, a climb
    /// or a view looked at is a way seen, and the area at its far end with it. A group (the ways out,
    /// taken in together) sees each of its members. Anything else teaches the map nothing. What is
    /// seen is drawn named but dimmed until it is visited or gone by.
    /// </summary>
    public static void See(Scene scene, Area here, NarrativeAnchor observed)
    {
        switch (observed)
        {
            case SyntheticAreaObservationObject { Area: var area } when area.Id != here.Id:
                if (scene.SeenAreas.Add(area.StableKey))
                    Console.WriteLine($"AreaVisits: {area.DisplayName} seen from {here.DisplayName}");
                if (IsNeighbour(scene, here, area)) scene.SeenLinks.Add(LinkKey(here, area));
                break;

            case SyntheticObservationObject { PointOfInterest: ConnectorPointOfInterest connector }:
                scene.SeenLinks.Add(connector.StableKey);
                foreach (var end in new[] { connector.AreaA, connector.AreaB })
                    if (end.Id != here.Id && scene.SeenAreas.Add(end.StableKey))
                        Console.WriteLine($"AreaVisits: {end.DisplayName} seen from {here.DisplayName} by {connector.DisplayName}");
                break;

            case ObservationGroup group:
                foreach (var member in group.Members) See(scene, here, member);
                break;
        }
    }

    // ── Going back ───────────────────────────────────────────────────────────

    /// <summary>
    /// Whether <paramref name="way"/>, seen from <paramref name="here"/>, is a way the party knows and
    /// may go back by: a neighbouring area already stood in, or a connector already gone by. Never a
    /// view (one way, and not back), a door locked against the comer at <paramref name="when"/>, or a
    /// step from public ground into private — private ground is gone back to only from within it.
    /// </summary>
    public static bool IsWayBack(Scene scene, Area here, Element way, TimePeriod when)
        => BackStep(scene, here, way, when) != null;

    /// <summary>Whether <paramref name="way"/> is a way already known from here, walkable back or not: what an observation of it remarks on.</summary>
    public static bool IsKnownWay(Scene scene, Area here, Element way) => way switch
    {
        Area area => area.Id != here.Id && IsNeighbour(scene, here, area) && scene.VisitedAreas.Contains(area.StableKey),
        ConnectorPointOfInterest c => c is not LandscapePointOfInterest && c.Touches(here) && scene.TakenLinks.Contains(c.StableKey),
        _ => false,
    };

    private static (Area To, string Key)? BackStep(Scene scene, Area here, Element way, TimePeriod when)
    {
        if (!IsKnownWay(scene, here, way)) return null;
        switch (way)
        {
            case Area area:
                return Allowed(here, area) ? (area, LinkKey(here, area)) : null;
            case ConnectorPointOfInterest c when c.AreaA.Id != c.AreaB.Id:
                if (c is DoorPointOfInterest door && here.Id != door.BackArea.Id
                    && door.EffectiveState(when) == DoorState.Locked) return null;
                var to = c.Other(here);
                return Allowed(here, to) ? (to, c.StableKey) : null;
        }
        return null;

        static bool Allowed(Area from, Area to) => from.IsPrivate || !to.IsPrivate;
    }

    /// <summary>
    /// Every area the party may go back to from <paramref name="from"/> at <paramref name="when"/>, each
    /// with the way there as <see cref="Move"/> returns one: only ways back (<see cref="IsWayBack"/>),
    /// breadth first, so each is the fewest steps. <paramref name="from"/> itself is not among them.
    /// </summary>
    public static Dictionary<Area, List<(Area To, string? Link)>> BackReach(Scene scene, Area from, TimePeriod when)
    {
        var routes = new Dictionary<Area, List<(Area To, string? Link)>> { [from] = new() };
        var queue = new Queue<Area>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var area = queue.Dequeue();
            var ways = new List<Element>();
            if (scene.AreaGraph.TryGetValue(area.Id, out var outs))
                foreach (var id in outs)
                    if (scene.GetArea(id) is { } next) ways.Add(next);
            foreach (var (fromId, tos) in scene.AreaGraph)
                if (tos.Contains(area.Id) && scene.GetArea(fromId) is { } prior) ways.Add(prior);
            ways.AddRange(area.PointsOfInterest.OfType<ConnectorPointOfInterest>());

            foreach (var way in ways)
                if (BackStep(scene, area, way, when) is { } step && !routes.ContainsKey(step.To))
                {
                    routes[step.To] = new List<(Area, string?)>(routes[area]) { (step.To, step.Key) };
                    queue.Enqueue(step.To);
                }
        }
        routes.Remove(from);
        return routes;
    }

    /// <summary>
    /// The party went back from <paramref name="from"/> to <paramref name="to"/> by ways it knew:
    /// the way taken, as <see cref="Move"/> returns one, and nothing new to record but the two ends.
    /// </summary>
    public static IReadOnlyList<(Area To, string? Link)> MoveBack(Scene scene, Area from, Area to, TimePeriod when)
    {
        Arrive(scene, from);
        Arrive(scene, to);
        if (BackReach(scene, from, when).TryGetValue(to, out var route))
        {
            foreach (var (_, link) in route) if (link != null) scene.TakenLinks.Add(link);
            return route;
        }
        return new[] { (to, (string?)null) };
    }

    /// <summary>
    /// The party went from <paramref name="from"/> to <paramref name="to"/>, by acting on
    /// <paramref name="by"/> (a connector, or the area moved into, or a person followed; null for a
    /// walk nobody acted on).
    /// </summary>
    /// <returns>
    /// The way gone, as the area reached and the key of the way taken at each step, for the walk to be
    /// shown on the map: a path or a view is one step along itself, a walk is its steps, and a move
    /// with no walk at all is one step with no way (<c>null</c>).
    /// </returns>
    public static IReadOnlyList<(Area To, string? Link)> Move(Scene scene, Area from, Area to, Element? by, TimePeriod when)
    {
        Arrive(scene, from);
        Arrive(scene, to);

        switch (by)
        {
            case PathPointOfInterest path:
                scene.TakenLinks.Add(path.StableKey);
                var route = new List<Area> { path.AreaA };
                route.AddRange(path.Via);
                route.Add(path.AreaB);
                foreach (var area in path.Via) Arrive(scene, area);
                for (int i = 1; i < route.Count; i++)
                    if (IsNeighbour(scene, route[i - 1], route[i]))
                        scene.TakenLinks.Add(LinkKey(route[i - 1], route[i]));
                return new[] { (to, (string?)path.StableKey) };

            case LandscapePointOfInterest view:
                scene.TakenLinks.Add(view.StableKey);
                RecordWalk(scene, from, to, when);
                return new[] { (to, (string?)view.StableKey) };

            case ConnectorPointOfInterest connector when connector.Touches(from) && connector.Touches(to):
                scene.TakenLinks.Add(connector.StableKey);
                return new[] { (to, (string?)connector.StableKey) };
        }

        if (IsNeighbour(scene, from, to))
        {
            scene.TakenLinks.Add(LinkKey(from, to));
            return new[] { (to, (string?)LinkKey(from, to)) };
        }
        return RecordWalk(scene, from, to, when)?.Select(s => (s.Area, (string?)s.Link)).ToList()
               ?? new List<(Area, string?)> { (to, null) };
    }

    private static List<(Area Area, string Link)>? RecordWalk(Scene scene, Area from, Area to, TimePeriod when)
    {
        var walk = Route(scene, from, to, when);
        if (walk == null) return null;
        foreach (var (area, link) in walk)
        {
            Arrive(scene, area);
            scene.TakenLinks.Add(link);
        }
        return walk;
    }

    private static bool IsNeighbour(Scene scene, Area a, Area b)
        => (scene.AreaGraph.TryGetValue(a.Id, out var outA) && outA.Contains(b.Id))
        || (scene.AreaGraph.TryGetValue(b.Id, out var outB) && outB.Contains(a.Id));

    /// <summary>
    /// The shortest walk from <paramref name="from"/> to <paramref name="to"/> over ways anybody may
    /// take at <paramref name="when"/>, as (area reached, key of the way taken) per step; empty when
    /// the two are one area, null when there is no such walk. Breadth first, ways tried in the
    /// scene's own order, so the same scene always gives the same walk.
    /// </summary>
    public static List<(Area Area, string Link)>? Route(Scene scene, Area from, Area to, TimePeriod when)
    {
        if (from.Id == to.Id) return new();

        var cameBy = new Dictionary<Area, (Area From, string Link)>();
        var queue = new Queue<Area>();
        queue.Enqueue(from);
        cameBy[from] = (from, "");

        while (queue.Count > 0)
        {
            var here = queue.Dequeue();
            foreach (var (next, link) in Ways(scene, here, when))
            {
                if (cameBy.ContainsKey(next)) continue;
                cameBy[next] = (here, link);
                if (next.Id == to.Id) return Unwind(next);
                queue.Enqueue(next);
            }
        }
        return null;

        List<(Area, string)> Unwind(Area end)
        {
            var steps = new List<(Area, string)>();
            for (var a = end; a.Id != from.Id; a = cameBy[a].From)
                steps.Add((a, cameBy[a].Link));
            steps.Reverse();
            return steps;
        }
    }

    /// <summary>The ways out of an area that need no roll, no key and no climb.</summary>
    private static IEnumerable<(Area To, string Link)> Ways(Scene scene, Area area, TimePeriod when)
    {
        if (scene.AreaGraph.TryGetValue(area.Id, out var outs))
            foreach (var id in outs)
                if (scene.GetArea(id) is { } next)
                    yield return (next, LinkKey(area, next));

        foreach (var connector in area.PointsOfInterest.OfType<ConnectorPointOfInterest>())
        {
            switch (connector)
            {
                case StairPointOfInterest:
                    yield return (connector.Other(area), connector.StableKey);
                    break;
                // A door always opens from behind; from the front only when it is not locked.
                case DoorPointOfInterest door
                    when area.Id == door.BackArea.Id || door.EffectiveState(when) == DoorState.Unlocked:
                    yield return (door.Other(area), door.StableKey);
                    break;
            }
        }
    }

    /// <summary>
    /// Every way between two areas of the scene: the neighbours of the area graph (once per pair)
    /// and every connector (once), with the key each is recorded under. For listing what was taken.
    /// </summary>
    public static IEnumerable<(Area A, Area B, string Key, ConnectorPointOfInterest? Connector)> AllLinks(Scene scene)
    {
        var seen = new HashSet<string>();
        foreach (var (fromId, tos) in scene.AreaGraph)
            foreach (var toId in tos)
                if (scene.GetArea(fromId) is { } a && scene.GetArea(toId) is { } b && a.Id != b.Id
                    && seen.Add(LinkKey(a, b)))
                    yield return (a, b, LinkKey(a, b), null);

        foreach (var c in scene.AllAreas.SelectMany(a => a.PointsOfInterest).OfType<ConnectorPointOfInterest>())
            if (seen.Add(c.StableKey))
                yield return (c.AreaA, c.AreaB, c.StableKey, c);
    }
}
