using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// A named way that runs past one or two areas to a farther one (road, track, lane, stream path):
/// the shortcut of open ground. Always passable from either end. Targeted by
/// <see cref="Verbs.FollowPathVerb"/>. Laid by <c>OutdoorLayout.LayPaths</c> after a factory has
/// built its sections, on a random share of the eligible pairs — a location may have none.
///
/// <para>A path used to join every pair of neighbouring areas, beside the graph edge
/// <c>MoveToAreaVerb</c> walks, so outdoors every way out was offered twice: once as the path, once
/// as the area, for the same difficulty-1 walk to the same place. Neighbours are now joined by the
/// edge alone, and a path is what the edge cannot be: two or three steps in one.</para>
///
/// <para>It carries no graph edge (<see cref="ConnectorPointOfInterest.AllowsGraphEdge"/> is false),
/// so <c>BuildingAudit</c> holds it to the rule every connector keeps: no edge between its two ends,
/// which is the same as never joining neighbours.</para>
/// </summary>
public class PathPointOfInterest : ConnectorPointOfInterest
{
    protected override string ConnectorKind => "path";

    /// <summary>
    /// The areas the path runs past, in order from <see cref="ConnectorPointOfInterest.AreaA"/>. A
    /// route through public open ground only (<c>OutdoorLayout.LayPaths</c>), so following it is never
    /// a way past a private yard or a gate.
    /// </summary>
    public IReadOnlyList<Area> Via { get; }

    public PathPointOfInterest(
        Area areaA,
        Area areaB,
        IReadOnlyList<Area> via,
        string displayName,
        List<string> descriptions,
        string[]? moods = null)
        : base(areaA, areaB, displayName, "path", descriptions, moods: moods)
    {
        Via = via;
    }

    /// <summary>
    /// A display name unique to the pair of areas a path joins: "Courtyard–Orchard Track".
    ///
    /// <para>Naming every track off a hub "Farmyard Track" made all but one of them invisible —
    /// observation candidates are de-duplicated by display name, so a courtyard with five identically
    /// named tracks offered exactly one way out. Naming the endpoints also tells the player where a
    /// path goes before they take it, which is the same job a door's rolled description does
    /// indoors.</para>
    /// </summary>
    public static string NameFor(Area a, Area b, string pathType)
    {
        var pair = $"{a.DisplayName}–{b.DisplayName}";

        // Drop any word of the path type that an endpoint already uses, or the label stutters:
        // "Square–Mill Lane" + "Lane" read "Square–Mill Lane Lane", and "Sandy Beach–Rocky Shore" +
        // "Shore Path" read "…Rocky Shore Shore Path". Filtering word by word rather than dropping
        // the whole type keeps the useful half ("…Rocky Shore Path"). The pair alone is still unique,
        // since two areas are only ever joined once.
        var endpointWords = new HashSet<string>(
            Words(a.DisplayName).Concat(Words(b.DisplayName)), StringComparer.OrdinalIgnoreCase);

        var kept = pathType.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                           .Where(word => !endpointWords.Contains(word))
                           .ToList();

        return kept.Count == 0 ? pair : $"{pair} {string.Join(' ', kept)}";
    }

    private static IEnumerable<string> Words(string phrase)
        => phrase.Split(new[] { ' ', '–', '-' }, StringSplitOptions.RemoveEmptyEntries);
}
