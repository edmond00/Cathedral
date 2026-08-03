using System;
using System.Collections.Generic;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// Base for any point of interest that joins two areas and is the only way between them: doors,
/// stairs, paths, cliffs, crossings, water, scale points. A connector belongs to <b>both</b> areas'
/// <c>PointsOfInterest</c> lists, which is what makes it visible and actionable from either side —
/// and which is also the source of the two problems this class exists to solve.
///
/// <para><b>Stable keys.</b> <c>SceneFactory.AssignStableKeys</c> walks areas in build order and keys
/// every PoI it finds, so a connector is visited twice and gets whichever key the later side hands
/// it. The key seeds the element's procedural description
/// (<c>SceneViewAdapter.DescriptionRng</c>), so a key that moves between builds re-rolls how the
/// thing looks between visits. Connectors therefore key themselves at construction, from the two
/// endpoint names sorted into a fixed order so the key does not depend on which side was passed
/// first, and the walk leaves any already-keyed element alone.</para>
///
/// <para><b>Graph exclusivity.</b> A connector is a <i>gate</i>: crossing it is meant to cost a roll,
/// a key, or a climb. Adding an <c>AreaGraph</c> edge beside one hands <c>MoveToAreaVerb</c> — base
/// difficulty 1, never fails — a way around it, which silently makes the connector decorative.
/// <see cref="AllowsGraphEdge"/> declares which connectors are exempt: a path is a walk with a name
/// on it and is always paired with an edge, everything else is not. <c>BuildingAudit</c> enforces
/// this for every subclass, which is how the game's existing decorative cliffs were found.</para>
/// </summary>
public abstract class ConnectorPointOfInterest : PointOfInterest
{
    /// <summary>One of the two areas this connector joins.</summary>
    public Area AreaA { get; }

    /// <summary>The other of the two areas this connector joins.</summary>
    public Area AreaB { get; }

    /// <summary>
    /// Whether an <c>AreaGraph</c> edge may legitimately exist beside this connector. False for
    /// everything that gates passage (doors, stairs, cliffs, crossings, water, scale points); true
    /// only for connectors that are themselves free to traverse.
    /// </summary>
    public virtual bool AllowsGraphEdge => false;

    /// <summary>
    /// A short tag naming the kind of connector, used to build the stable key and to report
    /// violations. Distinct per subclass so two different connectors between the same pair of areas
    /// do not collide.
    /// </summary>
    protected abstract string ConnectorKind { get; }

    protected ConnectorPointOfInterest(
        Area areaA,
        Area areaB,
        string displayName,
        string referenceLemma,
        List<string> descriptions,
        string[]? moods = null,
        List<ItemElement>? items = null,
        bool isNatural = false,
        string? keyDiscriminator = null)
        : base(displayName, referenceLemma, descriptions, items, moods, isNatural)
    {
        AreaA = areaA;
        AreaB = areaB;

        // Sorted endpoint names, so passing (kitchen, hall) and (hall, kitchen) key identically.
        // Ordinal rather than culture-aware: this key is compared across runs, never shown.
        string first  = areaA.DisplayName;
        string second = areaB.DisplayName;
        if (string.CompareOrdinal(first, second) > 0) (first, second) = (second, first);

        StableKey = keyDiscriminator == null
            ? $"{ConnectorKind}|{first}|{second}"
            : $"{ConnectorKind}|{first}|{second}|{keyDiscriminator}";
    }

    /// <summary>The area on the far side of this connector relative to <paramref name="from"/>.</summary>
    public Area Other(Area from) => from.Id == AreaA.Id ? AreaB : AreaA;

    /// <summary>Whether <paramref name="area"/> is one of the two this connector joins.</summary>
    public bool Touches(Area area) => area.Id == AreaA.Id || area.Id == AreaB.Id;

    /// <summary>
    /// Adds this connector to both endpoints' PoI lists and registers it with the scene. Every
    /// connector must be attached to both sides or it is only usable in one direction, and must be
    /// registered explicitly because the section walk in <c>SceneFactory.RegisterAll</c> only reaches
    /// PoIs already attached when it runs.
    /// </summary>
    public void AttachTo(Scene scene)
    {
        AreaA.PointsOfInterest.Add(this);
        AreaB.PointsOfInterest.Add(this);
        Register(scene);
    }
}
