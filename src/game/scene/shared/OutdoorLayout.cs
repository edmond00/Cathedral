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
/// <para>Both methods maintain the invariants <c>--building-audit</c> enforces: paths always come in
/// step with an <c>AreaGraph</c> edge, and every path is named for the pair it joins so no two in one
/// area share a display name.</para>
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
    /// kind of way ("Track", "Lane"); the display name is built from the pair it joins.
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
    /// Adds one path plus its matching graph edge. Silently skips a pair that is already joined —
    /// the spur shape can roll the same cross-link twice, and a second path between two areas would
    /// be a duplicate display name in both of them.
    /// </summary>
    private static void Link(Scene scene, Area a, Area b, string pathType)
    {
        if (a.Id == b.Id) return;
        if (scene.AreaGraph.TryGetValue(a.Id, out var already) && already.Contains(b.Id)) return;

        scene.ConnectAreasBidirectional(a, b);

        var path = new PathPointOfInterest(
            a, b,
            PathPointOfInterest.NameFor(a, b, pathType),
            new() { $"A worn {pathType.ToLowerInvariant()} running between {a.DisplayName.ToLowerInvariant()} and {b.DisplayName.ToLowerInvariant()}" },
            new[] { "worn", "muddy", "well-trodden" }
        );
        a.PointsOfInterest.Add(path);
        b.PointsOfInterest.Add(path);
        path.Register(scene);
    }

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
