using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene.Shared;

/// <summary>How a location's outdoor areas are wired together.</summary>
public enum LayoutShape
{
    /// <summary>Everything hangs off the first area. Compact, always one step from the hub.</summary>
    Hub,
    /// <summary>A single line: each area connects only to the next. Long and linear to walk.</summary>
    Chain,
    /// <summary>A chain whose ends meet, so there are two ways round to anywhere.</summary>
    Ring,
    /// <summary>A hub with a couple of extra cross-links, so some outlying areas are neighbours.</summary>
    Spur,
}

/// <summary>
/// Wires a location's outdoor areas together and hands out its buildings.
///
/// <para>Every inhabited location used to have a fixed skeleton — the farm was always a pure hub off
/// the courtyard, the village always the same square with the same two lanes carrying the same
/// workshops — so two farms differed only in which pens they rolled, never in how they were laid
/// out. This puts the shape itself in the RNG.</para>
///
/// <para>Neighbours are joined by graph edges alone (<see cref="Link"/>); paths are laid afterwards,
/// between areas the edges do not join (<see cref="LayPaths"/>). Both keep the invariants
/// <c>--building-audit</c> enforces: no path beside an edge, and every path named for the pair it
/// joins so no two in one area share a display name.</para>
/// </summary>
public static class OutdoorLayout
{
    /// <summary>
    /// Rolls a shape. Hub is weighted highest because it is the most legible to navigate; ring is
    /// rarest because it is the easiest to get turned around in.
    /// </summary>
    public static LayoutShape RollShape(Random rng)
    {
        double roll = rng.NextDouble();
        if (roll < 0.40) return LayoutShape.Hub;
        if (roll < 0.65) return LayoutShape.Spur;
        if (roll < 0.87) return LayoutShape.Chain;
        return LayoutShape.Ring;
    }

    /// <summary>
    /// Connects <paramref name="areas"/> according to <paramref name="shape"/>, with
    /// <c>areas[0]</c> as the hub or the head of the chain. <paramref name="pathType"/> names the
    /// kind of way ("Track", "Lane") that a path laid along these edges takes its name from.
    /// </summary>
    public static void Connect(
        Scene scene, IReadOnlyList<Area> areas, LayoutShape shape, string pathType, Random rng)
    {
        if (areas.Count < 2) return;

        switch (shape)
        {
            case LayoutShape.Chain:
            case LayoutShape.Ring:
                for (int i = 0; i < areas.Count - 1; i++)
                    Link(scene, areas[i], areas[i + 1], pathType);
                if (shape == LayoutShape.Ring && areas.Count > 2)
                    Link(scene, areas[^1], areas[0], pathType);
                break;

            case LayoutShape.Spur:
                for (int i = 1; i < areas.Count; i++)
                    Link(scene, areas[0], areas[i], pathType);
                // One or two cross-links between outlying areas, so the hub is not the only way
                // across and a couple of places turn out to be neighbours.
                for (int n = 0, want = rng.Next(1, 3); n < want && areas.Count > 3; n++)
                {
                    int a = rng.Next(1, areas.Count);
                    int b = rng.Next(1, areas.Count);
                    if (a != b) Link(scene, areas[a], areas[b], pathType);
                }
                break;

            default: // Hub
                for (int i = 1; i < areas.Count; i++)
                    Link(scene, areas[0], areas[i], pathType);
                break;
        }
    }

    /// <summary>
    /// Joins two neighbouring areas: one two-way graph edge, the walk <c>MoveToAreaVerb</c> takes, and
    /// a note of what kind of way it is on the ground (<paramref name="wayKind"/>, "Track", "Lane"),
    /// which names any path later laid along it. Silently skips a pair that is already joined — the
    /// spur shape can roll the same cross-link twice.
    ///
    /// <para>This used to lay a path beside the edge as well, so every way out of an outdoor area was
    /// offered twice for the same difficulty-1 walk. Paths are now <see cref="LayPaths"/>'s, and only
    /// ever join areas the edges do not.</para>
    /// </summary>
    public static void Link(Scene scene, Area a, Area b, string wayKind, string[]? moods = null, string? surface = null)
    {
        if (a.Id == b.Id) return;
        if (scene.AreaGraph.TryGetValue(a.Id, out var already) && already.Contains(b.Id)) return;

        scene.ConnectAreasBidirectional(a, b);
        scene.WayStyles[EdgeKey(a, b)] = new WayStyle(wayKind, moods ?? DefaultMoods, surface);
    }

    private static readonly string[] DefaultMoods = { "worn", "muddy", "well-trodden" };

    /// <summary>Chance that a pair of areas two steps apart gets a path between them.</summary>
    public const double PathChanceTwoSteps = 0.30;

    /// <summary>Chance for a pair three steps apart: rarer, since it skips more.</summary>
    public const double PathChanceThreeSteps = 0.15;

    /// <summary>
    /// Most paths one area may have. Each is one more thing on offer when the area is looked at, and a
    /// hub would otherwise sprout one to every spoke.
    /// </summary>
    public const int MaxPathsPerArea = 2;

