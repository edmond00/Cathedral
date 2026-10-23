using System.Linq;
using Cathedral.Game.Npc;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Building;

namespace Cathedral.Game.Scene;

/// <summary>
/// Whose space an action is happening in. The counterpart to <see cref="ProximityModel"/>: that one
/// decides who can see a crime, this one decides whether there is a crime to see.
///
/// <para>Two questions, deliberately separate. <b>Where you stand</b> is <see cref="Area.IsPrivate"/>
/// and applies to every verb at once — being in someone's bedroom makes anything you do there
/// trespass. <b>What you are acting on</b> is <see cref="ReachesPrivateArea"/>, and matters because a
/// great many objects are actionable from both sides of the line: a house door is listed in the
/// street's points of interest and in the room's, so picking its lock from the street would otherwise
/// read as a perfectly lawful act performed in a public place.</para>
///
/// <para>The rule is the obvious one — an object that reaches into a private area is private,
/// whichever side you are touching it from — and it is a <i>per-verb</i> test rather than a global
/// one. Only the verbs that are crimes because of where they lead consult it (unlock, slip into,
/// break); walking through an open door or looking at it stays what it was.</para>
/// </summary>
public static class PrivacyModel
{
    /// <summary>
    /// Whether standing in <paramref name="area"/> is itself trespass — the "where you stand" half,
    /// and the one place it is asked. <see cref="Verbs.Verb.IsIllegal"/> makes everything done here a
    /// crime on its strength; the narration header shows it to the player; and no routine is learned
    /// or walked where it holds.
    /// </summary>
    public static bool IsTrespassing(Scene scene, Area area)
        => area.IsPrivate && !(scene.SectionOf(area) is { } section && scene.ToleratedSections.Contains(section));

    /// <summary>
    /// Whether striking at <paramref name="target"/> from <paramref name="pov"/> is hunting: the
    /// target is a beast (shallow wildlife, or a named creature without a human anatomy) and the
    /// ground is <see cref="AreaTenure.Wild"/>, so the beast is nobody's. On public or private ground
    /// the same beast is somebody's — the farm's pig, the village's dog — and the blow stays a crime.
    /// People are never game, wherever they stand.
    /// </summary>
    public static bool IsHunting(PoV pov, Element? target)
        => pov.Where.IsWild
           && target is SceneNpc { Entity: ShallowNpcEntity or NpcEntity { Combatant.AnatomyType: not AnatomyType.Human } };

    /// <summary>
    /// Whether <paramref name="target"/> reaches into somebody's private area.
    ///
    /// <para>A connector answers from its own two endpoints, which is both cheaper and safer than a
    /// scene walk — it is the one kind of element that knows what it joins. Everything else is
    /// located by the areas that list it, so a point of interest standing inside a private room is
    /// private no matter which area the actor is observing it from.</para>
    /// </summary>
    public static bool ReachesPrivateArea(Scene scene, Element? target)
    {
        if (target is ConnectorPointOfInterest connector)
            return connector.AreaA.IsPrivate || connector.AreaB.IsPrivate;

        if (target is PointOfInterest poi)
            return scene.AllAreas.Any(a => a.IsPrivate && a.PointsOfInterest.Contains(poi));

        return false;
    }
}
