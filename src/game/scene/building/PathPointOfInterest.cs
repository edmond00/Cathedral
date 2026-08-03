using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// A named open path connecting two areas (road, track, lane, stream path).
/// Always passable from either end. Targeted by <see cref="Verbs.FollowPathVerb"/>.
///
/// Should be added to both <see cref="AreaA"/>.PointsOfInterest and
/// <see cref="AreaB"/>.PointsOfInterest so it is interactable from either side.
/// </summary>
public class PathPointOfInterest : ConnectorPointOfInterest
{
    protected override string ConnectorKind => "path";

    /// <summary>
    /// A path is the one connector that legitimately doubles an area-graph edge: it is a named walk,
    /// not a gate, and <c>OutdoorLayout.Link</c> always lays one down beside the edge it creates.
    /// </summary>
    public override bool AllowsGraphEdge => true;

    public PathPointOfInterest(
        Area areaA,
        Area areaB,
        string displayName,
        List<string> descriptions,
        string[]? moods = null)
        : base(areaA, areaB, displayName, "path", descriptions, moods: moods)
    {
    }

    /// <summary>
    /// A display name unique to the pair of areas a path joins: "Courtyard–Pigsty Track".
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