    /// <summary>
    /// Lays paths across the open ground of <paramref name="scene"/>: between two areas two or three
    /// steps apart, on a random share of the pairs (<see cref="PathChanceTwoSteps"/>,
    /// <see cref="PathChanceThreeSteps"/>), at most <see cref="MaxPathsPerArea"/> per area. Called by
    /// <c>SceneFactory.Build</c> once a factory has built its sections, so it covers every factory.
    ///
    /// <para><b>Only through public open ground.</b> A route is a shortest walk along graph edges whose
    /// every area, both ends included, is outdoors and not private. A path past a private yard would be
    /// a way to trespass without entering it, and since gates carry no edge, a route along edges never
    /// passes a door, a ford or a cliff. A pair some other connector already joins (a rolled crossing)
    /// gets no second way.</para>
    ///
    /// <para>The path is named after the ways it runs along — the commonest kind among the route's
    /// edges, so a village gets lanes and a cave passages — and its description names what it passes.</para>
    /// </summary>
    public static void LayPaths(Scene scene, Random rng)
    {
        var open   = scene.OutdoorAreas.Where(a => !a.IsPrivate).ToList();
        var index  = open.Select((a, i) => (a.Id, i)).ToDictionary(t => t.Id, t => t.i);
        var joined = scene.AllAreas.SelectMany(a => a.PointsOfInterest).OfType<ConnectorPointOfInterest>()
                          .Select(c => EdgeKey(c.AreaA, c.AreaB)).ToHashSet();
        var counts = new int[open.Count];

        for (int i = 0; i < open.Count; i++)
        {
            var routes = Routes(scene, open, index, i);
            for (int j = i + 1; j < open.Count; j++)
            {
                if (!routes.TryGetValue(j, out var route)) continue;
                int steps = route.Count - 1;
                if (steps is < 2 or > 3) continue;
                if (joined.Contains(EdgeKey(open[i], open[j]))) continue;

                // Drawn for every eligible pair, laid or not, so whether one pair gets a path never
                // shifts the draws of the pairs after it.
                double roll = rng.NextDouble();
                if (roll >= (steps == 2 ? PathChanceTwoSteps : PathChanceThreeSteps)) continue;
                if (counts[i] >= MaxPathsPerArea || counts[j] >= MaxPathsPerArea) continue;

                Lay(scene, route.Select(k => open[k]).ToList());
                counts[i]++;
                counts[j]++;
            }
        }
    }

    /// <summary>
    /// Shortest routes from <c>open[from]</c> to every open area within three steps, as lists of
    /// indices into <paramref name="open"/>, ends included. Only two-way edges count, and neighbours
    /// are visited in <paramref name="open"/>'s order, so which of two equal routes wins is fixed by
    /// build order.
    /// </summary>
    private static Dictionary<int, List<int>> Routes(Scene scene, List<Area> open, Dictionary<Guid, int> index, int from)
    {
        var parent = new Dictionary<int, int> { [from] = -1 };
        var depth  = new Dictionary<int, int> { [from] = 0 };
        var queue  = new Queue<int>();
        queue.Enqueue(from);

        while (queue.Count > 0)
        {
            int cur = queue.Dequeue();
            if (depth[cur] == 3) continue;
            if (!scene.AreaGraph.TryGetValue(open[cur].Id, out var next)) continue;

            var steps = next.Where(index.ContainsKey).Select(id => index[id])
                            .Where(k => scene.AreaGraph.TryGetValue(open[k].Id, out var back) && back.Contains(open[cur].Id))
                            .OrderBy(k => k);
            foreach (int k in steps)
            {
                if (parent.ContainsKey(k)) continue;
                parent[k] = cur;
                depth[k]  = depth[cur] + 1;
                queue.Enqueue(k);
            }
        }

        var routes = new Dictionary<int, List<int>>();
        foreach (int k in parent.Keys)
        {
            var route = new List<int>();
            for (int at = k; at != -1; at = parent[at]) route.Add(at);
            route.Reverse();
            routes[k] = route;
        }
        return routes;
    }

    private static void Lay(Scene scene, List<Area> route)
    {
        var a   = route[0];
        var b   = route[^1];
        var via = route.Skip(1).Take(route.Count - 2).ToList();

        // The commonest kind of way along the route; the first edge's on a tie.
        var style = Enumerable.Range(0, route.Count - 1)
            .Select(k => scene.WayStyles.GetValueOrDefault(EdgeKey(route[k], route[k + 1])))
            .OfType<WayStyle>()
            .GroupBy(st => st.Kind)
            .OrderByDescending(g => g.Count())
            .Select(g => g.First())
            .FirstOrDefault() ?? new WayStyle("Track", DefaultMoods, null);

        static string Lower(Area x) => x.DisplayName.ToLowerInvariant();
        var past = string.Join(" and ", via.Select(Lower));

        new PathPointOfInterest(
            a, b, via,
            PathPointOfInterest.NameFor(a, b, style.Kind),
            new() { $"A {style.Kind.ToLowerInvariant()} {style.Surface ?? "running"} from {Lower(a)} to {Lower(b)}, by way of {past}" },
            style.Moods
        ).AttachTo(scene);
    }

    /// <summary>The key of an edge in <see cref="Scene.WayStyles"/>, the same whichever end comes first.</summary>
    public static (Guid, Guid) EdgeKey(Area a, Area b)
        => a.Id.CompareTo(b.Id) < 0 ? (a.Id, b.Id) : (b.Id, a.Id);

    /// <summary>
    /// Spreads <paramref name="count"/> buildings across <paramref name="areas"/>, returning the
    /// outdoor area each one should be entered from. Every area gets at least one building before any
    /// gets a second, so a rolled lane is never left with nothing on it.
    /// </summary>
    public static List<Area> DistributeEntrances(IReadOnlyList<Area> areas, int count, Random rng)
    {
        var result = new List<Area>(count);
        var order  = areas.OrderBy(_ => rng.Next()).ToList();

        for (int i = 0; i < count; i++)
            result.Add(i < order.Count ? order[i] : areas[rng.Next(areas.Count)]);

        return result;
    }
}

/// <summary>
/// What kind of way an area-graph edge is on the ground, the moods a path along it takes, and where it
/// runs ("over the steppe"), which the path's description uses when there is one.
/// </summary>
public sealed record WayStyle(string Kind, string[] Moods, string? Surface);
