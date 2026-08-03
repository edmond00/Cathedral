using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Reads the country from a high place and picks out where things are.
///
/// <para>Succeeding records the location's landmark areas on the point of view, which is what makes
/// <see cref="GoTowardVerb"/> possible. The knowledge is the outcome — there is no movement, no
/// item, nothing but knowing where the mill is — and that is the point of climbing.</para>
/// </summary>
public class ObserveHorizonVerb : Verb
{
    public override string VerbId         => "observe_horizon";
    public override string DisplayName    => "Observe the Horizon";
    public override int    BaseDifficulty => 2;

    /// <summary>What a success teaches: reading a landscape and holding its shape in your head.</summary>
    public override string? GrantedModusMentisId(Element? target) => "topographia";

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        if (target is not HorizonPointOfInterest horizon) return false;
        if (!pov.Where.PointsOfInterest.Contains(horizon)) return false;

        // Nothing to gain from looking twice: once the landmarks are known they stay known for the
        // visit, and offering the action again would be an action that does nothing.
        return horizon.Landmarks.Any(a => !pov.RevealedLandmarks.Contains(a.Id));
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => "search the horizon for anything worth walking to";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
        => target is HorizonPointOfInterest horizon
            ? new OutcomeReport[] { new LandmarksRevealedOutcome(horizon.Landmarks) }
            : System.Array.Empty<OutcomeReport>();

    // Not recordable: what it produces is knowledge held on a PoV that a replay rebuilds empty.

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}

/// <summary>
/// Sets out straight for a landmark picked out from a high place, ignoring the paths.
///
/// <para>The one verb that moves between areas without a connector. That is deliberate and it is
/// earned: you may only head for somewhere you have <i>seen</i> from up here, and seeing it cost a
/// climb and a roll. Difficulty 2 covers the going, which is long rather than hard.</para>
///
/// <para>It hangs off the <see cref="HorizonPointOfInterest"/> rather than off the destination area,
/// and expands into one action per revealed landmark. Areas are the one observation kind
/// <c>SceneViewAdapter</c> does not refresh — their single <c>move</c> view is hand-wired in
/// <c>SceneSyntheticGraphFactory</c> — and <c>Scene.View</c> only lists areas that already border
/// you, so a distant landmark is not an observation at all. Hanging the verb on the view you are
/// looking through also reads better: you pick the thing out and you set off for it, from up here,
/// while you can still see it.</para>
/// </summary>
public class GoTowardVerb : Verb
{
    public override string VerbId         => "go_toward";
    public override string DisplayName    => "Go Toward";
    public override int    BaseDifficulty => 2;

    /// <summary>What a success teaches: holding a bearing across ground you have only seen from afar.</summary>
    public override string? GrantedModusMentisId(Element? target) => "cartography";

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
        => Reachable(pov, target).Count > 0;

    /// <summary>One action per landmark seen from here and not already underfoot.</summary>
    public override IEnumerable<VerbView> ExpandViews(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        foreach (var area in Reachable(pov, target))
            yield return new VerbView(this, $"set out for the {area.DisplayName.ToLowerInvariant()}", target, variant: area);
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => "set out for what I picked out from up here";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbView? view = null)
        => view?.Variant is Area area ? $"set out for the {area.DisplayName.ToLowerInvariant()}" : Verbatim(scene, pov, target);

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target, VerbView view)
        => view.Variant is Area area
            ? new OutcomeReport[] { new AreaMoveOutcome(area) }
            : System.Array.Empty<OutcomeReport>();

    /// <summary>
    /// The landmarks in this view that have been revealed and are worth walking to: not the one you
    /// are standing in, and not one that already borders you (walking to a neighbour is <c>move</c>,
    /// and offering both is noise).
    /// </summary>
    private static List<Area> Reachable(PoV pov, Element target)
    {
        if (target is not HorizonPointOfInterest horizon) return new List<Area>();
        if (!pov.Where.PointsOfInterest.Contains(horizon)) return new List<Area>();

        return horizon.Landmarks
            .Where(a => a.Id != pov.Where.Id && pov.RevealedLandmarks.Contains(a.Id))
            .ToList();
    }

    /// <summary>Cross-country is rougher going than a path. Rarely, it turns an ankle.</summary>
    public override IReadOnlyList<Wound?> FailurePenalties(Element? target) => new Wound?[]
    {
        null, null, null, null, new AnkleFractureRightWound(),
    };

    // Not recordable: it depends on landmarks revealed this visit, which a replay does not have.

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}
